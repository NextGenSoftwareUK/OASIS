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
    public Task NativeSetElementsKeepMembershipWindowsAndCardinality(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifySetElementJourney,nativeJourneySuffix:"-set-element-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifySetElementJourney(
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
        var first=new Holon{Id=Guid.NewGuid(),Name="set-a",Description=null,CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var last=new Holon{Id=Guid.NewGuid(),Name="set-b",Description="last",CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var a=first.Id;var b=last.Id;var missing=Guid.NewGuid();owned.AddRange(new[]{a,b});
        Assert.IsFalse(writer.Add(first).IsError);Assert.IsFalse(writer.Add(last).IsError);
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> action)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,watch.ElapsedMilliseconds);}
        }
        var saved=await Timed("create",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        using var source=CosmicSession.Create(manager,adapter,access).Result;
        var query=source.Query<Holon>().Result.Where(row=>row.Id==a||row.Id==b).Result;
        var root=CosmicLinq.Create(manager,adapter,query).Result.OrderBy(row=>row.Name);
        var combined=root.Concat(root).OrderBy(row=>row.Name);
        var head=await Timed("first",()=>combined.ToCosmicFirstAsync());
        Assert.IsFalse(head.IsError,head.Message+head.Exception);Assert.AreEqual(a,head.Result.Id);Assert.AreEqual(1,head.LoadedCount);
        var tail=await Timed("last",()=>combined.ToCosmicLastAsync());
        Assert.IsFalse(tail.IsError,tail.Message+tail.Exception);Assert.AreEqual(b,tail.Result.Id);
        var index=await Timed("index-occurrence",()=>combined.ToCosmicElementAtAsync(1));
        Assert.IsFalse(index.IsError,index.Message+index.Exception);Assert.AreEqual(a,index.Result.Id);
        var relative=await Timed("from-end",()=>combined.ToCosmicElementAtAsync(^2));
        Assert.IsFalse(relative.IsError,relative.Message+relative.Exception);Assert.AreEqual(b,relative.Result.Id);
        var unique=await Timed("union-single",()=>root.Union(root).ToCosmicSingleAsync(row=>row.Id==a));
        Assert.IsFalse(unique.IsError,unique.Message+unique.Exception);Assert.AreEqual(a,unique.Result.Id);
        var multiple=await Timed("concat-multiple",()=>combined.ToCosmicSingleOrDefaultAsync());
        Assert.IsTrue(multiple.IsError);Assert.AreEqual("CQL_SEQUENCE_MULTIPLE",multiple.ErrorCode);
        var intersect=await Timed("intersect-single",()=>root.Intersect(root.Where(row=>row.Id==a)).ToCosmicSingleAsync());
        Assert.IsFalse(intersect.IsError,intersect.Message+intersect.Exception);Assert.AreEqual(a,intersect.Result.Id);
        var except=await Timed("except-single",()=>root.Except(root.Where(row=>row.Id==a)).ToCosmicSingleAsync());
        Assert.IsFalse(except.IsError,except.Message+except.Exception);Assert.AreEqual(b,except.Result.Id);
        var supplied=new Holon{Id=Guid.NewGuid(),Name="caller default"};
        var window=await Timed("post-window-default",()=>combined.Take(1).ToCosmicFirstOrDefaultAsync(row=>row.Id==b,supplied));
        Assert.IsFalse(window.IsError,window.Message+window.Exception);Assert.AreSame(supplied,window.Result);Assert.AreEqual(0,window.LoadedCount);
        var selected=await Timed("selected-distinct-predicate",()=>combined.Select(row=>row.Name).Distinct().OrderBy(value=>value)
            .ToCosmicSingleAsync(value=>value=="set-b"));
        Assert.IsFalse(selected.IsError,selected.Message+selected.Exception);Assert.AreEqual("set-b",selected.Result);
        var nullable=await Timed("selected-null",()=>combined.Where(row=>row.Id==a).Select(row=>row.Description).ToCosmicFirstOrDefaultAsync("fallback"));
        Assert.IsFalse(nullable.IsError,nullable.Message+nullable.Exception);Assert.IsNull(nullable.Result);Assert.AreEqual(1,nullable.LoadedCount);
        var sync=await Timed("sync-selected",()=>Task.FromResult(combined.Select(row=>row.Name).Distinct().OrderBy(value=>value).Last()));
        Assert.AreEqual("set-b",sync);
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var denied=foreign.Query<Holon>().Result.Where(row=>row.Id==a||row.Id==b).Result;
        var foreignRoot=CosmicLinq.Create(manager,adapter,denied).Result;
        var hidden=await Timed("foreign-empty",()=>foreignRoot.Concat(foreignRoot).ToCosmicFirstOrDefaultAsync());
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.IsNull(hidden.Result);Assert.AreEqual(0,hidden.LoadedCount);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        var cancelled=await Timed("cancelled",()=>combined.ToCosmicFirstAsync(cancellation.Token));
        Assert.IsTrue(cancelled.IsError);Assert.AreEqual("CQL_QUERY_CANCELLED",cancelled.ErrorCode);
        return new NativeFixtureJourneyProof(10,1,new Dictionary<string,bool>{
            ["nativeFirstLast"]=true,["duplicateOccurrencesAndIndexes"]=true,
            ["allFourIdentitySets"]=true,["multipleNeverDefaults"]=true,
            ["predicateAfterWindow"]=true,["selectedDistinctAndPredicate"]=true,
            ["nullRowSeparateFromAbsence"]=true,["syncAndAsyncSameOwner"]=true,
            ["foreignPrivateRowsExcluded"]=true,["cancellationPreserved"]=true});
    }
}
