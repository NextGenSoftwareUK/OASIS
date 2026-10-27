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
    public async Task NativeAllCycleErrorsPreserveOwnedFixtureAndCannotBeShortCircuited()
    {
        var work = Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!;
        var originalPath = Path.Combine(work, "cql-v1-web4-fixture.json");
        var original = await File.ReadAllBytesAsync(originalPath);
        Assert.AreEqual("892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5", Convert.ToHexString(SHA256.HashData(original)));
        var journal = Path.Combine(work, "cql-all-cycle-fixture.json");
        Assert.IsFalse(File.Exists(journal), "Preserve any prior cycle outcome; never replay or reset it.");
        var id = Guid.NewGuid();
        var textId = id.ToString("D");
        var states = new List<object>();
        async Task Checkpoint(string stage) => await File.WriteAllTextAsync(journal,
            JsonSerializer.Serialize(new { id, stage, states, owner = nameof(NativeAllCycleErrorsPreserveOwnedFixtureAndCannotBeShortCircuited) }));
        await Checkpoint("before-native-preflight");

        const string mongoUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        var records = new MongoClient(mongoUri).GetDatabase("cosmic_cql_v1_web4_storage").GetCollection<BsonDocument>("Holon");
        var mongoId = new BsonDocument("HolonId", textId);
        Assert.AreEqual(0L, await records.CountDocumentsAsync(mongoId));
        await using var sql = new SqliteConnection("Data Source=" + Path.Combine(work, "cql-v1-web4.sqlite"));
        await sql.OpenAsync();
        async Task<long> Sql(string statement)
        {
            await using var command = sql.CreateCommand();
            command.CommandText = statement;
            command.Parameters.AddWithValue("$id", textId);
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }
        Assert.AreEqual(0L, await Sql("SELECT count(*) FROM Holon WHERE Id=$id"));
        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspection = driver.AsyncSession();
        var absent = await (await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN count(h) AS total", new { id = textId })).SingleAsync();
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
                states.Add(new { provider = provider.ProviderName, created = true });
                await Checkpoint("before-cycle-" + provider.ProviderName);

                // Corrupt only this separately owned synthetic record. Keep the
                // canonical payload/projection consistent so a typed cycle error
                // cannot be confused with missing membership or a bad projection.
                if (provider is MongoProvider)
                {
                    var projection = new BsonDocument { { "schema", 1 }, { "owner", textId }, { "source", BsonNull.Value },
                        { "declared", false }, { "members", new BsonArray() } };
                    var patch = new BsonDocument("$set", new BsonDocument { { "ParentHolonId", textId },
                        { "ChildRoleManifestJson", BsonNull.Value }, { "CosmicCqlMembership", projection } });
                    Assert.AreEqual(1L, (await records.UpdateOneAsync(mongoId, patch)).MatchedCount);
                }
                else if (provider is SqliteProvider)
                {
                    await using var patch = sql.CreateCommand();
                    patch.CommandText = "UPDATE Holon SET ParentHolonId=$id, PersistentValueJson=json_set(json_remove(PersistentValueJson,'$.ChildRoleManifestJson'),'$.ParentHolonId',$id) WHERE Id=$id";
                    patch.Parameters.AddWithValue("$id", textId);
                    Assert.AreEqual(1, await patch.ExecuteNonQueryAsync());
                }
                else
                {
                    var observed = await (await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot", new { id = textId })).SingleAsync();
                    var canonical = StoredRecordJson.DeserializeStoredHolon(observed["snapshot"].As<string>());
                    canonical.ParentHolonId = id;
                    ((Holon)canonical).ChildRoleManifestJson = null;
                    await (await inspection.RunAsync("MATCH (h:Holon {Id:$id})-[p:HAS_CQL_PROJECTION]->(h) SET h.ParentHolonId=$id,h.SnapshotJson=$snapshot,p.f_ParentHolonId=$id,p.f_HasChildManifest=false,p.f_ChildIds=[] CREATE (h)-[:HAS_CHILD]->(h)",
                        new { id = textId, snapshot = StoredRecordJson.Serialize(canonical) })).ConsumeAsync();
                }
                await Checkpoint("cycle-installed-" + provider.ProviderName);
                var adapter = (ICosmicQueryStorageProvider)provider;
                var scalar = (ICosmicQueryScalarStorageProvider)provider;
                var source = CosmicQuery<Holon>.Create().Where(row => row.Id == id).Result;
                var bounds = CosmicQueryTraversalOptions.Create(2, 2).Result;
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
                var cases = new[] {
                    source.All(row => CosmicQueryRelations.AnyDescendant(row, child => true, bounds)),
                    source.All(row => row.Name == "not this root" && CosmicQueryRelations.AnyDescendant(row, child => true, bounds)),
                    source.All(row => row.Id == id || CosmicQueryRelations.AnyDescendant(row, child => true, bounds)),
                    source.All(row => !CosmicQueryRelations.AnyDescendant(row, child => true, bounds)),
                    source.WhereAnyDescendant(child => true, bounds).Take(0).All(row => true),
                    source.All(row => row.Children.Any(child => true) || CosmicQueryRelations.AnyDescendant(row, child => true, bounds)) };
                foreach (var query in cases)
                {
                    Assert.IsFalse(query.IsError, query.Message);
                    var rejected = await Read(query.Result);
                    Assert.IsTrue(rejected.IsError, provider.ProviderName + ": cycle was published as a value");
                    Assert.AreEqual("CQL_DATA_INVALID", rejected.ErrorCode, rejected.Message);
                    Assert.IsFalse(rejected.IsLoaded);
                }
                var empty = await Read(source.Take(0).All(row => CosmicQueryRelations.AnyDescendant(row, child => true, bounds)).Result);
                Assert.IsFalse(empty.IsError, empty.Message);
                Assert.IsTrue(empty.Result);
                proofs.Add(new { provider = provider.ProviderName, cycleErrors = cases.Length, ordinaryCount = 1, emptyTerminal = true });
                complete = true;
            }
            finally
            {
                try
                {
                    if (complete)
                    {
                        await Checkpoint("before-exact-cleanup-" + provider.ProviderName);
                        if (provider is MongoProvider) Assert.AreEqual(1L, (await records.DeleteOneAsync(mongoId)).DeletedCount);
                        else if (provider is SqliteProvider)
                        {
                            await using var remove = sql.CreateCommand();
                            remove.CommandText = "DELETE FROM Holon WHERE Id=$id";
                            remove.Parameters.AddWithValue("$id", textId);
                            Assert.AreEqual(1, await remove.ExecuteNonQueryAsync());
                        }
                        else await (await inspection.RunAsync("MATCH (h:Holon {Id:$id}) DETACH DELETE h", new { id = textId })).ConsumeAsync();
                        states.Add(new { provider = provider.ProviderName, removed = true });
                        await Checkpoint("exact-cleanup-completed-" + provider.ProviderName);
                    }
                }
                finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
            }
        }
        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(originalPath));
        Assert.AreEqual(0L, await records.CountDocumentsAsync(mongoId));
        Assert.AreEqual(0L, await Sql("SELECT count(*) FROM Holon WHERE Id=$id"));
        Assert.AreEqual(0L, (await (await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN count(h) AS total", new { id = textId })).SingleAsync())["total"].As<long>());
        await File.WriteAllTextAsync(Path.Combine(work, "cql-all-cycle-result.json"), JsonSerializer.Serialize(new { id, checks = 24, providers = proofs, exactCleanupVerified = true, originalFixtureUnchanged = true }));
        File.Delete(journal);
    }
}
