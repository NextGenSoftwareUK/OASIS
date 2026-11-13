using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NeoProvider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    private sealed class PreflightReadGate : IHolonReplicaHeadAuditStorageProvider
    {
        public readonly TaskCompletionSource Reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly IHolonReplicaHeadAuditStorageProvider target;
        public PreflightReadGate(IHolonReplicaHeadAuditStorageProvider target) { this.target = target; }
        public async Task<OASISResult<HolonReplicaHeadReadResult>> ReadHolonReplicaHeadsAsync(HolonReplicaHeadReadRequest request, CancellationToken token = default)
        {
            Reached.TrySetResult();
            await Released.Task.WaitAsync(token);
            return await target.ReadHolonReplicaHeadsAsync(request, token);
        }
    }

    [TestMethod]
    public async Task NativeSqliteNeoPreflightRejectsTargetContentChangedBetweenSourceAndTargetReads()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Run-ReusableNeo4jTests.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        var path = Path.Combine(root.FullName, "LocalState/COSMIC/Neo4jCurrent/neo-target-preflight.db");
        var journal = Path.Combine(root.FullName, "LocalState/COSMIC/Neo4jCurrent/neo-target-preflight.pending.json");
        foreach (var file in new[] { path, path + "-wal", path + "-shm", journal }) Assert.IsFalse(File.Exists(file), "Preserve an unfinished outcome.");
        var input = new Holon { Id = Guid.NewGuid(), Name = "original-neo-preflight" };
        var actor = Guid.NewGuid(); var operation = Guid.NewGuid();
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        { await JsonSerializer.SerializeAsync(file, new { path, input.Id, actor, operation }); file.Flush(true); }
        var source = new SqliteProvider($"Data Source={path};Pooling=False");
        var target = new NeoProvider(uri, "neo4j", "oasis-local-test-only");
        using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var native = driver.AsyncSession();
        var complete = false;
        try
        {
            Assert.IsFalse((await source.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await target.ActivateProviderAsync()).IsError);
            var sourceBinding = await source.CaptureStorageIdentityAsync(); Assert.IsFalse(sourceBinding.IsError);
            var targetBinding = await target.CaptureStorageIdentityAsync(); Assert.IsFalse(targetBinding.IsError);
            var enrollment = new HolonReplicationEnrollment("owned-sqlite-neo-preflight", new[] { "target" });
            var original = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation, actor, ProviderType.SQLLiteDBOASIS,
                new[] { new HolonWriteCondition(input.Id, null) }, enrollment), ConditionalHolonOperation.Save, new[] { input }, saveChildren: false);
            var saved = await source.ExecuteReplicatingHolonWriteAsync(original, enrollment); Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
            var applied = await target.ApplyBoundCommittedHolonReplicaAsync(new CommittedHolonReplicaRequest(operation,
                enrollment.SourceId, ProviderType.SQLLiteDBOASIS, actor, original.Operation, saved.Result, saveChildren: false), targetBinding.Result);
            Assert.IsFalse(applied.IsError, applied.Message + applied.Exception);
            var request = new ConditionalHolonReceiptEvidenceRequest(original, sourceBinding.Result, targetBinding.Result, new[] { input.Id });
            var strict = await HolonReplicationReceiptPreflight.ReadAsync(source, target, request);
            Assert.AreEqual("COSMIC_REPLICA_HEAD_AUDIT_CONSISTENCY_UNSUPPORTED", strict.ErrorCode); Assert.IsNull(strict.Result);
            var matched = await HolonReplicationReceiptPreflight.ReadAsync(source, target, request, HolonReplicaHeadReadConsistency.ReadCommittedValidated);
            Assert.IsFalse(matched.IsError, matched.Message + matched.Exception);
            Assert.AreEqual(HolonReplicationReceiptComparisonKind.MatchesOriginalReceipt, matched.Result.Records[0].Kind);
            Assert.AreEqual(HolonReplicaHeadReadConsistency.ReadCommittedValidated, matched.Result.TargetConsistency);
            var id = input.Id.ToString("D");
            var current = await (await native.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot,h.ProviderRevisionId AS revision", new { id })).SingleAsync();
            var changed = StoredRecordJson.DeserializeStored<Holon>(current["snapshot"].As<string>());
            changed.Name = "independent-neo-change";
            var snapshot = StoredRecordJson.Serialize(changed);
            var gate = new PreflightReadGate(target);
            var comparing = HolonReplicationReceiptPreflight.ReadAsync(source, gate, request, HolonReplicaHeadReadConsistency.ReadCommittedValidated);
            try
            {
                await gate.Reached.Task.WaitAsync(TimeSpan.FromSeconds(15));
                await (await native.RunAsync("MATCH (h:Holon {Id:$id}) SET h.SnapshotJson=$snapshot,h.Name=$name RETURN h.Id", new { id, snapshot, name = changed.Name })).ConsumeAsync();
            }
            finally { gate.Released.TrySetResult(); }
            var rejected = await comparing;
            Assert.IsTrue(rejected.IsError, "A body edit between read scopes must not be accepted using an unchanged token.");
            Assert.AreEqual("COSMIC_REPLICA_HEAD_AUDIT_UNVERIFIED", rejected.ErrorCode); Assert.IsNull(rejected.Result);
            var retained = await (await native.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot,h.ProviderRevisionId AS revision", new { id })).SingleAsync();
            Assert.AreEqual(snapshot, retained["snapshot"].As<string>());
            Assert.AreEqual(current["revision"].As<string>(), retained["revision"].As<string>());
            await (await native.RunAsync("MATCH (h:Holon {Id:$id}) DETACH DELETE h", new { id })).ConsumeAsync();
            await (await native.RunAsync("MATCH (h:CosmicReplicaHead {HolonId:$id}) DELETE h", new { id })).ConsumeAsync();
            await (await native.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) DELETE r", new { operation = operation.ToString("D") })).ConsumeAsync();
            var remaining = await (await native.RunAsync("MATCH (n) WHERE (n:Holon AND n.Id=$id) OR (n:CosmicReplicaHead AND n.HolonId=$id) OR (n:CosmicWriteReceipt AND n.OperationId=$operation) RETURN count(n) AS count", new { id, operation = operation.ToString("D") })).SingleAsync();
            Assert.AreEqual(0L, remaining["count"].As<long>());
            complete = true;
        }
        finally
        {
            Assert.IsFalse((await target.DeActivateProviderAsync()).IsError); source.Dispose();
            if (complete)
            {
                foreach (var file in new[] { path, path + "-wal", path + "-shm" }) if (File.Exists(file)) File.Delete(file);
                File.Delete(journal);
            }
        }
    }
}
