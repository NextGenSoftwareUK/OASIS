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
    public sealed class FilterModelHolon:Holon {public int Score {get;set;}}

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeModelFiltersFreezeBeforePagingAndPreserveAccessOnBypass(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyModelQueryFilterJourney,nativeJourneySuffix:"-model-query-filter-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyModelQueryFilterJourney(
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
        var builder=new CosmicModelBuilder();
        builder.Entity<FilterModelHolon>().Property(row=>row.Score).HasMetadataKey("filter_score");
        var writable=builder.Build();Assert.IsFalse(writable.IsError,writable.Message);
        var threshold=20;
        builder.Entity<FilterModelHolon>().HasQueryFilter(row=>row.Score>=threshold).IsReadOnly();
        var filtered=builder.Build();Assert.IsFalse(filtered.IsError,filtered.Message);
        threshold=30;
        var later=builder.Build();Assert.IsFalse(later.IsError,later.Message);
        var bypass=filtered.Result.WithoutQueryFilters();
        Assert.IsTrue(filtered.Result.HasQueryFilters);Assert.IsFalse(bypass.HasQueryFilters);
        using var writer=CosmicSession.Create(manager,adapter,access,writable.Result).Result;
        var rows=new[]{10,20,30}.Select(score=>new FilterModelHolon{
            Id=Guid.NewGuid(),Name="model filter",Score=score,CreatedByAvatarId=actor,
            IsActive=true,IsPublic=false,IsNewHolon=true}).ToArray();
        owned.AddRange(rows.Select(row=>row.Id));
        foreach(var row in rows)Assert.IsFalse(writer.Add(row).IsError);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        var created=await Timed("create-filter-fixture",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        var ids=rows.Select(row=>row.Id).ToArray();
        using var reader=CosmicSession.Create(manager,adapter,access,filtered.Result).Result;
        var query=reader.Query<FilterModelHolon>().Result.Where(row=>ids.Contains(row.Id)).Result.OrderBy(row=>row.Score).Result;
        var selected=await Timed("read-filtered",()=>reader.ReadAsync(query,trackResults:false));
        Assert.IsFalse(selected.IsError,selected.Message+selected.Exception);
        CollectionAssert.AreEqual(new[]{20,30},selected.Result.Select(row=>row.Score).ToArray());
        var page=await Timed("read-filtered-page",()=>reader.ReadAsync(query.Take(1).Result,trackResults:false));
        Assert.IsFalse(page.IsError,page.Message+page.Exception);Assert.AreEqual(1,page.Result.Count);
        Assert.AreEqual(20,page.Result[0].Score);
        using var unfiltered=CosmicSession.Create(manager,adapter,access,bypass).Result;
        var bypassed=await Timed("read-explicit-bypass",()=>unfiltered.ReadAsync(
            unfiltered.Query<FilterModelHolon>().Result.Where(row=>ids.Contains(row.Id)).Result.OrderBy(row=>row.Score).Result));
        Assert.IsFalse(bypassed.IsError,bypassed.Message+bypassed.Exception);
        CollectionAssert.AreEqual(new[]{10,20,30},bypassed.Result.Select(row=>row.Score).ToArray());
        var originalVersion=bypassed.Result[0].Version;bypassed.Result[0].Score=99;
        var denied=await Timed("bypass-preserves-write-policy",()=>unfiltered.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(denied.IsError);Assert.IsFalse(denied.IsSaved);
        Assert.AreEqual("COSMIC_MODEL_READ_ONLY_ENTITY",denied.ErrorCode);
        Assert.AreEqual(originalVersion,bypassed.Result[0].Version);
        using var newer=CosmicSession.Create(manager,adapter,access,later.Result).Result;
        var current=await Timed("read-new-filter-snapshot",()=>newer.ReadAsync(
            newer.Query<FilterModelHolon>().Result.Where(row=>ids.Contains(row.Id)).Result,trackResults:false));
        Assert.IsFalse(current.IsError,current.Message+current.Exception);Assert.AreEqual(1,current.Result.Count);
        Assert.AreEqual(30,current.Result[0].Score);
        using var foreign=CosmicSession.Create(manager,adapter,
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,bypass).Result;
        var hidden=await Timed("bypass-preserves-caller-access",()=>foreign.ReadAsync(
            foreign.Query<FilterModelHolon>().Result.Where(row=>ids.Contains(row.Id)).Result,trackResults:false));
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        return new NativeFixtureJourneyProof(6,1,new Dictionary<string,bool>{
            ["mappedFilterCapturedOnce"]=true,["filterBeforePaging"]=true,
            ["closedValuesFrozenPerModel"]=true,["explicitBypassRetainsMapping"]=true,
            ["bypassPreservesReadOnlyPolicy"]=true,["bypassPreservesCallerPermissions"]=true});
    }
}
