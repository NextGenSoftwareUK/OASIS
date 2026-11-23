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
    public Task NativeSetCardinalityKeepsMembershipWindowsAndVisibility(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifySetCardinalityJourney,nativeJourneySuffix:"-set-cardinality-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifySetCardinalityJourney(
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
        var first=new Holon{Id=Guid.NewGuid(),Name="async-a",CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var last=new Holon{Id=Guid.NewGuid(),Name="async-b",CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var a=first.Id;var b=last.Id;var missing=Guid.NewGuid();owned.AddRange(new[]{a,b});
        Assert.IsFalse(writer.Add(first).IsError);Assert.IsFalse(writer.Add(last).IsError);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,watch.ElapsedMilliseconds);}
        }
        var saved=await Timed("create",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        using var source=CosmicSession.Create(manager,adapter,access).Result;
        var query=source.Query<SetCountProbeHolon>().Result.Where(row=>row.Id==a||row.Id==b).Result;
        var root=CosmicLinq.Create(manager,adapter,query).Result.OrderBy(row=>row.Name);
        var combined=root.Concat(root).OrderBy(row=>row.Name);
        SetCountProbeHolon.ConstructorCalls=0;
        var count=await Timed("concat-count",()=>combined.ToCosmicCountAsync());
        Assert.IsFalse(count.IsError,count.Message+count.Exception);Assert.AreEqual(4,count.Result);
        var distinct=await Timed("union-long-count",()=>root.Union(root).ToCosmicLongCountAsync());
        Assert.IsFalse(distinct.IsError,distinct.Message+distinct.Exception);Assert.AreEqual(2L,distinct.Result);
        var intersect=await Timed("intersect-count",()=>root.Intersect(root.Where(row=>row.Id==a)).ToCosmicCountAsync());
        Assert.IsFalse(intersect.IsError,intersect.Message+intersect.Exception);Assert.AreEqual(1,intersect.Result);
        var except=await Timed("except-count",()=>root.Except(root.Where(row=>row.Id==a)).ToCosmicCountAsync());
        Assert.IsFalse(except.IsError,except.Message+except.Exception);Assert.AreEqual(1,except.Result);
        var any=await Timed("any-predicate",()=>combined.ToCosmicAnyAsync(row=>row.Id==a));
        Assert.IsFalse(any.IsError,any.Message+any.Exception);Assert.IsTrue(any.Result);
        var notAll=await Timed("all-counterexample",()=>combined.ToCosmicAllAsync(row=>row.Id==a));
        Assert.IsFalse(notAll.IsError,notAll.Message+notAll.Exception);Assert.IsFalse(notAll.Result);
        var all=await Timed("all-visible",()=>combined.ToCosmicAllAsync(row=>row.Id==a||row.Id==b));
        Assert.IsFalse(all.IsError,all.Message+all.Exception);Assert.IsTrue(all.Result);
        var windowAny=await Timed("any-after-window",()=>combined.Take(1).ToCosmicAnyAsync(row=>row.Id==b));
        Assert.IsFalse(windowAny.IsError,windowAny.Message+windowAny.Exception);Assert.IsFalse(windowAny.Result);
        var windowAll=await Timed("all-after-window",()=>combined.Take(1).ToCosmicAllAsync(row=>row.Id==a));
        Assert.IsFalse(windowAll.IsError,windowAll.Message+windowAll.Exception);Assert.IsTrue(windowAll.Result);
        var empty=root.Except(root);
        var zero=await Timed("empty-count",()=>empty.ToCosmicLongCountAsync());
        Assert.IsFalse(zero.IsError,zero.Message+zero.Exception);Assert.AreEqual(0L,zero.Result);
        var vacuous=await Timed("empty-all",()=>empty.ToCosmicAllAsync(row=>row.Id==missing));
        Assert.IsFalse(vacuous.IsError,vacuous.Message+vacuous.Exception);Assert.IsTrue(vacuous.Result);
        var sync=await Timed("sync-count",()=>Task.FromResult(combined.Count()));
        Assert.AreEqual(4,sync);
        Assert.AreEqual(0,SetCountProbeHolon.ConstructorCalls,"Native count must not materialize typed records.");
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var denied=foreign.Query<Holon>().Result.Where(row=>row.Id==a||row.Id==b).Result;
        var foreignRoot=CosmicLinq.Create(manager,adapter,denied).Result;
        var hidden=await Timed("foreign-count",()=>foreignRoot.Concat(foreignRoot).ToCosmicCountAsync());
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result);
        var bounded=CosmicLinq.Create(manager,adapter,query,maximumResults:1).Result;
        var excessive=await Timed("bound-before-empty",()=>bounded.Concat(bounded).Take(0).ToCosmicCountAsync());
        Assert.IsTrue(excessive.IsError);Assert.AreEqual("CQL_QUERY_LIMIT",excessive.ErrorCode);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        var cancelled=await Timed("cancelled",()=>combined.ToCosmicAnyAsync(cancellation.Token));
        Assert.IsTrue(cancelled.IsError);Assert.AreEqual("CQL_QUERY_CANCELLED",cancelled.ErrorCode);
        return new NativeFixtureJourneyProof(10,1,new Dictionary<string,bool>{
            ["allFourIdentitySets"]=true,["duplicateOccurrencesCounted"]=true,
            ["countAndLongCountDomains"]=true,["anyAndAllNativeCounterexamples"]=true,
            ["predicatesAfterEarlierWindow"]=true,["emptyAllIsTrue"]=true,
            ["noTypedRecordConstruction"]=true,["foreignPrivateRowsExcluded"]=true,
            ["inputBoundBeforeEmptyOutput"]=true,["syncAsyncAndCancellation"]=true});
    }
    private sealed class SetCountProbeHolon:Holon
    {
        public static int ConstructorCalls;
        public SetCountProbeHolon(){Interlocked.Increment(ref ConstructorCalls);}
    }
}
