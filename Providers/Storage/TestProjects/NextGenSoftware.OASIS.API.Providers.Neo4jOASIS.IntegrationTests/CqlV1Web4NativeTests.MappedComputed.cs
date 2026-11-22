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
    public Task MappedComputedColumnsUseRealNativeFunctionsAndLiteralIdentity(string providerName)
        => RunGroupedDistinctNativeFixture(providerName, false, mappedComputedJourney: true);

    [TestMethod]
    public Task MongoMappedComputedColumnsUseAdmittedNativeDatabaseName()
        => RunGroupedDistinctNativeFixture("MongoDBOASIS", false, mappedComputedJourney: true);

    private static async Task<int> VerifyMappedComputedNative(IOASISStorageProvider provider,
        string work, string database, MongoClient? mongo, Dictionary<string, long> timings, List<Guid> created)
    {
        var reader = (INativeDataProjectionQueryProvider)provider;
        var mp = provider as MongoProvider;
        var sp = provider as SqliteProvider;
        var np = provider as Neo4jOASIS;
        var priorMongo = mp?.NativeImportDatabaseName;
        var priorSqlite = sp?.NativeImportConnectionString;
        var priorLabels = np?.NativeImportLabels;
        var nativeDb = database + "_cmp";
        Assert.IsTrue(nativeDb.Length <= 63, "Owned Mongo database names must fit the native contract.");
        var sqlitePath = Path.GetFullPath(Path.Combine(work, "computed-native.sqlite"));
        var label = "ComputedNative_" + Guid.NewGuid().ToString("N");
        var sourceName = np == null ? "items" : label;
        var ownedMongo = false; var ownedSqlite = false; var ownedNeo = false;
        using var graph = np == null ? null : GraphDatabase.Driver(
            "bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var session = graph?.AsyncSession();
        var rows = new[] {
            new Dictionary<string, object?> { ["key"]="null", ["stock"]=null, ["text"]=null, ["flag"]=true },
            new Dictionary<string, object?> { ["key"]="empty", ["stock"]=0, ["text"]="", ["flag"]=false },
            new Dictionary<string, object?> { ["key"]="bad", ["stock"]="invalid", ["text"]="ok", ["flag"]=true }
        };
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
                    foreach (var field in new[] { "stock", "text", "flag" })
                        doc.Add(field, row[field] == null ? BsonNull.Value : BsonValue.Create(row[field]));
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
                    create.CommandText = "CREATE TABLE items(id TEXT PRIMARY KEY,stock,text,flag)";
                    await create.ExecuteNonQueryAsync();
                    foreach (var row in rows)
                    {
                        await using var insert = native.CreateCommand();
                        insert.CommandText = "INSERT INTO items(id,stock,text,flag) VALUES($key,$stock,$text,$flag)";
                        insert.Parameters.AddWithValue("$key", row["key"]!);
                        foreach (var field in new[] { "stock", "text", "flag" })
                            insert.Parameters.AddWithValue("$" + field, row[field] ?? DBNull.Value);
                        await insert.ExecuteNonQueryAsync();
                    }
                }
                sp.NativeImportConnectionString = connection;
                fingerprint = async () => {
                    await using var native = new SqliteConnection(connection);
                    await native.OpenAsync();
                    await using var command = native.CreateCommand();
                    command.CommandText = "SELECT id,typeof(stock),stock,text,flag FROM items ORDER BY id";
                    await using var data = await command.ExecuteReaderAsync();
                    var result = new List<byte[]>();
                    while (await data.ReadAsync())
                        result.Add(JsonSerializer.SerializeToUtf8Bytes(new {
                            key = data.GetString(0), kind = data.GetString(1),
                            stock = data.IsDBNull(2) ? null : data.GetValue(2), text = data.IsDBNull(3) ? null : data.GetValue(3), flag = data.GetValue(4)
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
                    $"MATCH (n:{label}) RETURN n.key AS key,n.stock AS stock,n.text AS text,n.flag AS flag ORDER BY n.key")).ToListAsync())
                    .Select(row => JsonSerializer.SerializeToUtf8Bytes(new {
                        key = row["key"].As<string>(), stock = row["stock"], text = row["text"], flag = row["flag"]
                    })).ToArray();
            }
            var before = await fingerprint();
            Assert.AreEqual(3, before.Length);
            var actor = Guid.NewGuid();
            var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, actor, reader.NativeDataProviderType,
                sourceName, VirtualHolonMapping.SandboxConnectionName, reader.DescribeVirtualQueryConnection(null),
                new Dictionary<string, string> { ["stockAlias"]="stock", ["textAlias"]="text",
                    ["flagAlias"]="flag", ["keyAlias"]=mp != null ? "_id" : sp != null ? "id" : "key" });
            var request = new CqlProjectionV1Request {
                VirtualMappingId=mapping.Id, OrderBy="Id", Take=10,
                Columns=new[] {
                    new CqlProjectionV1Column { Field="Id", Alias="Id" },
                    new CqlProjectionV1Column { MetadataPath=new[] {"textAlias"}, ValueType="String",
                        Alias="Text", Function=CqlV1Function.TextCoalesce, NullReplacement="$literal" },
                    new CqlProjectionV1Column { MetadataPath=new[] {"textAlias"}, ValueType="String",
                        Alias="Other", Function=CqlV1Function.TextCoalesce, NullReplacement="other" },
                    new CqlProjectionV1Column { MetadataPath=new[] {"flagAlias"}, ValueType="Boolean",
                        Alias="Label", Function=CqlV1Function.BooleanTextCase, WhenTrue="yes", WhenFalse="no" },
                    new CqlProjectionV1Column { MetadataPath=new[] {"stockAlias"}, ValueType="Int32",
                        Alias="Stock", Function=CqlV1Function.Int32Coalesce, Nullable=true, Int32Replacement=7 }
                }
            };
            var compiled = CqlV1Compiler.CompileVirtualProjection(request);
            var filters = new[] {new NativeRecordFilter("keyAlias", CosmicQueryComparisonOperator.NotEqual, "bad")};
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var result = await mapping.QueryProjectionAsync(actor, reader, null, filters, compiled.Columns, 10);
            timings["computed"]=clock.ElapsedMilliseconds;
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(2, result.Result.Count);
            var cells = result.Result.Select(row => compiled.Select(row)).ToArray();
            Assert.IsTrue(cells.All(row => row[0] is Guid && row[4] is int));
            var nullRow = cells.Single(row => (string)row[3] == "yes");
            Assert.AreEqual("$literal", nullRow[1]); Assert.AreEqual("other", nullRow[2]); Assert.AreEqual(7, nullRow[4]);
            var emptyRow = cells.Single(row => (string)row[3] == "no");
            Assert.AreEqual("", emptyRow[1]); Assert.AreEqual("", emptyRow[2]); Assert.AreEqual(0, emptyRow[4]);
            foreach (var row in result.Result) {
                Assert.AreEqual(mapping.Id, row.MappingId);
                Assert.AreEqual(mapping.Version, row.MappingVersion);
                Assert.IsNull((await provider.LoadHolonAsync(row.Id)).Result, "Projection must not import data.");
            }
            var invalid = await mapping.QueryProjectionAsync(actor, reader, null,
                new[] {new NativeRecordFilter("keyAlias", CosmicQueryComparisonOperator.Equal, "bad")}, compiled.Columns, 10);
            Assert.IsTrue(invalid.IsError, "An invalid stored domain must not become the replacement literal.");
            Assert.IsNull(invalid.Result, "A refused projection must not publish partial rows.");
            var denied = await mapping.QueryProjectionAsync(Guid.NewGuid(), reader, null, filters, compiled.Columns, 10);
            Assert.AreEqual("VIRTUAL_HOLON_FORBIDDEN", denied.ErrorCode);
            var original=await mapping.QueryAsync(actor,reader,null,
                new[] {new NativeRecordFilter("keyAlias",CosmicQueryComparisonOperator.Equal,"null")},
                new[] {"keyAlias","stockAlias","textAlias","flagAlias"},1);
            Assert.IsFalse(original.IsError,original.Message+original.Exception);
            var virtualOnly=result.Result.Single(row=>row.Id!=original.Result.Single().Id).Id;
            await VerifyMappedNativeHttpAsync(provider,mapping,original.Result.Single(),createdObserver:created.Add,
                queryTimings:timings,virtualOnlyId:virtualOnly,computedColumns:request.Columns);
            var after = await fingerprint();
            Assert.AreEqual(before.Length, after.Length);
            for (var index = 0; index < before.Length; index++)
                CollectionAssert.AreEqual(before[index], after[index]);
            return 7;
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
                Assert.IsTrue(label.StartsWith("ComputedNative_", StringComparison.Ordinal));
                await (await session.RunAsync($"MATCH (n:{label}) DETACH DELETE n")).ConsumeAsync();
                var remaining = await (await session.RunAsync($"MATCH (n:{label}) RETURN count(n) AS total")).SingleAsync();
                Assert.AreEqual(0L, remaining["total"].As<long>());
            }
        }
    }
}
