using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static async Task VerifyMongoNativePredicates(MongoProvider provider, MongoClient client,
        string database, List<object> checks)
    {
        var nativeDatabase = database + "_native_filters";
        using (var existing = await client.ListDatabaseNamesAsync())
            Assert.IsFalse((await existing.ToListAsync()).Contains(nativeDatabase),
                "Never overwrite an existing native predicate fixture.");
        using (var names = await client.ListDatabaseNamesAsync())
            Assert.IsFalse((await names.ToListAsync()).Contains(database + "_native_filters"),
                "Never reuse or reset an existing native predicate fixture database.");
        var previous = provider.NativeImportDatabaseName;
        var complete = false;
        try
        {
            provider.NativeImportDatabaseName = nativeDatabase;
            var items = client.GetDatabase(nativeDatabase).GetCollection<BsonDocument>("items");
            await items.InsertManyAsync(new[]
            {
                new BsonDocument { { "_id", 1 }, { "code", "Case" }, { "label", "emoji" }, { "nullableCode", "Case" }, { "text", "\U0001F600" } },
                new BsonDocument { { "_id", 2 }, { "code", "case" }, { "label", "short" }, { "nullableCode", BsonNull.Value }, { "text", "x" } },
                new BsonDocument { { "_id", 3 }, { "code", new BsonArray { "Case" } }, { "label", "array" },
                    { "text", new BsonArray { "\U0001F600" } } },
                new BsonDocument { { "_id", 4 }, { "code", "Case" }, { "label", "nul" }, { "nullableCode", "case" }, { "text", "abc\u0000def" } }
            });
            var cases = new[]
            {
                new NativeRecordFilter("text", CosmicQueryComparisonOperator.Equal, 2L, NativeRecordFunction.Utf16Length),
                new NativeRecordFilter("text", CosmicQueryComparisonOperator.GreaterThan, 2L, NativeRecordFunction.Utf16Length),
                new NativeRecordFilter("_id", CosmicQueryComparisonOperator.GreaterThan, 3),
                new NativeRecordFilter("code", CosmicQueryComparisonOperator.NotEqual, "Case")
            };
            var expected = new[] { "emoji", "nul", "nul", "short" };
            for (var index = 0; index < cases.Length; index++)
            {
                var started = System.Diagnostics.Stopwatch.StartNew();
                var read = await provider.QueryNativePredicateRecordsAsync("items", new[] { cases[index] },
                    new[] { "label" }, 1);
                Assert.IsFalse(read.IsError, read.Message + read.Exception);
                Assert.AreEqual(expected[index], read.Result.Single().Fields["label"]);
                Assert.AreEqual(1, read.Result.Single().Fields.Count);
                checks.Add(new { provider = "MongoDBOASIS", nativePredicate = index,
                    elapsedMilliseconds = started.ElapsedMilliseconds });
            }
            var nullable = await provider.QueryNativePredicateRecordsAsync("items",
                new[] { new NativeRecordFilter("nullableCode", CosmicQueryComparisonOperator.NotEqual, "Case") },
                new[] { "label" }, 10);
            Assert.IsFalse(nullable.IsError, nullable.Message + nullable.Exception);
            CollectionAssert.AreEqual(new[] { "short", "nul" },
                nullable.Result.Select(row => (string)row.Fields["label"]).ToArray());
            checks.Add(new { provider = "MongoDBOASIS", nativePredicate = "explicit null, not missing" });
            var owner = Guid.NewGuid();
            var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, owner, provider.NativeDataProviderType,
                "items", VirtualHolonMapping.SandboxConnectionName, provider.DescribeVirtualQueryConnection(null),
                new Dictionary<string, string> { ["caption"] = "label", ["textAlias"] = "text" });
            var filter = new NativeRecordFilter("textAlias", CosmicQueryComparisonOperator.Equal, 2L, NativeRecordFunction.Utf16Length);
            var mapped = await mapping.QueryAsync(owner, provider, null, new[] { filter }, new[] { "caption", "textAlias" }, 2);
            Assert.IsFalse(mapped.IsError, mapped.Message + mapped.Exception);
            Assert.AreEqual("emoji", mapped.Result.Single().MetaData["caption"]);
            Assert.AreEqual("\U0001F600", mapped.Result.Single().PrepareSnapshot(owner).MetaData["textAlias"]);
            checks.Add(new { provider = "MongoDBOASIS", nativePredicate = "mapped length and unchanged snapshot field" });
            var denied = await mapping.QueryAsync(Guid.NewGuid(), provider, null, new[] { filter }, new[] { "caption" }, 2);
            Assert.AreEqual("VIRTUAL_HOLON_FORBIDDEN", denied.ErrorCode);
            Assert.IsNull(denied.Result);
            checks.Add(new { provider = "MongoDBOASIS", nativePredicate = "mapped predicate owner boundary" });
            var invalidMapping = new VirtualHolonMapping(Guid.NewGuid(), 1, owner, provider.NativeDataProviderType,
                "items", VirtualHolonMapping.SandboxConnectionName, provider.DescribeVirtualQueryConnection(null),
                new Dictionary<string, string> { ["alias"] = new string('a', 129) });
            var invalid = await invalidMapping.QueryAsync(owner, provider, null,
                new[] { new NativeRecordFilter("alias", CosmicQueryComparisonOperator.Equal, "Case") }, new[] { "alias" }, 1);
            Assert.AreEqual("VIRTUAL_HOLON_QUERY_INVALID", invalid.ErrorCode);
            Assert.IsNull(invalid.Result);
            checks.Add(new { provider = "MongoDBOASIS", nativePredicate = "invalid mapped source field returns an error" });
            Assert.AreEqual(4L, await items.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty),
                "Mapped source queries must not write records.");
            complete = true;
        }
        finally
        {
            provider.NativeImportDatabaseName = previous;
            if (complete)
            {
                await client.DropDatabaseAsync(nativeDatabase);
                using var remaining = await client.ListDatabaseNamesAsync();
                Assert.IsFalse((await remaining.ToListAsync()).Contains(nativeDatabase));
            }
        }
    }
}
