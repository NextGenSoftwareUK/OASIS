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
    public Task NativeSetCountsIgnorePublicationCapsAndKeepSemanticWindows(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifySetPublicationCounts,
            nativeJourneySuffix:"-set-publication-count-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifySetPublicationCounts(IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> action)
        {var clock=System.Diagnostics.Stopwatch.StartNew();try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}}
        var rows=Enumerable.Range(0,7).Select(index=>new Holon{Id=Guid.NewGuid(),Name="cap-"+index,
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true}).ToArray();
        var ids=rows.Select(row=>row.Id).ToArray();owned.AddRange(ids);var lastId=ids[6];
        using(var writer=CosmicSession.Create(manager,adapter,access).Result)
        {
            foreach(var row in rows)Assert.IsFalse(writer.Add(row).IsError);
            var saved=await Timed("create-count-fixtures",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        }
        using var source=CosmicSession.Create(manager,adapter,access).Result;
        var query=source.Query<SetCountProbeHolon>().Result.Where(row=>ids.Contains(row.Id)).Result;
        var root=CosmicLinq.Create(manager,adapter,query,maximumResults:7).Result.OrderBy(row=>row.Name);
        var concat=root.Concat(root);SetCountProbeHolon.ConstructorCalls=0;
        var count=await Timed("count-beyond-row-publication-cap",()=>concat.ToCosmicLongCountAsync());
        Assert.IsFalse(count.IsError,count.Message+count.Exception);Assert.AreEqual(14L,count.Result);
        var union=await Timed("union-count",()=>root.Union(root).ToCosmicCountAsync());
        Assert.IsFalse(union.IsError,union.Message+union.Exception);Assert.AreEqual(7,union.Result);
        var intersection=await Timed("intersect-count",()=>root.Intersect(root.Where(row=>row.Name=="cap-0")).ToCosmicCountAsync());
        Assert.IsFalse(intersection.IsError,intersection.Message+intersection.Exception);Assert.AreEqual(1,intersection.Result);
        var difference=await Timed("except-count",()=>root.Except(root.Where(row=>row.Name=="cap-0")).ToCosmicCountAsync());
        Assert.IsFalse(difference.IsError,difference.Message+difference.Exception);Assert.AreEqual(6,difference.Result);
        var taken=await Timed("semantic-take-kept",()=>concat.Take(3).ToCosmicLongCountAsync());
        Assert.IsFalse(taken.IsError,taken.Message+taken.Exception);Assert.AreEqual(3L,taken.Result);
        var window=await Timed("semantic-skip-take-kept",()=>concat.Skip(4).Take(3).ToCosmicLongCountAsync());
        Assert.IsFalse(window.IsError,window.Message+window.Exception);Assert.AreEqual(3L,window.Result);
        var any=await Timed("predicate-after-window",()=>concat.Take(3).ToCosmicAnyAsync(row=>row.Id==lastId));
        Assert.IsFalse(any.IsError,any.Message+any.Exception);Assert.IsFalse(any.Result);
        var all=await Timed("all-visible-count",()=>concat.ToCosmicAllAsync(row=>ids.Contains(row.Id)));
        Assert.IsFalse(all.IsError,all.Message+all.Exception);Assert.IsTrue(all.Result);
        Assert.AreEqual(0,SetCountProbeHolon.ConstructorCalls,"Native aggregates do not construct result entities.");
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var hiddenQuery=foreign.Query<Holon>().Result.Where(row=>ids.Contains(row.Id)).Result;
        var hiddenRoot=CosmicLinq.Create(manager,adapter,hiddenQuery,maximumResults:7).Result;
        var hidden=await Timed("foreign-count",()=>hiddenRoot.Concat(hiddenRoot).ToCosmicLongCountAsync());
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0L,hidden.Result);
        var bounded=CosmicLinq.Create(manager,adapter,query,maximumResults:6).Result;
        var excessive=await Timed("source-bound-before-empty-result",()=>bounded.Concat(bounded).Take(0).ToCosmicLongCountAsync());
        Assert.IsTrue(excessive.IsError);Assert.AreEqual("CQL_QUERY_LIMIT",excessive.ErrorCode);
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        var cancel=await Timed("cancel-count",()=>concat.ToCosmicLongCountAsync(cancelled.Token));
        Assert.IsTrue(cancel.IsError);Assert.AreEqual("CQL_QUERY_CANCELLED",cancel.ErrorCode);
        return new NativeFixtureJourneyProof(8,1,new Dictionary<string,bool>{
            ["allFourSetKinds"]=true,["duplicateCountIndependentOfPublicationCap"]=true,["semanticWindowsRetained"]=true,
            ["postWindowPredicatesRetained"]=true,["noEntityMaterialization"]=true,["foreignCallerIsolated"]=true,
            ["inputCompletenessBeforeEmptyOutput"]=true,["cancellationPreserved"]=true});
    }
}
