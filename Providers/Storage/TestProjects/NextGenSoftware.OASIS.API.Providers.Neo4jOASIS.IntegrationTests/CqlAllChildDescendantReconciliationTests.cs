using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class CqlAllChildDescendantReconciliationTests
{
    [TestMethod]
    public async Task ReviewedChildDescendantFailureIsProvedBeforeExactCleanup()
    {
        var work = Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!;
        var manifest = Path.Combine(work, "cql-all-child-descendant-fixture.json");
        var bytes = await File.ReadAllBytesAsync(manifest);
        Assert.AreEqual("FA5AE57DB5C47F0CC5966F88E89629154C85DAD9B452FD2D0CA4EBBB7E009DB5", Convert.ToHexString(SHA256.HashData(bytes)));
        using var document = JsonDocument.Parse(bytes);
        var fixture = document.RootElement;
        Assert.AreEqual("created-SQLLiteDBOASIS", fixture.GetProperty("stage").GetString());
        Assert.AreEqual("NativeAllChildDescendantScopesCannotLoseALaterChildError", fixture.GetProperty("owner").GetString());
        var ids = fixture.GetProperty("ids").EnumerateArray().Select(value => value.GetGuid()).ToArray();
        Assert.AreEqual(5, ids.Distinct().Count());
        var textIds = ids.Select(id => id.ToString("D")).ToArray();
        var originalPath = Path.Combine(work, "cql-v1-web4-fixture.json");
        var original = await File.ReadAllBytesAsync(originalPath);
        Assert.AreEqual("892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5", Convert.ToHexString(SHA256.HashData(original)));
        var mongo = new MongoClient("mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000")
            .GetDatabase("cosmic_cql_v1_web4_storage").GetCollection<BsonDocument>("Holon");
        Assert.AreEqual(0L, await mongo.CountDocumentsAsync(new BsonDocument("HolonId", new BsonDocument("$in", new BsonArray(textIds)))));
        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspection = driver.AsyncSession();
        Assert.AreEqual(0L, (await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids RETURN count(h) AS total", new { ids = textIds })).SingleAsync())["total"].As<long>());
        var connection = "Data Source=" + Path.Combine(work, "cql-v1-web4.sqlite");
        var provider = new SqliteProvider(connection) { IsVersionControlEnabled = false };
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var outcomes = new List<object>();
        try
        {
            var names = new[] { "protected child root", "matching child", "later private child", "grandchild one", "grandchild two" };
            var parents = new[] { Guid.Empty, ids[0], ids[0], ids[2], ids[2] };
            var snapshots = new List<object>();
            Guid actor = Guid.Empty;
            for (var index = 0; index < ids.Length; index++)
            {
                var loaded = await provider.LoadHolonAsync(ids[index], loadChildren: false);
                Assert.IsFalse(loaded.IsError, loaded.Message);
                Assert.AreEqual(names[index], loaded.Result.Name);
                Assert.AreEqual(parents[index], loaded.Result.ParentHolonId);
                if (index == 0) actor = loaded.Result.CreatedByAvatarId;
                snapshots.Add(new { loaded.Result.Id, loaded.Result.Name, loaded.Result.ParentHolonId, loaded.Result.Version });
            }
            Assert.AreNotEqual(Guid.Empty, actor);
            var rootId = ids[0];
            var source = CosmicQuery<Holon>.Create().Where(row => row.Id == rootId).Result;
            var adapter = (ICosmicQueryStorageProvider)provider;
            var scalar = (ICosmicQueryScalarStorageProvider)provider;
            var tight = CosmicQueryTraversalOptions.Create(2, 1).Result;
            var enough = CosmicQueryTraversalOptions.Create(2, 2).Result;
            async Task<NextGenSoftware.OASIS.Common.OASISResult<T>> Read<T>(CosmicScalarQuery<Holon, T> query)
            {
                var plan = CosmicScalarPlanner.Plan(query, adapter.QueryProviderType,
                    new[] { new CosmicScalarTarget(adapter.QueryProviderType, adapter.QueryCapabilities, scalar.ScalarCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                return await scalar.ExecuteCosmicScalarAsync(plan.Result);
            }
            var count = await Read(source.LongCount().Result);
            Assert.IsFalse(count.IsError, count.Message); Assert.AreEqual(1L, count.Result);
            var cases = new[] {
                source.All(row => row.Children.Any(child => child.Name == "matching child" || CosmicQueryRelations.AnyDescendant(child, descendant => true, tight))),
                source.All(row => row.Children.Any(child => child.Name == "never" && CosmicQueryRelations.AnyDescendant(child, descendant => true, tight))),
                source.All(row => !row.Children.Any(child => child.Name == "matching child" || CosmicQueryRelations.AnyDescendant(child, descendant => true, tight))),
                source.All(row => row.Name == "protected child root" || row.Children.Any(child => CosmicQueryRelations.AnyDescendant(child, descendant => true, tight))),
                source.All(row => row.Name == "never" && row.Children.Any(child => CosmicQueryRelations.AnyDescendant(child, descendant => true, tight))) };
            for (var index = 0; index < cases.Length; index++)
            {
                Assert.IsFalse(cases[index].IsError, cases[index].Message);
                var result = await Read(cases[index].Result);
                outcomes.Add(new { index, result.ErrorCode, result.IsError });
                Assert.IsTrue(result.IsError, "Protected case " + index + " was hidden");
                Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode, result.Message);
            }
            var positive = await Read(source.All(row => row.Children.Any(child => CosmicQueryRelations.AnyDescendant(child,
                descendant => descendant.Name == "grandchild one", enough))).Result);
            Assert.IsFalse(positive.IsError, positive.Message); Assert.IsTrue(positive.Result);
            var secured = source.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
            var hidden = await Read(secured.All(row => row.Children.Any(child => child.Name == "matching child"
                || CosmicQueryRelations.AnyDescendant(child, descendant => true, tight))).Result);
            Assert.IsFalse(hidden.IsError, hidden.Message); Assert.IsTrue(hidden.Result);
            var empty = await Read(source.Take(0).All(row => row.Children.Any(child => CosmicQueryRelations.AnyDescendant(child, descendant => true, tight))).Result);
            Assert.IsFalse(empty.IsError, empty.Message); Assert.IsTrue(empty.Result);
            CollectionAssert.AreEqual(bytes, await File.ReadAllBytesAsync(manifest));
            CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(originalPath));
            // Required evidence is durable before any destructive action. Only
            // the exact journalled five IDs are removed; no DB reset or replay.
            var evidence = Path.Combine(work, "cql-all-child-descendant-reconciled.json");
            await File.WriteAllTextAsync(evidence, JsonSerializer.Serialize(new { ids, snapshots, outcomes, checks = 9, originalFixtureUnchanged = true, cleanup = "ready" }));
            await using var sql = new SqliteConnection(connection);
            await sql.OpenAsync();
            await using var cleanup = sql.CreateCommand();
            var parameters = string.Join(",", Enumerable.Range(0, ids.Length).Select(index => "$id" + index));
            for (var index = 0; index < ids.Length; index++) cleanup.Parameters.AddWithValue("$id" + index, textIds[index]);
            cleanup.CommandText = "DELETE FROM Holon WHERE Id IN (" + parameters + ")";
            await cleanup.ExecuteNonQueryAsync();
            cleanup.CommandText = "SELECT count(*) FROM Holon WHERE Id IN (" + parameters + ")";
            Assert.AreEqual(0L, Convert.ToInt64(await cleanup.ExecuteScalarAsync()));
            await File.WriteAllTextAsync(evidence, JsonSerializer.Serialize(new { ids, snapshots, outcomes, checks = 9, originalFixtureUnchanged = true, cleanup = "verified-exact-IDs" }));
            File.Delete(manifest);
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
    }
}
