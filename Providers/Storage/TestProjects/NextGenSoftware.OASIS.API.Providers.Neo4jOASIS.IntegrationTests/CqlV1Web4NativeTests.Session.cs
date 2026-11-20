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
        first.Name="session changed";
        Assert.AreEqual(CosmicEntityState.Modified,session.State(first).Result);
        clock.Restart();
        var again=await session.ReadAsync(query);
        timings["identity-query"]=clock.ElapsedMilliseconds;
        Assert.IsFalse(again.IsError,again.Message+again.Exception);
        Assert.AreSame(first,again.Result.Single(row=>row.Id==first.Id));
        Assert.AreEqual("session changed",first.Name,"A query must not overwrite unsaved tracked values.");
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
        return 1;
    }
}
