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
    [TestMethod]
    public async Task ReviewPreservedOperationInventoryFailureWithoutReplayingTheWrite()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var mode = Environment.GetEnvironmentVariable("OASIS_OPERATION_INVENTORY_REVIEW");
        Assert.IsTrue(mode is "inspect" or "reconcile");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Run-ReusableNeo4jTests.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        var failure = Path.Combine(root.FullName, "TestResults/Neo4jCurrent/neo4j-operation-inventory-unicode-failure.trx");
        Assert.AreEqual("46FC3D369D9A35333B86A24EFD3108FF437C2267590E4CCD6FB9F5D8E1AA27B5",
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(failure))));
        var journal = Path.Combine(root.FullName, "LocalState/COSMIC/Neo4jCurrent/source-receipt.pending.json");
        using var record = JsonDocument.Parse(await File.ReadAllTextAsync(journal)); var data = record.RootElement;
        Assert.IsFalse(data.GetProperty("bindOriginal").GetBoolean());
        var id = data.GetProperty("id").GetGuid(); var operation = data.GetProperty("operation").GetGuid(); var actor = data.GetProperty("actor").GetGuid();
        var recordedSource = data.GetProperty("source");
        var expected = new CosmicStorageIdentity(ProviderType.Neo4jOASIS, recordedSource.GetProperty("EndpointFingerprint").GetString()!, recordedSource.GetProperty("StoreId").GetString()!);
        Assert.AreEqual(recordedSource.GetProperty("BindingFingerprint").GetString(), expected.BindingFingerprint);
        var targets = new[] { "target", "target-b", "\uE000", "\U0001F600" };
        var sourceId = "owned-source-receipt-" + operation;
        var original = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation, actor, ProviderType.Neo4jOASIS,
            new[] { new HolonWriteCondition(id, null) }, new HolonReplicationEnrollment(sourceId, targets)),
            ConditionalHolonOperation.Save, new[] { new Holon { Id = id, Name = "private-neo-original-receipt" } }, saveChildren: false);
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only")); using var native = driver.AsyncSession();
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var receipt = await provider.ReadBoundConditionalHolonWriteOutcomeAsync(original, expected);
            Assert.IsFalse(receipt.IsError, receipt.Message + receipt.Exception); Assert.AreEqual(ConditionalHolonReceiptState.Committed, receipt.Result.ReceiptState);
            var bound = provider.GetBoundHolonReplicationOutbox(expected); Assert.IsFalse(bound.IsError);
            var inventory = (IHolonReplicationOperationInventoryOutbox)bound.Result;
            var rows = await (await native.RunAsync("MATCH (o:CosmicHolonOutbox {OperationId:$operation}) RETURN o ORDER BY o.TargetId", new { operation = operation.ToString("D") })).ToListAsync();
            Assert.AreEqual(4, rows.Count);
            var nativeTargets = new List<string>();
            CommittedHolonReplicaRequest? committed = null;
            string? payloadDigest = null;
            foreach (var row in rows)
            {
                var node = row["o"].As<Neo4j.Driver.INode>(); var target = node["TargetId"].As<string>(); nativeTargets.Add(target);
                var payload = node["PayloadJson"].As<string>();
                Assert.AreEqual(node["PayloadDigest"].As<string>(), Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload))));
                var frozen = CommittedHolonReplicaPayload.ReadForTarget(payload, target);
                Assert.AreEqual(operation, frozen.OperationId); Assert.AreEqual(actor, frozen.ActorId); Assert.AreEqual(sourceId, frozen.SourceId);
                Assert.AreEqual(id, frozen.Roots.Single().Id);
                committed ??= frozen; payloadDigest ??= node["PayloadDigest"].As<string>();
                Assert.AreEqual(payloadDigest, node["PayloadDigest"].As<string>());
                Assert.AreEqual(target == "target" ? "Completed" : "Pending", node["DeliveryState"].As<string>());
                Assert.AreEqual(target == "target" ? 1L : 0L, node["AttemptCount"].As<long>());
            }
            CollectionAssert.AreEquivalent(targets, nativeTargets.ToArray());
            var pages = new List<object>(); var observed = new List<string>(); string? after = null;
            do
            {
                var page = await inventory.ReadOperationObligationsAsync(new(expected, operation, 1, after, HolonReplicaHeadReadConsistency.ReadCommittedValidated));
                Assert.IsFalse(page.IsError, page.Message + page.Exception);
                observed.AddRange(page.Result.Obligations.Select(row => row.TargetId));
                pages.Add(new { after, targets = page.Result.Obligations.Select(row => row.TargetId).ToArray(), next = page.Result.NextAfterTargetId });
                after = page.Result.NextAfterTargetId; Assert.IsTrue(pages.Count <= 8);
            } while (after != null);
            var comparison = await (await native.RunAsync("RETURN $bmp < $astral AS less, $bmp > $astral AS greater", new { bmp = "\uE000", astral = "\U0001F600" })).SingleAsync();
            var storedComparison = await (await native.RunAsync("MATCH (a:CosmicHolonOutbox {OperationId:$operation,TargetId:$bmp}), (b:CosmicHolonOutbox {OperationId:$operation,TargetId:$astral}) RETURN a.TargetId < b.TargetId AS less", new { operation = operation.ToString("D"), bmp = "\uE000", astral = "\U0001F600" })).SingleAsync();
            var indexed = await (await native.RunAsync("MATCH (o:CosmicHolonOutbox) USING INDEX o:CosmicHolonOutbox(OperationId,TargetId) WHERE o.OperationId=$operation AND o.TargetId>$after RETURN o.TargetId AS target ORDER BY o.TargetId", new { operation = operation.ToString("D"), after = "\uE000" })).ToListAsync();
            var evidence = Path.Combine(root.FullName, "TestResults/Neo4jCurrent/operation-inventory-unicode-" + mode + ".json");
            await File.WriteAllTextAsync(evidence, JsonSerializer.Serialize(new { operation, id, actor, source = expected.BindingFingerprint,
                nativeTargets, observed, pages, storedBmpLessThanAstral = storedComparison["less"].As<bool>(), indexedAfterBmp = indexed.Select(row => row["target"].As<string>()).ToArray(), bmpLessThanAstral = comparison["less"].As<bool>(), bmpGreaterThanAstral = comparison["greater"].As<bool>(), receipt = receipt.Result.ReceiptState.ToString() }));
            if (mode == "inspect") return;
            await CosmicNativeAcceptance.CosmicOperationInventoryProbe.VerifyAsync(bound.Result, expected, operation, sourceId, actor, targets,
                requireCompleted: true, consistency: HolonReplicaHeadReadConsistency.ReadCommittedValidated);
            var targetPath = Path.GetFullPath(data.GetProperty("targetPath").GetString()!);
            var requiredPath = Path.GetFullPath(Path.Combine(root.FullName, "LocalState/COSMIC/Neo4jCurrent/source-receipt-target.db"));
            Assert.AreEqual(requiredPath, targetPath, true);
            Assert.IsNotNull(committed);
            Assert.AreEqual(0, (int)(File.GetAttributes(targetPath) & FileAttributes.ReparsePoint));
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={targetPath};Mode=ReadOnly;Pooling=False"))
            {
                await connection.OpenAsync();
                var plan = CommittedHolonReplicaPlan.Create(committed, ProviderType.SQLLiteDBOASIS);
                using var receiptQuery = connection.CreateCommand();
                receiptQuery.CommandText = "SELECT RequestHash,ResultJson FROM CosmicWriteReceipt WHERE OperationId=$operation";
                receiptQuery.Parameters.AddWithValue("$operation", operation.ToString("D"));
                IHolon acknowledged;
                using (var reader = await receiptQuery.ExecuteReaderAsync())
                {
                    Assert.IsTrue(await reader.ReadAsync()); Assert.AreEqual(plan.Fingerprint, reader.GetString(0));
                    acknowledged = ConditionalHolonReceipt.Read(reader.GetString(1), plan).Single();
                    Assert.IsFalse(await reader.ReadAsync());
                }
                using var headQuery = connection.CreateCommand();
                headQuery.CommandText = "SELECT SourceId,SourceAuthority,SnapshotJson,SnapshotDigest,ProviderRevisionId,HardDeleted FROM CosmicReplicaHead WHERE HolonId=$id";
                headQuery.Parameters.AddWithValue("$id", id.ToString("D"));
                using (var reader = await headQuery.ExecuteReaderAsync())
                {
                    Assert.IsTrue(await reader.ReadAsync());
                    var head = HolonReplicaHeadRecord.Read(id, ProviderType.SQLLiteDBOASIS, reader.GetString(0), reader.GetString(1),
                        reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetInt64(5) == 1).Evidence;
                    Assert.AreEqual(sourceId, head.SourceId); Assert.AreEqual(ProviderType.Neo4jOASIS, head.SourceAuthority);
                    HolonReplicaHeadRecord.ValidateCurrent(id, ProviderType.SQLLiteDBOASIS, acknowledged, head);
                    Assert.AreEqual(CommittedHolonReplicaPlan.PortableContentDigest(committed.Roots.Single(), ProviderType.SQLLiteDBOASIS), head.PortableContentDigest);
                    Assert.IsFalse(await reader.ReadAsync());
                }
                using var currentQuery = connection.CreateCommand();
                currentQuery.CommandText = "SELECT Id,Name,Version,VersionId,ProviderRevisionId FROM Holon";
                using (var reader = await currentQuery.ExecuteReaderAsync())
                {
                    Assert.IsTrue(await reader.ReadAsync()); Assert.AreEqual(id.ToString("D"), reader.GetString(0));
                    Assert.AreEqual(acknowledged.Name, reader.GetString(1)); Assert.AreEqual(acknowledged.Version, reader.GetInt32(2));
                    Assert.AreEqual(acknowledged.VersionId, Guid.Parse(reader.GetString(3)));
                    Assert.AreEqual(CosmicConcurrency.GetProviderRevisionId(acknowledged, ProviderType.SQLLiteDBOASIS), Guid.Parse(reader.GetString(4)));
                    Assert.IsFalse(await reader.ReadAsync());
                }
                using var totals = connection.CreateCommand();
                totals.CommandText = "SELECT (SELECT count(*) FROM CosmicReplicaHead)+(SELECT count(*) FROM CosmicWriteReceipt)";
                Assert.AreEqual(2L, Convert.ToInt64(await totals.ExecuteScalarAsync()));
            }
            await (await native.RunAsync("MATCH (h:Holon {Id:$id}) DETACH DELETE h", new { id = id.ToString("D") })).ConsumeAsync();
            await (await native.RunAsync("MATCH (n) WHERE (n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId=$operation DETACH DELETE n", new { operation = operation.ToString("D") })).ConsumeAsync();
            var remaining = await (await native.RunAsync("MATCH (n) WHERE (n:Holon AND n.Id=$id) OR ((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId=$operation) RETURN count(n) AS count", new { id = id.ToString("D"), operation = operation.ToString("D") })).SingleAsync();
            Assert.AreEqual(0L, remaining["count"].As<long>());
            foreach (var file in new[] { targetPath, targetPath + "-wal", targetPath + "-shm" }) if (File.Exists(file)) File.Delete(file);
            File.Delete(journal);
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
    }
}
