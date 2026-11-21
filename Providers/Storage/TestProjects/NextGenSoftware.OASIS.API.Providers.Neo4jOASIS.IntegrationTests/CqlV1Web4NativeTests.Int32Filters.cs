using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task Int32MappedFiltersUseActualNativeDomainAndRejectWideIntegers(string providerName)
        => RunGroupedDistinctNativeFixture(providerName, false, int32FilterNative: true);

    private static async Task<int> VerifyNativeInt32Filters(IOASISStorageProvider provider,
        string work, string database, MongoClient? mongo, Dictionary<string, long> timings)
    {
        var reader = (INativeDataProjectionQueryProvider)provider;
        var mp = provider as MongoProvider;
        var sp = provider as SqliteProvider;
        var np = provider as Neo4jOASIS;
        var priorMongo = mp?.NativeImportDatabaseName;
        var priorSqlite = sp?.NativeImportConnectionString;
        var priorLabels = np?.NativeImportLabels;
        var nativeDb = database + "_int32_native";
        var sqlitePath = Path.GetFullPath(Path.Combine(work, "int32-native.sqlite"));
        var label = "Int32Native_" + Guid.NewGuid().ToString("N");
        var sourceName = np == null ? "items" : label;
        var ownedMongo = false; var ownedSqlite = false; var ownedNeo = false;
        using var graph = np == null ? null : GraphDatabase.Driver(
            "bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var session = graph?.AsyncSession();
        object?[] values = { int.MinValue, 0, int.MaxValue, (long)int.MinValue - 1,
            (long)int.MaxValue + 1, "not-an-integer", null };
        var rows = values.Select((value, index) => new Dictionary<string, object?> {
            ["key"] = index.ToString(System.Globalization.CultureInfo.InvariantCulture), ["stock"] = value
        }).Append(new Dictionary<string, object?> { ["key"] = "missing" }).ToArray();
        Func<Task<byte[][]>> fingerprint;
        try
        {
            if (mp != null)
            {
                Assert.IsNotNull(mongo);
                using (var names = await mongo.ListDatabaseNamesAsync())
                    Assert.IsFalse((await names.ToListAsync()).Contains(nativeDb));
                ownedMongo = true;
                mp.NativeImportDatabaseName = nativeDb;
                var collection = mongo.GetDatabase(nativeDb).GetCollection<BsonDocument>("items");
                await collection.InsertManyAsync(rows.Select(row => {
                    var doc = new BsonDocument("_id", (string)row["key"]!);
                    if (row.TryGetValue("stock", out var value))
                        doc.Add("stock", value == null ? BsonNull.Value : BsonValue.Create(value));
                    return doc;
                }));
                fingerprint = async () => (await collection.Find(FilterDefinition<BsonDocument>.Empty)
                    .Sort(new BsonDocument("_id", 1)).ToListAsync()).Select(row => row.ToBson()).ToArray();
            }
            else if (sp != null)
            {
                Assert.AreEqual(Path.GetFullPath(work), Path.GetDirectoryName(sqlitePath));
                Assert.IsFalse(File.Exists(sqlitePath));
                ownedSqlite = true;
                var connection = "Data Source=" + sqlitePath + ";Pooling=False";
                await using (var native = new SqliteConnection(connection))
                {
                    await native.OpenAsync();
                    await using var create = native.CreateCommand();
                    create.CommandText = "CREATE TABLE items(id TEXT PRIMARY KEY,stock)";
                    await create.ExecuteNonQueryAsync();
                    foreach (var row in rows)
                    {
                        await using var insert = native.CreateCommand();
                        insert.CommandText = row.ContainsKey("stock")
                            ? "INSERT INTO items(id,stock) VALUES($key,$stock)" : "INSERT INTO items(id) VALUES($key)";
                        insert.Parameters.AddWithValue("$key", row["key"]!);
                        if (row.TryGetValue("stock", out var value))
                            insert.Parameters.AddWithValue("$stock", value ?? DBNull.Value);
                        await insert.ExecuteNonQueryAsync();
                    }
                }
                sp.NativeImportConnectionString = connection;
                fingerprint = async () => {
                    await using var native = new SqliteConnection(connection);
                    await native.OpenAsync();
                    await using var command = native.CreateCommand();
                    command.CommandText = "SELECT id,typeof(stock),stock FROM items ORDER BY id";
                    await using var data = await command.ExecuteReaderAsync();
                    var result = new List<byte[]>();
                    while (await data.ReadAsync())
                        result.Add(JsonSerializer.SerializeToUtf8Bytes(new {
                            key = data.GetString(0), kind = data.GetString(1),
                            stock = data.IsDBNull(2) ? null : data.GetValue(2)
                        }));
                    return result.ToArray();
                };
            }
            else
            {
                Assert.IsNotNull(np); Assert.IsNotNull(session);
                var absent = await (await session.RunAsync($"MATCH (n:{label}) RETURN count(n) AS total")).SingleAsync();
                Assert.AreEqual(0L, absent["total"].As<long>());
                ownedNeo = true;
                await (await session.RunAsync($"UNWIND $rows AS row CREATE (n:{label}) SET n = row", new { rows })).ConsumeAsync();
                np.NativeImportLabels = label;
                fingerprint = async () => (await (await session.RunAsync(
                    $"MATCH (n:{label}) RETURN n.key AS key,n.stock AS stock ORDER BY n.key")).ToListAsync())
                    .Select(row => JsonSerializer.SerializeToUtf8Bytes(new {
                        key = row["key"].As<string>(), stock = row["stock"]
                    })).ToArray();
            }
            var before = await fingerprint();
            Assert.AreEqual(8, before.Length);
            var actor = Guid.NewGuid();
            var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, actor, reader.NativeDataProviderType,
                sourceName, VirtualHolonMapping.SandboxConnectionName, reader.DescribeVirtualQueryConnection(null),
                new Dictionary<string, string> { ["stockAlias"] = "stock" });
            var columns = new[] { new NativeProjectionColumn("stockAlias", "Stock", typeof(int), false) };
            var cases = new[] {
                (CqlV1Operator.Equal, 0, new[] { 0 }),
                (CqlV1Operator.NotEqual, 0, new[] { int.MinValue, int.MaxValue }),
                (CqlV1Operator.GreaterThan, 0, new[] { int.MaxValue }),
                (CqlV1Operator.GreaterThanOrEqual, 0, new[] { 0, int.MaxValue }),
                (CqlV1Operator.LessThan, 0, new[] { int.MinValue }),
                (CqlV1Operator.LessThanOrEqual, 0, new[] { int.MinValue, 0 }),
                (CqlV1Operator.GreaterThan, int.MaxValue, Array.Empty<int>()),
                (CqlV1Operator.LessThan, int.MinValue, Array.Empty<int>())
            };
            var projectedIds = new HashSet<Guid>();
            for (var index = 0; index < cases.Length; index++)
            {
                var (operation, value, expected) = cases[index];
                var filters = CqlV1Compiler.CompileVirtualMatches(new[] { new CqlV1Filter {
                    MetadataPath = new[] { "stockAlias" }, Scalar = CqlV1Scalar.Int32,
                    Operator = operation, Value = JsonSerializer.SerializeToElement(value)
                } });
                Assert.AreEqual(typeof(int), filters.Single().InputType);
                var clock = System.Diagnostics.Stopwatch.StartNew();
                var result = await mapping.QueryProjectionAsync(actor, reader, null, filters, columns, 10);
                timings["query" + index] = clock.ElapsedMilliseconds;
                Assert.IsFalse(result.IsError, result.Message + result.Exception);
                Assert.IsTrue(result.Result.All(row => row.Cells.Count == 1 &&
                    row.Cells[0].State == (NativeProjectionValueState)2 && row.Cells[0].Value is int));
                CollectionAssert.AreEquivalent(expected, result.Result.Select(row => (int)row.Cells[0].Value!).ToArray());
                foreach (var row in result.Result) projectedIds.Add(row.Id);
            }
            Assert.AreEqual(3, projectedIds.Count);
            var denied = await mapping.QueryProjectionAsync(Guid.NewGuid(), reader, null,
                new[] { new NativeRecordFilter("stockAlias", CosmicQueryComparisonOperator.Equal, 0) }, columns, 10);
            Assert.IsTrue(denied.IsError);
            Assert.AreEqual("VIRTUAL_HOLON_FORBIDDEN", denied.ErrorCode);
            foreach (var id in projectedIds)
                Assert.IsNull((await provider.LoadHolonAsync(id)).Result, "Native projection must not materialize a holon.");
            var after = await fingerprint();
            Assert.AreEqual(before.Length, after.Length);
            for (var index = 0; index < before.Length; index++)
                CollectionAssert.AreEqual(before[index], after[index]);
            return 9;
        }
        finally
        {
            if (mp != null) mp.NativeImportDatabaseName = priorMongo;
            if (sp != null) sp.NativeImportConnectionString = priorSqlite;
            if (np != null) np.NativeImportLabels = priorLabels;
            if (ownedMongo)
            {
                Assert.IsNotNull(mongo);
                Assert.IsTrue(nativeDb.StartsWith("cosmic_distinct_", StringComparison.Ordinal));
                await mongo.DropDatabaseAsync(nativeDb);
                using var names = await mongo.ListDatabaseNamesAsync();
                Assert.IsFalse((await names.ToListAsync()).Contains(nativeDb));
            }
            if (ownedSqlite)
            {
                Assert.AreEqual(Path.GetFullPath(work), Path.GetDirectoryName(sqlitePath));
                foreach (var file in new[] { sqlitePath, sqlitePath + "-wal", sqlitePath + "-shm" })
                {
                    if (File.Exists(file)) File.Delete(file);
                    Assert.IsFalse(File.Exists(file));
                }
            }
            if (ownedNeo)
            {
                Assert.IsNotNull(session);
                Assert.IsTrue(label.StartsWith("Int32Native_", StringComparison.Ordinal));
                await (await session.RunAsync($"MATCH (n:{label}) DETACH DELETE n")).ConsumeAsync();
                var remaining = await (await session.RunAsync($"MATCH (n:{label}) RETURN count(n) AS total")).SingleAsync();
                Assert.AreEqual(0L, remaining["total"].As<long>());
            }
        }
    }
}
