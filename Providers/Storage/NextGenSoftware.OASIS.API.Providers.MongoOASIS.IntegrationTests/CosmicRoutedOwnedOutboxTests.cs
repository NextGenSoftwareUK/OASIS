using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicConditionalWriteTests
{
    public class OwnedRoutingTarget : DispatchProxy, IBoundCommittedHolonReplicaStorageProvider, ICosmicStorageIdentityProvider
    {
        public ProviderType Identity;
        protected override object Invoke(MethodInfo method, object[] args) => method.Name switch
        {
            "get_IsProviderActivated" => true,
            "get_ProviderType" => new EnumValue<ProviderType>(Identity),
            "get_ProviderCategory" => new EnumValue<ProviderCategory>(ProviderCategory.Storage),
            "get_ProviderName" => "declaration-only-" + Identity,
            _ => throw new InvalidOperationException("Unexpected target call: " + method.Name)
        };
        // A declared native identity tests enrollment only; no target delivery is claimed here.
        public Task<OASISResult<CosmicStorageIdentity>> CaptureStorageIdentityAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new OASISResult<CosmicStorageIdentity>(RoutedTargetIdentity(Identity)));
        public OASISResult<IEnumerable<IHolon>> ApplyBoundCommittedHolonReplica(CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage)
            => throw new InvalidOperationException("Primary writes cannot deliver inline.");
        public Task<OASISResult<IEnumerable<IHolon>>> ApplyBoundCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Primary writes cannot deliver inline.");
        public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
            => throw new InvalidOperationException("Primary commits cannot deliver inline.");
        public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(
            CommittedHolonReplicaRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Primary commits cannot deliver inline.");
    }

    private static CosmicStorageIdentity RoutedTargetIdentity(ProviderType type)
        => new(type, new string('A', 64), "declaration-only-" + type);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task MongoRoutedOwnedSaveCommitsEffectiveTargetsOrRollsBackAllState(bool batch, bool fault)
    {
        await Task.Run(async () =>
        {
            const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
            const string name = "cosmic_routed_owned_outbox_fixture";
            Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
            Assert.NotNull(root);
            var state = Path.Combine(root.FullName, "LocalState/COSMIC/OwnedRoutingCurrent");
            Directory.CreateDirectory(state);
            using var lease = new FileStream(Path.Combine(state, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var journal = Path.Combine(state, "mongo-fixture.pending.json");
            Assert.False(File.Exists(journal), "Preserve the original pending outcome; never replay.");
            var client = new MongoClient(uri);
            using (var names = await client.ListDatabaseNamesAsync()) Assert.DoesNotContain(name, await names.ToListAsync());
            var actor = Guid.NewGuid();
            var operation = Guid.NewGuid();
            using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(file, new { database = name, actor, operation, batch, fault });
                file.Flush(true);
            }

            var provider = new Provider(uri, name);
            var complete = false;
            try
            {
                var activation = await provider.ActivateProviderAsync();
                Assert.False(activation.IsError, activation.Message);
                var nativeSource = (await provider.CaptureStorageIdentityAsync()).Result;
                string BoundTarget(ProviderType type) => new HolonReplicationRouteBinding(nativeSource,
                    RoutedTargetIdentity(type), HolonReplicationRoutes.DefaultPolicyBinding).TargetId;
                var boundSourceId = new HolonReplicationRouteBinding(nativeSource,
                    RoutedTargetIdentity(ProviderType.Neo4jOASIS), HolonReplicationRoutes.DefaultPolicyBinding).SourceId;
                var database = client.GetDatabase(name);
                var obligations = database.GetCollection<BsonDocument>("CosmicHolonOutbox");
                var receipts = database.GetCollection<BsonDocument>("CosmicWriteReceipt");
                var holons = database.GetCollection<BsonDocument>("Holon");
                if (fault)
                    await database.RunCommandAsync<BsonDocument>(new BsonDocument
                    { { "collMod", "CosmicHolonOutbox" }, { "validator", new BsonDocument("TargetId", BoundTarget(ProviderType.Neo4jOASIS)) }, { "validationLevel", "strict" } });

                var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false } } };
                dna.OASIS.SubscriptionConfig.PlanType = "Gold";
                var routing = new ProviderManager(null, dna)
                { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
                routing.RegisterProvider(provider);
                foreach (var identity in new[] { ProviderType.Neo4jOASIS, ProviderType.SQLLiteDBOASIS })
                {
                    var target = DispatchProxy.Create<IOASISStorageProvider, OwnedRoutingTarget>();
                    ((OwnedRoutingTarget)target).Identity = identity;
                    routing.RegisterProvider(target);
                }
                OASISRequestContext.AvatarRoutingOverrides = new ProviderRoutingOverrides(
                    autoReplicationEnabled: true, replicationProviders: new[] { ProviderType.Neo4jOASIS });
                OASISRequestContext.RequestRoutingOverrides = new ProviderRoutingOverrides(
                    replicationProviders: new[] { ProviderType.Neo4jOASIS, ProviderType.SQLLiteDBOASIS });
                var manager = new HolonManager(null, dna, routing);
                var inputs = Enumerable.Range(0, batch ? 2 : 1).Select(index => new Holon { Name = "routed-native-" + index }).ToArray();
                if (batch)
                {
                    var saved = await manager.SaveOwnedHolonsAsync(inputs, actor, new CosmicWriteOptions(operation),
                        saveChildren: false, providerType: ProviderType.MongoDBOASIS);
                    Assert.Equal(fault, saved.IsError);
                    Assert.Equal(!fault, saved.IsSaved);
                }
                else
                {
                    var saved = await manager.SaveOwnedHolonAsync(inputs[0], actor, new CosmicWriteOptions(operation),
                        saveChildren: false, providerType: ProviderType.MongoDBOASIS);
                    Assert.Equal(fault, saved.IsError);
                    Assert.Equal(!fault, saved.IsSaved);
                }
                Assert.Equal(fault ? 0 : inputs.Length, await holons.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                Assert.Equal(fault ? 0 : 1, await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                var frozen = await obligations.Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("TargetId", 1)).ToListAsync();
                Assert.Equal(fault ? 0 : 2, frozen.Count);
                if (fault)
                {
                    Assert.All(inputs, input => { Assert.Equal(Guid.Empty, input.Id); Assert.Equal(0, input.Version); Assert.True(HolonChangeTracking.HasChanges(input)); });
                }
                else
                {
                    Assert.Equal(new[] { BoundTarget(ProviderType.Neo4jOASIS), BoundTarget(ProviderType.SQLLiteDBOASIS) }, frozen.Select(row => row["TargetId"].AsString));
                    foreach (var row in frozen)
                    {
                        var payload = CommittedHolonReplicaPayload.Read(row["PayloadJson"].AsString);
                        Assert.Equal(operation, payload.OperationId);
                        Assert.Equal(actor, payload.ActorId);
                        Assert.Equal(boundSourceId, payload.SourceId);
                        Assert.Equal(inputs.Select(input => input.VersionId).OrderBy(id => id), payload.Roots.Select(input => input.VersionId).OrderBy(id => id));
                    }
                    Assert.All(inputs, input => { Assert.NotEqual(Guid.Empty, input.Id); Assert.Equal(1, input.Version); Assert.False(HolonChangeTracking.HasChanges(input)); });
                    var unchanged = await manager.SaveOwnedHolonsAsync(inputs, actor, new CosmicWriteOptions(Guid.NewGuid()),
                        saveChildren: false, providerType: ProviderType.MongoDBOASIS);
                    Assert.False(unchanged.IsError, unchanged.Message);
                    Assert.False(unchanged.IsSaved);
                    Assert.Equal(2, await obligations.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                    Assert.Equal(1, await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                    var reopened = new Provider(uri, name);
                    try
                    {
                        Assert.False((await reopened.ActivateProviderAsync()).IsError);
                        foreach (var input in inputs)
                        {
                            var loaded = await reopened.LoadHolonAsync(input.Id, loadChildren: false);
                            Assert.False(loaded.IsError, loaded.Message);
                            Assert.Equal(input.VersionId, loaded.Result.VersionId);
                        }
                    }
                    finally { Assert.False((await reopened.DeActivateProviderAsync()).IsError); }
                }
                complete = true;
            }
            finally
            {
                Assert.False((await provider.DeActivateProviderAsync()).IsError);
                if (complete)
                {
                    await client.DropDatabaseAsync(name);
                    using var remaining = await client.ListDatabaseNamesAsync();
                    Assert.DoesNotContain(name, await remaining.ToListAsync());
                    File.Delete(journal);
                }
            }
        });
    }
}
