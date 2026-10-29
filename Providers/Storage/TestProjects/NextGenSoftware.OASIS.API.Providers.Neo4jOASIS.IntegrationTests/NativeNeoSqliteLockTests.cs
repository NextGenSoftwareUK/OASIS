extern alias ShippingWeb4;

using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Policy = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive.CommittedOwnerReplicationPolicy;
using Neo = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using Sqlite = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class NativeHostedReplicaRestartTests
{
    private static async Task<long> VerifyNeoSourceWhileSqliteLocked(Neo source, Sqlite target,
        OASISDNA dna, ProviderManager registry, string file, Guid actor, Holon second, Guid secondOperation,
        CosmicStorageIdentity sourceBinding, CosmicStorageIdentity targetBinding, string targetId, long firstSequence)
    {
        var bound = source.GetBoundHolonReplicationOutbox(sourceBinding);
        Assert.IsFalse(bound.IsError, bound.Message);
        var outbox = bound.Result;
        var receiver = new CommittedHolonReplicaTarget(targetId, target, ProviderType.SQLLiteDBOASIS,
            new HolonReplicationRouteBinding(sourceBinding, targetBinding, HolonReplicationRoutes.DefaultPolicyBinding).SourceId, ProviderType.Neo4jOASIS, new Policy(),
            "native-sqlite-lock-v1", new Guid("013c121e-8acb-4f46-b80e-3201f9a83c9f"),
            HolonReplicationRoutes.DefaultPolicyBinding, targetBinding);
        var dispatcher = new HolonOutboxDispatcher(outbox, new[] { receiver }, "native-neo-sqlite-lock");
        long secondSequence;
        using (var blocker = new SqliteConnection($"Data Source={file};Pooling=False;Default Timeout=1"))
        {
            await blocker.OpenAsync();
            using var held = blocker.BeginTransaction(deferred: false);

            // The destination is genuinely write-locked by another native connection.
            // The established source catalogue must still enroll a new acknowledged save.
            await Task.Run(async () => {
                OASISRequestContext.AvatarRoutingOverrides = new ProviderRoutingOverrides(
                    autoReplicationEnabled: true, replicationProviders: new[] { ProviderType.SQLLiteDBOASIS });
                var saved = await new HolonManager(null, dna, registry).SaveOwnedHolonAsync(second, actor,
                    new CosmicWriteOptions(secondOperation), saveChildren: false, providerType: ProviderType.Neo4jOASIS);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                Assert.IsTrue(saved.IsSaved);
            });
            Assert.IsNull(OASISRequestContext.AvatarRoutingOverrides);
            secondSequence = firstSequence + 1;
            var queued = await outbox.InspectAsync(secondSequence);
            Assert.IsFalse(queued.IsError, queued.Message);
            Assert.AreEqual(secondOperation, queued.Result.OperationId);
            Assert.AreEqual(HolonDeliveryState.Pending, queued.Result.State);
            Assert.AreEqual(0, queued.Result.AttemptCount);

            var attempted = await dispatcher.DispatchOnceAsync(targetId);
            Assert.IsFalse(attempted.IsError, attempted.Message + attempted.Exception);
            Assert.AreEqual(1, attempted.Result.ClaimedCount);
            Assert.AreEqual(1, attempted.Result.DeferredCount);
            Assert.AreEqual(0, attempted.Result.CompletedCount);
            Assert.AreEqual(0, attempted.Result.ReconciliationCount);
            Assert.IsNotNull(attempted.Exception, "Preserve the native busy failure cause.");
            var first = await outbox.InspectAsync(firstSequence);
            Assert.AreEqual(HolonDeliveryState.Pending, first.Result.State);
            Assert.AreEqual(1, first.Result.AttemptCount);
            Assert.AreEqual("COSMIC_REPLICA_WRITE_FAILED", first.Result.ErrorCode);
            Assert.IsNotNull(first.Result.RecoveryEvidence);
            Assert.IsFalse(first.Result.RecoveryEvidence.RequiresNativeReconciliation);
            queued = await outbox.InspectAsync(secondSequence);
            Assert.AreEqual(HolonDeliveryState.Pending, queued.Result.State);
            Assert.AreEqual(0, queued.Result.AttemptCount, "The later save must never overtake the locked head.");
        }
        // Wait for the dispatcher's specified first retry delay; do not alter native state.
        await Task.Delay(TimeSpan.FromMilliseconds(1100));
        Assert.IsNull((await target.LoadHolonAsync(second.Id, loadChildren: false)).Result);
        return secondSequence;
    }
}
