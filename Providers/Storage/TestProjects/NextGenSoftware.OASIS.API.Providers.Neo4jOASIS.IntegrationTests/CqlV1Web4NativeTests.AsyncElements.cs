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
    public Task NativeAsyncElementsKeepCardinalityCancellationAndSessionOwnership(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyAsyncElementJourney,nativeJourneySuffix:"-async-element-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyAsyncElementJourney(
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
        var query=source.Query<Holon>().Result.Where(row=>row.Id==a||row.Id==b).Result;
        var root=CosmicLinq.Create(manager,adapter,query).Result.OrderBy(row=>row.Name);
        var one=await Timed("first",()=>root.ToCosmicFirstAsync());
        Assert.IsFalse(one.IsError,one.Message+one.Exception);Assert.AreEqual(a,one.Result.Id);Assert.AreEqual(1,one.LoadedCount);
        var tail=await Timed("last",()=>root.ToCosmicLastAsync());
        Assert.IsFalse(tail.IsError,tail.Message+tail.Exception);Assert.AreEqual(b,tail.Result.Id);
        var single=await Timed("single-predicate",()=>root.ToCosmicSingleAsync(row=>row.Id==a));
        Assert.IsFalse(single.IsError,single.Message+single.Exception);Assert.AreEqual(a,single.Result.Id);
        var ambiguous=await Timed("single-ambiguous",()=>root.ToCosmicSingleAsync());
        Assert.IsTrue(ambiguous.IsError);Assert.AreEqual("CQL_SEQUENCE_MULTIPLE",ambiguous.ErrorCode);
        var indexed=await Timed("element-at",()=>root.ToCosmicElementAtAsync(1));
        Assert.IsFalse(indexed.IsError,indexed.Message+indexed.Exception);Assert.AreEqual(b,indexed.Result.Id);
        var relative=await Timed("from-end",()=>root.ToCosmicElementAtOrDefaultAsync(^1));
        Assert.IsFalse(relative.IsError,relative.Message+relative.Exception);Assert.AreEqual(b,relative.Result.Id);
        var empty=root.Where(row=>row.Id==missing);
        var absent=await Timed("optional-reference",()=>empty.ToCosmicFirstOrDefaultAsync());
        Assert.IsFalse(absent.IsError,absent.Message+absent.Exception);Assert.IsNull(absent.Result);Assert.AreEqual(0,absent.LoadedCount);
        var zero=await Timed("optional-value",()=>empty.Select(row=>row.Version).ToCosmicSingleOrDefaultAsync());
        Assert.IsFalse(zero.IsError,zero.Message+zero.Exception);Assert.AreEqual(0,zero.Result);Assert.AreEqual(0,zero.LoadedCount);
        var value=await Timed("selected-value",()=>root.Select(row=>row.Version).ToCosmicFirstAsync());
        Assert.IsFalse(value.IsError,value.Message+value.Exception);Assert.AreEqual(first.Version,value.Result);Assert.AreEqual(1,value.LoadedCount);
        var staged=await Timed("staged-selected",()=>root.Take(2).Where(row=>row.Id==b).Select(row=>row.Name).ToCosmicSingleAsync());
        Assert.IsFalse(staged.IsError,staged.Message+staged.Exception);Assert.AreEqual("async-b",staged.Result);
        using var tracked=CosmicSession.Create(manager,adapter,access).Result;
        var trackedQuery=tracked.Linq<Holon>().Result.Where(row=>row.Id==a||row.Id==b).OrderBy(row=>row.Name);
        var trackedFirst=await Timed("session-first",()=>trackedQuery.ToCosmicFirstAsync());
        Assert.IsFalse(trackedFirst.IsError,trackedFirst.Message+trackedFirst.Exception);
        var trackedAgain=await Timed("session-again",()=>trackedQuery.ToCosmicFirstAsync());
        Assert.IsFalse(trackedAgain.IsError,trackedAgain.Message+trackedAgain.Exception);Assert.AreSame(trackedFirst.Result,trackedAgain.Result);
        var supplied=new Holon{Id=Guid.NewGuid(),Name="caller default"};
        var suppliedResult=await Timed("default-not-tracked",()=>trackedQuery.ToCosmicFirstOrDefaultAsync(row=>row.Id==missing,supplied));
        Assert.IsFalse(suppliedResult.IsError,suppliedResult.Message+suppliedResult.Exception);
        Assert.AreSame(supplied,suppliedResult.Result);Assert.AreEqual(0,suppliedResult.LoadedCount);
        Assert.AreEqual(CosmicEntityState.Detached,tracked.State(supplied).Result);
        var ended=CosmicSession.Create(manager,adapter,access).Result;
        var endedQuery=ended.Linq<Holon>().Result.Where(row=>row.Id==a);
        ended.Dispose();
        var lifetime=await Timed("disposed-session",()=>endedQuery.ToCosmicFirstAsync());
        Assert.IsTrue(lifetime.IsError);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        var cancelled=await Timed("cancelled",()=>root.ToCosmicFirstAsync(cancellation.Token));
        Assert.IsTrue(cancelled.IsError);Assert.AreEqual("CQL_QUERY_CANCELLED",cancelled.ErrorCode);
        var enumerated=false;
        var local=Enumerable.Range(0,1).Select(number=>{enumerated=true;return number;}).AsQueryable();
        var refused=await Timed("reject-local",()=>local.ToCosmicFirstAsync());
        Assert.IsTrue(refused.IsError);Assert.AreEqual("CQL_QUERY_PROVIDER_INVALID",refused.ErrorCode);Assert.IsFalse(enumerated);
        return new NativeFixtureJourneyProof(8,1,new Dictionary<string,bool>{
            ["nativeFirstLast"]=true,["singleCardinalityAndPredicate"]=true,
            ["nativeIndexAndFromEnd"]=true,["emptyScalarAndEntityDefaults"]=true,
            ["selectedAndPostWindowElements"]=true,["sessionTrackingAndLifetime"]=true,
            ["callerDefaultsRemainDetached"]=true,["cancellationAndNoEnumerableSubstitute"]=true});
    }
}
