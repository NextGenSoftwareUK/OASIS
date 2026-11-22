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
    public Task SessionUpdatesDetachedRootsWithOriginalRevisionAndWholeBatchRefusal(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,updateJourney:true);

    private static async Task<int> VerifyUpdateJourney(IOASISStorageProvider provider,
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
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();
        CosmicSession Open(Guid who)=>CosmicSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(who).Result).Result;
        using var original=Open(actor);using var current=Open(actor);using var staleContext=Open(actor);
        using var foreign=Open(Guid.NewGuid());using var missingContext=Open(actor);
        async Task<T> Timed<T>(string name,Func<Task<T>> operation)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try {return await operation();}
            finally {timings.Add(name,clock.ElapsedMilliseconds);}
        }
        void Saved(OASISResult<IEnumerable<IHolon>> result)
        {Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsSaved);}
        var root=new Holon {Id=Guid.NewGuid(),Name="original detached root",CreatedByAvatarId=actor,
            IsPublic=false,IsActive=true,IsNewHolon=true};
        ownedIds.Add(root.Id);Assert.IsFalse(original.Add(root).IsError);
        Saved(await Timed("create-owned-root",()=>original.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));
        var stale=(Holon)HolonChangeTracking.Clone(root);var dto=(Holon)HolonChangeTracking.Clone(root);
        dto.Name="detached persisted";
        Assert.IsFalse(current.Update(dto).IsError);Assert.AreEqual(CosmicEntityState.Modified,current.State(dto).Result);
        var conflictInstance=current.Update(HolonChangeTracking.Clone(dto));
        Assert.IsTrue(conflictInstance.IsError);Assert.AreEqual("COSMIC_SESSION_IDENTITY_CONFLICT",conflictInstance.ErrorCode);
        Saved(await Timed("detached-update",()=>current.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));
        Assert.AreEqual(CosmicEntityState.Unchanged,current.State(dto).Result);
        var loaded=await Timed("detached-database-value",()=>current.GetDatabaseValuesAsync(dto));
        Assert.IsFalse(loaded.IsError,loaded.Message);Assert.AreEqual("detached persisted",loaded.Result.Name);
        var beforeForced=CosmicConcurrency.GetProviderRevisionId(dto,native.QueryProviderType);
        Assert.IsFalse(current.Update(dto).IsError);
        Saved(await Timed("force-tracked-update",()=>current.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));
        Assert.AreNotEqual(beforeForced,CosmicConcurrency.GetProviderRevisionId(dto,native.QueryProviderType));

        stale.Name="stale must not win";Assert.IsFalse(staleContext.Update(stale).IsError);
        var staleWrite=await Timed("stale-update-refused",async ()=>{
            // An explicit empty GUID is an existing legacy token, not create-only.
            // Against this newer token it must reach native concurrency refusal.
            var legacyToken=await staleContext.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid(),
                new Dictionary<Guid,Guid?> {{stale.Id,Guid.Empty}}));
            Assert.IsTrue(legacyToken.IsError);Assert.IsFalse(legacyToken.IsSaved);
            Assert.AreEqual(CosmicConcurrencyException.Code,legacyToken.ErrorCode);
            return await staleContext.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        });
        Assert.IsTrue(staleWrite.IsError);Assert.IsFalse(staleWrite.IsSaved);
        Assert.AreEqual(CosmicEntityState.Modified,staleContext.State(stale).Result);
        var forbidden=HolonChangeTracking.Clone(dto);forbidden.Name="foreign must not win";
        Assert.IsFalse(foreign.Update(forbidden).IsError);
        var denied=await Timed("foreign-update-refused",()=>foreign.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(denied.IsError);Assert.IsFalse(denied.IsSaved);

        var noRevision=new Holon {Id=dto.Id,Name="missing revision must not win",ParentHolonId=dto.ParentHolonId,
            CreatedByAvatarId=actor,IsPublic=false,IsActive=true};
        var notInserted=new Holon {Id=Guid.Empty,Name="update-revision-guard-"+Guid.NewGuid().ToString("N"),
            CreatedByAvatarId=actor,IsPublic=false,IsActive=true,IsNewHolon=true};
        Assert.IsFalse(missingContext.Update(noRevision).IsError);Assert.IsFalse(missingContext.Add(notInserted).IsError);
        await Timed("missing-revision-whole-batch-refused",async ()=>{
            var absent=await missingContext.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
            Assert.IsTrue(absent.IsError);Assert.AreEqual("COSMIC_SESSION_UPDATE_REVISIONS_REQUIRED",absent.ErrorCode);
            var createOnly=await missingContext.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid(),
                new Dictionary<Guid,Guid?> {{dto.Id,null}}));
            Assert.IsTrue(createOnly.IsError);Assert.AreEqual("COSMIC_SESSION_UPDATE_REVISIONS_REQUIRED",createOnly.ErrorCode);
            Assert.AreEqual(Guid.Empty,notInserted.Id);
            var candidateName=notInserted.Name;
            var absentRows=await original.Linq<Holon>().Result.Where(row=>row.Name==candidateName).ToCosmicListAsync();
            Assert.IsFalse(absentRows.IsError,absentRows.Message);Assert.AreEqual(0,absentRows.Result.Count);
            return true;
        });
        await Timed("reload-cancels-explicit-update",async ()=>{
            Assert.IsFalse(current.Update(dto).IsError);
            var reloaded=await current.ReloadAsync(dto);Assert.IsFalse(reloaded.IsError,reloaded.Message);
            Assert.AreEqual(CosmicEntityState.Unchanged,current.State(dto).Result);
            var noop=await current.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
            Assert.IsFalse(noop.IsError,noop.Message);Assert.IsFalse(noop.IsSaved);return true;
        });
        await Timed("readonly-transition-cancels-update-intent",async ()=>{
            Assert.IsFalse(current.Update(dto).IsError);Assert.IsFalse(current.SetReadOnly(dto).IsError);
            var readonlyUpdate=current.Update(dto);
            Assert.IsTrue(readonlyUpdate.IsError);Assert.AreEqual("COSMIC_SESSION_READ_ONLY_ENTITY",readonlyUpdate.ErrorCode);
            Assert.IsFalse(current.SetReadOnly(dto,false).IsError);
            Assert.AreEqual(CosmicEntityState.Unchanged,current.State(dto).Result);
            var noop=await current.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
            Assert.IsFalse(noop.IsError,noop.Message);Assert.IsFalse(noop.IsSaved);return true;
        });
        await Timed("verify-native-survivor",async ()=>{
            var stored=await provider.LoadHolonAsync(dto.Id,loadChildren:false,recursive:false);
            Assert.IsFalse(stored.IsError,stored.Message+stored.Exception);Assert.IsNotNull(stored.Result);
            Assert.AreEqual("detached persisted",stored.Result.Name);return true;
        });
        return 1;
    }
}
