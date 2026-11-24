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
    public Task NativeAsyncReductionsPreserveDomainsWindowsAndSessionOwnership(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyAsyncReductionJourney,nativeJourneySuffix:"-async-reduction-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyAsyncReductionJourney(
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
        var first=new Holon{Id=Guid.NewGuid(),Name="async-reduction-a",CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var last=new Holon{Id=Guid.NewGuid(),Name="async-reduction-b",CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        first.MetaData["amount"]=1.25m;last.MetaData["amount"]=2.75m;
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
        var sum=await Timed("selector-sum",()=>root.ToCosmicSumAsync(row=>row.Version));
        Assert.IsFalse(sum.IsError,sum.Message+sum.Exception);Assert.AreEqual(first.Version+last.Version,sum.Result);
        var average=await Timed("integer-average",()=>root.Select(row=>row.Version).ToCosmicAverageAsync());
        Assert.IsFalse(average.IsError,average.Message+average.Exception);
        Assert.AreEqual((first.Version+last.Version)/2d,average.Result);
        var exact=await Timed("decimal-average",()=>root.ToCosmicAverageAsync(row=>CosmicQueryMetadata.Value<decimal>(row,"amount")));
        Assert.IsFalse(exact.IsError,exact.Message+exact.Exception);Assert.AreEqual(2m,exact.Result);
        var minimum=await Timed("string-min",()=>root.ToCosmicMinAsync(row=>row.Name));
        Assert.IsFalse(minimum.IsError,minimum.Message+minimum.Exception);Assert.AreEqual("async-reduction-a",minimum.Result);
        var maximum=await Timed("string-max",()=>root.Select(row=>row.Name).ToCosmicMaxAsync());
        Assert.IsFalse(maximum.IsError,maximum.Message+maximum.Exception);Assert.AreEqual("async-reduction-b",maximum.Result);
        var post=await Timed("post-window-sum",()=>root.Take(1).Where(row=>row.Id==b).ToCosmicSumAsync(row=>row.Version));
        Assert.IsFalse(post.IsError,post.Message+post.Exception);Assert.AreEqual(0,post.Result);
        var selected=await Timed("entity-selected-decimal",()=>root.Take(2).Where(row=>row.Id==b)
            .Select(row=>new{row.Version,Amount=CosmicQueryMetadata.Value<decimal>(row,"amount")})
            .ToCosmicAverageAsync(row=>row.Amount));
        Assert.IsFalse(selected.IsError,selected.Message+selected.Exception);Assert.AreEqual(2.75m,selected.Result);
        var empty=await Timed("nullable-empty",()=>root.Where(row=>row.Id==missing)
            .Select(row=>CosmicQueryMetadata.Value<decimal?>(row,"amount")).ToCosmicAverageAsync());
        Assert.IsFalse(empty.IsError,empty.Message+empty.Exception);Assert.IsNull(empty.Result);Assert.AreEqual(1,empty.LoadedCount);
        var tracked=CosmicSession.Create(manager,adapter,access).Result;
        var trackedQuery=tracked.Linq<Holon>().Result.Where(row=>row.Id==a||row.Id==b);
        var sessionSum=await Timed("session-sum",()=>trackedQuery.ToCosmicSumAsync(row=>row.Version));
        Assert.IsFalse(sessionSum.IsError,sessionSum.Message+sessionSum.Exception);
        Assert.AreEqual(first.Version+last.Version,sessionSum.Result);
        tracked.Dispose();
        var disposed=await Timed("disposed-session",()=>trackedQuery.ToCosmicMinAsync(row=>row.Version));
        Assert.IsTrue(disposed.IsError);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        var cancelled=await Timed("cancelled",()=>root.Select(row=>row.Version).ToCosmicSumAsync(cancellation.Token));
        Assert.IsTrue(cancelled.IsError);Assert.AreEqual("CQL_QUERY_CANCELLED",cancelled.ErrorCode);
        var enumerated=false;
        var local=Enumerable.Range(0,1).Select(number=>{enumerated=true;return number;}).AsQueryable();
        var refused=await Timed("reject-local",()=>local.ToCosmicAverageAsync());
        Assert.IsTrue(refused.IsError);Assert.AreEqual("CQL_QUERY_PROVIDER_INVALID",refused.ErrorCode);Assert.IsFalse(enumerated);
        return new NativeFixtureJourneyProof(7,1,new Dictionary<string,bool>{
            ["nativeSelectorSumAndIntegerAverage"]=true,["exactDecimalAverage"]=true,
            ["nativeStringExtrema"]=true,["postWindowReduction"]=true,
            ["selectedAndNullableResults"]=true,["sessionReductionLifetime"]=true,
            ["cancellationAndNoEnumerableReduction"]=true});
    }
}
