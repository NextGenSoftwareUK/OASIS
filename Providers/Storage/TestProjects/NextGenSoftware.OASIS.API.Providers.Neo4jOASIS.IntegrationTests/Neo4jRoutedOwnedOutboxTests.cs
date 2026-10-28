using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
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
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    public class RoutedOwnedTarget : DispatchProxy, IBoundCommittedHolonReplicaStorageProvider, ICosmicStorageIdentityProvider
    {
        public ProviderType Identity;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
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
            => throw new InvalidOperationException("Primary writes cannot deliver inline.");
        public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Primary writes cannot deliver inline.");
    }

    private static CosmicStorageIdentity RoutedTargetIdentity(ProviderType type)
        => new(type, new string('A', 64), "declaration-only-" + type);

    [DataTestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public async Task NativeNeoRoutedOwnedSaveBindsTargetsAndRollsBackTheCompleteTransaction(bool batch, bool fault)
    {
        await Task.Run(async () =>
        {
            const string uri = "bolt://127.0.0.1:17687";
            Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
            Assert.IsNotNull(root);
            var state = Path.Combine(root.FullName, "LocalState/COSMIC/OwnedRoutingCurrent");
            Directory.CreateDirectory(state);
            using var lease = new FileStream(Path.Combine(state, "neo-environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var journal = Path.Combine(state, "neo-fixture.pending.json");
            Assert.IsFalse(File.Exists(journal), "Preserve original pending outcome.");
            var actor = Guid.NewGuid();
            var operation = Guid.NewGuid();
            using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(file, new { actor, operation, batch, fault });
                file.Flush(true);
            }
            using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
            using var inspect = driver.AsyncSession();
            var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
            var inputs = Enumerable.Range(0, batch ? 2 : 1).Select(index => new Holon { Name = "routed-native-" + index }).ToArray();
            var parameters = new { op = operation.ToString("D"), actor = actor.ToString("D") };
            async Task<long> Count(string query) => (await (await inspect.RunAsync(query, parameters)).SingleAsync())["count"].As<long>();
            async Task<object> Counter() => (await (await inspect.RunAsync("OPTIONAL MATCH (s:CosmicHolonOutboxSequence {Name:'primary-holon-outbox-v1'}) RETURN s.Value AS value")).SingleAsync())["value"];
            var complete = false;
            try
            {
                Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
                var nativeSource = (await provider.CaptureStorageIdentityAsync()).Result;
                string BoundTarget(ProviderType type) => new HolonReplicationRouteBinding(nativeSource,
                    RoutedTargetIdentity(type), HolonReplicationRoutes.DefaultPolicyBinding).TargetId;
                var boundSourceId = new HolonReplicationRouteBinding(nativeSource,
                    RoutedTargetIdentity(ProviderType.MongoDBOASIS), HolonReplicationRoutes.DefaultPolicyBinding).SourceId;
                var before = await Counter();
                if (fault)
                    await (await inspect.RunAsync("CREATE (:CosmicHolonOutbox {OperationId:$op,TargetId:$target,Sequence:-1,OwnedFixture:true})", new { op = operation.ToString("D"), target = BoundTarget(ProviderType.SQLLiteDBOASIS) })).ConsumeAsync();
                var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false } } };
                dna.OASIS.SubscriptionConfig.PlanType = "Gold";
                var routing = new ProviderManager(null, dna)
                { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
                routing.RegisterProvider(provider);
                foreach (var identity in new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS })
                {
                    var target = DispatchProxy.Create<IOASISStorageProvider, RoutedOwnedTarget>();
                    ((RoutedOwnedTarget)target).Identity = identity;
                    routing.RegisterProvider(target);
                }
                OASISRequestContext.AvatarRoutingOverrides = new ProviderRoutingOverrides(
                    autoReplicationEnabled: true, replicationProviders: new[] { ProviderType.MongoDBOASIS });
                OASISRequestContext.RequestRoutingOverrides = new ProviderRoutingOverrides(
                    replicationProviders: new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS });
                var manager = new HolonManager(null, dna, routing);
                if (batch)
                {
                    var saved = await manager.SaveOwnedHolonsAsync(inputs, actor, new CosmicWriteOptions(operation), saveChildren: false, providerType: ProviderType.Neo4jOASIS);
                    Assert.AreEqual(fault, saved.IsError, saved.Message + saved.Exception);
                    Assert.AreEqual(!fault, saved.IsSaved);
                }
                else
                {
                    var saved = await manager.SaveOwnedHolonAsync(inputs[0], actor, new CosmicWriteOptions(operation), saveChildren: false, providerType: ProviderType.Neo4jOASIS);
                    Assert.AreEqual(fault, saved.IsError, saved.Message + saved.Exception);
                    Assert.AreEqual(!fault, saved.IsSaved);
                }
                Assert.AreEqual(fault ? 0L : 1L, await Count("MATCH (r:CosmicWriteReceipt {OperationId:$op}) RETURN count(r) AS count"));
                Assert.AreEqual(fault ? 1L : 2L, await Count("MATCH (o:CosmicHolonOutbox {OperationId:$op}) RETURN count(o) AS count"));
                if (fault)
                {
                    Assert.AreEqual(before, await Counter());
                    Assert.AreEqual(0L, await Count("MATCH (h:Holon {CreatedByAvatarId:$actor}) RETURN count(h) AS count"));
                    foreach (var input in inputs) { Assert.AreEqual(Guid.Empty, input.Id); Assert.AreEqual(0, input.Version); Assert.IsTrue(HolonChangeTracking.HasChanges(input)); }
                }
                else
                {
                    var rows = await (await inspect.RunAsync("MATCH (o:CosmicHolonOutbox {OperationId:$op}) RETURN o ORDER BY o.TargetId", parameters)).ToListAsync();
                    var targets = new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS };
                    for (var index = 0; index < rows.Count; index++)
                    {
                        var row = rows[index]["o"].As<Neo4j.Driver.INode>();
                        Assert.AreEqual(BoundTarget(targets[index]), row["TargetId"].As<string>());
                        var payload = CommittedHolonReplicaPayload.Read(row["PayloadJson"].As<string>());
                        Assert.AreEqual(operation, payload.OperationId);
                        Assert.AreEqual(actor, payload.ActorId);
                        Assert.AreEqual(boundSourceId, payload.SourceId);
                        CollectionAssert.AreEquivalent(inputs.Select(input => input.VersionId).ToArray(), payload.Roots.Select(input => input.VersionId).ToArray());
                    }
                    foreach (var input in inputs) { Assert.AreEqual(1, input.Version); Assert.IsFalse(HolonChangeTracking.HasChanges(input)); }
                    var counterBeforeNoOp = await Counter();
                    var noOpOperation = Guid.NewGuid();
                    var noOp = await manager.SaveOwnedHolonsAsync(inputs, actor, new CosmicWriteOptions(noOpOperation), saveChildren: false, providerType: ProviderType.Neo4jOASIS);
                    Assert.IsFalse(noOp.IsError, noOp.Message);
                    Assert.IsFalse(noOp.IsSaved);
                    Assert.AreEqual(counterBeforeNoOp, await Counter());
                    var noOpReceipt = await (await inspect.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$op}) RETURN count(r) AS count", new { op = noOpOperation.ToString("D") })).SingleAsync();
                    Assert.AreEqual(0L, noOpReceipt["count"].As<long>());
                    Assert.AreEqual(1L, await Count("MATCH (r:CosmicWriteReceipt {OperationId:$op}) RETURN count(r) AS count"));
                    Assert.AreEqual(2L, await Count("MATCH (o:CosmicHolonOutbox {OperationId:$op}) RETURN count(o) AS count"));
                    var reopened = new Provider(uri, "neo4j", "oasis-local-test-only");
                    try
                    {
                        Assert.IsFalse((await reopened.ActivateProviderAsync()).IsError);
                        foreach (var input in inputs)
                        {
                            var loaded = await reopened.LoadHolonAsync(input.Id, loadChildren: false);
                            Assert.IsFalse(loaded.IsError, loaded.Message);
                            Assert.AreEqual(input.VersionId, loaded.Result.VersionId);
                        }
                    }
                    finally { Assert.IsFalse((await reopened.DeActivateProviderAsync()).IsError); }
                }
                complete = true;
            }
            finally
            {
                Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
                if (complete)
                {
                    // Delete only this intent's acknowledged IDs/operation. Never reset
                    // the shared monotonic sequence or unrelated retained graph state.
                    await (await inspect.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = inputs.Where(input => input.Id != Guid.Empty).Select(input => input.Id.ToString("D")).ToArray() })).ConsumeAsync();
                    await (await inspect.RunAsync("MATCH (r) WHERE (r:CosmicWriteReceipt OR r:CosmicHolonOutbox) AND r.OperationId=$op DELETE r", parameters)).ConsumeAsync();
                    Assert.AreEqual(0L, await Count("MATCH (r) WHERE (r:CosmicWriteReceipt OR r:CosmicHolonOutbox) AND r.OperationId=$op RETURN count(r) AS count"));
                    File.Delete(journal);
                }
            }
        });
    }
}
