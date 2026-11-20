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
    public Task SessionTracksIdentityAndFlushesOwnedChanges(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,sessionJourney:true);

    private static async Task<int> VerifySessionJourney(IOASISStorageProvider provider,
        List<Guid> ownedIds, Dictionary<string,long> timings)
    {
        var native=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA { OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings {LogSwitchingProviders=false}
        }};
        var registry=new ProviderManager(null,dna) {
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false
        };
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();
        var opened=CosmicSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(actor).Result);
        Assert.IsFalse(opened.IsError,opened.Message);
        using var session=opened.Result;
        Holon New(string name)=>new() {Id=Guid.NewGuid(),Name=name,CreatedByAvatarId=actor,IsPublic=false,IsActive=true,IsNewHolon=true};
        var first=New("session first");var second=New("session second");
        ownedIds.Add(first.Id);ownedIds.Add(second.Id);
        Assert.IsFalse(session.Add(first).IsError);
        Assert.IsFalse(session.Add(second).IsError);
        Assert.AreEqual(CosmicEntityState.Added,session.State(first).Result);
        var clock=System.Diagnostics.Stopwatch.StartNew();
        var created=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        timings["create-batch"]=clock.ElapsedMilliseconds;
        Assert.IsFalse(created.IsError,created.Message+created.Exception);
        Assert.IsTrue(created.IsSaved);
        Assert.AreEqual(CosmicEntityState.Unchanged,session.State(first).Result);
        var ids=new[]{first.Id,second.Id};
        var query=session.Query<Holon>().Result.Where(row=>ids.Contains(row.Id)).Result;
        clock.Restart();
        var initial=await session.ReadAsync(query);
        timings["initial-query"]=clock.ElapsedMilliseconds;
        Assert.IsFalse(initial.IsError,initial.Message+initial.Exception);
        Assert.AreEqual(2,initial.Result.Count);
        Assert.AreSame(first,initial.Result.Single(row=>row.Id==first.Id));
        var linqRoot=session.Linq<Holon>();
        Assert.IsFalse(linqRoot.IsError,linqRoot.Message);
        clock.Restart();
        var standardRows=await linqRoot.Result.Where(row=>ids.Contains(row.Id))
            .OrderBy(row=>row.Name).Take(2).ToCosmicListAsync();
        timings["standard-linq-query"]=clock.ElapsedMilliseconds;
        Assert.IsFalse(standardRows.IsError,standardRows.Message+standardRows.Exception);
        Assert.AreEqual(2,standardRows.Result.Count);
        Assert.AreSame(first,standardRows.Result.Single(row=>row.Id==first.Id));
        clock.Restart();
        var terminal=linqRoot.Result.Single(row=>row.Id==first.Id);
        timings["standard-linq-entity-terminal"]=clock.ElapsedMilliseconds;
        Assert.AreSame(first,terminal);
        Assert.AreEqual(2,linqRoot.Result.Where(row=>ids.Contains(row.Id)).Count());
        clock.Restart();
        var untrackedLinq=await linqRoot.Result.AsCosmicNoTracking()
            .Where(row=>ids.Contains(row.Id)).ToCosmicListAsync();
        timings["standard-linq-no-tracking"]=clock.ElapsedMilliseconds;
        Assert.IsFalse(untrackedLinq.IsError,untrackedLinq.Message+untrackedLinq.Exception);
        var untrackedFirst=untrackedLinq.Result.Single(row=>row.Id==first.Id);
        Assert.AreNotSame(first,untrackedFirst);
        Assert.AreEqual(CosmicEntityState.Detached,session.State(untrackedFirst).Result);
        untrackedFirst.Name="untracked LINQ edit";
        Assert.AreEqual(CosmicEntityState.Unchanged,session.State(first).Result);
        Assert.AreSame(first,linqRoot.Result.AsCosmicNoTracking().AsCosmicTracking().Single(row=>row.Id==first.Id));

        first.Name="session changed";
        Assert.AreEqual(CosmicEntityState.Modified,session.State(first).Result);
        clock.Restart();
        var again=await session.ReadAsync(query);
        timings["identity-query"]=clock.ElapsedMilliseconds;
        Assert.IsFalse(again.IsError,again.Message+again.Exception);
        Assert.AreSame(first,again.Result.Single(row=>row.Id==first.Id));
        Assert.AreEqual("session changed",first.Name,"A query must not overwrite unsaved tracked values.");
        clock.Restart();
        var projected=await linqRoot.Result.Where(row=>ids.Contains(row.Id))
            .Select(row=>row.Name).ToCosmicListAsync();
        timings["standard-linq-projection"]=clock.ElapsedMilliseconds;
        Assert.IsFalse(projected.IsError,projected.Message+projected.Exception);
        CollectionAssert.AreEquivalent(new[]{"session first","session second"},projected.Result.ToArray());
        Assert.AreSame(first,linqRoot.Result.Single(row=>row.Id==first.Id));
        Assert.AreEqual("session changed",first.Name,"LINQ identity resolution must preserve pending edits.");

        Assert.AreEqual(CosmicEntityState.Modified,session.State(first).Result);
        clock.Restart();
        var databaseValues=await session.GetDatabaseValuesAsync(first);
        timings["database-values-with-pending-edit"]=clock.ElapsedMilliseconds;
        Assert.IsFalse(databaseValues.IsError,databaseValues.Message+databaseValues.Exception);
        Assert.AreNotSame(first,databaseValues.Result);
        Assert.AreEqual("session first",databaseValues.Result.Name);
        Assert.AreEqual("session changed",first.Name,"Database values must not overwrite unsaved edits.");
        Assert.AreEqual(CosmicEntityState.Detached,session.State(databaseValues.Result).Result);
        var third=New("session third");ownedIds.Add(third.Id);
        Assert.IsFalse(session.Add(third).IsError);
        clock.Restart();
        var flushed=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        timings["mixed-create-update-batch"]=clock.ElapsedMilliseconds;
        Assert.IsFalse(flushed.IsError,flushed.Message+flushed.Exception);
        Assert.IsTrue(flushed.IsSaved);
        Assert.AreEqual(CosmicEntityState.Unchanged,session.State(first).Result);
        Assert.AreEqual(CosmicEntityState.Unchanged,session.State(third).Result);
        var unchanged=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        Assert.IsFalse(unchanged.IsError,unchanged.Message);
        Assert.IsFalse(unchanged.IsSaved);
        clock.Restart();
        var detachedRows=await session.ReadAsync(query,trackResults:false);
        timings["no-tracking-query"]=clock.ElapsedMilliseconds;
        Assert.IsFalse(detachedRows.IsError,detachedRows.Message+detachedRows.Exception);
        var detached=detachedRows.Result.Single(row=>row.Id==first.Id);
        Assert.AreNotSame(first,detached);
        Assert.AreEqual(CosmicEntityState.Detached,session.State(detached).Result);
        detached.Name="not queued by this session";
        var detachedNoop=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        Assert.IsFalse(detachedNoop.IsError,detachedNoop.Message);
        Assert.IsFalse(detachedNoop.IsSaved);
        first.Name="explicitly discard this edit";
        clock.Restart();
        var refreshed=await session.ReloadAsync(first);
        timings["explicit-reload"]=clock.ElapsedMilliseconds;
        Assert.IsFalse(refreshed.IsError,refreshed.Message+refreshed.Exception);
        Assert.AreSame(first,refreshed.Result);
        Assert.AreEqual("session changed",first.Name);
        Assert.AreEqual(CosmicEntityState.Unchanged,session.State(first).Result);
        Assert.IsFalse(session.Detach(second).IsError);
        Assert.AreEqual(CosmicEntityState.Detached,session.State(second).Result);
        Assert.IsFalse(session.Clear().IsError);
        Assert.AreEqual(CosmicEntityState.Detached,session.State(first).Result);
        ids=new[]{first.Id,second.Id,third.Id};
        query=session.Query<Holon>().Result.Where(row=>ids.Contains(row.Id)).Result;
        clock.Restart();
        var reloaded=await session.ReadAsync(query);
        timings["fresh-query-after-clear"]=clock.ElapsedMilliseconds;
        Assert.IsFalse(reloaded.IsError,reloaded.Message+reloaded.Exception);
        Assert.AreEqual(3,reloaded.Result.Count);
        var persisted=reloaded.Result.Single(row=>row.Id==first.Id);
        Assert.AreNotSame(first,persisted);
        Assert.AreEqual("session changed",persisted.Name);
        using var foreign=CosmicSession.Create(manager,native,
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var foreignQuery=foreign.Query<Holon>().Result.Where(row=>ids.Contains(row.Id)).Result;
        clock.Restart();
        var denied=await foreign.ReadAsync(foreignQuery);
        timings["foreign-private-query"]=clock.ElapsedMilliseconds;
        Assert.IsFalse(denied.IsError,denied.Message+denied.Exception);
        Assert.AreEqual(0,denied.Result.Count,"Session tracking must not bypass native actor permissions.");
        var foreignLinq=foreign.Linq<Holon>();
        Assert.IsFalse(foreignLinq.IsError,foreignLinq.Message);
        Assert.AreEqual(0,foreignLinq.Result.Where(row=>ids.Contains(row.Id)).Count());

        return 1;
    }
}
