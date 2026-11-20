using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task SessionReadOnlyRootsSuppressUpdatesAndRetainExplicitLifecycle(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,readOnlyJourney:true);

    private static async Task<int> VerifyReadOnlyJourney(IOASISStorageProvider provider,
        List<Guid> ownedIds,Dictionary<string,long> timings)
    {
        var native=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings {LogSwitchingProviders=false}
        }};
        var registry=new ProviderManager(null,dna) {
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false
        };
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();
        using var session=CosmicSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        async Task<T> Timed<T>(string name,Func<Task<T>> operation)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try {return await operation();}
            finally {timings.Add(name,clock.ElapsedMilliseconds);}
        }
        Holon New(string name)=>new() {Id=Guid.NewGuid(),Name=name,
            CreatedByAvatarId=actor,IsPublic=false,IsActive=true,IsNewHolon=true};
        void Saved(OASISResult<IEnumerable<IHolon>> result)
        {Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsSaved);}
        var first=New("first persisted");var second=New("second persisted");
        ownedIds.Add(first.Id);ownedIds.Add(second.Id);
        Assert.IsFalse(session.Add(first).IsError);Assert.IsFalse(session.Add(second).IsError);
        var addedRefused=session.SetReadOnly(first);
        Assert.IsTrue(addedRefused.IsError);Assert.AreEqual("COSMIC_SESSION_READ_ONLY_INVALID",addedRefused.ErrorCode);
        Saved(await Timed("create-roots",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));
        var originalRevision=CosmicConcurrency.GetProviderRevisionId(first,session.Authority);
        Assert.IsFalse(session.SetReadOnly(first).IsError);Assert.IsFalse(session.SetReadOnly(second).IsError);
        Assert.IsTrue(session.IsReadOnly(first).Result);
        first.Name="ignored while read-only";second.Name="also ignored";
        Assert.AreEqual(CosmicEntityState.Unchanged,session.State(first).Result);
        var ignored=await Timed("read-only-flush-is-noop",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(ignored.IsError,ignored.Message);Assert.IsFalse(ignored.IsSaved);
        var persisted=await Timed("read-only-native-values-preserved",()=>session.GetDatabaseValuesAsync(first));
        Assert.IsFalse(persisted.IsError,persisted.Message);Assert.AreEqual("first persisted",persisted.Result.Name);
        Assert.AreEqual(originalRevision,CosmicConcurrency.GetProviderRevisionId(persisted.Result,session.Authority));

        Assert.IsFalse(session.SetReadOnly(first,false).IsError);Assert.IsFalse(session.IsReadOnly(first).Result);
        var baseline=await Timed("mutable-transition-adopts-baseline",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(baseline.IsError,baseline.Message);Assert.IsFalse(baseline.IsSaved);
        first.Name="mutable stored value";
        Saved(await Timed("subsequent-mutable-update",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));
        var changed=await Timed("mutable-native-values-verified",()=>session.GetDatabaseValuesAsync(first));
        Assert.IsFalse(changed.IsError,changed.Message);Assert.AreEqual("mutable stored value",changed.Result.Name);
        Assert.AreNotEqual(originalRevision,CosmicConcurrency.GetProviderRevisionId(changed.Result,session.Authority));
        Assert.IsFalse(session.SetReadOnly(first).IsError);
        first.Name="discard on explicit reload";
        var reloaded=await Timed("reload-retains-read-only",()=>session.ReloadAsync(first));
        Assert.IsFalse(reloaded.IsError,reloaded.Message);Assert.AreSame(first,reloaded.Result);
        Assert.AreEqual("mutable stored value",first.Name);Assert.IsTrue(session.IsReadOnly(first).Result);
        Assert.IsFalse(session.Remove(first,softDelete:false).IsError);
        Assert.AreEqual(CosmicEntityState.Deleted,session.State(first).Result);
        Saved(await Timed("explicit-read-only-delete",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));
        Assert.AreEqual(CosmicEntityState.Detached,session.State(first).Result);
        var absent=await provider.LoadHolonAsync(first.Id,loadChildren:false,recursive:false);
        Assert.IsNull(absent.Result);
        Assert.IsTrue(!absent.IsError || absent.ErrorCode==OASISResultErrorCodes.RecordNotFound,absent.Message);
        Assert.IsFalse(session.Detach(second).IsError);Assert.IsFalse(session.Attach(second).IsError);
        Assert.IsFalse(session.IsReadOnly(second).Result,"Detach must remove the prior read-only flag.");
        Assert.IsFalse(session.SetReadOnly(second).IsError);Assert.IsFalse(session.Clear().IsError);
        Assert.IsFalse(session.Attach(second).IsError);Assert.IsFalse(session.IsReadOnly(second).Result,
            "Clear must remove prior per-entity flags.");
        return 1;
    }
}
