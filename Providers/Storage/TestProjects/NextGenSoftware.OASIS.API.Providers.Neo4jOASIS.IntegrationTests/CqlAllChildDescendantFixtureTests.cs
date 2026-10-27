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
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlAllRetainedFixtureTests
{
    [TestMethod]
    public async Task NativeAllChildDescendantScopesCannotLoseALaterChildError()
    {
        var work = Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!;
        var originalPath = Path.Combine(work, "cql-v1-web4-fixture.json");
        var original = await File.ReadAllBytesAsync(originalPath);
        Assert.AreEqual("892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5", Convert.ToHexString(SHA256.HashData(original)));
        var journal = Path.Combine(work, "cql-all-child-descendant-fixture.json");
        Assert.IsFalse(File.Exists(journal), "Preserve prior graph outcomes; never replay or reset.");
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
        var textIds = ids.Select(id => id.ToString("D")).ToArray();
        var actor = Guid.NewGuid();
        var other = Guid.NewGuid();
        var states = new List<object>();
        async Task Checkpoint(string stage) => await File.WriteAllTextAsync(journal,
            JsonSerializer.Serialize(new { ids, stage, states, owner = nameof(NativeAllChildDescendantScopesCannotLoseALaterChildError) }));
        await Checkpoint("before-native-preflight");
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        var records = new MongoClient(uri).GetDatabase("cosmic_cql_v1_web4_storage").GetCollection<BsonDocument>("Holon");
        var mongoIds = new BsonDocument("HolonId", new BsonDocument("$in", new BsonArray(textIds)));
        Assert.AreEqual(0L, await records.CountDocumentsAsync(mongoIds));
        await using var sql = new SqliteConnection("Data Source=" + Path.Combine(work, "cql-v1-web4.sqlite"));
        await sql.OpenAsync();
        void BindIds(SqliteCommand command)
        { for (var index = 0; index < textIds.Length; index++) command.Parameters.AddWithValue("$id" + index, textIds[index]); }
        var sqlIds = string.Join(",", Enumerable.Range(0, textIds.Length).Select(index => "$id" + index));
        async Task<long> SqlCount()
        {
            await using var command = sql.CreateCommand();
            command.CommandText = "SELECT count(*) FROM Holon WHERE Id IN (" + sqlIds + ")";
            BindIds(command);
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }
        Assert.AreEqual(0L, await SqlCount());
        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspection = driver.AsyncSession();
        async Task<long> NeoCount() => (await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids RETURN count(h) AS total", new { ids = textIds })).SingleAsync())["total"].As<long>();
        Assert.AreEqual(0L, await NeoCount());
        var providers = new IOASISStorageProvider[] {
            new MongoProvider(uri, "cosmic_cql_v1_web4_storage") { IsVersionControlEnabled = false },
            new SqliteProvider("Data Source=" + Path.Combine(work, "cql-v1-web4.sqlite")) { IsVersionControlEnabled = false },
            new Neo4jOASIS("bolt://127.0.0.1:17687", "neo4j", "oasis-local-test-only") { IsVersionControlEnabled = false } };
        var proofs = new List<object>();
        foreach (var provider in providers)
        {
            var complete = false;
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            try
            {
                var root = new Holon { Id = ids[0], Name = "protected child root", CreatedByAvatarId = actor, IsPublic = true };
                root.Children.Add(new Holon { Id = ids[1], ParentHolonId = root.Id, Name = "matching child", CreatedByAvatarId = actor, IsPublic = true });
                var later = new Holon { Id = ids[2], ParentHolonId = root.Id, Name = "later private child", CreatedByAvatarId = other };
                later.Children.Add(new Holon { Id = ids[3], ParentHolonId = later.Id, Name = "grandchild one", CreatedByAvatarId = other, IsPublic = true });
                later.Children.Add(new Holon { Id = ids[4], ParentHolonId = later.Id, Name = "grandchild two", CreatedByAvatarId = other, IsPublic = true });
                root.Children.Add(later);
                await Checkpoint("before-create-" + provider.ProviderName);
                var saved = await provider.SaveHolonAsync(root, saveChildren: true);
                Assert.IsFalse(saved.IsError, saved.Message);
                states.Add(new { provider = provider.ProviderName, created = true });
                await Checkpoint("created-" + provider.ProviderName);
                var adapter = (ICosmicQueryStorageProvider)provider;
                var scalar = (ICosmicQueryScalarStorageProvider)provider;
                var rootId = ids[0];
                var source = CosmicQuery<Holon>.Create().Where(row => row.Id == rootId).Result;
                var tight = CosmicQueryTraversalOptions.Create(2, 1).Result;
                var sufficient = CosmicQueryTraversalOptions.Create(2, 2).Result;
                var outerShallow = CosmicQueryTraversalOptions.Create(1, 2).Result;
                var outerDeep = CosmicQueryTraversalOptions.Create(2, 4).Result;
                async Task<NextGenSoftware.OASIS.Common.OASISResult<T>> Read<T>(CosmicScalarQuery<Holon, T> query)
                {
                    var plan = CosmicScalarPlanner.Plan(query, adapter.QueryProviderType,
                        new[] { new CosmicScalarTarget(adapter.QueryProviderType, adapter.QueryCapabilities, scalar.ScalarCapabilities) });
                    Assert.IsFalse(plan.IsError, plan.Message);
                    return await scalar.ExecuteCosmicScalarAsync(plan.Result);
                }
                var count = await Read(source.LongCount().Result);
                Assert.IsFalse(count.IsError, count.Message); Assert.AreEqual(1L, count.Result);
                var errors = new[] {
                    source.All(row => row.Children.Any(child => child.Name == "matching child" || CosmicQueryRelations.AnyDescendant(child, descendant => true, tight))),
                    source.All(row => row.Children.Any(child => child.Name == "never" && CosmicQueryRelations.AnyDescendant(child, descendant => true, tight))),
                    source.All(row => !row.Children.Any(child => child.Name == "matching child" || CosmicQueryRelations.AnyDescendant(child, descendant => true, tight))),
                    source.All(row => row.Name == "protected child root" || row.Children.Any(child => CosmicQueryRelations.AnyDescendant(child, descendant => true, tight))),
                    source.All(row => row.Name == "never" && row.Children.Any(child => CosmicQueryRelations.AnyDescendant(child, descendant => true, tight))) };
                errors = errors.Concat(new[] {
                    source.All(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "matching child"
                        || CosmicQueryRelations.AnyDescendant(child, descendant => true, tight), outerShallow)),
                    source.All(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "never"
                        && CosmicQueryRelations.AnyDescendant(child, descendant => true, tight), outerShallow)),
                    source.All(row => row.Name == "protected child root" || CosmicQueryRelations.AnyDescendant(row,
                        child => CosmicQueryRelations.AnyDescendant(child, descendant => true, tight), outerShallow))
                }).ToArray();
                foreach (var query in errors)
                {
                    Assert.IsFalse(query.IsError, query.Message);
                    var result = await Read(query.Result);
                    Assert.IsTrue(result.IsError, provider.ProviderName + ": a protected scope error was hidden");
                    Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode, result.Message);
                }
                var positive = await Read(source.All(row => row.Children.Any(child => CosmicQueryRelations.AnyDescendant(child,
                    descendant => descendant.Name == "grandchild one", sufficient))).Result);
                Assert.IsFalse(positive.IsError, positive.Message); Assert.IsTrue(positive.Result);
                var secured = source.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
                var hidden = await Read(secured.All(row => row.Children.Any(child => child.Name == "matching child"
                    || CosmicQueryRelations.AnyDescendant(child, descendant => true, tight))).Result);
                Assert.IsFalse(hidden.IsError, hidden.Message); Assert.IsTrue(hidden.Result);
                var empty = await Read(source.Take(0).All(row => row.Children.Any(child => CosmicQueryRelations.AnyDescendant(child, descendant => true, tight))).Result);
                Assert.IsFalse(empty.IsError, empty.Message); Assert.IsTrue(empty.Result);
                var reverse = await Read(source.All(row => CosmicQueryRelations.AnyDescendant(row,
                    child => child.Children.Any(grandchild => grandchild.Name == "grandchild one"), outerDeep)).Result);
                Assert.IsFalse(reverse.IsError, reverse.Message); Assert.IsTrue(reverse.Result);
                var reverseHidden = await Read(secured.All(row => CosmicQueryRelations.AnyDescendant(row,
                    child => child.Children.Any(grandchild => grandchild.Name == "grandchild one"), outerDeep)).Result);
                Assert.IsFalse(reverseHidden.IsError, reverseHidden.Message); Assert.IsFalse(reverseHidden.Result);
                var reverseEmpty = await Read(source.Take(0).All(row => CosmicQueryRelations.AnyDescendant(row,
                    child => CosmicQueryRelations.AnyDescendant(child, descendant => true, tight), outerShallow)).Result);
                Assert.IsFalse(reverseEmpty.IsError, reverseEmpty.Message); Assert.IsTrue(reverseEmpty.Result);
                proofs.Add(new { provider = provider.ProviderName, protectedErrors = errors.Length, ordinaryCount = 1, positive = true, hiddenScopeExcluded = true, emptyTerminal = true, reversePositive = true, reverseHiddenScopeExcluded = true, reverseEmptyTerminal = true });
                complete = true;
            }
            finally
            {
                try
                {
                    if (complete)
                    {
                        await Checkpoint("before-exact-cleanup-" + provider.ProviderName);
                        if (provider is MongoProvider) Assert.AreEqual(5L, (await records.DeleteManyAsync(mongoIds)).DeletedCount);
                        else if (provider is SqliteProvider)
                        {
                            await using var remove = sql.CreateCommand();
                            remove.CommandText = "DELETE FROM Holon WHERE Id IN (" + sqlIds + ")";
                            BindIds(remove); await remove.ExecuteNonQueryAsync(); Assert.AreEqual(0L, await SqlCount());
                        }
                        else
                        {
                            await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = textIds })).ConsumeAsync();
                            Assert.AreEqual(0L, await NeoCount());
                        }
                        states.Add(new { provider = provider.ProviderName, removed = true });
                        await Checkpoint("exact-cleanup-completed-" + provider.ProviderName);
                    }
                }
                finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
            }
        }
        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(originalPath));
        Assert.AreEqual(0L, await records.CountDocumentsAsync(mongoIds));
        Assert.AreEqual(0L, await SqlCount()); Assert.AreEqual(0L, await NeoCount());
        await File.WriteAllTextAsync(Path.Combine(work, "cql-all-child-descendant-result.json"), JsonSerializer.Serialize(new { ids, checks = 45, providers = proofs, exactCleanupVerified = true, originalFixtureUnchanged = true }));
        File.Delete(journal);
    }
}
