using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlAllRetainedFixtureTests
{
    [TestMethod]
    public Task NativeAllCycleErrorsPreserveOwnedFixtureAndCannotBeShortCircuited() => VerifyCycleFixture(mixed: false);

    [TestMethod]
    public Task NativeAllMixedCycleErrorsKeepTheirChildScope() => VerifyCycleFixture(mixed: true);

    private static async Task VerifyCycleFixture(bool mixed)
    {
        var work = Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!;
        var originalPath = Path.Combine(work, "cql-v1-web4-fixture.json");
        var original = await File.ReadAllBytesAsync(originalPath);
        Assert.AreEqual("892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5", Convert.ToHexString(SHA256.HashData(original)));
        var journal = Path.Combine(work, mixed ? "cql-all-mixed-cycle-fixture.json" : "cql-all-cycle-fixture.json");
        Assert.IsFalse(File.Exists(journal), "Preserve any prior cycle outcome; never replay or reset it.");
        var id = Guid.NewGuid();
        var textId = id.ToString("D");
        var childId = mixed ? Guid.NewGuid() : id;
        var childText = childId.ToString("D");
        var ownedIds = mixed ? new[] { textId, childText } : new[] { textId };
        var states = new List<object>();
        async Task Checkpoint(string stage) => await File.WriteAllTextAsync(journal,
            JsonSerializer.Serialize(new { id, ids = ownedIds, stage, states, owner = mixed ? nameof(NativeAllMixedCycleErrorsKeepTheirChildScope) : nameof(NativeAllCycleErrorsPreserveOwnedFixtureAndCannotBeShortCircuited) }));
        await Checkpoint("before-native-preflight");

        const string mongoUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        var records = new MongoClient(mongoUri).GetDatabase("cosmic_cql_v1_web4_storage").GetCollection<BsonDocument>("Holon");
        var mongoId = new BsonDocument("HolonId", new BsonDocument("$in", new BsonArray(ownedIds)));
        Assert.AreEqual(0L, await records.CountDocumentsAsync(mongoId));
        await using var sql = new SqliteConnection("Data Source=" + Path.Combine(work, "cql-v1-web4.sqlite"));
        await sql.OpenAsync();
        async Task<long> Sql(string statement)
        {
            await using var command = sql.CreateCommand();
            command.CommandText = statement;
            command.Parameters.AddWithValue("$id", textId);
            command.Parameters.AddWithValue("$child", childText);
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }
        Assert.AreEqual(0L, await Sql("SELECT count(*) FROM Holon WHERE Id IN ($id,$child)"));
        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspection = driver.AsyncSession();
        var absent = await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids RETURN count(h) AS total", new { ids = ownedIds })).SingleAsync();
        Assert.AreEqual(0L, absent["total"].As<long>());

        var providers = new IOASISStorageProvider[] {
            new MongoProvider(mongoUri, "cosmic_cql_v1_web4_storage") { IsVersionControlEnabled = false },
            new SqliteProvider("Data Source=" + Path.Combine(work, "cql-v1-web4.sqlite")) { IsVersionControlEnabled = false },
            new Neo4jOASIS("bolt://127.0.0.1:17687", "neo4j", "oasis-local-test-only") { IsVersionControlEnabled = false } };
        var proofs = new List<object>();
        foreach (var provider in providers)
        {
            var complete = false;
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            try
            {
                await Checkpoint("before-create-" + provider.ProviderName);
                var root = new Holon { Id = id, Name = "owned All cycle proof", IsPublic = true };
                var saved = await provider.SaveHolonAsync(root, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message);
                if (mixed)
                {
                    var child = new Holon { Id = childId, ParentHolonId = id, Name = "owned All mixed cycle child" };
                    var childSaved = await provider.SaveHolonAsync(child, saveChildren: false);
                    Assert.IsFalse(childSaved.IsError, childSaved.Message);
                }
                states.Add(new { provider = provider.ProviderName, created = true });
                await Checkpoint("before-cycle-" + provider.ProviderName);

                // Corrupt only this separately owned synthetic record. Keep the
                // canonical payload/projection consistent so a typed cycle error
                // cannot be confused with missing membership or a bad projection.
                for (var index = 0; index < ownedIds.Length; index++)
                {
                    var current = ownedIds[index];
                    var parent = mixed ? ownedIds[1 - index] : current;
                    if (provider is MongoProvider)
                    {
                        var projection = new BsonDocument { { "schema", 1 }, { "owner", current }, { "source", BsonNull.Value },
                            { "declared", false }, { "members", new BsonArray() } };
                        var patch = new BsonDocument("$set", new BsonDocument { { "ParentHolonId", parent },
                            { "ChildRoleManifestJson", BsonNull.Value }, { "CosmicCqlMembership", projection } });
                        Assert.AreEqual(1L, (await records.UpdateOneAsync(new BsonDocument("HolonId", current), patch)).MatchedCount);
                    }
                    else if (provider is SqliteProvider)
                    {
                        await using var patch = sql.CreateCommand();
                        patch.CommandText = "UPDATE Holon SET ParentHolonId=$parent, PersistentValueJson=json_set(json_remove(PersistentValueJson,'$.ChildRoleManifestJson'),'$.ParentHolonId',$parent) WHERE Id=$id";
                        patch.Parameters.AddWithValue("$id", current);
                        patch.Parameters.AddWithValue("$parent", parent);
                        Assert.AreEqual(1, await patch.ExecuteNonQueryAsync());
                    }
                    else
                    {
                        var observed = await (await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot", new { id = current })).SingleAsync();
                        var canonical = StoredRecordJson.DeserializeStoredHolon(observed["snapshot"].As<string>());
                        canonical.ParentHolonId = Guid.Parse(parent);
                        ((Holon)canonical).ChildRoleManifestJson = null;
                        await (await inspection.RunAsync("MATCH (h:Holon {Id:$id})-[p:HAS_CQL_PROJECTION]->(h) SET h.ParentHolonId=$parent,h.SnapshotJson=$snapshot,p.f_ParentHolonId=$parent,p.f_HasChildManifest=false,p.f_ChildIds=[]",
                            new { id = current, parent, snapshot = StoredRecordJson.Serialize(canonical) })).ConsumeAsync();
                    }
                }
                if (provider is Neo4jOASIS)
                    await (await inspection.RunAsync("MATCH (a:Holon {Id:$id}),(b:Holon {Id:$child}) MERGE (a)-[:HAS_CHILD]->(b) MERGE (b)-[:HAS_CHILD]->(a)",
                        new { id = textId, child = childText })).ConsumeAsync();
                await Checkpoint("cycle-installed-" + provider.ProviderName);
                var adapter = (ICosmicQueryStorageProvider)provider;
                var scalar = (ICosmicQueryScalarStorageProvider)provider;
                var source = CosmicQuery<Holon>.Create().Where(row => row.Id == id).Result;
                var bounds = CosmicQueryTraversalOptions.Create(2, 2).Result;
                var shallow = CosmicQueryTraversalOptions.Create(1, 2).Result;
                async Task<NextGenSoftware.OASIS.Common.OASISResult<T>> Read<T>(CosmicScalarQuery<Holon, T> query)
                {
                    var plan = CosmicScalarPlanner.Plan(query, adapter.QueryProviderType,
                        new[] { new CosmicScalarTarget(adapter.QueryProviderType, adapter.QueryCapabilities, scalar.ScalarCapabilities) });
                    Assert.IsFalse(plan.IsError, plan.Message);
                    return await scalar.ExecuteCosmicScalarAsync(plan.Result);
                }
                var baseline = await Read(source.LongCount().Result);
                Assert.IsFalse(baseline.IsError, baseline.Message);
                Assert.AreEqual(1L, baseline.Result);
                var cases = mixed ? new[] {
                    source.All(row => row.Children.Any(child => CosmicQueryRelations.AnyDescendant(child, descendant => true, bounds))),
                    source.All(row => row.Children.Any(child => child.Name == "not this child" && CosmicQueryRelations.AnyDescendant(child, descendant => true, bounds))),
                    source.All(row => row.Children.Any(child => child.Name == "owned All mixed cycle child" || CosmicQueryRelations.AnyDescendant(child, descendant => true, bounds))),
                    source.All(row => !row.Children.Any(child => CosmicQueryRelations.AnyDescendant(child, descendant => true, bounds))),
                    source.All(row => row.Name == "not this root" && row.Children.Any(child => CosmicQueryRelations.AnyDescendant(child, descendant => true, bounds))),
                    source.All(row => row.Id == id || row.Children.Any(child => CosmicQueryRelations.AnyDescendant(child, descendant => true, bounds))),
                    source.WhereAnyDescendant(child => true, bounds).Take(0).All(row => row.Children.Any(child => CosmicQueryRelations.AnyDescendant(child, descendant => true, bounds)))
                } : new[] {
                    source.All(row => CosmicQueryRelations.AnyDescendant(row, child => true, bounds)),
                    source.All(row => row.Name == "not this root" && CosmicQueryRelations.AnyDescendant(row, child => true, bounds)),
                    source.All(row => row.Id == id || CosmicQueryRelations.AnyDescendant(row, child => true, bounds)),
                    source.All(row => !CosmicQueryRelations.AnyDescendant(row, child => true, bounds)),
                    source.WhereAnyDescendant(child => true, bounds).Take(0).All(row => true),
                    source.All(row => row.Children.Any(child => true) || CosmicQueryRelations.AnyDescendant(row, child => true, bounds)) };
                if (mixed)
                    cases = cases.Concat(new[] {
                        source.All(row => CosmicQueryRelations.AnyDescendant(row,
                            child => CosmicQueryRelations.AnyDescendant(child, descendant => true, bounds), shallow)),
                        source.All(row => CosmicQueryRelations.AnyDescendant(row,
                            child => child.Name == "owned All mixed cycle child" || CosmicQueryRelations.AnyDescendant(child, descendant => true, bounds), shallow)),
                        source.All(row => CosmicQueryRelations.AnyDescendant(row,
                            child => child.Name == "never" && CosmicQueryRelations.AnyDescendant(child, descendant => true, bounds), shallow)),
                        source.All(row => CosmicQueryRelations.AnyDescendant(row,
                            child => child.Children.Any(grandchild => CosmicQueryRelations.AnyDescendant(grandchild, descendant => true, bounds)), shallow))
                    }).ToArray();
                foreach (var query in cases)
                {
                    Assert.IsFalse(query.IsError, query.Message);
                    var rejected = await Read(query.Result);
                    Assert.IsTrue(rejected.IsError, provider.ProviderName + ": cycle was published as a value");
                    Assert.AreEqual("CQL_DATA_INVALID", rejected.ErrorCode, rejected.Message);
                    Assert.IsFalse(rejected.IsLoaded);
                }
                var emptyQuery = mixed
                    ? source.Take(0).All(row => row.Children.Any(child => CosmicQueryRelations.AnyDescendant(child, descendant => true, bounds)))
                    : source.Take(0).All(row => CosmicQueryRelations.AnyDescendant(row, child => true, bounds));
                var empty = await Read(emptyQuery.Result);
                Assert.IsFalse(empty.IsError, empty.Message);
                Assert.IsTrue(empty.Result);
                if (mixed)
                {
                    var secured = source.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
                    var hidden = await Read(secured.All(row => row.Children.Any(child =>
                        CosmicQueryRelations.AnyDescendant(child, descendant => true, bounds))).Result);
                    Assert.IsFalse(hidden.IsError, hidden.Message);
                    Assert.IsFalse(hidden.Result);
                    var reverseHidden = await Read(secured.All(row => CosmicQueryRelations.AnyDescendant(row,
                        child => CosmicQueryRelations.AnyDescendant(child, descendant => true, bounds), shallow)).Result);
                    Assert.IsFalse(reverseHidden.IsError, reverseHidden.Message);
                    Assert.IsFalse(reverseHidden.Result);
                }
                proofs.Add(new { provider = provider.ProviderName, cycleErrors = cases.Length, ordinaryCount = 1,
                    emptyTerminal = true, hiddenChildExcluded = mixed, reverseHiddenScopeExcluded = mixed });
                complete = true;
            }
            finally
            {
                try
                {
                    if (complete)
                    {
                        await Checkpoint("before-exact-cleanup-" + provider.ProviderName);
                        if (provider is MongoProvider) Assert.AreEqual((long)ownedIds.Length, (await records.DeleteManyAsync(mongoId)).DeletedCount);
                        else if (provider is SqliteProvider)
                        {
                            await using var remove = sql.CreateCommand();
                            remove.CommandText = "DELETE FROM Holon WHERE Id IN ($id,$child)";
                            remove.Parameters.AddWithValue("$id", textId);
                            remove.Parameters.AddWithValue("$child", childText);
                            await remove.ExecuteNonQueryAsync();
                            Assert.AreEqual(0L, await Sql("SELECT count(*) FROM Holon WHERE Id IN ($id,$child)"));
                        }
                        else await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = ownedIds })).ConsumeAsync();
                        states.Add(new { provider = provider.ProviderName, removed = true });
                        await Checkpoint("exact-cleanup-completed-" + provider.ProviderName);
                    }
                }
                finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
            }
        }
        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(originalPath));
        Assert.AreEqual(0L, await records.CountDocumentsAsync(mongoId));
        Assert.AreEqual(0L, await Sql("SELECT count(*) FROM Holon WHERE Id IN ($id,$child)"));
        Assert.AreEqual(0L, (await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids RETURN count(h) AS total", new { ids = ownedIds })).SingleAsync())["total"].As<long>());
        await File.WriteAllTextAsync(Path.Combine(work, mixed ? "cql-all-mixed-cycle-result.json" : "cql-all-cycle-result.json"), JsonSerializer.Serialize(new { id, ids = ownedIds, checks = mixed ? 45 : 24, providers = proofs, exactCleanupVerified = true, originalFixtureUnchanged = true }));
        File.Delete(journal);
    }
}
