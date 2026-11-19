using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task GroupedDistinctMixedHttpUsesRealNativeProvider(string providerName)
        => RunGroupedDistinctNativeFixture(providerName, false, true);

    private static async Task<int> VerifyGroupedDistinctMixedNativeHttpProvider(
        IOASISStorageProvider provider, string work, string database, MongoClient? mongo, List<Guid> created)
    {
        var reader = (INativeDataProjectionQueryProvider)provider;
        var actor = Guid.NewGuid();
        var mongoProvider = provider as MongoProvider;
        var sqliteProvider = provider as SqliteProvider;
        var neoProvider = provider as Neo4jOASIS;
        var previousMongo = mongoProvider?.NativeImportDatabaseName;
        var previousSqlite = sqliteProvider?.NativeImportConnectionString;
        var previousLabels = neoProvider?.NativeImportLabels;
        var label = "DistinctHttp_" + Guid.NewGuid().ToString("N");
        var ownsNeoSource = false;
        using var graph = neoProvider == null ? null
            : GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var inspection = graph?.AsyncSession();
        var sourceName = "items";
        Func<Task<byte[][]>> fingerprint;
        try
        {
            if (mongoProvider != null)
            {
                Assert.IsNotNull(mongo);
                mongoProvider.NativeImportDatabaseName = database;
                var source = mongo.GetDatabase(database).GetCollection<BsonDocument>(sourceName);
                await source.InsertManyAsync(new[] {
                    new BsonDocument { { "_id", 1 }, { "text", "😀" } },
                    new BsonDocument { { "_id", 2 }, { "text", "abc\0def" } }
                });
                fingerprint = async () => (await source.Find(FilterDefinition<BsonDocument>.Empty)
                    .Sort(new BsonDocument("_id", 1)).ToListAsync()).Select(row => row.ToBson()).ToArray();
            }
            else if (sqliteProvider != null)
            {
                var path = Path.GetFullPath(Path.Combine(work, "native-http.sqlite"));
                Assert.AreEqual(Path.GetFullPath(work), Path.GetDirectoryName(path));
                Assert.IsFalse(File.Exists(path));
                var connection = "Data Source=" + path + ";Pooling=False";
                await using (var native = new SqliteConnection(connection))
                {
                    await native.OpenAsync();
                    await using var command = native.CreateCommand();
                    command.CommandText = "CREATE TABLE items(id INTEGER PRIMARY KEY,text TEXT); INSERT INTO items VALUES(1,$emoji),(2,$nul)";
                    command.Parameters.AddWithValue("$emoji", "😀");
                    command.Parameters.AddWithValue("$nul", "abc\0def");
                    await command.ExecuteNonQueryAsync();
                }
                sqliteProvider.NativeImportConnectionString = connection;
                fingerprint = async () => new[] { await File.ReadAllBytesAsync(path) };
            }
            else
            {
                Assert.IsNotNull(neoProvider); Assert.IsNotNull(inspection);
                sourceName = label; neoProvider.NativeImportLabels = label;
                var prior = await (await inspection.RunAsync($"MATCH (n:{label}) RETURN count(n) AS n")).SingleAsync();
                Assert.AreEqual(0L, prior["n"].As<long>());
                ownsNeoSource = true;
                await (await inspection.RunAsync($"CREATE (:{label} {{text:$emoji}}), (:{label} {{text:$nul}})",
                    new { emoji = "😀", nul = "abc\0def" })).ConsumeAsync();
                fingerprint = async () => (await (await inspection.RunAsync(
                    $"MATCH (n:{label}) RETURN elementId(n) AS key,n.text AS text ORDER BY key")).ToListAsync())
                    .Select(row => JsonSerializer.SerializeToUtf8Bytes(new { key = row["key"].As<string>(), text = row["text"].As<string>() })).ToArray();
            }
            var before = await fingerprint();
            var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, actor, reader.NativeDataProviderType,
                sourceName, VirtualHolonMapping.SandboxConnectionName, reader.DescribeVirtualQueryConnection(null),
                new Dictionary<string, string> { ["textAlias"] = "text" });
            var original = await mapping.QueryAsync(actor, reader, null,
                new[] { new NativeRecordFilter("textAlias", CosmicQueryComparisonOperator.Equal, 2L, NativeRecordFunction.Utf16Length) },
                new[] { "textAlias" }, 1);
            Assert.IsFalse(original.IsError, original.Message + original.Exception);
            Assert.AreEqual(1, original.Result.Count);
            await VerifyMappedNativeHttpAsync(provider, mapping, original.Result.Single(),
                groupedDistinctOnly: true, createdObserver: created.Add);
            var after = await fingerprint();
            Assert.AreEqual(before.Length, after.Length);
            for (var index = 0; index < before.Length; index++) CollectionAssert.AreEqual(before[index], after[index]);
            return 3;
        }
        finally
        {
            if (mongoProvider != null) mongoProvider.NativeImportDatabaseName = previousMongo;
            if (sqliteProvider != null) sqliteProvider.NativeImportConnectionString = previousSqlite;
            if (neoProvider != null)
            {
                neoProvider.NativeImportLabels = previousLabels;
                if (ownsNeoSource)
                {
                    Assert.IsNotNull(inspection);
                    await (await inspection.RunAsync($"MATCH (n:{label}) DETACH DELETE n")).ConsumeAsync();
                    var remaining = await (await inspection.RunAsync($"MATCH (n:{label}) RETURN count(n) AS n")).SingleAsync();
                    Assert.AreEqual(0L, remaining["n"].As<long>());
                }
            }
        }
    }
}
