extern alias ShippingWeb4;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using System.Text.Json;
using Worker = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive.HolonReplicationDeliveryHostedService;
using Neo = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using Sqlite = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public partial class NativeHostedReplicaRestartTests
{
    private sealed class PassLogger : ILogger<Worker>
    {
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (level >= LogLevel.Warning) Completed.TrySetException(exception ?? new InvalidOperationException(formatter(state, exception)));
            if (formatter(state, exception) == "HyperDrive replication delivery pass completed.") Completed.TrySetResult();
        }
    }

    [TestMethod]
    public Task ShippingWorkerRecoversRoutedNativeObligationAndDoesNotRedeliverAfterRestart()
        => VerifyShippingWorkerRestart(false);

    [TestMethod]
    public Task ShippingWorkerAcceptsNewNeoSaveAfterRestartDuringDestinationMetadataOutage()
        => VerifyShippingWorkerRestart(false, coldNewSave: true);

    [TestMethod]
    public Task NativeNeoAndSqliteRoutingPreservesEligibilityAndAvatarFaultIsolation()
        => VerifyShippingWorkerRestart(false, nativeRouting: true);

    private async Task VerifyShippingWorkerRestart(bool crash, string admission = null, bool olderBoundTransport = false, bool coldNewSave = false, bool nativeRouting = false)
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        var state = Path.Combine(root.FullName, "LocalState/COSMIC/OwnedRoutingCurrent");
        Directory.CreateDirectory(state);
        using var lease = new FileStream(Path.Combine(state, "hosted-restart.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var journal = Path.Combine(state, "hosted-restart.pending.json");
        var file = Path.Combine(root.FullName, "TestResults/Neo4jCurrent/hosted-restart-target.db");
        Assert.IsFalse(File.Exists(journal));
        var committedBoundary = Path.Combine(state, "hosted-restart-committed.json");
        Assert.IsFalse(File.Exists(committedBoundary));
        var ownedFiles = new[] { file, file + "-wal", file + "-shm", file + "-journal" };
        foreach (var path in ownedFiles) Assert.IsFalse(File.Exists(path), "Reconcile original owned fixture before reuse.");
        var actor = Guid.NewGuid();
        var operation = Guid.NewGuid();
        var input = new Holon { Id = coldNewSave || nativeRouting ? Guid.NewGuid() : Guid.Empty, Name = "coffee bought while worker stopped" };
        var coldInput = new Holon { Id = Guid.NewGuid(), Name = "coffee bought after Neo restart" };
        var coldOperation = Guid.NewGuid();
        using (var intent = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(intent, new { actor, operation, firstId = input.Id, coldOperation, coldId = coldInput.Id, coldNewSave, nativeRouting, target = file });
            intent.Flush(true);
        }
        var source = new Neo(uri, "neo4j", "oasis-local-test-only");
        var target = new Sqlite($"Data Source={file};Pooling=False");
        using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspect = driver.AsyncSession();
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
        { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false } } };
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        IHolonReplicationAuthorizationPolicy policy = admission == null || admission == "legacy-route" ? null
            : new HolonReplicationActorGrant(new[] { admission == "denied" ? Guid.NewGuid() : actor },
                DateTime.UtcNow.AddMinutes(admission == "expired" ? -1 : 5));
        var coldRegistration = false;
        DestinationProbeUnavailable probe = null;
        ProviderManager Registry()
        {
            var registry = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
            registry.RegisterProvider(source);
            if (coldRegistration)
            {
                var receiver = System.Reflection.DispatchProxy.Create<IOASISStorageProvider, DestinationProbeUnavailable>();
                probe = (DestinationProbeUnavailable)receiver;
                probe.Native = target;
                Assert.IsFalse(HolonReplicationRouteBinding.IsPinned(receiver));
                registry.RegisterProvider(receiver);
            }
            else registry.RegisterProvider(target);
            if (policy != null && admission != "policy-changed") HolonReplicationRoutes.UseAuthorizationBinding(registry, () => policy.BindingIdentity);
            return registry;
        }
        var registry = Registry();
        string targetId = null;
        async Task<string> FrozenPayload() => (await (await inspect.RunAsync(
            "MATCH (o:CosmicHolonOutbox {OperationId:$op}) RETURN o.PayloadJson AS payload",
            new { op = operation.ToString("D") })).SingleAsync())["payload"].As<string>();
        var complete = false;
        try
        {
            Assert.IsFalse((await source.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await target.ActivateProviderAsync()).IsError);
            var sourceBinding = await HolonReplicationRouteBinding.CapturePinnedAsync(source);
            var targetBinding = await HolonReplicationRouteBinding.CapturePinnedAsync(target);
            targetId = new HolonReplicationRouteBinding(sourceBinding, targetBinding,
                (admission == "policy-changed" ? null : policy?.BindingIdentity) ?? HolonReplicationRoutes.DefaultPolicyBinding).TargetId;
            if (admission == "legacy-route") targetId = HolonReplicationRoutes.TargetId(ProviderType.SQLLiteDBOASIS);
            var outbox = source.GetHolonReplicationOutbox().Result;
            var existing = await outbox.PeekNextAsync(targetId);
            Assert.IsFalse(existing.IsError, existing.Message);
            Assert.IsNull(existing.Result, "Do not deliver an unrelated retained obligation.");

            // No worker exists yet. The request settings disappear before startup;
            // recovery must use the durable target enrollment, not ambient routing.
            await Task.Run(async () =>
            {
                OASISRequestContext.AvatarRoutingOverrides = new ProviderRoutingOverrides(
                    autoReplicationEnabled: true, replicationProviders: new[] { ProviderType.SQLLiteDBOASIS });
                var saved = await new HolonManager(null, dna, registry).SaveOwnedHolonAsync(input, actor,
                    new CosmicWriteOptions(operation, expectedRevisions: null, replicationEnrollment: admission == "legacy-route"
                        ? new HolonReplicationEnrollment(HolonReplicationRoutes.SourceId(ProviderType.Neo4jOASIS), new[] { targetId })
                        : olderBoundTransport ? new HolonReplicationEnrollment(new HolonReplicationRouteBinding(sourceBinding,targetBinding,
                            HolonReplicationRoutes.DefaultPolicyBinding).SourceId,new[]{targetId},sourceBinding) : null),
                    saveChildren: false, providerType: ProviderType.Neo4jOASIS);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                Assert.IsTrue(saved.IsSaved);
            });
            Assert.IsNull(OASISRequestContext.AvatarRoutingOverrides);
            var pending = await outbox.PeekNextAsync(targetId);
            Assert.IsFalse(pending.IsError, pending.Message);
            Assert.AreEqual(operation, pending.Result.OperationId);
            Assert.AreEqual(HolonDeliveryState.Pending, pending.Result.State);
            Assert.AreEqual(0, pending.Result.AttemptCount);
            if(admission == "legacy-route" || olderBoundTransport) Assert.IsNull(pending.Result.NativeRoute);
            else
            {
                Assert.IsNotNull(pending.Result.NativeRoute);
                Assert.AreEqual(targetBinding.BindingFingerprint,pending.Result.NativeRoute.TargetStorage.BindingFingerprint);
            }
            var originalPayload = await FrozenPayload();
            var before = await target.LoadHolonAsync(input.Id, loadChildren: false);
            Assert.IsNull(before.Result);

            async Task RunOneRealPass()
            {
                var logger = new PassLogger();
                using var worker = policy == null
                    ? new Worker(logger, () => registry, () => dna.OASIS.HyperDriveMode == HyperDriveModes.V2)
                    : new Worker(logger, () => registry, () => dna.OASIS.HyperDriveMode == HyperDriveModes.V2, policy);
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await worker.StartAsync(deadline.Token);
                try { await logger.Completed.Task.WaitAsync(deadline.Token); }
                finally
                {
                    using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await worker.StopAsync(shutdown.Token);
                }
            }
            long coldSequence = 0;
            Guid? coldTargetRevision = null;
            if (coldNewSave)
            {
                Assert.IsFalse((await source.DeActivateProviderAsync()).IsError);
                target.Dispose();
                source = new Neo(uri, "neo4j", "oasis-local-test-only");
                target = new Sqlite($"Data Source={file};Pooling=False");
                Assert.IsFalse((await source.ActivateProviderAsync()).IsError);
                Assert.IsFalse((await target.ActivateProviderAsync()).IsError);
                coldRegistration = true;
                registry = Registry();
                var freshBinding = await HolonReplicationRouteBinding.CapturePinnedAsync(source);
                Assert.AreEqual(sourceBinding.BindingFingerprint, freshBinding.BindingFingerprint);
                var boundOutbox = source.GetBoundHolonReplicationOutbox(freshBinding).Result;
                var approval = await ((IHolonReplicationDestinationCatalogue)boundOutbox).ReadDestinationBindingAsync(ProviderType.SQLLiteDBOASIS);
                Assert.IsFalse(approval.IsError, approval.Message);
                Assert.AreEqual(operation, approval.Result.EstablishedOperationId);
                Assert.AreEqual(targetBinding.BindingFingerprint, approval.Result.Route.TargetStorage.BindingFingerprint);
                await Task.Run(async () => {
                    OASISRequestContext.AvatarRoutingOverrides = new ProviderRoutingOverrides(autoReplicationEnabled: true,
                        replicationProviders: new[] { ProviderType.SQLLiteDBOASIS });
                    var saved = await new HolonManager(null, dna, registry).SaveOwnedHolonAsync(coldInput, actor,
                        new CosmicWriteOptions(coldOperation), saveChildren: false, providerType: ProviderType.Neo4jOASIS);
                    Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                    Assert.IsTrue(saved.IsSaved);
                });
                Assert.AreEqual(0, probe.ProbeCalls, "A fresh source must enroll from native approval without destination recapture.");
                coldSequence = (await (await inspect.RunAsync("MATCH (o:CosmicHolonOutbox {OperationId:$op}) RETURN o.Sequence AS sequence",
                    new { op = coldOperation.ToString("D") })).SingleAsync())["sequence"].As<long>();
                outbox = source.GetHolonReplicationOutbox().Result;
                var queued = await outbox.InspectAsync(coldSequence);
                Assert.IsFalse(queued.IsError, queued.Message);
                Assert.AreEqual(coldOperation, queued.Result.OperationId);
                Assert.AreEqual(HolonDeliveryState.Pending, queued.Result.State);
                Assert.AreEqual(0, queued.Result.AttemptCount);
                Assert.IsNull((await target.LoadHolonAsync(coldInput.Id, loadChildren: false)).Result);
            }
            if (crash)
            {
                await CrashAfterNativeCommit(root.FullName, committedBoundary);
                var interrupted = await outbox.InspectAsync(pending.Result.Sequence);
                Assert.IsFalse(interrupted.IsError, interrupted.Message);
                Assert.AreEqual(HolonDeliveryState.InFlight, interrupted.Result.State);
                Assert.AreEqual(1, interrupted.Result.AttemptCount);
                var nativeCopy = await target.LoadHolonAsync(input.Id, loadChildren: false);
                Assert.IsFalse(nativeCopy.IsError, nativeCopy.Message);
                Assert.AreEqual(input.VersionId, nativeCopy.Result.VersionId);
                // Wait on the authority's actual clock/lease. No mutation of delivery
                // state, synthetic clock advance or unsafe second source save.
                var wait = System.Diagnostics.Stopwatch.StartNew();
                while (true)
                {
                    var nativeLease = await (await inspect.RunAsync("MATCH (o:CosmicHolonOutbox {OperationId:$op}) RETURN o.LeaseUntilUtcTicks - (datetime.realtime().epochMillis * 10000 + 621355968000000000) AS remaining", new { op = operation.ToString("D") })).SingleAsync();
                    if (nativeLease["remaining"].As<long>() <= 0) break;
                    Assert.IsTrue(wait.Elapsed < TimeSpan.FromMinutes(3), "Original native lease did not expire within the recovery deadline.");
                    await Task.Delay(1000);
                }
            }
            await RunOneRealPass();
            var delivered = await outbox.InspectAsync(pending.Result.Sequence);
            Assert.IsFalse(delivered.IsError, delivered.Message);
            if (admission != null)
            {
                Assert.AreEqual(HolonDeliveryState.ReconciliationRequired, delivered.Result.State);
                Assert.AreEqual(admission switch
                {
                    "legacy-route" => "COSMIC_REPLICATION_ROUTE_UNBOUND",
                    "policy-changed" => "COSMIC_REPLICATION_ROUTE_CHANGED",
                    _ => "COSMIC_REPLICA_AUTHORIZATION_DENIED"
                }, delivered.Result.ErrorCode);
                Assert.AreEqual(1, delivered.Result.AttemptCount);
                Assert.IsNull((await target.LoadHolonAsync(input.Id, loadChildren: false)).Result);
                var primary = await source.LoadHolonAsync(input.Id, loadChildren: false);
                Assert.IsFalse(primary.IsError, primary.Message);
                Assert.AreEqual(input.VersionId, primary.Result.VersionId);
                Assert.AreEqual(originalPayload, await FrozenPayload());
                await RunOneRealPass();
                var blocked = await outbox.InspectAsync(pending.Result.Sequence);
                Assert.AreEqual(HolonDeliveryState.ReconciliationRequired, blocked.Result.State);
                Assert.AreEqual(1, blocked.Result.AttemptCount);
                Assert.IsNull((await target.LoadHolonAsync(input.Id, loadChildren: false)).Result);
                complete = true;
                return; // Test-owned fixture cleanup, never automatic permission retry.
            }
            Assert.AreEqual(HolonDeliveryState.Completed, delivered.Result.State);
            Assert.AreEqual(crash ? 2 : 1, delivered.Result.AttemptCount);
            var copy = await target.LoadHolonAsync(input.Id, loadChildren: false);
            Assert.IsFalse(copy.IsError, copy.Message);
            Assert.AreEqual(input.VersionId, copy.Result.VersionId);
            Assert.AreEqual(actor, copy.Result.CreatedByAvatarId);

            if (coldNewSave)
            {
                var deliveredCold = await outbox.InspectAsync(coldSequence);
                Assert.IsFalse(deliveredCold.IsError, deliveredCold.Message);
                Assert.AreEqual(HolonDeliveryState.Completed, deliveredCold.Result.State);
                Assert.AreEqual(1, deliveredCold.Result.AttemptCount);
                Assert.AreEqual(0, probe.ProbeCalls);
                var coldCopy = await target.LoadHolonAsync(coldInput.Id, loadChildren: false);
                Assert.IsFalse(coldCopy.IsError, coldCopy.Message);
                Assert.AreEqual(coldInput.VersionId, coldCopy.Result.VersionId);
                Assert.AreEqual(actor, coldCopy.Result.CreatedByAvatarId);
                coldTargetRevision = CosmicConcurrency.GetProviderRevisionId(coldCopy.Result, ProviderType.SQLLiteDBOASIS);
                Assert.IsNotNull(coldTargetRevision);
            }

            // Join the old worker before releasing provider resources, then create
            // new provider/service instances. This is service restart, not OS crash.
            Assert.IsFalse((await source.DeActivateProviderAsync()).IsError);
            target.Dispose();
            source = new Neo(uri, "neo4j", "oasis-local-test-only");
            target = new Sqlite($"Data Source={file};Pooling=False");
            Assert.IsFalse((await source.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await target.ActivateProviderAsync()).IsError);
            registry = Registry();
            outbox = source.GetHolonReplicationOutbox().Result;
            await RunOneRealPass();
            var retained = await outbox.InspectAsync(pending.Result.Sequence);
            Assert.IsFalse(retained.IsError, retained.Message);
            Assert.AreEqual(HolonDeliveryState.Completed, retained.Result.State);
            Assert.AreEqual(crash ? 2 : 1, retained.Result.AttemptCount);
            Assert.AreEqual(originalPayload, await FrozenPayload());
            var fresh = await target.LoadHolonAsync(input.Id, loadChildren: false);
            Assert.IsFalse(fresh.IsError, fresh.Message);
            Assert.AreEqual(copy.Result.VersionId, fresh.Result.VersionId);
            Assert.AreEqual(copy.Result.Version, fresh.Result.Version);
            if (coldNewSave)
            {
                Assert.AreEqual(0, probe.ProbeCalls);
                var coldCopy = await target.LoadHolonAsync(coldInput.Id, loadChildren: false);
                Assert.IsFalse(coldCopy.IsError, coldCopy.Message);
                Assert.AreEqual(coldInput.VersionId, coldCopy.Result.VersionId);
                Assert.AreEqual(coldTargetRevision, CosmicConcurrency.GetProviderRevisionId(coldCopy.Result, ProviderType.SQLLiteDBOASIS));
                var coldPrimary = await source.LoadHolonAsync(coldInput.Id, loadChildren: false);
                Assert.IsFalse(coldPrimary.IsError, coldPrimary.Message);
                Assert.AreEqual(coldInput.VersionId, coldPrimary.Result.VersionId);
            }
            if (nativeRouting) await VerifyNativeReadRouting(registry, dna, source, input, actor);
            complete = true;
        }
        finally
        {
            Assert.IsFalse((await source.DeActivateProviderAsync()).IsError);
            target.Dispose();
            if (complete)
            {
                await (await inspect.RunAsync("MATCH (h:Holon {Id:$id}) DETACH DELETE h", new { id = input.Id.ToString("D") })).ConsumeAsync();
                await (await inspect.RunAsync("MATCH (r) WHERE (r:CosmicWriteReceipt OR r:CosmicHolonOutbox) AND r.OperationId=$op DELETE r", new { op = operation.ToString("D") })).ConsumeAsync();
                await (await inspect.RunAsync("MATCH (d:CosmicDestinationBinding {EstablishedOperationId:$op}) DELETE d", new { op = operation.ToString("D") })).ConsumeAsync();
                var remaining = await (await inspect.RunAsync("MATCH (r) WHERE (r:Holon AND r.Id=$id) OR ((r:CosmicWriteReceipt OR r:CosmicHolonOutbox) AND r.OperationId=$op) RETURN count(r) AS count", new { id = input.Id.ToString("D"), op = operation.ToString("D") })).SingleAsync();
                Assert.AreEqual(0L, remaining["count"].As<long>());
                foreach (var path in ownedFiles) { if (File.Exists(path)) File.Delete(path); Assert.IsFalse(File.Exists(path)); }
                if (coldNewSave)
                {
                    await (await inspect.RunAsync("MATCH (h:Holon {Id:$id}) DETACH DELETE h", new { id = coldInput.Id.ToString("D") })).ConsumeAsync();
                    await (await inspect.RunAsync("MATCH (r) WHERE (r:CosmicWriteReceipt OR r:CosmicHolonOutbox) AND r.OperationId=$op DELETE r",
                        new { op = coldOperation.ToString("D") })).ConsumeAsync();
                    var remainingCold = await (await inspect.RunAsync("MATCH (r) WHERE (r:Holon AND r.Id=$id) OR ((r:CosmicWriteReceipt OR r:CosmicHolonOutbox) AND r.OperationId=$op) RETURN count(r) AS count",
                        new { id = coldInput.Id.ToString("D"), op = coldOperation.ToString("D") })).SingleAsync();
                    Assert.AreEqual(0L, remainingCold["count"].As<long>());
                }
                if (crash) File.Delete(committedBoundary);
                File.Delete(journal);
            }
        }
    }
}
