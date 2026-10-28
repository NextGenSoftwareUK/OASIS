using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jStorageIdentityTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NeoBoundReplicaChecksNativeIdentityBeforeGraphOrReceiptReplay(bool synchronous)
    {
        Assert.AreEqual(Uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Run-ReusableNeo4jTests.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        var journal = Path.Combine(root.FullName, "LocalState/COSMIC/OwnedRoutingCurrent/neo-bound-receiver.pending.json");
        Assert.IsFalse(File.Exists(journal), "An unfinished native mutation must be reconciled, never reseeded.");
        var record = new Holon { Id = Guid.NewGuid(), Name = "bound-native-receiver", Version = 1,
            VersionId = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid(), IsActive = true };
        CosmicConcurrency.SetProviderRevisionId(record, ProviderType.MongoDBOASIS, Guid.NewGuid());
        var request = new CommittedHolonReplicaRequest(Guid.NewGuid(), "bound-neo-fixture-" + Guid.NewGuid().ToString("N"),
            ProviderType.MongoDBOASIS, record.CreatedByAvatarId, ConditionalHolonOperation.Save, new[] { record }, saveChildren: false);
        Directory.CreateDirectory(Path.GetDirectoryName(journal)!);
        await File.WriteAllTextAsync(journal, JsonSerializer.Serialize(new { record.Id, request.OperationId, request.SourceId, synchronous }));
        var provider = new Provider(Uri, "neo4j", "oasis-local-test-only");
        using var driver = GraphDatabase.Driver(Uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspection = driver.AsyncSession();
        var complete = false;
        var keys = new { id = record.Id.ToString(), operation = request.OperationId.ToString() };
        async Task<long[]> Counts()
        {
            var rows = await (await inspection.RunAsync(
                "OPTIONAL MATCH (h:Holon {Id:$id}) WITH count(h) AS holons " +
                "OPTIONAL MATCH (r:CosmicWriteReceipt {OperationId:$operation}) WITH holons,count(r) AS receipts " +
                "OPTIONAL MATCH (s:CosmicReplicaHead {HolonId:$id}) RETURN holons,receipts,count(s) AS heads", keys)).SingleAsync();
            return new[] { rows["holons"].As<long>(), rows["receipts"].As<long>(), rows["heads"].As<long>() };
        }
        async Task<NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolon>>> Apply(CosmicStorageIdentity expected)
            => synchronous ? provider.ApplyBoundCommittedHolonReplica(request, expected)
                : await provider.ApplyBoundCommittedHolonReplicaAsync(request, expected);
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var captured = await provider.CaptureStorageIdentityAsync();
            Assert.IsFalse(captured.IsError, captured.Message + captured.Exception);
            var expected = captured.Result;
            var wrongStore = new CosmicStorageIdentity(expected.Authority, expected.EndpointFingerprint, "different-native-store");
            var wrongEndpoint = new CosmicStorageIdentity(expected.Authority,
                expected.EndpointFingerprint == new string('A', 64) ? new string('B', 64) : new string('A', 64), expected.StoreId);
            var wrongAuthority = new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS, expected.EndpointFingerprint, expected.StoreId);
            foreach (var invalid in new[] { wrongStore, wrongEndpoint, wrongAuthority })
            {
                Assert.AreEqual("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH", (await Apply(invalid)).ErrorCode);
                CollectionAssert.AreEqual(new long[] { 0, 0, 0 }, await Counts());
            }
            var first = await Apply(expected);
            Assert.IsFalse(first.IsError, first.Message + first.Exception);
            CollectionAssert.AreEqual(new long[] { 1, 1, 1 }, await Counts());
            var revision = CosmicConcurrency.GetProviderRevisionId(first.Result.Single(), ProviderType.Neo4jOASIS);
            var replay = await Apply(expected);
            Assert.IsFalse(replay.IsError, replay.Message + replay.Exception);
            Assert.AreEqual(revision, CosmicConcurrency.GetProviderRevisionId(replay.Result.Single(), ProviderType.Neo4jOASIS));
            Assert.AreEqual("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH", (await Apply(wrongStore)).ErrorCode,
                "An existing exact receipt cannot bypass native binding admission.");
            CollectionAssert.AreEqual(new long[] { 1, 1, 1 }, await Counts());

            provider.Host = "bolt://127.0.0.1:1"; // Mutable configuration cannot relabel the serving driver.
            var pinned = await Apply(expected);
            Assert.IsFalse(pinned.IsError, pinned.Message + pinned.Exception);
            Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
            provider = new Provider(Uri, "neo4j", "oasis-local-test-only");
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var reopened = await Apply(expected);
            Assert.IsFalse(reopened.IsError, reopened.Message + reopened.Exception);
            Assert.AreEqual(revision, CosmicConcurrency.GetProviderRevisionId(reopened.Result.Single(), ProviderType.Neo4jOASIS));
            CollectionAssert.AreEqual(new long[] { 1, 1, 1 }, await Counts());
            var head = await (await inspection.RunAsync("MATCH (r:CosmicReplicaHead {HolonId:$id}) RETURN r.SourceId AS source", keys)).SingleAsync();
            Assert.AreEqual(request.SourceId, head["source"].As<string>());
            await (await inspection.RunAsync("MATCH (n) WHERE (n:Holon AND n.Id=$id) OR " +
                "(n:CosmicReplicaHead AND n.HolonId=$id) OR (n:CosmicWriteReceipt AND n.OperationId=$operation) DETACH DELETE n", keys)).ConsumeAsync();
            CollectionAssert.AreEqual(new long[] { 0, 0, 0 }, await Counts());
            complete = true;
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
        if (complete) File.Delete(journal);
    }

    [TestMethod]
    public async Task NeoBoundReplicaRefusesUncapturedInjectedDriverWithoutMutation()
    {
        Assert.AreEqual(Uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var native = GraphDatabase.Driver(Uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        var provider = new Provider(Uri, "neo4j", "oasis-local-test-only", () => native);
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var identity = new CosmicStorageIdentity(ProviderType.Neo4jOASIS, new string('A', 64), "opaque-native-id");
            var denied = await provider.ApplyBoundCommittedHolonReplicaAsync(null!, identity);
            Assert.AreEqual("COSMIC_REPLICA_STORAGE_BINDING_INVALID", denied.ErrorCode);
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
    }
}
