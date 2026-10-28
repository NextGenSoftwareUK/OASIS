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
public class NativeHostedReplicaRestartTests
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
    public async Task ShippingWorkerRecoversRoutedNativeObligationAndDoesNotRedeliverAfterRestart()
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
        var ownedFiles = new[] { file, file + "-wal", file + "-shm", file + "-journal" };
        foreach (var path in ownedFiles) Assert.IsFalse(File.Exists(path), "Reconcile original owned fixture before reuse.");
        var actor = Guid.NewGuid();
        var operation = Guid.NewGuid();
        using (var intent = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(intent, new { actor, operation, target = file });
            intent.Flush(true);
        }
        var source = new Neo(uri, "neo4j", "oasis-local-test-only");
        var target = new Sqlite($"Data Source={file};Pooling=False");
        using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspect = driver.AsyncSession();
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
        { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false } } };
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        ProviderManager Registry()
        {
            var registry = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
            registry.RegisterProvider(source);
            registry.RegisterProvider(target);
            return registry;
        }
        var registry = Registry();
        var input = new Holon { Name = "coffee bought while worker stopped" };
        var targetId = HolonReplicationRoutes.TargetId(ProviderType.SQLLiteDBOASIS);
        async Task<string> FrozenPayload() => (await (await inspect.RunAsync(
            "MATCH (o:CosmicHolonOutbox {OperationId:$op}) RETURN o.PayloadJson AS payload",
            new { op = operation.ToString("D") })).SingleAsync())["payload"].As<string>();
        var complete = false;
        try
        {
            Assert.IsFalse((await source.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await target.ActivateProviderAsync()).IsError);
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
                    new CosmicWriteOptions(operation), saveChildren: false, providerType: ProviderType.Neo4jOASIS);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                Assert.IsTrue(saved.IsSaved);
            });
            Assert.IsNull(OASISRequestContext.AvatarRoutingOverrides);
            var pending = await outbox.PeekNextAsync(targetId);
            Assert.IsFalse(pending.IsError, pending.Message);
            Assert.AreEqual(operation, pending.Result.OperationId);
            Assert.AreEqual(HolonDeliveryState.Pending, pending.Result.State);
            Assert.AreEqual(0, pending.Result.AttemptCount);
            var originalPayload = await FrozenPayload();
            var before = await target.LoadHolonAsync(input.Id, loadChildren: false);
            Assert.IsNull(before.Result);

            async Task RunOneRealPass()
            {
                var logger = new PassLogger();
                using var worker = new Worker(logger, () => registry, () => dna.OASIS.HyperDriveMode == HyperDriveModes.V2);
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await worker.StartAsync(deadline.Token);
                try { await logger.Completed.Task.WaitAsync(deadline.Token); }
                finally
                {
                    using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await worker.StopAsync(shutdown.Token);
                }
            }
            await RunOneRealPass();
            var delivered = await outbox.InspectAsync(pending.Result.Sequence);
            Assert.IsFalse(delivered.IsError, delivered.Message);
            Assert.AreEqual(HolonDeliveryState.Completed, delivered.Result.State);
            Assert.AreEqual(1, delivered.Result.AttemptCount);
            var copy = await target.LoadHolonAsync(input.Id, loadChildren: false);
            Assert.IsFalse(copy.IsError, copy.Message);
            Assert.AreEqual(input.VersionId, copy.Result.VersionId);
            Assert.AreEqual(actor, copy.Result.CreatedByAvatarId);

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
            Assert.AreEqual(1, retained.Result.AttemptCount);
            Assert.AreEqual(originalPayload, await FrozenPayload());
            var fresh = await target.LoadHolonAsync(input.Id, loadChildren: false);
            Assert.IsFalse(fresh.IsError, fresh.Message);
            Assert.AreEqual(copy.Result.VersionId, fresh.Result.VersionId);
            Assert.AreEqual(copy.Result.Version, fresh.Result.Version);
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
                var remaining = await (await inspect.RunAsync("MATCH (r) WHERE (r:Holon AND r.Id=$id) OR ((r:CosmicWriteReceipt OR r:CosmicHolonOutbox) AND r.OperationId=$op) RETURN count(r) AS count", new { id = input.Id.ToString("D"), op = operation.ToString("D") })).SingleAsync();
                Assert.AreEqual(0L, remaining["count"].As<long>());
                foreach (var path in ownedFiles) { if (File.Exists(path)) File.Delete(path); Assert.IsFalse(File.Exists(path)); }
                File.Delete(journal);
            }
        }
    }
}
