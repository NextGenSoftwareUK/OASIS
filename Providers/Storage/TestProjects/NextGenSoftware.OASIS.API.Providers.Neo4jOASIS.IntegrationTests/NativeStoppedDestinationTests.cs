extern alias ShippingWeb4;

using System.Net.Sockets;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using Worker = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive.HolonReplicationDeliveryHostedService;
using Mongo = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using Sqlite = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class NativeHostedReplicaRestartTests
{
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
    }

    [TestMethod]
    public async Task ShippingWorkerPersistsNewSaveWhileMongoDestinationProcessIsStopped()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string database = "cosmic_stopped_destination_fixture";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var phase = Environment.GetEnvironmentVariable("OASIS_OFFLINE_REPLICATION_PHASE");
        Assert.IsTrue(phase is "seed" or "offline" or "deliver", "Use the explicit process-owning runner.");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        var state = Path.Combine(root.FullName, "LocalState/COSMIC/OwnedRoutingCurrent");
        Directory.CreateDirectory(state);
        using var lease = File.Open(Path.Combine(state, "worker-offline.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var journal = Path.Combine(state, "worker-offline.pending.json");
        var file = Path.Combine(root.FullName, "TestResults/OwnedRoutingCurrent/worker-offline.db");
        var ownedFiles = new[] { file, file + "-wal", file + "-shm", file + "-journal" };
        var client = new MongoClient(uri);
        StoppedDestinationIntent intent;
        if (phase == "seed")
        {
            Assert.IsFalse(File.Exists(journal));
            foreach (var path in ownedFiles) Assert.IsFalse(File.Exists(path));
            using var names = await client.ListDatabaseNamesAsync();
            Assert.IsFalse((await names.ToListAsync()).Contains(database));
            intent = new();
            using var planned = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(planned, intent);
            planned.Flush(true);
        }
        else
        {
            intent = JsonSerializer.Deserialize<StoppedDestinationIntent>(await File.ReadAllTextAsync(journal));
            Assert.IsNotNull(intent);
            Assert.AreEqual(phase == "offline" ? "seeded" : "offline-saved", intent.Phase);
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
            var source = await HolonReplicationRouteBinding.CapturePinnedAsync(sqlite);
            if (phase == "seed")
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
            if (phase is "seed" or "offline")
            {
                var index = phase == "seed" ? 0 : 1;
                var input = new Holon { Id = intent.Ids[index], Name = phase == "seed" ? "before process outage" : "bought while Mongo stopped" };
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
                if (phase == "seed") intent.FirstSequence = first.Result.Sequence;
                var obligation = await outbox.InspectAsync(intent.FirstSequence + index);
                Assert.IsFalse(obligation.IsError, obligation.Message);
                Assert.AreEqual(intent.Operations[index], obligation.Result.OperationId);
                Assert.AreEqual(HolonDeliveryState.Pending, obligation.Result.State);
                Assert.AreEqual(0, obligation.Result.AttemptCount);
                if (phase == "offline") Assert.IsFalse(mongo.IsProviderActivated);
                await Publish(phase == "seed" ? "seeded" : "offline-saved");
                return;
            }

            Assert.AreEqual(intent.TargetFingerprint, (await HolonReplicationRouteBinding.CapturePinnedAsync(mongo)).BindingFingerprint);
            foreach (var id in intent.Ids) Assert.IsNull((await mongo.LoadHolonAsync(id, loadChildren: false)).Result);
            async Task Pass()
            {
                var logger = new PassLogger();
                using var worker = new Worker(logger, () => registry, () => true);
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await worker.StartAsync(deadline.Token);
                try { await logger.Completed.Task.WaitAsync(deadline.Token); }
                finally { using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(10)); await worker.StopAsync(shutdown.Token); }
            }
            await Pass();
            var revisions = new List<Guid?>();
            for (var index = 0; index < intent.Ids.Length; index++)
            {
                var delivered = await outbox.InspectAsync(intent.FirstSequence + index);
                Assert.IsFalse(delivered.IsError, delivered.Message);
                Assert.AreEqual(HolonDeliveryState.Completed, delivered.Result.State);
                Assert.AreEqual(1, delivered.Result.AttemptCount);
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
            var db = client.GetDatabase(database);
            var rows = await db.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            CollectionAssert.AreEquivalent(intent.Ids.Select(id => id.ToString("D")).ToArray(), rows.Select(row => row["HolonId"].AsString).ToArray());
            var receipts = await db.GetCollection<BsonDocument>("CosmicWriteReceipt").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            CollectionAssert.AreEquivalent(intent.Operations.Select(op => op.ToString("D")).ToArray(), receipts.Select(row => row["_id"].AsString).ToArray());
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
