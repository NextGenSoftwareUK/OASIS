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
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeAsyncCardinalityPreservesWindowsScopesAndCancellation(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyAsyncCardinalityJourney,nativeJourneySuffix:"-async-cardinality-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyAsyncCardinalityJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        using var writer=CosmicSession.Create(manager,adapter,access).Result;
        var first=new Holon{Id=Guid.NewGuid(),Name="async-cardinality-a",CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var last=new Holon{Id=Guid.NewGuid(),Name="async-cardinality-b",CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var a=first.Id;var b=last.Id;owned.AddRange(new[]{a,b});
        Assert.IsFalse(writer.Add(first).IsError);Assert.IsFalse(writer.Add(last).IsError);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,watch.ElapsedMilliseconds);}
        }
        var saved=await Timed("create",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        using var source=CosmicSession.Create(manager,adapter,access).Result;
        var query=source.Query<Holon>().Result.Where(row=>row.Id==a||row.Id==b).Result;
        var root=CosmicLinq.Create(manager,adapter,query).Result.OrderBy(row=>row.Name);
        var count=await Timed("count",()=>root.ToCosmicCountAsync());
        Assert.IsFalse(count.IsError,count.Message+count.Exception);Assert.AreEqual(2,count.Result);Assert.AreEqual(1,count.LoadedCount);
        var longCount=await Timed("long-count",()=>root.ToCosmicLongCountAsync(row=>row.Version>=0));
        Assert.IsFalse(longCount.IsError,longCount.Message+longCount.Exception);Assert.AreEqual(2L,longCount.Result);
        var any=await Timed("any",()=>root.ToCosmicAnyAsync());
        Assert.IsFalse(any.IsError,any.Message+any.Exception);Assert.IsTrue(any.Result);
        var all=await Timed("all-true",()=>root.ToCosmicAllAsync(row=>row.Id==a||row.Id==b));
        Assert.IsFalse(all.IsError,all.Message+all.Exception);Assert.IsTrue(all.Result);
        var notAll=await Timed("all-false",()=>root.ToCosmicAllAsync(row=>row.Id==a));
        Assert.IsFalse(notAll.IsError,notAll.Message+notAll.Exception);Assert.IsFalse(notAll.Result);
        var postCount=await Timed("post-window-count",()=>root.Take(1).ToCosmicCountAsync(row=>row.Id==b));
        Assert.IsFalse(postCount.IsError,postCount.Message+postCount.Exception);Assert.AreEqual(0,postCount.Result);
        var postAll=await Timed("post-window-all",()=>root.Take(1).ToCosmicAllAsync(row=>row.Id==a));
        Assert.IsFalse(postAll.IsError,postAll.Message+postAll.Exception);Assert.IsTrue(postAll.Result);
        var selectedCount=await Timed("selected-count",()=>root.Select(row=>row.Version).ToCosmicCountAsync(version=>version>0));
        Assert.IsFalse(selectedCount.IsError,selectedCount.Message+selectedCount.Exception);Assert.AreEqual(2,selectedCount.Result);
        var selectedAll=await Timed("selected-all",()=>root.Select(row=>row.Version).ToCosmicAllAsync(version=>version>0));
        Assert.IsFalse(selectedAll.IsError,selectedAll.Message+selectedAll.Exception);Assert.IsTrue(selectedAll.Result);
        var entityCount=await Timed("entity-stage-count",()=>root.Take(2).Where(row=>row.Id==b).ToCosmicLongCountAsync());
        Assert.IsFalse(entityCount.IsError,entityCount.Message+entityCount.Exception);Assert.AreEqual(1L,entityCount.Result);
        var entitySelected=await Timed("entity-selected-any",()=>root.Take(2).Where(row=>row.Id==b).Select(row=>row.Name)
            .ToCosmicAnyAsync(name=>name=="async-cardinality-b"));
        Assert.IsFalse(entitySelected.IsError,entitySelected.Message+entitySelected.Exception);Assert.IsTrue(entitySelected.Result);
        var tracked=CosmicSession.Create(manager,adapter,access).Result;
        var trackedQuery=tracked.Linq<Holon>().Result.Where(row=>row.Id==a||row.Id==b);
        var sessionCount=await Timed("session-count",()=>trackedQuery.ToCosmicCountAsync());
        Assert.IsFalse(sessionCount.IsError,sessionCount.Message+sessionCount.Exception);Assert.AreEqual(2,sessionCount.Result);
        tracked.Dispose();
        var disposed=await Timed("disposed-session",()=>trackedQuery.ToCosmicAnyAsync());
        Assert.IsTrue(disposed.IsError);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        var cancelled=await Timed("cancelled",()=>root.ToCosmicAnyAsync(cancellation.Token));
        Assert.IsTrue(cancelled.IsError);Assert.AreEqual("CQL_QUERY_CANCELLED",cancelled.ErrorCode);
        var enumerated=false;
        var local=Enumerable.Range(0,1).Select(number=>{enumerated=true;return number;}).AsQueryable();
        var refused=await Timed("reject-local",()=>local.ToCosmicCountAsync());
        Assert.IsTrue(refused.IsError);Assert.AreEqual("CQL_QUERY_PROVIDER_INVALID",refused.ErrorCode);Assert.IsFalse(enumerated);
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var hidden=await Timed("foreign-count",()=>foreign.Linq<Holon>().Result.Where(row=>row.Id==a||row.Id==b).ToCosmicCountAsync());
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result);
        return new NativeFixtureJourneyProof(7,1,new Dictionary<string,bool>{
            ["nativeCounts"]=true,["nativeAnyAll"]=true,["postWindowPredicates"]=true,
            ["selectedAndEntityStageCardinality"]=true,["sessionScalarLifetime"]=true,
            ["foreignVisibilityPreserved"]=true,["cancellationAndNoEnumerableReduction"]=true});
    }
}
