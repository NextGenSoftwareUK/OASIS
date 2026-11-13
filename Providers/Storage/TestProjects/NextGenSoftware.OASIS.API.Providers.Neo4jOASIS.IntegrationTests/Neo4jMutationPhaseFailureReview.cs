using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
public class Neo4jMutationPhaseFailureReview
{
    [TestMethod]
    public async Task ReviewKnownReceiptConsistencyAssertionFailureWithoutReplayingWrites()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Run-ReusableNeo4jTests.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        var journal = Path.Combine(root.FullName, "LocalState/COSMIC/Neo4jCurrent/migration-fence.pending.json");
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(journal)); var j = doc.RootElement;
        Assert.AreEqual(Guid.Parse("3e9085b0-1eef-47fb-bea5-e9b6c294fef0"), j.GetProperty("operation").GetGuid());
        var actor = j.GetProperty("actor").GetGuid(); var ids = j.GetProperty("ids").EnumerateArray().Select(v => v.GetGuid()).ToArray();
        var operations = new[] { "operation", "unrelatedOperation", "phaseSeedOp", "phaseReplicaOp", "phaseOtherOp" }.Select(n => j.GetProperty(n).GetGuid())
            .Concat(j.GetProperty("blockedOperations").EnumerateArray().Select(v => v.GetGuid())).ToArray();
        var migrations = new[] { "migration", "competing", "phaseMigration", "phaseRaceMigration" }.Select(n => j.GetProperty(n).GetGuid()).ToArray();
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only")); using var session = driver.AsyncSession();
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var binding = await provider.CaptureStorageIdentityAsync(); Assert.IsFalse(binding.IsError);
            var oldScope = new HolonMigrationFenceRequest(migrations[0], actor, binding.Result, new string('A',64), new string('B',64), new[] { ids[0] });
            Assert.AreEqual(HolonMigrationFenceState.Cancelled, (await provider.ReadHolonMigrationFenceAsync(oldScope)).Result.State);
            var phase = new HolonMigrationFenceRequest(migrations[2], actor, binding.Result, new string('A',64), new string('B',64), new[] { ids[0], ids[2], ids[3] });
            var started = await provider.ReadHolonMigrationFenceAsync(phase); Assert.IsFalse(started.IsError);
            Assert.AreEqual(HolonMigrationFenceState.MutationStarted, started.Result.State);
            Assert.IsTrue(started.Result.MutationDigest is var digest && (digest == new string('C',64) || digest == new string('D',64)));
            var args = new { ids = ids.Select(id => id.ToString("D")).ToArray(), operations = operations.Select(id => id.ToString("D")).ToArray(),
                migrations = migrations.Select(id => id.ToString("D")).ToArray() };
            var records = await (await session.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids RETURN h.Id AS id,h.Name AS name,h.SnapshotJson AS snapshot", args)).ToListAsync();
            Assert.AreEqual(4, records.Count);
            var expectedNames = new[] { "owned-neo-migration-barrier", "owned-neo-unrelated", "phase-protected", "phase-incoming" };
            foreach (var row in records)
            {
                var index = Array.IndexOf(args.ids, row["id"].As<string>()); Assert.IsTrue(index >= 0 && index < 4);
                Assert.AreEqual(expectedNames[index], row["name"].As<string>());
                using var snapshot = JsonDocument.Parse(row["snapshot"].As<string>());
                Assert.AreEqual(1, snapshot.RootElement.GetProperty("Version").GetInt32());
                Assert.AreEqual(actor, snapshot.RootElement.GetProperty("CreatedByAvatarId").GetGuid());
            }
            var queue = await (await session.RunAsync("MATCH (o:CosmicHolonOutbox) WHERE o.OperationId IN $operations RETURN o.OperationId AS operation,o.PayloadJson AS payload,o.PayloadDigest AS hash,o.DeliveryState AS state", args)).ToListAsync();
            Assert.AreEqual(2, queue.Count);
            foreach (var row in queue)
            {
                var payload = row["payload"].As<string>(); Assert.AreEqual(row["hash"].As<string>(), Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))));
                var intent = CommittedHolonReplicaPayload.Read(payload); Assert.AreEqual(actor, intent.ActorId);
                var original = row["operation"].As<string>() == operations[0].ToString("D");
                Assert.AreEqual(original ? operations[0] : operations[2], intent.OperationId);
                Assert.AreEqual(original ? ids[0] : ids[2], intent.Roots.Single().Id);
                Assert.AreEqual(original ? "InFlight" : "Pending", row["state"].As<string>());
            }
            var counts = await (await session.RunAsync("MATCH (r:CosmicWriteReceipt) WHERE r.OperationId IN $operations RETURN count(r) AS count", args)).SingleAsync();
            Assert.AreEqual(4L, counts["count"].As<long>());
            // Known test-only marker/data, all native calls terminal, no remote
            // delivery or cutover method exists in the fixture. Do not cancel or
            // replay the started protocol; remove only this exact owned dataset.
            const string owned = "(n:Holon AND n.Id IN $ids) OR (n:CosmicReplicaHead AND n.HolonId IN $ids) OR (n:OasisRecordVersion AND n.Kind='Holon' AND n.RecordId IN $ids) OR (n:CosmicMigrationRecordGate AND n.HolonId IN $ids) OR (n:CosmicMigrationFence AND n.MigrationId IN $migrations) OR ((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId IN $operations)";
            var evidence = Path.Combine(root.FullName, "TestResults/Neo4jCurrent/mutation-phase-reviewed-journal.json");
            File.Copy(journal, evidence, false);
            await (await session.RunAsync("MATCH (n) WHERE " + owned + " DETACH DELETE n", args)).ConsumeAsync();
            var remaining = await (await session.RunAsync("MATCH (n) WHERE " + owned + " RETURN count(n) AS count", args)).SingleAsync(); Assert.AreEqual(0L, remaining["count"].As<long>());
            File.Delete(journal);
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
    }
}
