extern alias ShippingWeb4;

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
using System.Text.Json;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NextGenSoftware.OASIS.Common;
using Worker = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive.HolonReplicationDeliveryHostedService;
using Mongo = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using Sqlite = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class NativeHostedReplicaRestartTests
{
    public class DestinationProbeUnavailable : DispatchProxy, IBoundCommittedHolonReplicaStorageProvider, ICosmicStorageIdentityProvider
    {
        public IOASISStorageProvider Native;
        public int ProbeCalls;
        protected override object Invoke(MethodInfo method,object[] args)
        {
            try { return method.Invoke(Native,args); }
            catch(TargetInvocationException error) when(error.InnerException != null)
            { ExceptionDispatchInfo.Capture(error.InnerException).Throw();throw; }
        }
        public Task<OASISResult<CosmicStorageIdentity>> CaptureStorageIdentityAsync(CancellationToken token=default)
        { ProbeCalls++;throw new InvalidOperationException("Controlled destination identity probe is unavailable."); }
        public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
            =>throw new InvalidOperationException("Descriptor recovery must use the bound receiver.");
        public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,CancellationToken token=default)
            =>throw new InvalidOperationException("Descriptor recovery must use the bound receiver.");
        public OASISResult<IEnumerable<IHolon>> ApplyBoundCommittedHolonReplica(CommittedHolonReplicaRequest request,CosmicStorageIdentity expected)
            =>((IBoundCommittedHolonReplicaStorageProvider)Native).ApplyBoundCommittedHolonReplica(request,expected);
        public Task<OASISResult<IEnumerable<IHolon>>> ApplyBoundCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,CosmicStorageIdentity expected,CancellationToken token=default)
            =>((IBoundCommittedHolonReplicaStorageProvider)Native).ApplyBoundCommittedHolonReplicaAsync(request,expected,token);
    }

    [DataTestMethod]
    [DataRow(false,false,false)]
    [DataRow(true,false,false)]
    [DataRow(false,true,false)]
    [DataRow(true,true,false)]
    [DataRow(false,true,true)]
    [DataRow(true,true,true)]
    public async Task ShippingWorkerUsesBoundMongoAndSqliteSourcesAcrossProviderRestart(bool mongoSource,bool unavailableProbe,bool newSaveAfterRestart)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string databaseName = "cosmic_bound_source_worker_fixture";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        var state = Path.Combine(root.FullName, "LocalState/COSMIC/OwnedRoutingCurrent");
        Directory.CreateDirectory(state);
        using var lease = new FileStream(Path.Combine(state, "worker-pair.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var journal = Path.Combine(state, "worker-pair.pending.json");
        var file = Path.Combine(root.FullName, "TestResults/OwnedRoutingCurrent/worker-pair.db");
        var ownedFiles = new[] { file, file + "-wal", file + "-shm", file + "-journal" };
        Assert.IsFalse(File.Exists(journal), "Reconcile the original native outcome before reuse.");
        foreach (var path in ownedFiles) Assert.IsFalse(File.Exists(path), "Never overwrite an existing fixture.");
        var client = new MongoClient(uri);
        using (var databases = await client.ListDatabaseNamesAsync())
            Assert.IsFalse((await databases.ToListAsync()).Contains(databaseName));
        var actor = Guid.NewGuid();
        var operation = Guid.NewGuid();
        var input = new Holon { Id=Guid.NewGuid(), Name="coffee bought before worker startup" };
        var coldOperation = Guid.NewGuid();
        var coldInput = new Holon { Id=Guid.NewGuid(), Name="coffee bought after source restart during metadata outage" };
        using (var intent = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(intent, new { actor, operation, input.Id, coldOperation, coldId=coldInput.Id, mongoSource, unavailableProbe, newSaveAfterRestart, databaseName, file });
            intent.Flush(true);
        }

        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
        { HyperDriveMode=HyperDriveModes.V2, StorageProviders=new StorageProviderSettings { LogSwitchingProviders=false } } };
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        Sqlite sqlite = null;
        Mongo mongo = null;
        IOASISStorageProvider source = null, target = null;
        ProviderManager registry = null;
        var coldRecovery = false;
        DestinationProbeUnavailable probe = null;
        IOASISStorageProvider registeredTarget = null;
        CosmicStorageIdentity originalSource = null, originalTarget = null;
        async Task OpenProviders()
        {
            sqlite = new Sqlite($"Data Source={file};Pooling=False");
            mongo = new Mongo(uri, databaseName);
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await mongo.ActivateProviderAsync()).IsError);
            source = mongoSource ? mongo : sqlite;
            target = mongoSource ? sqlite : mongo;
            registry = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled=false, IsAutoReplicationEnabled=false, IsAutoLoadBalanceEnabled=false };
            registry.RegisterProvider(source);
            registeredTarget = target;
            probe = null;
            if(coldRecovery)
            {
                // Receiver-only registration: its independent source role is
                // outside this test. The actual native receiver remains active.
                registeredTarget=DispatchProxy.Create<IOASISStorageProvider,DestinationProbeUnavailable>();
                probe=(DestinationProbeUnavailable)registeredTarget;
                probe.Native=target;
                Assert.IsFalse(HolonReplicationRouteBinding.IsPinned(registeredTarget));
            }
            registry.RegisterProvider(registeredTarget);
        }
        async Task CloseProviders()
        {
            if (mongo != null) Assert.IsFalse((await mongo.DeActivateProviderAsync()).IsError);
            sqlite?.Dispose();
            mongo = null; sqlite = null;
        }
        async Task RunPass()
        {
            var logger = new PassLogger();
            using var worker = new Worker(logger, () => registry, () => true);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await worker.StartAsync(deadline.Token);
            try { await logger.Completed.Task.WaitAsync(deadline.Token); }
            finally
            {
                using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await worker.StopAsync(shutdown.Token);
            }
        }
        var complete = false;
        try
        {
            await OpenProviders();
            originalSource = await HolonReplicationRouteBinding.CapturePinnedAsync(source);
            originalTarget = await HolonReplicationRouteBinding.CapturePinnedAsync(target);
            var binding = new HolonReplicationRouteBinding(originalSource, originalTarget, HolonReplicationRoutes.DefaultPolicyBinding);
            // Request routing is gone before any worker is constructed. Recovery
            // must discover the durable obligation through its native bound source.
            await Task.Run(async () =>
            {
                OASISRequestContext.RequestRoutingOverrides = new ProviderRoutingOverrides(autoReplicationEnabled:true,
                    replicationProviders:new[] { target.ProviderType.Value });
                var saved = await new HolonManager(null,dna,registry).SaveOwnedHolonAsync(input,actor,
                    new CosmicWriteOptions(operation),saveChildren:false,providerType:source.ProviderType.Value);
                Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
                Assert.IsTrue(saved.IsSaved);
            });
            var capture = ((IBoundHolonReplicationOutboxSource)source).GetBoundHolonReplicationOutbox(originalSource);
            Assert.IsFalse(capture.IsError,capture.Message);
            var pending = await capture.Result.PeekNextAsync(binding.TargetId);
            Assert.IsFalse(pending.IsError,pending.Message);
            Assert.AreEqual(operation,pending.Result.OperationId);
            Assert.AreEqual(HolonDeliveryState.Pending,pending.Result.State);
            Assert.AreEqual(0,pending.Result.AttemptCount);
            Assert.IsNotNull(pending.Result.NativeRoute);
            Assert.AreEqual(originalTarget.BindingFingerprint,pending.Result.NativeRoute.TargetStorage.BindingFingerprint);
            var inventory=await capture.Result.ReadRouteHeadsAsync();
            Assert.IsFalse(inventory.IsError,inventory.Message);
            Assert.AreEqual(binding.TargetId,inventory.Result.Heads.Single().NativeRoute.TargetId);
            Assert.IsNull((await target.LoadHolonAsync(input.Id,loadChildren:false)).Result);

            // Restart providers before delivery as well as afterwards. Both
            // source directions must recapture the same native store identities.
            coldRecovery=unavailableProbe;
            await CloseProviders();
            await OpenProviders();
            Assert.AreEqual(originalSource.BindingFingerprint,(await HolonReplicationRouteBinding.CapturePinnedAsync(source)).BindingFingerprint);
            if(!unavailableProbe)
                Assert.AreEqual(originalTarget.BindingFingerprint,(await HolonReplicationRouteBinding.CapturePinnedAsync(target)).BindingFingerprint);
            HolonDeliveryInspection coldPending = null;
            if (newSaveAfterRestart)
            {
                Assert.IsNotNull(probe);
                Assert.IsFalse(HolonReplicationRouteBinding.IsPinned(registeredTarget));
                var sourceOutbox = ((IBoundHolonReplicationOutboxSource)source).GetBoundHolonReplicationOutbox(originalSource).Result;
                var approved = await ((IHolonReplicationDestinationCatalogue)sourceOutbox).ReadDestinationBindingAsync(target.ProviderType.Value);
                Assert.IsFalse(approved.IsError, approved.Message);
                Assert.AreEqual(operation, approved.Result.EstablishedOperationId);
                Assert.AreEqual(originalTarget.BindingFingerprint, approved.Result.Route.TargetStorage.BindingFingerprint);
                await Task.Run(async () =>
                {
                    OASISRequestContext.RequestRoutingOverrides = new ProviderRoutingOverrides(autoReplicationEnabled: true,
                        replicationProviders: new[] { target.ProviderType.Value });
                    var saved = await new HolonManager(null, dna, registry).SaveOwnedHolonAsync(coldInput, actor,
                        new CosmicWriteOptions(coldOperation), saveChildren: false, providerType: source.ProviderType.Value);
                    Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                    Assert.IsTrue(saved.IsSaved);
                });
                Assert.AreEqual(0, probe.ProbeCalls, "New enrollment must reuse source-owned physical approval.");
                // This isolated source owns only these two operations; the native
                // monotonic sequence must expose the new obligation after the first.
                var queued = await sourceOutbox.InspectAsync(pending.Result.Sequence + 1);
                Assert.IsFalse(queued.IsError, queued.Message);
                coldPending = queued.Result;
                Assert.AreEqual(coldOperation, coldPending.OperationId);
                Assert.AreEqual(HolonDeliveryState.Pending, coldPending.State);
                Assert.AreEqual(0, coldPending.AttemptCount);
                Assert.IsNull((await target.LoadHolonAsync(coldInput.Id, loadChildren: false)).Result);
            }
            await RunPass();
            if(probe != null) Assert.AreEqual(0,probe.ProbeCalls,"Stored destination meaning must not require a fresh identity probe.");
            var outbox = ((IBoundHolonReplicationOutboxSource)source).GetBoundHolonReplicationOutbox(originalSource).Result;
            var delivered = await outbox.InspectAsync(pending.Result.Sequence);
            Assert.IsFalse(delivered.IsError,delivered.Message);
            Assert.AreEqual(HolonDeliveryState.Completed,delivered.Result.State);
            Assert.AreEqual(1,delivered.Result.AttemptCount);
            var copy = await target.LoadHolonAsync(input.Id,loadChildren:false);
            Assert.IsFalse(copy.IsError,copy.Message);
            Assert.AreEqual(actor,copy.Result.CreatedByAvatarId);
            Assert.AreEqual(input.VersionId,copy.Result.VersionId);
            var targetRevision = CosmicConcurrency.GetProviderRevisionId(copy.Result,target.ProviderType.Value);
            Assert.IsNotNull(targetRevision);

            Guid? coldTargetRevision = null;
            if (newSaveAfterRestart)
            {
                var deliveredCold = await outbox.InspectAsync(coldPending.Sequence);
                Assert.IsFalse(deliveredCold.IsError, deliveredCold.Message);
                Assert.AreEqual(HolonDeliveryState.Completed, deliveredCold.Result.State);
                Assert.AreEqual(1, deliveredCold.Result.AttemptCount);
                var coldCopy = await target.LoadHolonAsync(coldInput.Id, loadChildren: false);
                Assert.IsFalse(coldCopy.IsError, coldCopy.Message);
                Assert.AreEqual(coldInput.VersionId, coldCopy.Result.VersionId);
                Assert.AreEqual(actor, coldCopy.Result.CreatedByAvatarId);
                coldTargetRevision = CosmicConcurrency.GetProviderRevisionId(coldCopy.Result, target.ProviderType.Value);
                Assert.IsNotNull(coldTargetRevision);
            }
            await CloseProviders();
            await OpenProviders();
            await RunPass();
            if(probe != null) Assert.AreEqual(0,probe.ProbeCalls);
            var retained = await ((IBoundHolonReplicationOutboxSource)source).GetBoundHolonReplicationOutbox(originalSource)
                .Result.InspectAsync(pending.Result.Sequence);
            Assert.IsFalse(retained.IsError,retained.Message);
            Assert.AreEqual(HolonDeliveryState.Completed,retained.Result.State);
            Assert.AreEqual(1,retained.Result.AttemptCount);
            var fresh = await target.LoadHolonAsync(input.Id,loadChildren:false);
            Assert.IsFalse(fresh.IsError,fresh.Message);
            Assert.AreEqual(targetRevision,CosmicConcurrency.GetProviderRevisionId(fresh.Result,target.ProviderType.Value));
            Assert.AreEqual(input.VersionId,fresh.Result.VersionId);
            var primary = await source.LoadHolonAsync(input.Id,loadChildren:false);
            Assert.IsFalse(primary.IsError,primary.Message);
            Assert.AreEqual(input.VersionId,primary.Result.VersionId);
            Assert.AreEqual(actor,primary.Result.CreatedByAvatarId);
            Assert.AreEqual(originalSource.BindingFingerprint,(await HolonReplicationRouteBinding.CapturePinnedAsync(source)).BindingFingerprint);
            Assert.AreEqual(originalTarget.BindingFingerprint,(await HolonReplicationRouteBinding.CapturePinnedAsync(target)).BindingFingerprint);
            if (newSaveAfterRestart)
            {
                var coldCopy = await target.LoadHolonAsync(coldInput.Id, loadChildren: false);
                Assert.IsFalse(coldCopy.IsError, coldCopy.Message);
                Assert.AreEqual(coldTargetRevision, CosmicConcurrency.GetProviderRevisionId(coldCopy.Result, target.ProviderType.Value));
                Assert.AreEqual(coldInput.VersionId, coldCopy.Result.VersionId);
                var coldPrimary = await source.LoadHolonAsync(coldInput.Id, loadChildren: false);
                Assert.IsFalse(coldPrimary.IsError, coldPrimary.Message);
                Assert.AreEqual(coldInput.VersionId, coldPrimary.Result.VersionId);
            }
            // Exact test ownership is verified before native database deletion.
            var db = client.GetDatabase(databaseName);
            var rows = await db.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            var expectedIds = newSaveAfterRestart ? new[] { input.Id.ToString("D"), coldInput.Id.ToString("D") } : new[] { input.Id.ToString("D") };
            CollectionAssert.AreEquivalent(expectedIds, rows.Select(row => row["HolonId"].AsString).ToArray());
            var receipts = await db.GetCollection<BsonDocument>("CosmicWriteReceipt").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            var expectedOperations = newSaveAfterRestart ? new[] { operation.ToString("D"), coldOperation.ToString("D") } : new[] { operation.ToString("D") };
            CollectionAssert.AreEquivalent(expectedOperations, receipts.Select(receipt => receipt["_id"].AsString).ToArray());
            complete = true;
        }
        finally
        {
            await CloseProviders();
            if (complete)
            {
                await client.DropDatabaseAsync(databaseName);
                using var databases = await client.ListDatabaseNamesAsync();
                Assert.IsFalse((await databases.ToListAsync()).Contains(databaseName));
                foreach (var path in ownedFiles) { if(File.Exists(path))File.Delete(path);Assert.IsFalse(File.Exists(path)); }
                File.Delete(journal);
            }
        }
    }
}
