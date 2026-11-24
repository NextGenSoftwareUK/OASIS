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
    /// <summary>Q62: global query filters apply natively to every session LINQ query, with explicit bypass.</summary>
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task SessionGlobalQueryFiltersApplyNativelyWithBypass(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyQueryFilterJourney,nativeJourneySuffix:"-query-filters-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyQueryFilterJourney(
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
        Holon New(string name)
        {
            var row=new Holon{Id=Guid.NewGuid(),Name=name,CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
            owned.Add(row.Id);return row;
        }
        async Task<List<Holon>> Stored(params Guid[] ids)
        {
            using var reader=CosmicSession.Create(manager,adapter,access).Result;
            var rows=await reader.Linq<Holon>(trackResults:false).Result.Where(row=>ids.Contains(row.Id)).ToCosmicListAsync();
            Assert.IsFalse(rows.IsError,rows.Message);return rows.Result.ToList();
        }
        var checks=0;
        using var session=CosmicSession.Create(manager,adapter,access).Result;
        var rows=new[]{New("tenant-row-1"),New("tenant-row-2"),New("tenant-row-3")};
        rows[0].Description="tenant-a";rows[1].Description="tenant-a";rows[2].Description="tenant-b";
        foreach(var row in rows)Assert.IsFalse(session.Add(row).IsError);
        Assert.IsFalse((await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))).IsError);
        var ids=rows.Select(row=>row.Id).ToArray();
        using var reader=CosmicSession.Create(manager,adapter,access).Result;
        Assert.IsFalse(reader.HasQueryFilter<Holon>(row=>row.Description=="tenant-a").IsError);
        var filtered=await reader.Linq<Holon>(CosmicQueryTrackingMode.NoTracking).Result.Where(row=>ids.Contains(row.Id)).ToCosmicListAsync();
        Assert.IsFalse(filtered.IsError,filtered.ErrorCode+": "+filtered.Message);
        CollectionAssert.AreEquivalent(new[]{ids[0],ids[1]},filtered.Result.Select(row=>row.Id).ToArray());checks++;
        var counted=await reader.Linq<Holon>(CosmicQueryTrackingMode.NoTracking).Result.Where(row=>ids.Contains(row.Id)).ToCosmicListAsync();
        Assert.AreEqual(2,counted.Result.Count,"The filter applies to every query, not once.");checks++;
        var bypass=await reader.Linq<Holon>(CosmicQueryTrackingMode.NoTracking,100,ignoreQueryFilters:true).Result.Where(row=>ids.Contains(row.Id)).ToCosmicListAsync();
        Assert.IsFalse(bypass.IsError,bypass.Message);Assert.AreEqual(3,bypass.Result.Count);checks++;
        // S20: Auto flush makes the session's own pending insert visible to its next async query.
        Assert.IsFalse(reader.SetFlushMode(CosmicFlushMode.Auto).IsError);
        var pending=New("tenant-row-4");pending.Description="tenant-a";Assert.IsFalse(reader.Add(pending).IsError);
        var withPending=ids.Append(pending.Id).ToArray();
        var flushedQuery=await reader.Linq<Holon>(CosmicQueryTrackingMode.NoTracking).Result.Where(row=>withPending.Contains(row.Id)).ToCosmicListAsync();
        Assert.IsFalse(flushedQuery.IsError,flushedQuery.ErrorCode+": "+flushedQuery.Message);
        Assert.AreEqual(3,flushedQuery.Result.Count,"The auto-flushed insert is visible and still filtered.");
        Assert.AreEqual(CosmicEntityState.Unchanged,reader.State(pending).Result);checks++;
        var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var hidden=await foreign.Linq<Holon>(CosmicQueryTrackingMode.NoTracking,100,ignoreQueryFilters:true).Result.Where(row=>ids.Contains(row.Id)).ToCosmicListAsync();
        Assert.IsFalse(hidden.IsError,hidden.Message);Assert.AreEqual(0,hidden.Result.Count,"Bypass never widens read access.");checks++;
        foreign.Dispose();
        return new NativeFixtureJourneyProof(checks,1,new Dictionary<string,bool>{
            ["globalFilterVerified"]=true,["explicitBypassVerified"]=true,["accessNotWidenedVerified"]=true,["autoFlushVerified"]=true});
    }
}
