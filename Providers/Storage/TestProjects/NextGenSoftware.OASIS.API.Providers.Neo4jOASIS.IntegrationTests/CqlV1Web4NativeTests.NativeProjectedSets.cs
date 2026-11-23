using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    [TestMethod]
    public Task NativeProjectedSetsUseTupleEqualityAndOneSnapshot()
        =>RunGroupedDistinctNativeFixture("SQLLiteDBOASIS",false,projectedSetJourney:true);
    private static async Task<int> VerifyNativeProjectedSetJourney(IOASISStorageProvider provider,
        List<Guid> ownedIds,Dictionary<string,long> timings)
    {
        var queryProvider=(ICosmicQueryStorageProvider)provider;
        var native=(ICosmicQueryProjectedSetStorageProvider)provider;var authority=queryProvider.QueryProviderType;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();var other=Guid.NewGuid();
        using var session=CosmicSession.Create(manager,queryProvider,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        using var foreign=CosmicSession.Create(manager,queryProvider,CosmicQueryReadAccess.OwnerOrPublic(other).Result).Result;
        async Task<T> Timed<T>(string name,Func<Task<T>> operation)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await operation();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        Holon New(string name,Guid owner)=>new(){Id=Guid.NewGuid(),Name=name,CreatedByAvatarId=owner,
            IsPublic=false,IsActive=true,IsNewHolon=true};
        var a=New("a",actor);var b=New("b",actor);var c=New("a",actor);var hidden=New("hidden",other);
        ownedIds.AddRange(new[]{a.Id,b.Id,c.Id,hidden.Id});
        Assert.IsFalse(session.AddRange(new[]{a,b,c}).IsError);
        var saved=await Timed("create-owned",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        Assert.IsFalse(foreign.Add(hidden).IsError);
        var hiddenSaved=await Timed("create-foreign",()=>foreign.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(hiddenSaved.IsError,hiddenSaved.Message+hiddenSaved.Exception);Assert.IsTrue(hiddenSaved.IsSaved);
        var ids=ownedIds.ToArray();var leftIds=new[]{a.Id,b.Id};var rightIds=new[]{c.Id,hidden.Id};
        var root=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result.Where(row=>ids.Contains(row.Id)).Result;
        var left=root.Where(row=>leftIds.Contains(row.Id)).Result.OrderBy(row=>row.Name).Take(2).Result
            .Select(row=>new{row.Id,row.Name}).Result.Select(row=>row.Name).Result;
        var right=root.Where(row=>rightIds.Contains(row.Id)).Result.OrderBy(row=>row.Name).Take(2).Result
            .Select(row=>new{row.Version,row.Name}).Result.Select(row=>row.Name).Result.Take(1).Result;
        var target=new CosmicProjectionCompositionTarget(authority,queryProvider.QueryCapabilities,
            native.ProjectionCapabilities,native.CompositionCapabilities);
        async Task<string[]> Read(CosmicSetQueryKind kind,string label,bool outer=false,bool bounded=false)
        {
            var query=CosmicProjectedSetQuery<Holon,string>.Create(left,right,kind).Result;
            if(kind!=CosmicSetQueryKind.Concat || outer)query=query.OrderBy(value=>value).Result;
            if(outer)query=query.Skip(1).Result.Take(1).Result;
            if(bounded)query=query.Take(0).Result;
            var options=bounded?new CosmicProjectedSetOptions(new CosmicSetQueryOptions(maximumLeftInputRows:1)):new CosmicProjectedSetOptions();
            var plan=CosmicProjectedSetPlanner.Plan(query,authority,target,native.ProjectedSetCapabilities,options);
            Assert.IsFalse(plan.IsError,plan.Message);
            var result=await Timed(label,()=>CosmicProjectedSetExecutor.ExecuteAsync(plan.Result,new[]{queryProvider}));
            if(bounded)
            {
                Assert.IsTrue(result.IsError);Assert.AreEqual("CQL_QUERY_LIMIT",result.ErrorCode);
                Assert.IsNull(result.Result);return Array.Empty<string>();
            }
            Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsLoaded);
            return result.Result.Providers.Single().Rows.ToArray();
        }
        CollectionAssert.AreEqual(new[]{"a","b"},await Read(CosmicSetQueryKind.Union,"union"));
        CollectionAssert.AreEqual(new[]{"a","b","a"},await Read(CosmicSetQueryKind.Concat,"concat"));
        CollectionAssert.AreEqual(new[]{"a"},await Read(CosmicSetQueryKind.Intersect,"intersect"));
        CollectionAssert.AreEqual(new[]{"b"},await Read(CosmicSetQueryKind.Except,"except"));
        CollectionAssert.AreEqual(new[]{"a"},await Read(CosmicSetQueryKind.Concat,"global-window",outer:true));
        await Read(CosmicSetQueryKind.Union,"bound-before-empty-page",bounded:true);
        var ordinary=left.Take(1).Where(value=>value=="b").Result;
        var plain=CosmicProjectionCompositionPlanner.Plan(ordinary,authority,new[]{target});
        Assert.IsFalse(plain.IsError,plain.Message);
        var result=await Timed("ordinary-selected-window",()=>CosmicProjectionCompositionExecutor.ExecuteAsync(plain.Result,new[]{queryProvider}));
        Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsLoaded);
        Assert.AreEqual(0,result.Result.Providers.Single().Rows.Count);
        return 7;
    }
}
