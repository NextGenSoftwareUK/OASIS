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
        var previous = provider.NativeImportDatabaseName;
        var complete = false;
        try
        {
            provider.NativeImportDatabaseName = nativeDatabase;
            var items = client.GetDatabase(nativeDatabase).GetCollection<BsonDocument>("items");
            await items.InsertManyAsync(new[]
            {
                new BsonDocument { { "_id", 1 }, { "code", "Case" }, { "label", "emoji" }, { "text", "\U0001F600" } },
                new BsonDocument { { "_id", 2 }, { "code", "case" }, { "label", "short" }, { "text", "x" } },
                new BsonDocument { { "_id", 3 }, { "code", new BsonArray { "Case" } }, { "label", "array" },
                    { "text", new BsonArray { "\U0001F600" } } },
                new BsonDocument { { "_id", 4 }, { "code", "Case" }, { "label", "nul" }, { "text", "abc\u0000def" } }
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
