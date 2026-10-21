using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class Neo4jNativeCqlTests
{
    [TestMethod]
    public async Task NativeCosmicCqlPredicatesProjectionUpdatesAndHistoricalUpgrade()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work = Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!;
        var manifest = Path.Combine(work, "cql-native-fixture.json");
        Assert.IsFalse(File.Exists(manifest), "Preserve and reconcile the previous failed CQL fixture before another run.");
        using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspection = driver.AsyncSession();
        var count = (await (await inspection.RunAsync("MATCH (h:Holon) RETURN count(h) AS count")).SingleAsync())["count"].As<long>();
        Assert.AreEqual(0L, count, "Native CQL acceptance requires the retained fixture database to contain no unresolved holons.");
        var records = Enumerable.Range(0, 5).Select(i => new Holon { Id = Guid.NewGuid(), Name = "CQL Coffee ☕ " + i }).ToArray();
        var ids = records.Select(row => row.Id.ToString()).ToArray();
        await File.WriteAllTextAsync(manifest, JsonSerializer.Serialize(new { ids, stage = "before-create" }));
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only") { IsVersionControlEnabled = true };
        var complete = false;
        CosmicQueryPlan<Holon> Plan(Expression<Func<Holon, bool>> expression)
        {
            var built = CosmicQuery<Holon>.Create().Where(expression);
            Assert.IsFalse(built.IsError, built.Message);
            var planned = CosmicQueryPlanner.Plan(built.Result, ProviderType.Neo4jOASIS,
                new[] { new CosmicQueryTarget(ProviderType.Neo4jOASIS, Provider.CqlCapabilities) });
            Assert.IsFalse(planned.IsError, planned.Message); return planned.Result;
        }
        async Task Matches(Expression<Func<Holon, bool>> expression, params int[] expected)
        {
            var result = await provider.ExecuteCosmicQueryAsync(Plan(expression));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEquivalent(expected.Select(i => records[i].Id).ToArray(), result.Result.Select(row => row.Id).ToArray());
        }
        try
        {
            var activation = await provider.ActivateProviderAsync(); Assert.IsFalse(activation.IsError, activation.Message + activation.Exception);
            records[0].MetaData["score"] = 7; records[0].MetaData["a.b"] = "literal' quoted";
            records[1].MetaData["score"] = "7"; records[1].MetaData["a"] = new Dictionary<string, object> { ["b"] = "nested" };
            records[2].MetaData["score"] = null!; records[4].MetaData["score"] = 7.0m;
            for (var i = 0; i < records.Length; i++)
            {
                var saved = await provider.SaveHolonAsync(records[i], saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                records[i] = (Holon)saved.Result;
            }
            await Matches(h => CosmicQueryMetadata.Value<int>(h, "score") == 7, 0);
            await Matches(h => CosmicQueryMetadata.Value<int?>(h, "score") != 7, 2);
            await Matches(h => !(CosmicQueryMetadata.Value<int>(h, "score") == 7), 1, 2, 3, 4);
            await Matches(h => CosmicQueryMetadata.Exists(h, "score") && CosmicQueryMetadata.IsNull(h, "score"), 2);
            var choices = new int?[] { 7, null };
            await Matches(h => choices.Contains(CosmicQueryMetadata.Value<int?>(h, "score")), 0, 2);
            await Matches(h => CosmicQueryMetadata.Value<string>(h, "a.b") == "literal' quoted", 0);
            await Matches(h => CosmicQueryMetadata.Value<string>(h, "a", "b").StartsWith("nest", StringComparison.Ordinal), 1);
            await Matches(h => h.Name.Contains("Coffee ☕", StringComparison.Ordinal), 0, 1, 2, 3, 4);
            records[0].MetaData.Remove("score");
            var update = await provider.SaveHolonAsync(records[0], saveChildren: false); Assert.IsFalse(update.IsError, update.Message + update.Exception);
            records[0] = (Holon)update.Result;
            await Matches(h => CosmicQueryMetadata.Value<int>(h, "score") == 7);
            await Matches(h => h.Version >= 2, 0);
            var limited = await provider.ExecuteCosmicQueryAsync(Plan(h => true), 1);
            Assert.IsTrue(limited.IsError); Assert.AreEqual("CQL_QUERY_LIMIT", limited.ErrorCode); Assert.IsNull(limited.Result);
            using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            var stopped = await provider.ExecuteCosmicQueryAsync(Plan(h => true), cancellationToken: cancelled.Token);
            Assert.AreEqual("CQL_QUERY_CANCELLED", stopped.ErrorCode);
            var before = (await (await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot", new { id = ids[1] })).SingleAsync())["snapshot"].As<string>();
            await (await inspection.RunAsync("MATCH (h:Holon {Id:$id})-[p:HAS_CQL_PROJECTION]->(h) DELETE p", new { id = ids[1] })).ConsumeAsync();
            var targetId = records[0].Id;
            var invalid = await provider.ExecuteCosmicQueryAsync(Plan(h => h.Id == targetId));
            Assert.AreEqual("CQL_DATA_INVALID", invalid.ErrorCode, invalid.Message); Assert.IsNull(invalid.Result);
            Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
            activation = await provider.ActivateProviderAsync(); Assert.IsFalse(activation.IsError, activation.Message + activation.Exception);
            var after = (await (await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot", new { id = ids[1] })).SingleAsync())["snapshot"].As<string>();
            Assert.AreEqual(before, after, "Projection upgrade must not invent a logical edit or revision.");
            await Matches(h => CosmicQueryMetadata.Value<string>(h, "score") == "7", 1);
            // Observe a real native lock wait: activation must derive its projection
            // from the concurrent writer's committed snapshot, never a pre-lock image.
            Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
            await (await inspection.RunAsync("MATCH (h:Holon {Id:$id})-[p:HAS_CQL_PROJECTION]->(h) DELETE p", new { id = ids[1] })).ConsumeAsync();
            var edited = StoredRecordJson.DeserializeStoredHolon(before);
            edited.MetaData["score"] = "8"; edited.Version++;
            edited.PreviousVersionId = edited.VersionId; edited.VersionId = Guid.NewGuid();
            var revision = Guid.NewGuid(); CosmicConcurrency.SetProviderRevisionId(edited, ProviderType.Neo4jOASIS, revision);
            var concurrentSnapshot = StoredRecordJson.Serialize(edited);
            await File.WriteAllTextAsync(manifest, JsonSerializer.Serialize(new { ids, stage = "before-concurrent-upgrade", concurrentSnapshot }));
            using (var writerSession = driver.AsyncSession())
            using (var writer = await writerSession.BeginTransactionAsync())
            {
                await (await writer.RunAsync("MATCH (h:Holon {Id:$id}) SET h.SnapshotJson=$snapshot,h.ProviderRevisionId=$revision RETURN h.Id",
                    new { id = ids[1], snapshot = concurrentSnapshot, revision = revision.ToString() })).ConsumeAsync();
                var upgrading = provider.ActivateProviderAsync();
                var deadline = DateTime.UtcNow.AddSeconds(10); var blocked = false;
                do
                {
                    var running = await (await inspection.RunAsync("SHOW TRANSACTIONS YIELD currentQuery,status WHERE currentQuery CONTAINS '__oasisHolonLock' AND currentQuery CONTAINS 'collect(current)' RETURN status")).ToListAsync();
                    blocked = running.Any(row => row["status"].As<string>().StartsWith("Blocked", StringComparison.OrdinalIgnoreCase));
                    if (!blocked) await Task.Delay(50);
                } while (!blocked && !upgrading.IsCompleted && DateTime.UtcNow < deadline);
                Assert.IsTrue(blocked, "Upgrade must reach the native holon lock before the concurrent snapshot commits.");
                await writer.CommitAsync();
                activation = await upgrading; Assert.IsFalse(activation.IsError, activation.Message + activation.Exception);
            }
            await Matches(h => CosmicQueryMetadata.Value<string>(h, "score") == "8", 1);
            await Matches(h => h.Version >= 2, 0, 1);
            complete = true;
        }
        finally
        {
            var deactivated = await provider.DeActivateProviderAsync(); Assert.IsFalse(deactivated.IsError, deactivated.Message);
            if (complete)
            {
                await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids })).ConsumeAsync();
                await (await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids DETACH DELETE v", new { ids })).ConsumeAsync();
                File.Delete(manifest);
            }
        }
    }
}
