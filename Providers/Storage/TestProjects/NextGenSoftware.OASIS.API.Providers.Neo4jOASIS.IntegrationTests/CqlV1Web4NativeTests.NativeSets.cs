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
    [DataTestMethod]
    [DataRow("SQLLiteDBOASIS")]
    public Task NativeSetOperationsPreserveWindowsAndMultiplicity(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,setJourney:true);
    private static async Task<int> VerifyNativeSetJourney(IOASISStorageProvider provider,
        List<Guid> ownedIds,Dictionary<string,long> timings)
    {
        var queryProvider=(ICosmicQueryStorageProvider)provider;
        var nativeSet=(ICosmicSetQueryStorageProvider)provider;var authority=queryProvider.QueryProviderType;
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings {LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna) {IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();var other=Guid.NewGuid();
        using var session=CosmicSession.Create(manager,queryProvider,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        using var foreign=CosmicSession.Create(manager,queryProvider,CosmicQueryReadAccess.OwnerOrPublic(other).Result).Result;
        async Task<T> Timed<T>(string name,Func<Task<T>> operation)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await operation();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        Holon New(string suffix,Guid owner)=>new(){Id=Guid.NewGuid(),Name="set-native-"+suffix,
            CreatedByAvatarId=owner,IsPublic=false,IsActive=true,IsNewHolon=true};
        var a=New("a",actor);var b=New("b",actor);var c=New("c",actor);var hidden=New("0",other);
        ownedIds.AddRange(new[]{a.Id,b.Id,c.Id,hidden.Id});
        Assert.IsFalse(session.AddRange(new[]{a,b,c}).IsError);
        var own=await Timed("create-owned",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(own.IsError,own.Message+own.Exception);Assert.IsTrue(own.IsSaved);
        Assert.IsFalse(foreign.Add(hidden).IsError);
        var theirs=await Timed("create-foreign",()=>foreign.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(theirs.IsError,theirs.Message+theirs.Exception);Assert.IsTrue(theirs.IsSaved);
        var ids=ownedIds.ToArray();
        var root=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result
            .Where(row=>ids.Contains(row.Id)).Result;
        var left=root.OrderBy(row=>row.Name).Take(2).Result;
        var right=root.OrderBy(row=>row.Name).Skip(1).Take(2).Result;
        async Task<Guid[]> Read(CosmicSetQueryKind kind,string label,bool outer=false,bool bounded=false)
        {
            var query=CosmicSetQuery<Holon>.Create(left,right,kind).Result;
            if(kind!=CosmicSetQueryKind.Concat)query=query.OrderBy(row=>row.Name).Result;
            if(outer)query=query.Skip(1).Result.Take(2).Result;
            if(bounded)query=query.Take(0).Result;
            var options=bounded?new CosmicSetQueryOptions(maximumLeftInputRows:1):new CosmicSetQueryOptions();
            var planned=CosmicSetQueryPlanner.Plan(query,authority,new CosmicQueryTarget(authority,queryProvider.QueryCapabilities),
                nativeSet.SetQueryCapabilities,options);
            Assert.IsFalse(planned.IsError,planned.Message);
            var result=await Timed(label,()=>CosmicSetQueryExecutor.ExecuteAsync(planned.Result,new[]{queryProvider}));
            if(bounded)
            {
                Assert.IsTrue(result.IsError);Assert.AreEqual("CQL_QUERY_LIMIT",result.ErrorCode);
                Assert.IsNull(result.Result);return Array.Empty<Guid>();
            }
            Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsLoaded);
            var rows=result.Result.Providers.Single().Rows;
            Assert.IsFalse(rows.Any(row=>row.Id==hidden.Id));
            return rows.Select(row=>row.Id).ToArray();
        }
        CollectionAssert.AreEqual(new[]{a.Id,b.Id,c.Id},await Read(CosmicSetQueryKind.Union,"union"));
        CollectionAssert.AreEqual(new[]{a.Id,b.Id,b.Id,c.Id},await Read(CosmicSetQueryKind.Concat,"concat"));
        CollectionAssert.AreEqual(new[]{b.Id},await Read(CosmicSetQueryKind.Intersect,"intersect"));
        CollectionAssert.AreEqual(new[]{a.Id},await Read(CosmicSetQueryKind.Except,"except"));
        CollectionAssert.AreEqual(new[]{b.Id,b.Id},await Read(CosmicSetQueryKind.Concat,"concat-window",outer:true));
        await Read(CosmicSetQueryKind.Union,"bound-before-empty-page",bounded:true);
        using var plain=CosmicStatelessSession.Create(manager,queryProvider,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        var ordinary=await Timed("ordinary-get",()=>plain.GetAsync<Holon>(a.Id));
        Assert.IsFalse(ordinary.IsError,ordinary.Message);Assert.IsTrue(ordinary.IsLoaded);
        Assert.AreEqual(a.Name,ordinary.Result.Name);
        return 7;
    }
}
