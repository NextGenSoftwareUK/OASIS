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
            var projectionFields = new BsonDocument
            {
                { "amount", new BsonInt64(9007199254740993L) }, { "small", 123 }, { "flag", true },
                { "empty", "" }, { "big", new string('x', 1048580) },
                { "rate", 3.25d }, { "low", -double.MaxValue }, { "high", double.MaxValue },
                { "tiny", double.Epsilon }, { "zero", -0d },
                { "badNaN", double.NaN }, { "badInf", double.PositiveInfinity }, { "badNegativeInf", double.NegativeInfinity },
                { "optionalDouble", BsonNull.Value }
            };
            await items.UpdateOneAsync(new BsonDocument("_id", 1), new BsonDocument("$set", projectionFields));
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
            var stringCases = new (NativeRecordFunction Function, string Field, string Value, string[] Expected)[]
            {
                (NativeRecordFunction.Contains, "text", "\u0000", new[] { "nul" }),
                (NativeRecordFunction.StartsWith, "text", "abc", new[] { "nul" }),
                (NativeRecordFunction.EndsWith, "text", "def", new[] { "nul" }),
                (NativeRecordFunction.Contains, "text", "\U0001F600", new[] { "emoji" }),
                (NativeRecordFunction.Contains, "text", "", new[] { "emoji", "short", "nul" }),
                (NativeRecordFunction.Contains, "code", "CASE", Array.Empty<string>())
            };
            foreach (var item in stringCases)
            {
                var stringWatch = System.Diagnostics.Stopwatch.StartNew();
                var read = await provider.QueryNativePredicateRecordsAsync("items",
                    new[] { new NativeRecordFilter(item.Field, CosmicQueryComparisonOperator.Equal, item.Value, item.Function) },
                    new[] { "label" }, item.Expected.Length == 1 ? 1 : 10);
                Assert.IsFalse(read.IsError, read.Message + read.Exception);
                CollectionAssert.AreEqual(item.Expected, read.Result.Select(row => (string)row.Fields["label"]).ToArray());
                checks.Add(new { provider = "MongoDBOASIS", nativePredicate = "ordinal string " + item.Function, literal = item.Value, elapsedMilliseconds = stringWatch.ElapsedMilliseconds });
            }
            var negativeCases = new (NativeRecordFunction Function, string Field, string Value, string[] Expected)[]
            {
                (NativeRecordFunction.Contains, "text", "\u0000", new[] { "emoji", "short", "array" }),
                (NativeRecordFunction.StartsWith, "text", "abc", new[] { "emoji", "short", "array" }),
                (NativeRecordFunction.EndsWith, "text", "def", new[] { "emoji", "short", "array" }),
                (NativeRecordFunction.Contains, "nullableCode", "Case", new[] { "short", "array", "nul" })
            };
            foreach (var item in negativeCases)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var read = await provider.QueryNativePredicateRecordsAsync("items",
                    new[] { new NativeRecordFilter(item.Field, CosmicQueryComparisonOperator.NotEqual, item.Value, item.Function) },
                    new[] { "label" }, 10);
                Assert.IsFalse(read.IsError, read.Message + read.Exception);
                CollectionAssert.AreEqual(item.Expected, read.Result.Select(row => (string)row.Fields["label"]).ToArray());
                checks.Add(new { provider = "MongoDBOASIS", nativePredicate = "total negative string " + item.Function,
                    field = item.Field, elapsedMilliseconds = watch.ElapsedMilliseconds });
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
            var mappedMatch = await mapping.QueryAsync(owner, provider, null,
                new[] { new NativeRecordFilter("textAlias", CosmicQueryComparisonOperator.Equal, "\u0000", NativeRecordFunction.Contains) },
                new[] { "caption", "textAlias" }, 2);
            Assert.IsFalse(mappedMatch.IsError, mappedMatch.Message + mappedMatch.Exception);
            Assert.AreEqual("nul", mappedMatch.Result.Single().MetaData["caption"]);
            Assert.AreEqual("abc\u0000def", mappedMatch.Result.Single().PrepareSnapshot(owner).MetaData["textAlias"]);
            checks.Add(new { provider = "MongoDBOASIS", nativePredicate = "mapped string match and unchanged snapshot" });
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

            var nativeBefore = (await items.Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync())
                .Select(document => document.ToBson()).ToArray();
            await VerifyNativeDoubleProjection(provider, "items",
                new NativeRecordFilter("_id", CosmicQueryComparisonOperator.Equal, 1), "small", true, checks);
            var projectionWatch = System.Diagnostics.Stopwatch.StartNew();
            var lengthColumns = new[] {
                new NativeProjectionColumn("label", "Caption", typeof(string)),
                new NativeProjectionColumn("text", "Original", typeof(string)),
                new NativeProjectionColumn("text", "Units", typeof(string), function: NativeRecordFunction.Utf16Length),
                new NativeProjectionColumn("text", "UnitsAgain", typeof(string), function: NativeRecordFunction.Utf16Length),
                new NativeProjectionColumn("empty", "EmptyUnits", typeof(string), function: NativeRecordFunction.Utf16Length)
            };
            var nativeLengthFilter = new[] { new NativeRecordFilter("text", CosmicQueryComparisonOperator.Equal, 2L, NativeRecordFunction.Utf16Length) };
            var projected = await provider.QueryNativeProjectionRecordsAsync("items", nativeLengthFilter, lengthColumns, 1);
            Assert.IsFalse(projected.IsError, projected.Message + projected.Exception);
            var projectionRow = projected.Result.Single();
            var originalRow = await provider.QueryNativePredicateRecordsAsync("items", nativeLengthFilter, new[] { "label" }, 1);
            Assert.AreEqual(originalRow.Result.Single().NativeKey, projectionRow.NativeKey);
            Assert.AreEqual("emoji", projectionRow.Cells[0].Value);
            Assert.AreEqual("😀", projectionRow.Cells[1].Value);
            Assert.AreEqual(2, projectionRow.Cells[2].Value);
            Assert.AreEqual(2, projectionRow.Cells[3].Value);
            Assert.AreEqual(0, projectionRow.Cells[4].Value);
            await VerifyMappedNativeProjectionCompiler(provider, mapping, owner, mapped.Result.Single());
            checks.Add(new { provider = "MongoDBOASIS", nativeProjection = "raw/computed/repeated aliases and original identity", mappedCompilerVerified = true, elapsedMilliseconds = projectionWatch.ElapsedMilliseconds });

            projectionWatch.Restart();
            var allNative = new[] { new NativeRecordFilter("_id", CosmicQueryComparisonOperator.GreaterThanOrEqual, 1) };
            var optional = new[] { new NativeProjectionColumn("nullableCode", "Units", typeof(string), true, NativeRecordFunction.Utf16Length) };
            var nullableProjection = await provider.QueryNativeProjectionRecordsAsync("items", allNative, optional, 4);
            Assert.IsFalse(nullableProjection.IsError, nullableProjection.Message + nullableProjection.Exception);
            CollectionAssert.AreEqual(new[] { NativeProjectionValueState.Present, NativeProjectionValueState.Null,
                NativeProjectionValueState.Missing, NativeProjectionValueState.Present },
                nullableProjection.Result.Select(row => row.Cells[0].State).ToArray());
            Assert.AreEqual(4, nullableProjection.Result[0].Cells[0].Value);
            Assert.AreEqual(4, nullableProjection.Result[3].Cells[0].Value);
            var nulProjection = await provider.QueryNativeProjectionRecordsAsync("items",
                new[] { new NativeRecordFilter("label", CosmicQueryComparisonOperator.Equal, "nul") },
                new[] { new NativeProjectionColumn("text", "Units", typeof(string), function: NativeRecordFunction.Utf16Length) }, 1);
            Assert.IsFalse(nulProjection.IsError, nulProjection.Message);
            Assert.AreEqual(7, nulProjection.Result.Single().Cells[0].Value);
            checks.Add(new { provider = "MongoDBOASIS", nativeProjection = "independent missing/null states and full embedded-NUL length", elapsedMilliseconds = projectionWatch.ElapsedMilliseconds });

            projectionWatch.Restart();
            var invalidProjection = await provider.QueryNativeProjectionRecordsAsync("items", allNative,
                new[] { new NativeProjectionColumn("text", "Units", typeof(string), true, NativeRecordFunction.Utf16Length) }, 4);
            Assert.AreEqual("NATIVE_PROJECTION_DATA_INVALID", invalidProjection.ErrorCode);
            Assert.IsNull(invalidProjection.Result);
            foreach (var badId in new[] { 2, 3 }) {
                var requiredProjection = await provider.QueryNativeProjectionRecordsAsync("items",
                    new[] { new NativeRecordFilter("_id", CosmicQueryComparisonOperator.Equal, badId) },
                    new[] { new NativeProjectionColumn("nullableCode", "Units", typeof(string), false, NativeRecordFunction.Utf16Length) }, 1);
                Assert.AreEqual("NATIVE_PROJECTION_DATA_INVALID", requiredProjection.ErrorCode);
                Assert.IsNull(requiredProjection.Result);
            }
            var duplicateAliases = await provider.QueryNativeProjectionRecordsAsync("items", nativeLengthFilter,
                new[] { new NativeProjectionColumn("text", "Same", typeof(string)),
                    new NativeProjectionColumn("label", "Same", typeof(string)) }, 1);
            Assert.AreEqual("NATIVE_PROJECTION_QUERY_INVALID", duplicateAliases.ErrorCode);
            checks.Add(new { provider = "MongoDBOASIS", nativeProjection = "invalid source/null/missing cannot become partial or fabricated results", elapsedMilliseconds = projectionWatch.ElapsedMilliseconds });

            projectionWatch.Restart();
            var firstNative = new[] { new NativeRecordFilter("_id", CosmicQueryComparisonOperator.Equal, 1) };
            var exact = await provider.QueryNativeProjectionRecordsAsync("items", firstNative,
                new[] { new NativeProjectionColumn("amount", "Exact", typeof(long)),
                    new NativeProjectionColumn("small", "Small", typeof(int)), new NativeProjectionColumn("flag", "Flag", typeof(bool)) }, 1);
            Assert.IsFalse(exact.IsError, exact.Message);
            Assert.AreEqual(9007199254740993L, exact.Result.Single().Cells[0].Value);
            Assert.AreEqual(123, exact.Result.Single().Cells[1].Value);
            Assert.AreEqual(true, exact.Result.Single().Cells[2].Value);
            var wrongWidth = await provider.QueryNativeProjectionRecordsAsync("items", firstNative,
                new[] { new NativeProjectionColumn("small", "WrongWidth", typeof(long)) }, 1);
            Assert.AreEqual("NATIVE_PROJECTION_DATA_INVALID", wrongWidth.ErrorCode);
            checks.Add(new { provider = "MongoDBOASIS", nativeProjection = "exact Int64/Int32/Boolean domains without numeric coercion", elapsedMilliseconds = projectionWatch.ElapsedMilliseconds });

            projectionWatch.Restart();
            var oversizedProjection = await provider.QueryNativeProjectionRecordsAsync("items", firstNative,
                new[] { new NativeProjectionColumn("big", "TooLarge", typeof(string)) }, 1);
            Assert.AreEqual("NATIVE_PROJECTION_PAYLOAD_LIMIT", oversizedProjection.ErrorCode);
            Assert.IsNull(oversizedProjection.Result);
            var excessiveWork = await provider.QueryNativeProjectionRecordsAsync("items", firstNative,
                new[] { new NativeProjectionColumn("big", "Units", typeof(string), function: NativeRecordFunction.Utf16Length) }, 1);
            Assert.AreEqual("NATIVE_PROJECTION_RESOURCE_LIMIT", excessiveWork.ErrorCode);
            using (var cancelled = new System.Threading.CancellationTokenSource()) {
                cancelled.Cancel();
                await Assert.ThrowsExceptionAsync<OperationCanceledException>(() =>
                    provider.QueryNativeProjectionRecordsAsync("items", firstNative, lengthColumns, 1, cancellationToken: cancelled.Token));
            }
            await CqlV1Web4NativeTests.VerifyMappedNativeHttpAsync(provider, mapping, mapped.Result.Single());
            var nativeAfter = (await items.Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync())
                .Select(document => document.ToBson()).ToArray();
            Assert.AreEqual(nativeBefore.Length, nativeAfter.Length);
            for (var index = 0; index < nativeBefore.Length; index++) CollectionAssert.AreEqual(nativeBefore[index], nativeAfter[index]);
            checks.Add(new { provider = "MongoDBOASIS", nativeProjection = "bounded payload/work/cancellation and unchanged native documents", elapsedMilliseconds = projectionWatch.ElapsedMilliseconds });
            checks.Add(new { provider = "MongoDBOASIS", nativeHttp = "mixed raw/computed and database-loaded private definition", jwt = true, discovery = "controlled owned fixture", successAttempts = 3, foreignActorAttempts = 2, globalOrderingVerified = true, orderedAttempts = 3, identityHttpVerified = true, mixedCountHttpVerified = true, mixedNumericHttpVerified = true, selectedCountHttpVerified = true });
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
