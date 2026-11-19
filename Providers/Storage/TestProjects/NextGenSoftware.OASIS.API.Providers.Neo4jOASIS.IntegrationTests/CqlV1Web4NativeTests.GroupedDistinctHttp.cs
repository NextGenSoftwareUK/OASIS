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
        IOASISStorageProvider provider, string work, string database, MongoClient? mongo, List<Guid> created,
        bool guidCounts = false, Dictionary<string, long>? queryTimings = null)
    {
        var reader = (INativeDataProjectionQueryProvider)provider;
        var actor = Guid.NewGuid();
        var mongoProvider = provider as MongoProvider;
        var sqliteProvider = provider as SqliteProvider;
        var neoProvider = provider as Neo4jOASIS;
        var previousMongo = mongoProvider?.NativeImportDatabaseName;
        var nativeMongoDatabase = database + "_native_http";
        var ownsMongoSource = false;
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
                using (var names = await mongo.ListDatabaseNamesAsync())
                    Assert.IsFalse((await names.ToListAsync()).Contains(nativeMongoDatabase),
                        "Never overwrite an existing native source database.");
                ownsMongoSource = true;
                mongoProvider.NativeImportDatabaseName = nativeMongoDatabase;
                var source = mongo.GetDatabase(nativeMongoDatabase).GetCollection<BsonDocument>(sourceName);
                var nativeRows = new[] {
                    new BsonDocument { { "_id", 1 }, { "text", "😀" } },
                    new BsonDocument { { "_id", 2 }, { "text", "abc\0def" } }
                };
                if (guidCounts)
                {
                    nativeRows[0].Add("identity", MixedGuidFirst.ToString("D").ToUpperInvariant());
                    nativeRows[1].Add("identity", MixedGuidSecond.ToString("D"));
                }
                await source.InsertManyAsync(nativeRows);
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
                    command.CommandText = guidCounts
                        ? "CREATE TABLE items(id INTEGER PRIMARY KEY,text TEXT,identity TEXT); INSERT INTO items VALUES(1,$emoji,$first),(2,$nul,$second)"
                        : "CREATE TABLE items(id INTEGER PRIMARY KEY,text TEXT); INSERT INTO items VALUES(1,$emoji),(2,$nul)";
                    if (guidCounts)
                    {
                        command.Parameters.AddWithValue("$first", MixedGuidFirst.ToString("D").ToUpperInvariant());
                        command.Parameters.AddWithValue("$second", MixedGuidSecond.ToString("D"));
                    }
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
                if (guidCounts)
                {
                    await (await inspection.RunAsync(
                        $"CREATE (:{label} {{text:$emoji,identity:$first}}), (:{label} {{text:$nul,identity:$second}})",
                        new { emoji = "😀", nul = "abc\0def", first = MixedGuidFirst.ToString("D").ToUpperInvariant(),
                            second = MixedGuidSecond.ToString("D") })).ConsumeAsync();
                    fingerprint = async () => (await (await inspection.RunAsync(
                        $"MATCH (n:{label}) RETURN elementId(n) AS key,n.text AS text,n.identity AS identity ORDER BY key")).ToListAsync())
                        .Select(row => JsonSerializer.SerializeToUtf8Bytes(new { key = row["key"].As<string>(),
                            text = row["text"].As<string>(), identity = row["identity"].As<string>() })).ToArray();
                }
                else
                {
                    await (await inspection.RunAsync($"CREATE (:{label} {{text:$emoji}}), (:{label} {{text:$nul}})",
                        new { emoji = "😀", nul = "abc\0def" })).ConsumeAsync();
                    fingerprint = async () => (await (await inspection.RunAsync(
                        $"MATCH (n:{label}) RETURN elementId(n) AS key,n.text AS text ORDER BY key")).ToListAsync())
                        .Select(row => JsonSerializer.SerializeToUtf8Bytes(new { key = row["key"].As<string>(), text = row["text"].As<string>() })).ToArray();
                }
            }
            var before = await fingerprint();
            var mappedFields = new Dictionary<string, string> { ["textAlias"] = "text" };
            if (guidCounts) mappedFields.Add("guidAlias", "identity");
            var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, actor, reader.NativeDataProviderType,
                sourceName, VirtualHolonMapping.SandboxConnectionName, reader.DescribeVirtualQueryConnection(null), mappedFields);
            var original = await mapping.QueryAsync(actor, reader, null,
                new[] { new NativeRecordFilter("textAlias", CosmicQueryComparisonOperator.Equal, 2L, NativeRecordFunction.Utf16Length) },
                guidCounts ? new[] { "textAlias", "guidAlias" } : new[] { "textAlias" }, 1);
            Assert.IsFalse(original.IsError, original.Message + original.Exception);
            Assert.AreEqual(1, original.Result.Count);
            await VerifyMappedNativeHttpAsync(provider, mapping, original.Result.Single(),
                groupedDistinctOnly: true, createdObserver: created.Add,
                guidDistinct: guidCounts, queryTimings: queryTimings);
            var after = await fingerprint();
            Assert.AreEqual(before.Length, after.Length);
            for (var index = 0; index < before.Length; index++) CollectionAssert.AreEqual(before[index], after[index]);
            return 3;
        }
        finally
        {
            if (mongoProvider != null)
            {
                mongoProvider.NativeImportDatabaseName = previousMongo;
                if (ownsMongoSource)
                {
                    Assert.IsNotNull(mongo);
                    Assert.IsTrue(nativeMongoDatabase.StartsWith("cosmic_distinct_", StringComparison.Ordinal));
                    await mongo.DropDatabaseAsync(nativeMongoDatabase);
                    using var names = await mongo.ListDatabaseNamesAsync();
                    Assert.IsFalse((await names.ToListAsync()).Contains(nativeMongoDatabase));
                }
            }
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
