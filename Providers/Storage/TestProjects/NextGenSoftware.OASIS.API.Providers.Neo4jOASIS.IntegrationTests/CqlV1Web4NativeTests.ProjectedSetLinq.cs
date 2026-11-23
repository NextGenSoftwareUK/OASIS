using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeProjectedSetLinqUsesRealValuesAndIndependentWindows(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,projectedSetLinqJourney:true);
    private static async Task<int> VerifyProjectedSetLinqJourney(IOASISStorageProvider provider,
        List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();var foreignActor=Guid.NewGuid();
        async Task<T> Timed<T>(string label,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(label,clock.ElapsedMilliseconds);}
        }
        Holon New(string name,Guid owner)=>new(){Id=Guid.NewGuid(),Name=name,CreatedByAvatarId=owner,
            IsPublic=false,IsActive=true,IsNewHolon=true};
        var a=New("a",actor);var b=New("b",actor);var c=New("a",actor);var hidden=New("hidden",foreignActor);
        owned.AddRange(new[]{a.Id,b.Id,c.Id,hidden.Id});
        using var session=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(foreignActor).Result).Result;
        Assert.IsFalse(session.AddRange(new[]{a,b,c}).IsError);
        var saved=await Timed("create-owned",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        Assert.IsFalse(foreign.Add(hidden).IsError);
        var other=await Timed("create-foreign",()=>foreign.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(other.IsError,other.Message+other.Exception);Assert.IsTrue(other.IsSaved);
        var allIds=owned.ToArray();var leftIds=new[]{a.Id,b.Id};var rightIds=new[]{c.Id,hidden.Id};
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result.Where(row=>allIds.Contains(row.Id)).Result;
        var root=CosmicLinq.Create(manager,adapter,source).Result;
        var left=root.Where(row=>leftIds.Contains(row.Id)).OrderBy(row=>row.Name).Take(2)
            .Select(row=>new{row.Id,row.Name}).Select(row=>row.Name);
        var right=root.Where(row=>rightIds.Contains(row.Id)).OrderBy(row=>row.Name).Take(2)
            .Select(row=>new{row.Version,row.Name}).Select(row=>row.Name).Take(1);
        async Task Verify(string label,IQueryable<string> query,string[] expected)
        {
            var result=await Timed(label,()=>query.ToCosmicListAsync());
            Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsLoaded);
            CollectionAssert.AreEqual(expected,result.Result.ToArray());
        }
        await Verify("union",left.Union(right).OrderBy(value=>value),new[]{"a","b"});
        await Verify("concat",left.Concat(right),new[]{"a","b","a"});
        await Verify("intersect",left.Intersect(right).OrderBy(value=>value),new[]{"a"});
        await Verify("except",left.Except(right).OrderBy(value=>value),new[]{"b"});
        await Verify("global-window",left.Concat(right).OrderBy(value=>value).Skip(1).Take(1),new[]{"a"});
        return 5;
    }
}
