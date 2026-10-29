extern alias ShippingWeb4;

using System.Net.Sockets;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Policy = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive.CommittedOwnerReplicationPolicy;
using Worker = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive.HolonReplicationDeliveryHostedService;
using Mongo = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using Sqlite = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class NativeHostedReplicaRestartTests
{
    private sealed class StoppedDestinationLogger(bool outage) : ILogger<Worker>
    {
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception exception, Func<TState, Exception, string> format)
        {
            var message = format(state, exception);
            // Only the stopped Mongo source-role observation is expected. A
            // SQLite source, dispatcher or policy failure must fail the test.
            var expected = outage && ((message.StartsWith("Replication source", StringComparison.Ordinal)
                && message.Contains("MongoDBOASIS", StringComparison.Ordinal))
                || message.StartsWith("Replication route inventory MongoDBOASIS failed:", StringComparison.Ordinal));
            if (level >= LogLevel.Warning && !expected)
                Completed.TrySetException(exception ?? new InvalidOperationException(message));
            if (message == "HyperDrive replication delivery pass completed.") Completed.TrySetResult();
        }
    }

    public sealed class StoppedDestinationIntent
    {
        public string Phase { get; set; } = "planned";
        public Guid Actor { get; set; } = Guid.NewGuid();
        public Guid[] Ids { get; set; } = new[] { Guid.NewGuid(), Guid.NewGuid() };
        public Guid[] Operations { get; set; } = new[] { Guid.NewGuid(), Guid.NewGuid() };
        public string SourceFingerprint { get; set; }
        public string TargetFingerprint { get; set; }
        public string TargetId { get; set; }
        public long FirstSequence { get; set; }
        public Guid WitnessId { get; set; } = Guid.NewGuid();
        public Guid WitnessOperation { get; set; } = Guid.NewGuid();
    }

    [TestMethod]
    public async Task ShippingWorkerPersistsNewSaveWhileMongoDestinationProcessIsStopped()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string database = "cosmic_stopped_destination_fixture";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var phase = Environment.GetEnvironmentVariable("OASIS_OFFLINE_REPLICATION_PHASE");
        Assert.IsTrue(phase is "seed" or "offline" or "deliver" or "active" or "reconcile-active" or "exhaust" or "routing" or "cleanup-unwritten" or "reconcile-routing" or "consistency" or "reconcile-consistency", "Use the explicit process-owning runner.");
        var exhaust = phase == "exhaust";
        var routingProof = phase == "routing";
        var consistencyProof = phase is "consistency" or "reconcile-consistency";
        var cleanupUnwritten = phase == "cleanup-unwritten";
        var activeOutage = phase == "active" || exhaust || routingProof || phase == "consistency";
        var reconcileActive = phase == "reconcile-active";
        var reconcileRouting = phase is "reconcile-routing" or "reconcile-consistency";
        var seeding = phase == "seed" || activeOutage;
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        var state = Path.Combine(root.FullName, "LocalState/COSMIC/OwnedRoutingCurrent");
        Directory.CreateDirectory(state);
        using var lease = File.Open(Path.Combine(state, "worker-offline.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var journal = Path.Combine(state, "worker-offline.pending.json");
        var file = Path.Combine(root.FullName, "TestResults/OwnedRoutingCurrent/worker-offline.db");
        var ownedFiles = new[] { file, file + "-wal", file + "-shm", file + "-journal" };
        if (routingProof || consistencyProof) OASISHyperDrive.DataDirectory = Path.Combine(root.FullName, "TestResults/OwnedRoutingCurrent/routing-quota");
        var client = new MongoClient(uri);
        StoppedDestinationIntent intent;
        if (seeding)
        {
            Assert.IsFalse(File.Exists(journal));
            foreach (var path in ownedFiles) Assert.IsFalse(File.Exists(path));
            using var names = await client.ListDatabaseNamesAsync();
            Assert.IsFalse((await names.ToListAsync()).Contains(database));
            intent = new();
            var initial = journal + ".next";
            using (var planned = new FileStream(initial, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { await JsonSerializer.SerializeAsync(planned, intent); planned.Flush(true); }
            File.Move(initial, journal, false); // Publish only a complete flushed journal to the observing runner.
        }
        else
        {
            intent = JsonSerializer.Deserialize<StoppedDestinationIntent>(await File.ReadAllTextAsync(journal));
            Assert.IsNotNull(intent);
            Assert.AreEqual(cleanupUnwritten ? "planned" : reconcileActive || reconcileRouting ? "ready-to-stop" : phase == "offline" ? "seeded" : "offline-saved", intent.Phase);
        }
        async Task Publish(string nextPhase)
        {
            intent.Phase = nextPhase;
            var next = journal + ".next";
            using (var stream = new FileStream(next, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { await JsonSerializer.SerializeAsync(stream, intent); stream.Flush(true); }
            File.Move(next, journal, true);
        }

        if (phase == "offline")
        {
            using var socket = new TcpClient();
            await Assert.ThrowsExceptionAsync<SocketException>(async () => await socket.ConnectAsync("127.0.0.1", 27029));
        }
        else
        {
            // Readiness observation only; no business write or automatic replay.
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(40));
            while (true)
            {
                var hello = await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1), cancellationToken: deadline.Token);
                if (hello.GetValue("isWritablePrimary", false).AsBoolean) break;
                await Task.Delay(250, deadline.Token);
            }
        }

        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false } } };
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var sqlite = new Sqlite($"Data Source={file};Pooling=False");
        var mongo = new Mongo(uri, database);
        var complete = false;
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            if (phase != "offline") Assert.IsFalse((await mongo.ActivateProviderAsync()).IsError);
            else Assert.IsFalse(mongo.IsProviderActivated, "Offline enrollment cannot activate a stopped destination.");
            var registry = new ProviderManager(null, dna) {
                IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
            registry.RegisterProvider(sqlite);
            registry.RegisterProvider(mongo);
            if (cleanupUnwritten)
            {
                Assert.IsNull(intent.SourceFingerprint);
                Assert.IsNull(intent.TargetFingerprint);
                Assert.AreEqual(0L, intent.FirstSequence);
                foreach (var collection in new[] { "Holon", "CosmicWriteReceipt", "CosmicHolonOutbox", "CosmicDestinationBinding" })
                    Assert.AreEqual(0L, await client.GetDatabase(database).GetCollection<BsonDocument>(collection)
                        .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty), "Preserve any unexpected native data.");
                using (var unread = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={file};Mode=ReadOnly;Pooling=False"))
                {
                    await unread.OpenAsync();
                    using var query = unread.CreateCommand();
                    foreach (var table in new[] { "Holon", "CosmicWriteReceipt", "CosmicHolonOutbox", "CosmicDestinationBinding" })
                    {
                        query.CommandText = "SELECT COUNT(*) FROM " + table;
                        Assert.AreEqual(0L, Convert.ToInt64(await query.ExecuteScalarAsync()));
                    }
                }
                complete = true;
                return; // Proven unwritten owned fixture only; no source save or delivery replay.
            }
            var source = await HolonReplicationRouteBinding.CapturePinnedAsync(sqlite);
            if (seeding)
            {
                var target = await HolonReplicationRouteBinding.CapturePinnedAsync(mongo);
                intent.SourceFingerprint = source.BindingFingerprint;
                intent.TargetFingerprint = target.BindingFingerprint;
                intent.TargetId = new HolonReplicationRouteBinding(source, target, HolonReplicationRoutes.DefaultPolicyBinding).TargetId;
                // Persist discovered identities before the first business mutation.
                await Publish("planned");
            }
            else Assert.AreEqual(intent.SourceFingerprint, source.BindingFingerprint);
            var outbox = sqlite.GetBoundHolonReplicationOutbox(source).Result;
            async Task Pass(bool outage = false)
            {
                var logger = new StoppedDestinationLogger(outage);
                using var worker = new Worker(logger, () => registry, () => true);
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(25));
                await worker.StartAsync(deadline.Token);
                try { await logger.Completed.Task.WaitAsync(deadline.Token); }
                finally { using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(10)); await worker.StopAsync(shutdown.Token); }
            }
            async Task WaitForRunner(string signal)
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
                var marker = Path.Combine(root.FullName, "TestResults/OwnedRoutingCurrent/worker-offline-" + signal);
                while (!File.Exists(marker)) await Task.Delay(100, deadline.Token);
            }
            async Task VerifyPrimaryWitness(bool unavailable)
            {
                registry.IsAutoLoadBalanceEnabled = true;
                registry.AddProvidersToAutoLoadBalanceList(new[] { ProviderType.SQLLiteDBOASIS });
                foreach (var mode in new[] { "sync", "async", "load-balance" })
                {
                    var router = new OASISHyperDrive(registry);
                    var request = new StorageOperationRequest { Operation = "LoadHolon", HolonId = intent.WitnessId, AvatarId = intent.Actor };
                    var read = mode == "sync" ? router.RouteRequest<IHolon>(request)
                        : mode == "async" ? await router.RouteRequestAsync<IHolon>(request)
                        : await router.LoadBalanceRequestAsync<IHolon>(request);
                    Assert.AreEqual(unavailable, read.IsError, read.Message + read.Exception);
                    if (unavailable)
                    {
                        Assert.IsNull(read.Result);
                        Assert.IsNotNull(read.Exception, "Preserve the real primary connection failure.");
                    }
                    else
                    {
                        Assert.AreEqual(intent.WitnessId, read.Result.Id);
                        Assert.AreEqual("primary-only record", read.Result.Name);
                        Assert.AreEqual(intent.Actor, read.Result.CreatedByAvatarId);
                    }
                    Assert.IsNull((await sqlite.LoadHolonAsync(intent.WitnessId, loadChildren: false)).Result,
                        "Read confirmation cannot create a replica or substitute its empty answer.");
                }
                registry.IsAutoLoadBalanceEnabled = false;

                // The request's Mongo authority deliberately differs from the node's SQLite
                // default. A successful empty SQLite read cannot prove the Mongo record absent.
                // Use a separate node registry so switching defaults does not deactivate the
                // main fixture's Mongo receiver, which must remain alive across the outage.
                var preferredRegistry = new ProviderManager(null, dna) {
                    IsAutoFailOverEnabled = true, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
                preferredRegistry.RegisterProvider(sqlite);
                preferredRegistry.RegisterProvider(mongo);
                Assert.IsFalse(preferredRegistry.SetAndActivateCurrentStorageProvider(sqlite).IsError);
                Assert.IsFalse(preferredRegistry.SetAndReplaceAutoFailOverListForProviders(new[] {
                    new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),
                    new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError);
                foreach (var mode in new[] { "sync", "async", "explicit" })
                {
                    var router = new OASISHyperDrive(preferredRegistry);
                    var request = new StorageOperationRequest {
                        Operation = "LoadHolon", HolonId = intent.WitnessId,
                        AvatarId = intent.Actor, PreferredProvider = ProviderType.MongoDBOASIS
                    };
                    var read = mode == "sync" ? router.RouteRequest<IHolon>(request)
                        : mode == "async" ? await router.RouteRequestAsync<IHolon>(request)
                        : await router.FailoverRequestAsync<IHolon>(request);
                    Assert.AreEqual(unavailable, read.IsError, read.Message + read.Exception);
                    if (unavailable)
                    {
                        Assert.IsNull(read.Result);
                        Assert.IsNotNull(read.Exception, "The preferred native authority's outage must survive an alternate miss.");
                        var diagnostic = router.LastFailoverDiagnostic;
                        Assert.IsNotNull(diagnostic);
                        Assert.IsFalse(diagnostic.Succeeded);
                        Assert.AreEqual(ProviderType.MongoDBOASIS, diagnostic.SelectedProvider);
                        Assert.AreEqual(2, diagnostic.Attempts.Count, "Do not retry the known failed authority for confirmation.");
                        Assert.AreEqual(ProviderType.MongoDBOASIS, diagnostic.Attempts[0].Provider);
                        Assert.AreEqual(ProviderType.SQLLiteDBOASIS, diagnostic.Attempts[1].Provider);
                        Assert.IsFalse(diagnostic.Attempts[0].Succeeded);
                        Assert.IsTrue(diagnostic.Attempts[1].Succeeded);
                        Assert.IsTrue(read.MetaData.ContainsKey("hyperDriveFailoverDiagnostic"));
                    }
                    else
                    {
                        Assert.AreEqual(intent.WitnessId, read.Result.Id);
                        Assert.AreEqual("primary-only record", read.Result.Name);
                        Assert.AreEqual(intent.Actor, read.Result.CreatedByAvatarId);
                    }
                    Assert.IsNull((await sqlite.LoadHolonAsync(intent.WitnessId, loadChildren: false)).Result);
                }
                Assert.IsTrue(mongo.IsProviderActivated, "Read routing must preserve the main fixture's active receiver.");
            }

            async Task SaveAt(int index)
            {
                var input = new Holon { Id = intent.Ids[index], Name = index == 0 ? "before process outage" : "bought while Mongo stopped" };
                await Task.Run(async () => {
                    OASISRequestContext.RequestRoutingOverrides = new ProviderRoutingOverrides(autoReplicationEnabled: true,
                        replicationProviders: new[] { ProviderType.MongoDBOASIS });
                    var saved = await new HolonManager(null, dna, registry).SaveOwnedHolonAsync(input, intent.Actor,
                        new CosmicWriteOptions(intent.Operations[index]), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
                    Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                    Assert.IsTrue(saved.IsSaved);
                });
                var first = await outbox.PeekNextAsync(intent.TargetId);
                Assert.IsFalse(first.IsError, first.Message);
                if (index == 0) intent.FirstSequence = first.Result.Sequence;
                var obligation = await outbox.InspectAsync(intent.FirstSequence + index);
                Assert.IsFalse(obligation.IsError, obligation.Message);
                Assert.AreEqual(intent.Operations[index], obligation.Result.OperationId);
                Assert.AreEqual(HolonDeliveryState.Pending, obligation.Result.State);
                Assert.AreEqual(0, obligation.Result.AttemptCount);
            }

            if (reconcileActive || reconcileRouting)
            {
                // Resume only the verified original pending obligations. Never repeat
                // either source save or mutate its delivery state/receipt.
                for (var index = 0; index < intent.Operations.Length; index++)
                {
                    var original = await outbox.InspectAsync(intent.FirstSequence + index);
                    Assert.IsFalse(original.IsError, original.Message);
                    Assert.AreEqual(intent.Operations[index], original.Result.OperationId);
                    Assert.AreEqual(HolonDeliveryState.Pending, original.Result.State);
                    Assert.AreEqual(reconcileActive && index == 0 ? 1 : 0, original.Result.AttemptCount);
                }
                var existingReceipts = await client.GetDatabase(database).GetCollection<BsonDocument>("CosmicWriteReceipt")
                    .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
                var expectedExisting = consistencyProof ? new[] { intent.WitnessOperation.ToString("D") } : Array.Empty<string>();
                CollectionAssert.AreEquivalent(expectedExisting, existingReceipts.Select(row => row["_id"].AsString).ToArray(),
                    "Only the journaled primary witness may already be committed; no replication replay is permitted.");
            }
            if (activeOutage)
            {
                await SaveAt(0);
                if (consistencyProof)
                {
                    var witness = new Holon { Id = intent.WitnessId, Name = "primary-only record" };
                    var witnessSave = await new HolonManager(null, dna, registry).SaveOwnedHolonAsync(witness, intent.Actor,
                        new CosmicWriteOptions(intent.WitnessOperation), saveChildren: false, providerType: ProviderType.MongoDBOASIS);
                    Assert.IsFalse(witnessSave.IsError, witnessSave.Message + witnessSave.Exception);
                    Assert.IsTrue(witnessSave.IsSaved);
                    Assert.IsNull((await sqlite.LoadHolonAsync(intent.WitnessId, loadChildren: false)).Result);
                }
                if (routingProof || consistencyProof)
                    Assert.IsFalse(registry.SetAndActivateCurrentStorageProvider(mongo).IsError);
                await Publish("ready-to-stop");
                await WaitForRunner("stopped");
                using (var socket = new TcpClient())
                    await Assert.ThrowsExceptionAsync<SocketException>(async () => await socket.ConnectAsync("127.0.0.1", 27029));
                Assert.IsTrue(mongo.IsProviderActivated, "Keep the actual native receiver client alive across the process outage.");
                await SaveAt(1);
                if (consistencyProof) await VerifyPrimaryWitness(unavailable: true);
                if (routingProof)
                {
                    registry.IsAutoFailOverEnabled = true;
                    Assert.IsFalse(registry.SetAndReplaceAutoFailOverListForProviders(new[] {
                        new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError);
                    foreach (var mode in new[] { "sync", "async", "explicit" })
                    {
                        var router = new OASISHyperDrive(registry);
                        var request = new StorageOperationRequest { Operation = "LoadHolon", HolonId = intent.Ids[0],
                            AvatarId = intent.Actor, PreferredProvider = ProviderType.MongoDBOASIS };
                        var result = mode == "sync" ? router.RouteRequest<IHolon>(request)
                            : mode == "async" ? await router.RouteRequestAsync<IHolon>(request)
                            : await router.FailoverRequestAsync<IHolon>(request);
                        Assert.IsFalse(result.IsError, result.Message + result.Exception);
                        Assert.AreEqual(intent.Ids[0], result.Result.Id);
                        Assert.AreEqual(intent.Actor, result.Result.CreatedByAvatarId);
                        Assert.AreEqual("before process outage", result.Result.Name);
                        var diagnostic = router.LastFailoverDiagnostic;
                        Assert.IsNotNull(diagnostic);
                        Assert.IsTrue(diagnostic.Succeeded);
                        Assert.IsFalse(diagnostic.Exhausted);
                        Assert.AreEqual(mode == "explicit" ? ProviderType.Default : ProviderType.MongoDBOASIS, diagnostic.OriginalProvider);
                        Assert.AreEqual(mode == "explicit", diagnostic.IsExplicitRequest);
                        Assert.AreEqual(ProviderType.SQLLiteDBOASIS, diagnostic.SelectedProvider);
                        Assert.AreEqual(2, diagnostic.Attempts.Count);
                        Assert.IsFalse(diagnostic.Attempts[0].Succeeded);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Attempts[0].Message));
                        Assert.AreEqual(ProviderType.MongoDBOASIS, diagnostic.Attempts[0].Provider);
                        Assert.AreEqual(ProviderType.SQLLiteDBOASIS, diagnostic.Attempts[1].Provider);
                        Assert.IsTrue(diagnostic.Attempts[1].Succeeded);
                        Assert.IsTrue(result.MetaData.ContainsKey("hyperDriveFailoverDiagnostic"));
                    }
                    registry.IsAutoFailOverEnabled = false;
                }
                if (exhaust)
                {
                    var approval = await ((IHolonReplicationDestinationCatalogue)outbox).ReadDestinationBindingAsync(ProviderType.MongoDBOASIS);
                    Assert.IsFalse(approval.IsError, approval.Message);
                    var binding = approval.Result.Route;
                    var receiver = new CommittedHolonReplicaTarget(intent.TargetId, mongo, ProviderType.MongoDBOASIS,
                        binding.SourceId, ProviderType.SQLLiteDBOASIS, new Policy(), "native-exhaustion-fixture-v1",
                        new Guid("e1d13ed7-2146-4f40-a799-c2bbbfd9113f"), binding.PolicyBinding, binding.TargetStorage);
                    var dispatcher = new HolonOutboxDispatcher(outbox, new[] { receiver }, "native-budget-one", maximumAttempts: 1);
                    var attempted = await dispatcher.DispatchOnceAsync(intent.TargetId);
                    Assert.IsFalse(attempted.IsError, attempted.Message + attempted.Exception);
                    Assert.AreEqual(1, attempted.Result.ClaimedCount);
                    Assert.AreEqual(1, attempted.Result.ReconciliationCount);
                    Assert.AreEqual(0, attempted.Result.CompletedCount);
                }
                else await Pass(outage: true);
                var deferred = await outbox.InspectAsync(intent.FirstSequence);
                Assert.IsFalse(deferred.IsError, deferred.Message);
                Assert.AreEqual(exhaust ? HolonDeliveryState.ReconciliationRequired : HolonDeliveryState.Pending, deferred.Result.State,
                    "A known pre-commit failure retries only within its explicit attempt budget.");
                Assert.AreEqual(1, deferred.Result.AttemptCount);
                Assert.IsFalse(string.IsNullOrWhiteSpace(deferred.Result.ErrorCode));
                if (exhaust)
                {
                    Assert.AreEqual("COSMIC_REPLICA_WRITE_FAILED", deferred.Result.ErrorCode);
                    StringAssert.StartsWith(deferred.Result.Message, "Attempt limit reached.");
                    Assert.IsNotNull(deferred.Result.RecoveryEvidence);
                    Assert.IsFalse(deferred.Result.RecoveryEvidence.RequiresNativeReconciliation,
                        "Budget exhaustion is distinct from an ambiguous native commit.");
                }
                var ordered = await outbox.InspectAsync(intent.FirstSequence + 1);
                Assert.AreEqual(HolonDeliveryState.Pending, ordered.Result.State);
                Assert.AreEqual(0, ordered.Result.AttemptCount, "Later operations cannot overtake the deferred target head.");
                await Publish("ready-to-restart");
                await WaitForRunner("restarted");
                using var ready = new CancellationTokenSource(TimeSpan.FromSeconds(40));
                while (!(await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1), cancellationToken: ready.Token))
                    .GetValue("isWritablePrimary", false).AsBoolean) await Task.Delay(250, ready.Token);
                var freshTarget = await mongo.CaptureStorageIdentityAsync();
                Assert.IsFalse(freshTarget.IsError, freshTarget.Message);
                Assert.AreEqual(intent.TargetFingerprint, freshTarget.Result.BindingFingerprint);
            }
            else if (phase is "seed" or "offline")
            {
                await SaveAt(phase == "seed" ? 0 : 1);
                if (phase == "offline") Assert.IsFalse(mongo.IsProviderActivated);
                await Publish(phase == "seed" ? "seeded" : "offline-saved");
                return;
            }

            Assert.AreEqual(intent.TargetFingerprint, (await HolonReplicationRouteBinding.CapturePinnedAsync(mongo)).BindingFingerprint);
            foreach (var id in intent.Ids) Assert.IsNull((await mongo.LoadHolonAsync(id, loadChildren: false)).Result);
            if (consistencyProof)
            {
                Assert.IsFalse(registry.SetAndActivateCurrentStorageProvider(mongo).IsError);
                await VerifyPrimaryWitness(unavailable: false);
            }
            if (exhaust)
            {
                var blockedBefore = await outbox.InspectAsync(intent.FirstSequence);
                await Pass();
                await Pass();
                var blockedAfter = await outbox.InspectAsync(intent.FirstSequence);
                Assert.IsFalse(blockedAfter.IsError, blockedAfter.Message);
                Assert.AreEqual(HolonDeliveryState.ReconciliationRequired, blockedAfter.Result.State);
                Assert.AreEqual(1, blockedAfter.Result.AttemptCount);
                Assert.AreEqual(blockedBefore.Result.ErrorCode, blockedAfter.Result.ErrorCode);
                Assert.AreEqual(blockedBefore.Result.Message, blockedAfter.Result.Message);
                var later = await outbox.InspectAsync(intent.FirstSequence + 1);
                Assert.AreEqual(HolonDeliveryState.Pending, later.Result.State);
                Assert.AreEqual(0, later.Result.AttemptCount);
                foreach (var id in intent.Ids)
                {
                    var original = await sqlite.LoadHolonAsync(id, loadChildren: false);
                    Assert.IsFalse(original.IsError, original.Message);
                    Assert.AreEqual(1, original.Result.Version);
                    Assert.AreEqual(intent.Actor, original.Result.CreatedByAvatarId);
                    Assert.IsNull((await mongo.LoadHolonAsync(id, loadChildren: false)).Result);
                }
                Assert.AreEqual(0L, await client.GetDatabase(database).GetCollection<BsonDocument>("CosmicWriteReceipt")
                    .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                using (var native = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={file};Mode=ReadOnly;Pooling=False"))
                {
                    await native.OpenAsync();
                    using var query = native.CreateCommand();
                    query.CommandText = "SELECT OperationId FROM CosmicWriteReceipt";
                    using var sourceReceiptsReader = await query.ExecuteReaderAsync();
                    var retained = new List<string>();
                    while (await sourceReceiptsReader.ReadAsync()) retained.Add(sourceReceiptsReader.GetString(0));
                    CollectionAssert.AreEquivalent(intent.Operations.Select(op => op.ToString("D")).ToArray(), retained.ToArray());
                }
                complete = true;
                return; // Exact test-owned cleanup, never an automatic operator reset.
            }
            await Pass();
            var revisions = new List<Guid?>();
            for (var index = 0; index < intent.Ids.Length; index++)
            {
                var delivered = await outbox.InspectAsync(intent.FirstSequence + index);
                Assert.IsFalse(delivered.IsError, delivered.Message);
                Assert.AreEqual(HolonDeliveryState.Completed, delivered.Result.State);
                Assert.AreEqual((activeOutage || reconcileActive) && index == 0 ? 2 : 1, delivered.Result.AttemptCount);
                var primary = await sqlite.LoadHolonAsync(intent.Ids[index], loadChildren: false);
                var copy = await mongo.LoadHolonAsync(intent.Ids[index], loadChildren: false);
                Assert.IsFalse(primary.IsError, primary.Message);
                Assert.IsFalse(copy.IsError, copy.Message);
                Assert.AreEqual(primary.Result.VersionId, copy.Result.VersionId);
                Assert.AreEqual(intent.Actor, copy.Result.CreatedByAvatarId);
                revisions.Add(CosmicConcurrency.GetProviderRevisionId(copy.Result, ProviderType.MongoDBOASIS));
                Assert.IsNotNull(revisions[index]);
            }
            await Pass();
            for (var index = 0; index < intent.Ids.Length; index++)
                Assert.AreEqual(revisions[index], CosmicConcurrency.GetProviderRevisionId(
                    (await mongo.LoadHolonAsync(intent.Ids[index], loadChildren: false)).Result, ProviderType.MongoDBOASIS));
            if (routingProof)
            {
                registry.IsAutoLoadBalanceEnabled = true;
                registry.AddProvidersToAutoLoadBalanceList(new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS });
                var selections = new List<ProviderType>();
                for (var index = 0; index < 4; index++)
                {
                    var router = new OASISHyperDrive(registry);
                    var request = new StorageOperationRequest { Operation = "LoadHolon", HolonId = intent.Ids[0], AvatarId = intent.Actor };
                    var result = index % 2 == 0 ? router.RouteRequest<IHolon>(request, LoadBalancingStrategy.RoundRobin)
                        : await router.RouteRequestAsync<IHolon>(request, LoadBalancingStrategy.RoundRobin);
                    Assert.IsFalse(result.IsError, result.Message + result.Exception);
                    Assert.AreEqual(intent.Ids[0], result.Result.Id);
                    Assert.AreEqual(intent.Actor, result.Result.CreatedByAvatarId);
                    Assert.AreEqual("before process outage", result.Result.Name);
                    selections.Add(registry.LastProviderSelectionDiagnostic.SelectedProvider);
                    Assert.IsTrue(result.Result.ProviderUniqueStorageKey.ContainsKey(registry.LastProviderSelectionDiagnostic.SelectedProvider));
                }
                CollectionAssert.AreEqual(new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS,
                    ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS }, selections.ToArray());
                CollectionAssert.AreEquivalent(new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS },
                    registry.GetProviderAutoLoadBalanceList().Select(value => value.Value).ToArray());

                // Exercise every public selection strategy through real reads. Subscription
                // filtering precedes scoring: Free may select SQLite only, even with Mongo
                // configured and physically available. Gold may use either native store.
                foreach (var plan in new[] { "Gold", "Free" })
                foreach (var strategy in new[] { LoadBalancingStrategy.RoundRobin,
                    LoadBalancingStrategy.WeightedRoundRobin, LoadBalancingStrategy.LeastConnections,
                    LoadBalancingStrategy.Geographic, LoadBalancingStrategy.CostBased,
                    LoadBalancingStrategy.Performance, LoadBalancingStrategy.Auto })
                foreach (var mode in new[] { "sync", "async", "explicit" })
                {
                    dna.OASIS.SubscriptionConfig.PlanType = plan;
                    var router = new OASISHyperDrive(registry);
                    var request = new StorageOperationRequest {
                        Operation = "LoadHolon", HolonId = intent.Ids[0], AvatarId = intent.Actor
                    };
                    var result = mode == "sync" ? router.RouteRequest<IHolon>(request, strategy)
                        : mode == "async" ? await router.RouteRequestAsync<IHolon>(request, strategy)
                        : await router.LoadBalanceRequestAsync<IHolon>(request, strategy);
                    var context = $"{plan}/{strategy}/{mode}: {result.Message}";
                    Assert.IsFalse(result.IsError, context);
                    Assert.AreEqual(intent.Ids[0], result.Result.Id, context);
                    Assert.AreEqual(intent.Actor, result.Result.CreatedByAvatarId, context);
                    Assert.AreEqual("before process outage", result.Result.Name, context);
                    var selection = registry.LastProviderSelectionDiagnostic;
                    Assert.IsNotNull(selection, context);
                    var permitted = plan == "Free" ? new[] { ProviderType.SQLLiteDBOASIS }
                        : new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS };
                    CollectionAssert.AreEquivalent(permitted, selection.Candidates.Select(value => value.Provider).ToArray(), context);
                    Assert.IsTrue(permitted.Contains(selection.SelectedProvider), context);
                    Assert.IsTrue(result.Result.ProviderUniqueStorageKey.ContainsKey(selection.SelectedProvider), context);
                    CollectionAssert.AreEquivalent(new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS },
                        registry.GetProviderAutoLoadBalanceList().Select(value => value.Value).ToArray(), context);
                }
                dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            }
            var db = client.GetDatabase(database);
            var rows = await db.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            var expectedIds = consistencyProof ? intent.Ids.Append(intent.WitnessId) : intent.Ids.AsEnumerable();
            CollectionAssert.AreEquivalent(expectedIds.Select(id => id.ToString("D")).ToArray(), rows.Select(row => row["HolonId"].AsString).ToArray());
            var receipts = await db.GetCollection<BsonDocument>("CosmicWriteReceipt").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            var expectedOperations = consistencyProof ? intent.Operations.Append(intent.WitnessOperation) : intent.Operations.AsEnumerable();
            CollectionAssert.AreEquivalent(expectedOperations.Select(op => op.ToString("D")).ToArray(), receipts.Select(row => row["_id"].AsString).ToArray());
            complete = true;
        }
        finally
        {
            if (mongo.IsProviderActivated) Assert.IsFalse((await mongo.DeActivateProviderAsync()).IsError);
            sqlite.Dispose();
            if (complete)
            {
                await client.DropDatabaseAsync(database);
                using var names = await client.ListDatabaseNamesAsync();
                Assert.IsFalse((await names.ToListAsync()).Contains(database));
                foreach (var path in ownedFiles) { if (File.Exists(path)) File.Delete(path); Assert.IsFalse(File.Exists(path)); }
                File.Delete(journal);
            }
        }
    }
}
