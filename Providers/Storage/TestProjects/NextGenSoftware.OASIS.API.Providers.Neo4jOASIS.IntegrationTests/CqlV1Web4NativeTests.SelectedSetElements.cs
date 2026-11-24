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
    public Task NativeSelectedSetElementsKeepTupleEqualityAndNativeWindows(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifySelectedSetElementJourney,nativeJourneySuffix:"-selected-set-element-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifySelectedSetElementJourney(
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
        var first=new Holon{Id=Guid.NewGuid(),Name="async-a",Description=null,CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
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
        var values=root.Select(row=>row.Version);
        var union=await Timed("tuple-union-single",()=>values.Union(values).ToCosmicSingleAsync());
        Assert.IsFalse(union.IsError,union.Message+union.Exception);Assert.AreEqual(first.Version,union.Result);
        var identities=await Timed("entity-union-multiple",()=>root.Union(root).ToCosmicSingleAsync());
        Assert.IsTrue(identities.IsError);Assert.AreEqual("CQL_SEQUENCE_MULTIPLE",identities.ErrorCode);
        var repeated=await Timed("tuple-concat-multiple",()=>values.Concat(values).ToCosmicSingleOrDefaultAsync(-1));
        Assert.IsTrue(repeated.IsError);Assert.AreEqual("CQL_SEQUENCE_MULTIPLE",repeated.ErrorCode);
        var intersect=await Timed("tuple-intersect",()=>values.Intersect(values).ToCosmicSingleAsync());
        Assert.IsFalse(intersect.IsError,intersect.Message+intersect.Exception);Assert.AreEqual(first.Version,intersect.Result);
        var except=await Timed("tuple-except-default",()=>values.Except(values).ToCosmicSingleOrDefaultAsync(-1));
        Assert.IsFalse(except.IsError,except.Message+except.Exception);Assert.AreEqual(-1,except.Result);Assert.AreEqual(0,except.LoadedCount);
        var names=root.Select(row=>row.Name);var combined=names.Concat(names).OrderBy(value=>value);
        var head=await Timed("tuple-first",()=>combined.ToCosmicFirstAsync());
        Assert.IsFalse(head.IsError,head.Message+head.Exception);Assert.AreEqual("async-a",head.Result);
        var tail=await Timed("tuple-last",()=>combined.ToCosmicLastAsync());
        Assert.IsFalse(tail.IsError,tail.Message+tail.Exception);Assert.AreEqual("async-b",tail.Result);
        var relative=await Timed("tuple-from-end",()=>combined.ToCosmicElementAtAsync(^2));
        Assert.IsFalse(relative.IsError,relative.Message+relative.Exception);Assert.AreEqual("async-b",relative.Result);
        var window=await Timed("tuple-post-window",()=>combined.Take(1).ToCosmicFirstOrDefaultAsync(value=>value=="async-b","fallback"));
        Assert.IsFalse(window.IsError,window.Message+window.Exception);Assert.AreEqual("fallback",window.Result);Assert.AreEqual(0,window.LoadedCount);
        var nulls=root.Where(row=>row.Id==a).Select(row=>row.Description);
        var nullable=await Timed("tuple-present-null",()=>nulls.Union(nulls).ToCosmicSingleOrDefaultAsync("fallback"));
        Assert.IsFalse(nullable.IsError,nullable.Message+nullable.Exception);Assert.IsNull(nullable.Result);Assert.AreEqual(1,nullable.LoadedCount);
        var sync=await Timed("tuple-sync",()=>Task.FromResult(combined.ElementAt(1)));
        Assert.AreEqual("async-a",sync);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        var cancelled=await Timed("tuple-cancelled",()=>combined.ToCosmicLastAsync(cancellation.Token));
        Assert.IsTrue(cancelled.IsError);Assert.AreEqual("CQL_QUERY_CANCELLED",cancelled.ErrorCode);
        return new NativeFixtureJourneyProof(8,1,new Dictionary<string,bool>{
            ["tupleEqualityIndependentOfEntityIdentity"]=true,["allFourSelectedSets"]=true,
            ["duplicateOccurrencesAndFromEnd"]=true,["predicateAfterWindow"]=true,
            ["ambiguityNeverUsesCallerDefault"]=true,["presentNullSeparateFromAbsence"]=true,
            ["syncAndAsyncSameNativeOwner"]=true,["cancellationPreserved"]=true});
    }
}
