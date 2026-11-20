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
    [DataRow("MongoDBOASIS")]
    public Task UtcDateMappedFiltersPreserveTicksAndNativeTemporalValues(string providerName)
        => RunGroupedDistinctNativeFixture(providerName, false, utcDateFilterNative: true);

    private static async Task<int> VerifyNativeUtcDates(IOASISStorageProvider provider,
        string work, string database, MongoClient? mongo, Dictionary<string, long> timings)
    {
        var reader = (INativeDataProjectionQueryProvider)provider;
        var mp = provider as MongoProvider;
        var sp = provider as SqliteProvider;
        var np = provider as Neo4jOASIS;
        var priorMongo = mp?.NativeImportDatabaseName;
        var priorSqlite = sp?.NativeImportConnectionString;
        var priorLabels = np?.NativeImportLabels;
        var nativeDb = database + "_utc";
        Assert.IsTrue(nativeDb.Length <= 63, "Owned Mongo fixture database name must fit the native limit.");
        var sqlitePath = Path.GetFullPath(Path.Combine(work, "utc-date-native.sqlite"));
        var label = "UtcDateNative_" + Guid.NewGuid().ToString("N");
        var sourceName = np == null ? "items" : label;
        var ownedMongo = false; var ownedSqlite = false; var ownedNeo = false;
        using var graph = np == null ? null : GraphDatabase.Driver(
            "bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var session = graph?.AsyncSession();
        object?[] values = {
            "2000-01-01T00:00:00Z", "2000-01-01T00:00:00.0000001Z",
            "2000-01-01T00:00:00.0000002Z", "2000-01-01T00:00:00.0000000Z",
            "0001-01-01T00:00:00Z", "9999-12-31T23:59:59.9999999Z",
            "2000-02-29T12:30:45.1234567Z", "2000-02-30T00:00:00Z",
            "1900-02-29T00:00:00Z", "2000-01-01T00:00:00+00:00",
            "2000-01-01T00:00:00.000000001Z", "", 1L, null };
        static DateTime Utc(string value) => DateTime.Parse(value,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
        var valid = values.Take(7).Select(value => Utc((string)value!)).ToList();
        var boundary = Utc("2000-01-01T00:00:00.0000001Z");
        var nativeCount = 15 + (mp != null ? 2 : np != null ? 3 : 0);
        var rows = values.Select((value, index) => new Dictionary<string, object?> {
            ["key"] = index.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["tag"] = index.ToString(System.Globalization.CultureInfo.InvariantCulture), ["stamp"] = value
        }).Append(new Dictionary<string, object?> { ["key"] = "missing", ["tag"] = "missing" }).ToArray();
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
                    var doc = new BsonDocument { { "_id", (string)row["key"]! }, { "tag", (string)row["tag"]! } };
                    if (row.TryGetValue("stamp", out var value))
                        doc.Add("stamp", value == null ? BsonNull.Value : BsonValue.Create(value));
                    return doc;
                }));
                await collection.InsertManyAsync(new[] {
                    new BsonDocument { { "_id","native-date" }, { "tag","native-date" },
                        { "stamp",new BsonDateTime(Utc("2000-01-01T00:00:00Z")) } },
                    new BsonDocument { { "_id","native-outside" }, { "tag","native-outside" },
                        { "stamp",new BsonDateTime(long.MaxValue) } }
                });
                valid.Add(Utc("2000-01-01T00:00:00Z"));
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
                    create.CommandText = "CREATE TABLE items(id TEXT PRIMARY KEY,tag TEXT,stamp)";
                    await create.ExecuteNonQueryAsync();
                    foreach (var row in rows)
                    {
                        await using var insert = native.CreateCommand();
                        insert.CommandText = row.ContainsKey("stamp")
                            ? "INSERT INTO items(id,tag,stamp) VALUES($key,$key,$stamp)" : "INSERT INTO items(id,tag) VALUES($key,$key)";
                        insert.Parameters.AddWithValue("$key", row["key"]!);
                        if (row.TryGetValue("stamp", out var value))
                            insert.Parameters.AddWithValue("$stamp", value ?? DBNull.Value);
                        await insert.ExecuteNonQueryAsync();
                    }
                }
                sp.NativeImportConnectionString = connection;
                fingerprint = async () => {
                    await using var native = new SqliteConnection(connection);
                    await native.OpenAsync();
                    await using var command = native.CreateCommand();
                    command.CommandText = "SELECT id,typeof(stamp),stamp,tag FROM items ORDER BY id";
                    await using var data = await command.ExecuteReaderAsync();
                    var result = new List<byte[]>();
                    while (await data.ReadAsync())
                        result.Add(JsonSerializer.SerializeToUtf8Bytes(new {
                            key = data.GetString(0), tag = data.GetString(3), kind = data.GetString(1),
                            stamp = data.IsDBNull(2) ? null : data.GetValue(2)
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
                await (await session.RunAsync($"CREATE (n:{label} {{key:'native-date',tag:'native-date',stamp:datetime('2000-01-01T02:00:00.0000002+02:00')}}),"
                    + $"(bad:{label} {{key:'native-nano',tag:'native-nano',stamp:datetime('2000-01-01T00:00:00.000000001Z')}}),"
                    + $"(local:{label} {{key:'native-local',tag:'native-local',stamp:localdatetime('2000-01-01T00:00:00')}})")).ConsumeAsync();
                valid.Add(Utc("2000-01-01T00:00:00.0000002Z"));
                np.NativeImportLabels = label;
                fingerprint = async () => (await (await session.RunAsync(
                    $"MATCH (n:{label}) RETURN n.key AS key,n.tag AS tag,toString(n.stamp) AS stamp,valueType(n.stamp) AS kind ORDER BY n.key")).ToListAsync())
                    .Select(row => JsonSerializer.SerializeToUtf8Bytes(new {
                        key = row["key"].As<string>(), tag = row["tag"].As<string>(), stamp = row["stamp"]?.ToString(), kind = row["kind"].As<string>()
                    })).ToArray();
            }
            var before = await fingerprint();
            Assert.AreEqual(nativeCount, before.Length);
            var actor = Guid.NewGuid();
            var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, actor, reader.NativeDataProviderType,
                sourceName, VirtualHolonMapping.SandboxConnectionName, reader.DescribeVirtualQueryConnection(null),
                new Dictionary<string, string> { ["stampAlias"] = "stamp", ["tagAlias"] = "tag" });
            var columns = new[] { new NativeProjectionColumn("stampAlias", "Stamp", typeof(DateTime), false) };
            var cases = new[] {
                (CqlV1Operator.Equal,boundary), (CqlV1Operator.NotEqual,boundary),
                (CqlV1Operator.GreaterThan,boundary), (CqlV1Operator.GreaterThanOrEqual,boundary),
                (CqlV1Operator.LessThan,boundary), (CqlV1Operator.LessThanOrEqual,boundary),
                (CqlV1Operator.Equal,Utc("2000-01-01T00:00:00Z")),
                (CqlV1Operator.Equal,Utc("2000-02-29T12:30:45.1234567Z")),
                (CqlV1Operator.GreaterThan,DateTime.SpecifyKind(DateTime.MaxValue,DateTimeKind.Utc)),
                (CqlV1Operator.LessThan,DateTime.SpecifyKind(DateTime.MinValue,DateTimeKind.Utc))
            };
            var projectedIds = new HashSet<Guid>();
            for (var index = 0; index < cases.Length; index++)
            {
                var (operation,value)=cases[index];
                var expected=valid.Where(date=>operation switch {
                    CqlV1Operator.Equal=>date==value, CqlV1Operator.NotEqual=>date!=value,
                    CqlV1Operator.GreaterThan=>date>value, CqlV1Operator.GreaterThanOrEqual=>date>=value,
                    CqlV1Operator.LessThan=>date<value, CqlV1Operator.LessThanOrEqual=>date<=value,
                    _=>throw new InvalidOperationException()
                }).ToArray();
                var filters=CqlV1Compiler.CompileVirtualMatches(new[] {new CqlV1Filter {
                    MetadataPath=new[]{"stampAlias"},Scalar=CqlV1Scalar.DateTime,
                    Operator=operation,Value=JsonSerializer.SerializeToElement(value)
                }});
                Assert.AreEqual(typeof(DateTime),filters.Single().InputType);
                Assert.AreEqual(value.Ticks,((DateTime)filters.Single().Value).Ticks);
                var clock=System.Diagnostics.Stopwatch.StartNew();
                var result=await mapping.QueryProjectionAsync(actor,reader,null,filters,columns,100);
                timings["query"+index]=clock.ElapsedMilliseconds;
                Assert.IsFalse(result.IsError,result.Message+result.Exception);
                Assert.IsTrue(result.Result.All(row=>row.Cells.Count==1 &&
                    row.Cells[0].State==NativeProjectionValueState.Present &&
                    row.Cells[0].Value is DateTime stamp && stamp.Kind==DateTimeKind.Utc));
                CollectionAssert.AreEquivalent(expected,result.Result.Select(row=>(DateTime)row.Cells[0].Value!).ToArray());
                foreach(var row in result.Result)projectedIds.Add(row.Id);
            }
            Assert.AreEqual(valid.Count,projectedIds.Count);
            var nullableColumns=new[]{new NativeProjectionColumn("stampAlias","Stamp",typeof(DateTime),true)};
            foreach(var tag in new[]{"13","missing"})
            {
                var clock=System.Diagnostics.Stopwatch.StartNew();
                var result=await mapping.QueryProjectionAsync(actor,reader,null,
                    new[]{new NativeRecordFilter("tagAlias",CosmicQueryComparisonOperator.Equal,tag)},nullableColumns,100);
                timings["nullable-"+tag]=clock.ElapsedMilliseconds;
                Assert.IsFalse(result.IsError,result.Message+result.Exception);
                Assert.AreEqual(1,result.Result.Count);
                Assert.IsTrue(result.Result.Single().Cells[0].State is NativeProjectionValueState.Null or NativeProjectionValueState.Missing);
                Assert.IsNull(result.Result.Single().Cells[0].Value);
                projectedIds.Add(result.Result.Single().Id);
                clock.Restart();
                var required=await mapping.QueryProjectionAsync(actor,reader,null,
                    new[]{new NativeRecordFilter("tagAlias",CosmicQueryComparisonOperator.Equal,tag)},columns,100);
                timings["required-"+tag]=clock.ElapsedMilliseconds;
                Assert.IsTrue(required.IsError);
                Assert.AreEqual("NATIVE_PROJECTION_DATA_INVALID",required.ErrorCode);
            }
            foreach(var selected in new[]{columns,nullableColumns})
            {
                var clock=System.Diagnostics.Stopwatch.StartNew();
                var invalid=await mapping.QueryProjectionAsync(actor,reader,null,
                    new[]{new NativeRecordFilter("tagAlias",CosmicQueryComparisonOperator.Equal,"12")},selected,100);
                timings[selected[0].Nullable?"invalid-nullable":"invalid-required"]=clock.ElapsedMilliseconds;
                Assert.IsTrue(invalid.IsError);
                Assert.AreEqual("NATIVE_PROJECTION_DATA_INVALID",invalid.ErrorCode);
            }
            var denied=await mapping.QueryProjectionAsync(Guid.NewGuid(),reader,null,
                new[]{new NativeRecordFilter("stampAlias",CosmicQueryComparisonOperator.Equal,boundary)},columns,100);
            Assert.IsTrue(denied.IsError);
            Assert.AreEqual("VIRTUAL_HOLON_FORBIDDEN",denied.ErrorCode);
            foreach(var id in projectedIds)
                Assert.IsNull((await provider.LoadHolonAsync(id)).Result,"Native date queries must not materialize a holon.");
            var after = await fingerprint();
            Assert.AreEqual(before.Length, after.Length);
            for (var index = 0; index < before.Length; index++)
                CollectionAssert.AreEqual(before[index], after[index]);
            return 17;
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
                Assert.IsTrue(label.StartsWith("UtcDateNative_", StringComparison.Ordinal));
                await (await session.RunAsync($"MATCH (n:{label}) DETACH DELETE n")).ConsumeAsync();
                var remaining = await (await session.RunAsync($"MATCH (n:{label}) RETURN count(n) AS total")).SingleAsync();
                Assert.AreEqual(0L, remaining["total"].As<long>());
            }
        }
    }
}
