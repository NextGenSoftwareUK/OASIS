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
        // O13: session statistics count native queries, flushes and tracked entities.
        var stats=reader.Statistics;
        Assert.AreEqual(4L,stats.QueryCount);Assert.AreEqual(1L,stats.FlushCount);Assert.AreEqual(0L,stats.FailedFlushCount);
        Assert.AreEqual(1,stats.EntityCount);Assert.AreEqual(0,stats.AddedCount);checks++;
        // S07: merge a disconnected copy onto the tracked instance and save it against its revision.
        using(var merger=CosmicSession.Create(manager,adapter,access).Result)
        {
            var detached=new Holon{Id=ids[0],Name="merged-name",Description="tenant-a",CreatedByAvatarId=actor,IsActive=true,IsPublic=false};
            var merged=await merger.MergeAsync(detached);
            Assert.IsFalse(merged.IsError,merged.ErrorCode+": "+merged.Message);Assert.AreNotSame(detached,merged.Result);
            Assert.AreEqual(CosmicEntityState.Modified,merger.State(merged.Result).Result);
            var mergedSave=await merger.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
            Assert.IsFalse(mergedSave.IsError,mergedSave.ErrorCode+": "+mergedSave.Message);
        }
        var afterMerge=(await Stored(ids[0])).Single();Assert.AreEqual("merged-name",afterMerge.Name);checks++;
        // O14: tenant-bound sessions stamp, filter and refuse cross-tenant writes natively.
        var tenantKey="tenant"+Guid.NewGuid().ToString("N");
        using(var tenantA=CosmicSession.Create(manager,adapter,access).Result)
        using(var tenantB=CosmicSession.Create(manager,adapter,access).Result)
        {
            Assert.IsFalse(tenantA.SetTenant("A",tenantKey).IsError);Assert.IsFalse(tenantB.SetTenant("B",tenantKey).IsError);
            var x=New("tenant-x");var y=New("tenant-y");
            Assert.IsFalse(tenantA.Add(x).IsError);Assert.IsFalse(tenantB.Add(y).IsError);
            Assert.IsFalse((await tenantA.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))).IsError);
            Assert.IsFalse((await tenantB.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))).IsError);
            var both=new[]{x.Id,y.Id};
            var tenantQuery=tenantA.Linq<Holon>(CosmicQueryTrackingMode.NoTracking,100,ignoreQueryFilters:true);
            Assert.IsFalse(tenantQuery.IsError,"Tenant query: "+tenantQuery.ErrorCode+": "+tenantQuery.Message+" "+tenantQuery.Exception);
            var seenByA=await tenantQuery.Result.Where(row=>both.Contains(row.Id)).ToCosmicListAsync();
            Assert.IsFalse(seenByA.IsError,seenByA.ErrorCode+": "+seenByA.Message);
            CollectionAssert.AreEqual(new[]{x.Id},seenByA.Result.Select(row=>row.Id).ToArray(),"The tenant predicate is not bypassable.");
            Assert.IsNull((await tenantA.FindAsync<Holon>(y.Id)).Result,"Find stays inside the tenant.");
            var intruder=HolonChangeTracking.Clone(x);intruder.Name="cross-tenant";
            Assert.IsFalse(tenantB.Attach(intruder).IsError);Assert.IsFalse(tenantB.Update(intruder).IsError);
            var refused=await tenantB.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
            Assert.IsTrue(refused.IsError);Assert.AreEqual("COSMIC_TENANT_MISMATCH",refused.ErrorCode);
        }
        checks++;
        // O11: a factory opens sessions that share configuration but not state.
        var factory=new CosmicSessionFactory(manager,adapter,access,null,
            opened=>opened.HasQueryFilter<Holon>(row=>row.Description=="tenant-a"),
            opened=>opened.SetFlushMode(CosmicFlushMode.Auto));
        using(var f1=factory.OpenSession().Result)
        using(var f2=factory.OpenSession().Result)
        {
            Assert.AreEqual(CosmicFlushMode.Auto,f1.FlushMode);Assert.AreNotSame(f1,f2);
            var viaFactory=await f1.Linq<Holon>(CosmicQueryTrackingMode.NoTracking).Result.Where(row=>ids.Contains(row.Id)).ToCosmicListAsync();
            Assert.IsFalse(viaFactory.IsError,viaFactory.Message);Assert.AreEqual(2,viaFactory.Result.Count,"The factory filter applies.");
            Assert.AreEqual(0,f2.Statistics.QueryCount,"Sessions share configuration, not state.");
            Assert.AreEqual(2L,factory.OpenedCount);
        }
        checks++;
        var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var hidden=await foreign.Linq<Holon>(CosmicQueryTrackingMode.NoTracking,100,ignoreQueryFilters:true).Result.Where(row=>ids.Contains(row.Id)).ToCosmicListAsync();
        Assert.IsFalse(hidden.IsError,hidden.Message);Assert.AreEqual(0,hidden.Result.Count,"Bypass never widens read access.");checks++;
        foreign.Dispose();
        return new NativeFixtureJourneyProof(checks,1,new Dictionary<string,bool>{
            ["globalFilterVerified"]=true,["explicitBypassVerified"]=true,["accessNotWidenedVerified"]=true,["autoFlushVerified"]=true,["sessionStatisticsVerified"]=true,["disconnectedMergeVerified"]=true,["tenantIsolationVerified"]=true,["sessionFactoryVerified"]=true});
    }
}
