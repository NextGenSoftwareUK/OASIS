using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeBoundOriginalReceiptPreservesHistoricalIntentAndQueue(bool bindOriginal)
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Run-ReusableNeo4jTests.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        var journal = Path.Combine(root.FullName, "LocalState/COSMIC/Neo4jCurrent/source-receipt.pending.json");
        Directory.CreateDirectory(Path.GetDirectoryName(journal)!);
        Assert.IsFalse(File.Exists(journal), "Unfinished original outcomes require reconciliation.");
        var targetPath = Path.Combine(root.FullName, "LocalState/COSMIC/Neo4jCurrent/source-receipt-target.db");
        Assert.IsFalse(File.Exists(targetPath), "Preserve any unfinished replica fixture.");
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspection = driver.AsyncSession();
        var input = new Holon { Id = Guid.NewGuid(), Name = "private-neo-original-receipt" };
        var operation = Guid.NewGuid(); var appendOperation = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var arguments = new { id = input.Id.ToString("D"), operation = operation.ToString("D") };
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var captured = await provider.CaptureStorageIdentityAsync(); Assert.IsFalse(captured.IsError);
            var expected = captured.Result;
            var enrollment = new HolonReplicationEnrollment("owned-source-receipt-" + operation, new[] { "target", "target-b", "\uE000", "\U0001F600" }, bindOriginal ? expected : null);
            var request = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation, actor,
                ProviderType.Neo4jOASIS, new[] { new HolonWriteCondition(input.Id, null) }, enrollment),
                ConditionalHolonOperation.Save, new[] { input }, saveChildren: false);
            using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(file, new { id = input.Id, operation, appendOperation, actor, source = expected, bindOriginal, targetPath });
                file.Flush(true);
            }
            var absent = await provider.ReadBoundConditionalHolonWriteOutcomeAsync(request, expected);
            Assert.IsFalse(absent.IsError, absent.Message + absent.Exception);
            Assert.AreEqual(ConditionalHolonReceiptState.Absent, absent.Result.ReceiptState);
            var saved = await provider.ExecuteReplicatingHolonWriteAsync(request, enrollment);
            Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
            async Task<string> Snapshot()
            {
                var rows = await (await inspection.RunAsync(
                    "MATCH (n) WHERE (n:Holon AND n.Id=$id) OR ((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId=$operation) RETURN labels(n) AS labels, properties(n) AS values ORDER BY labels(n)[0]", arguments)).ToListAsync();
                Assert.AreEqual(2 + enrollment.TargetIds.Count, rows.Count);
                return JsonSerializer.Serialize(rows.Select(row => new { labels = row["labels"], values = row["values"] }));
            }
            var before = await Snapshot();
            var verified = await provider.ReadBoundConditionalHolonWriteOutcomeAsync(request, expected);
            Assert.IsFalse(verified.IsError, verified.Message + verified.Exception);
            Assert.AreEqual(ConditionalHolonReceiptState.Committed, verified.Result.ReceiptState);
            Assert.IsFalse(verified.IsSaved);
            var comparison = new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS, new string('A', 64), Guid.NewGuid());
            var projected = await provider.ReadConditionalHolonReceiptEvidenceAsync(new(request, expected, comparison, new[] { input.Id }));
            Assert.IsFalse(projected.IsError, projected.Message + projected.Exception);
            Assert.AreEqual(input.Id, projected.Result.ReceiptEvidence.Records.Single().HolonId);
            Assert.AreEqual(1, projected.Result.ReceiptEvidence.OriginalSelectedRecordCount);
            Assert.AreEqual(comparison.BindingFingerprint, projected.Result.ReceiptEvidence.ComparisonTargetFingerprint);

            Assert.AreSame(bindOriginal ? expected : null, request.Write.ReplicationEnrollment.SourceStorageIdentity);
            Assert.IsFalse(JsonSerializer.Serialize(verified.Result).Contains(input.Name, StringComparison.Ordinal));
            var wrong = new CosmicStorageIdentity(expected.Authority, expected.EndpointFingerprint, Guid.NewGuid());
            var rejected = await provider.ReadBoundConditionalHolonWriteOutcomeAsync(request, wrong);
            Assert.IsTrue(rejected.IsError); Assert.IsNull(rejected.Result);
            var foreign = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation, Guid.NewGuid(),
                ProviderType.Neo4jOASIS, request.Write.Conditions, enrollment), request.Operation, request.Roots, saveChildren: false);
            var foreignOutcome = await provider.ReadBoundConditionalHolonWriteOutcomeAsync(foreign, expected);
            Assert.IsTrue(foreignOutcome.IsError); Assert.IsNull(foreignOutcome.Result);
            Assert.AreEqual(before, await Snapshot(), "Evidence reads must not alter graph, receipt or pending queue.");
            Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var reconstructed = await provider.ReadBoundConditionalHolonWriteOutcomeAsync(request, expected);
            Assert.IsFalse(reconstructed.IsError, reconstructed.Message + reconstructed.Exception);
            Assert.AreEqual(ConditionalHolonReceiptState.Committed, reconstructed.Result.ReceiptState);
            Assert.AreEqual(before, await Snapshot());
            // Actual Neo source -> independent SQLite receiver. Source receipt is
            // immutable; the target observation uses its native snapshot reader.
            using (var replica = new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={targetPath};Pooling=False"))
            {
                Assert.IsFalse((await replica.ActivateProviderAsync()).IsError);
                var targetBinding = await replica.CaptureStorageIdentityAsync(); Assert.IsFalse(targetBinding.IsError);
                var preflightRequest = new ConditionalHolonReceiptEvidenceRequest(request, expected, targetBinding.Result, new[] { input.Id });
                var missing = await HolonReplicationReceiptPreflight.ReadAsync(provider, replica, preflightRequest);
                Assert.IsFalse(missing.IsError, missing.Message + missing.Exception);
                Assert.AreEqual(HolonReplicationReceiptComparisonKind.MissingReplicaHead, missing.Result.Records[0].Kind);
                var applied = await replica.ApplyBoundCommittedHolonReplicaAsync(new CommittedHolonReplicaRequest(
                    operation, enrollment.SourceId, ProviderType.Neo4jOASIS, actor, request.Operation, saved.Result, saveChildren: false), targetBinding.Result);
                Assert.IsFalse(applied.IsError, applied.Message + applied.Exception);
                var boundSource = provider.GetBoundHolonReplicationOutbox(expected);
                Assert.IsFalse(boundSource.IsError, boundSource.Message + boundSource.Exception);
                await CosmicNativeAcceptance.CosmicOperationInventoryProbe.CompleteAlreadyAppliedTargetAsync(
                    boundSource.Result, expected, operation, "target", HolonReplicaHeadReadConsistency.ReadCommittedValidated);
                await CosmicNativeAcceptance.CosmicOperationInventoryProbe.VerifyAsync(boundSource.Result, expected,
                    operation, enrollment.SourceId, actor, enrollment.TargetIds, requireCompleted: true,
                    consistency: HolonReplicaHeadReadConsistency.ReadCommittedValidated, migrationSource: provider);

                await CosmicNativeAcceptance.CosmicLineageInventoryProbe.VerifyPreviewAsync(boundSource.Result,
                    expected, targetBinding.Result, enrollment.SourceId, operation, enrollment.TargetIds.Count,
                    () => HolonReplicationReceiptPreflight.ReadAsync(provider, replica, preflightRequest),
                    HolonReplicaHeadReadConsistency.ReadCommittedValidated);
                await CosmicNativeAcceptance.CosmicLineageInventoryProbe.VerifyWindowAsync(boundSource.Result, expected,
                    enrollment.SourceId, operation, enrollment.TargetIds.Count, appendOperation, async () =>
                    {
                        var prior = saved.Result.Single();
                        var edit = NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(prior);
                        edit.Name = "later-lineage-operation";
                        var update = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(appendOperation, actor, ProviderType.Neo4jOASIS,
                            new[] { new HolonWriteCondition(input.Id, CosmicConcurrency.GetProviderRevisionId(prior, ProviderType.Neo4jOASIS)) }, enrollment),
                            ConditionalHolonOperation.Save, new[] { edit }, saveChildren: false);
                        var appended = await provider.ExecuteReplicatingHolonWriteAsync(update, enrollment);
                        Assert.IsFalse(appended.IsError, appended.Message + appended.Exception);
                    }, HolonReplicaHeadReadConsistency.ReadCommittedValidated);
                var matched = await HolonReplicationReceiptPreflight.ReadAsync(provider, replica, preflightRequest);
                Assert.IsFalse(matched.IsError, matched.Message + matched.Exception);
                Assert.AreEqual(HolonReplicationReceiptComparisonKind.MatchesOriginalReceipt, matched.Result.Records[0].Kind);
                Assert.AreEqual(HolonReplicaHeadReadConsistency.NativeSnapshot, matched.Result.TargetConsistency);
                Assert.IsFalse(matched.IsSaved);
                Assert.AreEqual(matched.Result.ObservationDigest,
                    (await HolonReplicationReceiptPreflight.ReadAsync(provider, replica, preflightRequest)).Result.ObservationDigest);
                using var native = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={targetPath};Pooling=False");
                await native.OpenAsync();
                using var edit = native.CreateCommand();
                edit.CommandText = "UPDATE Holon SET Name=$name WHERE Id=$id";
                edit.Parameters.AddWithValue("$name", "independent-sqlite-edit");
                edit.Parameters.AddWithValue("$id", input.Id.ToString("D"));
                Assert.AreEqual(1, await edit.ExecuteNonQueryAsync());
                var changed = await HolonReplicationReceiptPreflight.ReadAsync(provider, replica, preflightRequest);
                Assert.IsTrue(changed.IsError, "An unchanged revision cannot certify changed current SQLite content.");
                Assert.IsNull(changed.Result);
                Assert.AreEqual("COSMIC_REPLICA_HEAD_AUDIT_UNVERIFIED", changed.ErrorCode);

            }
            foreach (var path in new[] { targetPath, targetPath + "-wal", targetPath + "-shm" })
                if (File.Exists(path)) File.Delete(path);
            await (await inspection.RunAsync("MATCH (h:Holon {Id:$id}) DETACH DELETE h", arguments)).ConsumeAsync();
            await (await inspection.RunAsync("MATCH (n) WHERE (n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId IN $operations DETACH DELETE n", new { operations = new[] { operation.ToString("D"), appendOperation.ToString("D") } })).ConsumeAsync();
            var remaining = await (await inspection.RunAsync("MATCH (n) WHERE (n:Holon AND n.Id=$id) OR ((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId IN $operations) RETURN count(n) AS count", new { arguments.id, operations = new[] { operation.ToString("D"), appendOperation.ToString("D") } })).SingleAsync();
            Assert.AreEqual(0L, remaining["count"].As<long>());
            File.Delete(journal);
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
    }
}
