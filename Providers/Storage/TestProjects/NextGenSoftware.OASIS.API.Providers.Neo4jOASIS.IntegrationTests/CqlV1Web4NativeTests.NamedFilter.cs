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
    public Task NativeNamedFiltersSelectivelyBypassWithoutRemovingCallerPermissions(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyNamedFilterJourney,nativeJourneySuffix:"-named-filter-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyNamedFilterJourney(
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
        builder.Entity<FilterModelHolon>().Property(row=>row.Score).HasMetadataKey("named_filter_score");
        builder.Entity<FilterModelHolon>().ShadowProperty<string>("Label").HasMetadataKey("named_filter_label");
        var writable=builder.Build();Assert.IsFalse(writable.IsError,writable.Message);
        var threshold=30;
        builder.Entity<FilterModelHolon>()
            .HasQueryFilter(row=>row.Score>=20)
            .HasQueryFilter("minimum",row=>row.Score>=threshold)
            .HasQueryFilter("maximum",row=>row.Score<=30).IsReadOnly();
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        var filtered=built.Result;
        threshold=40;
        var later=builder.Build();Assert.IsFalse(later.IsError,later.Message);
        var lowerBypass=filtered.WithoutQueryFilters("minimum");
        var upperBypass=filtered.WithoutQueryFilters("maximum");
        var bothBypass=lowerBypass.WithoutQueryFilters("maximum");
        Assert.IsTrue(bothBypass.HasQueryFilters,"Unnamed filter must survive selective bypass.");
        Assert.AreEqual("named_filter_label",bothBypass.GetShadowProperties<FilterModelHolon>().Single().MetadataKey);
        Assert.ThrowsException<ArgumentException>(()=>filtered.WithoutQueryFilters("missing"));
        Assert.ThrowsException<ArgumentException>(()=>filtered.WithoutQueryFilters("minimum","minimum"));
        Assert.ThrowsException<ArgumentException>(()=>lowerBypass.WithoutQueryFilters("minimum"));
        Assert.ThrowsException<ArgumentException>(()=>builder.Entity<FilterModelHolon>().HasQueryFilter("bad name",row=>true));
        using var writer=CosmicSession.Create(manager,adapter,access,writable.Result).Result;
        var rows=new[]{10,20,30,40}.Select(score=>new FilterModelHolon{
            Id=Guid.NewGuid(),Name="named model filter",Score=score,CreatedByAvatarId=actor,
            IsActive=true,IsPublic=false,IsNewHolon=true}).ToArray();
        owned.AddRange(rows.Select(row=>row.Id));
        foreach(var row in rows)Assert.IsFalse(writer.Add(row).IsError);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        var created=await Timed("create-named-filter-fixture",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        var ids=rows.Select(row=>row.Id).ToArray();
        async Task Read(CosmicModel model,string name,int[] expected,int? take=null)
        {
            using var session=CosmicSession.Create(manager,adapter,access,model).Result;
            var query=session.Query<FilterModelHolon>().Result.Where(row=>ids.Contains(row.Id)).Result.OrderBy(row=>row.Score).Result;
            if(take.HasValue)query=query.Take(take.Value).Result;
            var result=await Timed(name,()=>session.ReadAsync(query,trackResults:false));
            Assert.IsFalse(result.IsError,result.Message+result.Exception);
            CollectionAssert.AreEqual(expected,result.Result.Select(row=>row.Score).ToArray(),name);
        }
        await Read(filtered,"all-named-filters",new[]{30});
        await Read(lowerBypass,"minimum-only-bypass",new[]{20,30});
        await Read(upperBypass,"maximum-only-bypass",new[]{30,40});
        await Read(bothBypass,"both-bypass-keeps-unnamed",new[]{20,30,40});
        await Read(lowerBypass,"named-filter-before-page",new[]{20},1);
        await Read(later.Result,"new-build-captures-changed-value",Array.Empty<int>());
        using var foreign=CosmicSession.Create(manager,adapter,
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,bothBypass).Result;
        var hidden=await Timed("selective-bypass-preserves-access",()=>foreign.ReadAsync(
            foreign.Query<FilterModelHolon>().Result.Where(row=>ids.Contains(row.Id)).Result,trackResults:false));
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        using var deniedSession=CosmicSession.Create(manager,adapter,access,bothBypass).Result;
        var added=deniedSession.Add(new FilterModelHolon{Id=Guid.NewGuid(),Score=50,CreatedByAvatarId=actor,IsActive=true,IsNewHolon=true});
        Assert.IsFalse(added.IsError);
        var denied=await Timed("selective-bypass-preserves-readonly",()=>deniedSession.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(denied.IsError);Assert.IsFalse(denied.IsSaved);
        Assert.AreEqual("COSMIC_MODEL_READ_ONLY_ENTITY",denied.ErrorCode);
        return new NativeFixtureJourneyProof(8,1,new Dictionary<string,bool>{
            ["distinctNamedFiltersComposeWithUnnamed"]=true,["selectiveBypassKeepsOtherFilters"]=true,
            ["filterBeforePaging"]=true,["closedValuesFrozenPerModel"]=true,
            ["selectiveBypassRetainsShadowMapping"]=true,["invalidNamesRefused"]=true,
            ["bypassPreservesReadOnlyPolicy"]=true,["bypassPreservesCallerPermissions"]=true});
    }
}
