using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeMigrationBarrierPreservesReceiptAndRejectsAffectedWrites(bool reactivate)
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Run-ReusableNeo4jTests.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        var journal = Path.Combine(root.FullName, "LocalState/COSMIC/Neo4jCurrent/migration-fence.pending.json");
        Directory.CreateDirectory(Path.GetDirectoryName(journal)!);
        Assert.IsFalse(File.Exists(journal), "Reconcile retained outcomes before another fixture.");
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        var other = new Provider(uri, "neo4j", "oasis-local-test-only");
        var actor = Guid.NewGuid(); var operation = Guid.NewGuid(); var unrelatedOperation = Guid.NewGuid();
        var record = new Holon { Id = Guid.NewGuid(), Name = "owned-neo-migration-barrier" };
        var unrelated = new Holon { Id = Guid.NewGuid(), Name = "owned-neo-unrelated" };
        var migration = Guid.NewGuid(); var competing = Guid.NewGuid();
        var blockedOperations = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(file, new { actor, operation, unrelatedOperation, migration, competing, blockedOperations, ids = new[] { record.Id, unrelated.Id } });
            file.Flush(true);
        }
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await other.ActivateProviderAsync()).IsError);
            var binding = await provider.CaptureStorageIdentityAsync(); Assert.IsFalse(binding.IsError, binding.Message);
            var request = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation, actor,
                ProviderType.Neo4jOASIS, new[] { new HolonWriteCondition(record.Id, null) }),
                ConditionalHolonOperation.Save, new[] { record }, saveChildren: false);
            var saved = await provider.ExecuteConditionalHolonWriteAsync(request);
            Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
            var fence = new HolonMigrationFenceRequest(migration, actor, binding.Result, new string('A', 64), new string('B', 64), new[] { record.Id });
            // Independent drivers contend on the same native fence identity.
            var acquisitions = await Task.WhenAll(provider.AcquireHolonMigrationFenceAsync(fence),
                other.AcquireHolonMigrationFenceAsync(fence));
            var held = acquisitions[0];
            Assert.IsFalse(acquisitions[1].IsError, acquisitions[1].Message + acquisitions[1].Exception);
            Assert.AreEqual(held.Result.OwnerToken, acquisitions[1].Result.OwnerToken);
            Assert.IsFalse(held.IsError, held.Message + held.Exception);
            Assert.AreEqual(HolonMigrationFenceState.Held, held.Result.State);
            var repeat = await other.AcquireHolonMigrationFenceAsync(fence);
            Assert.IsFalse(repeat.IsError, repeat.Message + repeat.Exception);
            Assert.AreEqual(held.Result.OwnerToken, repeat.Result.OwnerToken);
            if (reactivate)
            {
                Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
                Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            }
            var observed = await provider.ReadHolonMigrationFenceAsync(fence);
            Assert.IsFalse(observed.IsError, observed.Message + observed.Exception);
            Assert.AreEqual(held.Result.OwnerToken, observed.Result.OwnerToken);
            var replay = await other.ExecuteConditionalHolonWriteAsync(request);
            Assert.IsTrue(replay.IsError); Assert.AreEqual("COSMIC_MIGRATION_RECORD_FENCED", replay.ErrorCode);
            // New updates and both delete modes must use the same admission gate.
            var committed = saved.Result.Single();
            var kinds = new[] { ConditionalHolonOperation.Save, ConditionalHolonOperation.SoftDelete, ConditionalHolonOperation.HardDelete };
            for (var i = 0; i < kinds.Length; i++)
            {
                var blocked = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(blockedOperations[i], actor,
                    ProviderType.Neo4jOASIS, new[] { new HolonWriteCondition(record.Id,
                        CosmicConcurrency.GetProviderRevisionId(committed, ProviderType.Neo4jOASIS)) }),
                    kinds[i], new[] { committed }, saveChildren: false);
                var refused = await other.ExecuteConditionalHolonWriteAsync(blocked);
                Assert.IsTrue(refused.IsError); Assert.AreEqual("COSMIC_MIGRATION_RECORD_FENCED", refused.ErrorCode);
            }
            var historical = await other.ReadBoundConditionalHolonWriteOutcomeAsync(request, binding.Result);
            Assert.IsFalse(historical.IsError, historical.Message + historical.Exception);
            Assert.AreEqual(ConditionalHolonReceiptState.Committed, historical.Result.ReceiptState);
            var overlap = new HolonMigrationFenceRequest(competing, actor, binding.Result, new string('A', 64), new string('B', 64), new[] { record.Id });
            var rejected = await other.AcquireHolonMigrationFenceAsync(overlap);
            Assert.IsTrue(rejected.IsError); Assert.AreEqual("COSMIC_MIGRATION_FENCE_CONFLICT", rejected.ErrorCode);
            var unrelatedRequest = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(unrelatedOperation, actor,
                ProviderType.Neo4jOASIS, new[] { new HolonWriteCondition(unrelated.Id, null) }),
                ConditionalHolonOperation.Save, new[] { unrelated }, saveChildren: false);
            var independent = await other.ExecuteConditionalHolonWriteAsync(unrelatedRequest);
            Assert.IsFalse(independent.IsError, independent.Message + independent.Exception);
            var foreignCancel = await other.CancelHolonMigrationFenceAsync(fence, Guid.NewGuid());
            Assert.IsTrue(foreignCancel.IsError); Assert.AreEqual("COSMIC_MIGRATION_FENCE_OWNER_MISMATCH", foreignCancel.ErrorCode);
            var cancelled = await provider.CancelHolonMigrationFenceAsync(fence, held.Result.OwnerToken);
            Assert.IsFalse(cancelled.IsError, cancelled.Message + cancelled.Exception);
            Assert.AreEqual(HolonMigrationFenceState.Cancelled, cancelled.Result.State);
            var terminal = await other.AcquireHolonMigrationFenceAsync(fence);
            Assert.IsTrue(terminal.IsError); Assert.AreEqual("COSMIC_MIGRATION_FENCE_TERMINAL", terminal.ErrorCode);
            var resumed = await other.ExecuteConditionalHolonWriteAsync(request);
            Assert.IsFalse(resumed.IsError, resumed.Message + resumed.Exception);
            // Delete only this journal's exact fixture identities. Global schema survives.
            using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
            using var session = driver.AsyncSession();
            var args = new { ids = new[] { record.Id.ToString("D"), unrelated.Id.ToString("D") },
                operations = new[] { operation, unrelatedOperation }.Concat(blockedOperations).Select(id => id.ToString("D")).ToArray(),
                migrations = new[] { migration.ToString("D"), competing.ToString("D") } };
            const string owned = "(n:Holon AND n.Id IN $ids) OR (n:CosmicMigrationRecordGate AND n.HolonId IN $ids) OR (n:CosmicMigrationFence AND n.MigrationId IN $migrations) OR ((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId IN $operations)";
            await (await session.RunAsync("MATCH (n) WHERE " + owned + " DETACH DELETE n", args)).ConsumeAsync();
            var remaining = await (await session.RunAsync("MATCH (n) WHERE " + owned + " RETURN count(n) AS count", args)).SingleAsync();
            Assert.AreEqual(0L, remaining["count"].As<long>());
            File.Delete(journal);
        }
        finally
        {
            Assert.IsFalse((await other.DeActivateProviderAsync()).IsError);
            Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
        }
    }
}
