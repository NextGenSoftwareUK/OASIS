using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    [TestMethod]
    [DoNotParallelize]
    public async Task NativeNeoEntityProjectionPreservesStagesAndProofBeforeDtoConstruction()
    {
        var rootPath=Environment.GetEnvironmentVariable("OASIS_CQL_NEO_ENTITY_PROJECTION_ROOT")
            ?? throw new InvalidOperationException("Use the explicit Neo entity-projection runner.");
        const string uri="bolt://127.0.0.1:17687";
        const string username="neo4j", password="oasis-local-test-only";
        var state=Path.Combine(rootPath,"LocalState/COSMIC/NeoEntityProjectionCurrent");Directory.CreateDirectory(state);
        var journal=Path.Combine(state,"fixture.pending.json");
        Assert.IsFalse(File.Exists(journal),"Preserve/reconcile prior projection fixture; never replay creates.");
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic(username,password));
        await using var inspection=driver.AsyncSession();
        async Task<long> Count()=> (await (await inspection.RunAsync("MATCH(h:Holon) RETURN count(h) AS n")).SingleAsync())["n"].As<long>();
        Assert.AreEqual(0L,await Count(),"Preserve unexpected native data.");
        var ids=Enumerable.Range(0,5).Select(_=>Guid.NewGuid()).ToArray();var actor=Guid.NewGuid();var other=Guid.NewGuid();
        async Task Checkpoint(string stage,bool first=false)
        {
            var next=first ? journal : journal+".next";
            await using(var file=new FileStream(next,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
            {await JsonSerializer.SerializeAsync(file,new{ids,actor,other,stage});file.Flush(true);}
            if(!first)File.Move(next,journal,true);
        }
        await Checkpoint("before-create",true);
        var provider=new Neo4jOASIS(uri,username,password){IsVersionControlEnabled=false};var active=false;
        try
        {
            var activation=await provider.ActivateProviderAsync();Assert.IsFalse(activation.IsError,activation.Message);active=true;
            var checks=new List<object>();
            await VerifyVirtualNativeLabel(provider, inspection, actor,
                new NativeConnection { ["uri"] = uri, ["username"] = username, ["password"] = password }, checks);
            var saved=await provider.SaveHolonAsync(CreateEntityProjectionFixture(ids,actor,other),saveChildren:true);
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.AreEqual(5L,await Count());
            await Checkpoint("created-awaiting-projection-verification");

            await VerifyEntityProjection((ICosmicQueryEntityProjectionStorageProvider)provider,ids[0],checks);
            await VerifyStandardEntityProjection((ICosmicQueryEntityProjectionStorageProvider)provider,ids[0],checks);
            await VerifyEntitySelectedComposition((ICosmicQueryEntityProjectionCompositionStorageProvider)provider,ids[0],ids,checks);
            await VerifyEntitySelectedReselection((ICosmicQueryEntityProjectionStorageProvider)provider,ids[0],checks);
            await VerifyEntitySelectedCardinality((ICosmicQueryEntityProjectionCardinalityStorageProvider)provider,ids[0],checks);
            await VerifyEntitySelectedAll((ICosmicQueryEntityProjectedAllStorageProvider)provider,ids[0],checks);
            await VerifyMetadataPresenceWire(provider, ids[0], checks);
            await VerifyDistinctWire(provider, ids, checks);
            await VerifyNativeFlattening(provider,ids[0],ids[1],checks);
            await VerifyChildSource(provider, actor, ids[0], ids[1], checks);
            await VerifyJoinOutputOrdering(provider, ids, checks);
            await VerifyNativeStringLength(provider, ids, checks);
            Assert.AreEqual(143,checks.Count);
            var deactivation=await provider.DeActivateProviderAsync();Assert.IsFalse(deactivation.IsError,deactivation.Message);active=false;
            await Checkpoint("before-exact-cleanup");
            var deleted=await (await inspection.RunAsync("MATCH(h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids=ids.Select(id=>id.ToString("D")).ToArray()})).ConsumeAsync();
            Assert.AreEqual(5,deleted.Counters.NodesDeleted);Assert.AreEqual(0L,await Count());
            await File.WriteAllTextAsync(Path.Combine(rootPath,"TestResults/Neo4jCurrent/neo-entity-projection-native.json"),
                JsonSerializer.Serialize(new{provider="Neo4jOASIS",checks=checks.Count,cases=checks,exactCleanupVerified=true}));
            File.Delete(journal);
        }
        finally{if(active)Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);}
    }

    private static async Task VerifyVirtualNativeLabel(Neo4jOASIS provider, IAsyncSession inspection, Guid actor, NativeConnection named, List<object> checks)
    {
        var label = "VirtualFixture_" + Guid.NewGuid().ToString("N");
        var precedingLabels = provider.NativeImportLabels;
        provider.NativeImportLabels = label;
        try
        {
            await (await inspection.RunAsync($"CREATE (:`{label}` {{ tier:'Gold', name:'Ada', hidden:'private', text:$emoji }}), " +
                $"(:`{label}` {{ tier:'gold', name:'Grace', text:$nul }}), (:`{label}` {{ tier:1, name:'Alan', text:[$emoji] }})",
                new { emoji = "\U0001F600", nul = "abc\u0000def" })).ConsumeAsync();
            var typedCases = new[]
            {
                new NativeRecordFilter("text", CosmicQueryComparisonOperator.Equal, 2L, NativeRecordFunction.Utf16Length),
                new NativeRecordFilter("text", CosmicQueryComparisonOperator.GreaterThan, 2L, NativeRecordFunction.Utf16Length),
                new NativeRecordFilter("tier", CosmicQueryComparisonOperator.GreaterThan, 0L),
                new NativeRecordFilter("tier", CosmicQueryComparisonOperator.NotEqual, "Gold")
            };
            var expected = new[] { "Ada", "Grace", "Alan", "Grace" };
            for (var index = 0; index < typedCases.Length; index++)
            {
                var started = System.Diagnostics.Stopwatch.StartNew();
                var selected = await provider.QueryNativePredicateRecordsAsync(label, new[] { typedCases[index] },
                    new[] { "name" }, 1);
                Assert.IsFalse(selected.IsError, selected.Message + selected.Exception);
                Assert.AreEqual(expected[index], selected.Result.Single().Fields["name"]);
                Assert.AreEqual(1, selected.Result.Single().Fields.Count);
                checks.Add(new { provider = "Neo4jOASIS", nativePredicate = index,
                    elapsedMilliseconds = started.ElapsedMilliseconds });
            }
            var stringCases = new (NativeRecordFunction Function, string Field, string Value, string[] Expected)[]
            {
                (NativeRecordFunction.Contains, "text", "\u0000", new[] { "Grace" }),
                (NativeRecordFunction.StartsWith, "text", "abc", new[] { "Grace" }),
                (NativeRecordFunction.EndsWith, "text", "def", new[] { "Grace" }),
                (NativeRecordFunction.Contains, "text", "\U0001F600", new[] { "Ada" }),
                (NativeRecordFunction.Contains, "text", "", new[] { "Ada", "Grace" }),
                (NativeRecordFunction.Contains, "name", "ADA", Array.Empty<string>())
            };
            foreach (var item in stringCases)
            {
                var stringWatch = System.Diagnostics.Stopwatch.StartNew();
                var read = await provider.QueryNativePredicateRecordsAsync(label,
                    new[] { new NativeRecordFilter(item.Field, CosmicQueryComparisonOperator.Equal, item.Value, item.Function) },
                    new[] { "name" }, item.Expected.Length == 1 ? 1 : 10);
                Assert.IsFalse(read.IsError, read.Message + read.Exception);
                CollectionAssert.AreEqual(item.Expected.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                    read.Result.Select(row => (string)row.Fields["name"]).OrderBy(value => value, StringComparer.Ordinal).ToArray());
                checks.Add(new { provider = "Neo4jOASIS", nativePredicate = "ordinal string " + item.Function, literal = item.Value, elapsedMilliseconds = stringWatch.ElapsedMilliseconds });
            }
            var negativeCases = new (NativeRecordFunction Function, string Field, string Value, string[] Expected)[]
            {
                (NativeRecordFunction.Contains, "text", "\u0000", new[] { "Ada", "Alan" }),
                (NativeRecordFunction.StartsWith, "text", "abc", new[] { "Ada", "Alan" }),
                (NativeRecordFunction.EndsWith, "text", "def", new[] { "Ada", "Alan" }),
                (NativeRecordFunction.Contains, "missing", "Case", new[] { "Ada", "Grace", "Alan" })
            };
            foreach (var item in negativeCases)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var read = await provider.QueryNativePredicateRecordsAsync(label,
                    new[] { new NativeRecordFilter(item.Field, CosmicQueryComparisonOperator.NotEqual, item.Value, item.Function) },
                    new[] { "name" }, 10);
                Assert.IsFalse(read.IsError, read.Message + read.Exception);
                CollectionAssert.AreEqual(item.Expected.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                    read.Result.Select(row => (string)row.Fields["name"]).OrderBy(value => value, StringComparer.Ordinal).ToArray());
                checks.Add(new { provider = "Neo4jOASIS", nativePredicate = "total negative string " + item.Function,
                    field = item.Field, elapsedMilliseconds = watch.ElapsedMilliseconds });
            }
            var sourceCount = await (await inspection.RunAsync($"MATCH (n:`{label}`) RETURN count(n) AS n")).SingleAsync();
            Assert.AreEqual(3L, sourceCount["n"].As<long>(), "Native source queries must not write records.");

            var native = await provider.ReadNativeRecordsAsync(label, null, 5);
            Assert.IsFalse(native.IsError, native.Message);
            var imported = native.Result.Records.Single(record => Equals(record.Fields["name"], "Ada"));
            var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, actor, provider.NativeDataProviderType,
                label, VirtualHolonMapping.SandboxConnectionName, provider.DescribeVirtualQueryConnection(null),
                new Dictionary<string, string> { ["tier"] = "tier", ["name"] = "name" });
            var lengthMapping = new VirtualHolonMapping(Guid.NewGuid(), 1, actor, provider.NativeDataProviderType,
                label, VirtualHolonMapping.SandboxConnectionName, provider.DescribeVirtualQueryConnection(null),
                new Dictionary<string, string> { ["name"] = "name", ["textAlias"] = "text" });
            var mappedLength = await lengthMapping.QueryAsync(actor, provider, null,
                new[] { new NativeRecordFilter("textAlias", CosmicQueryComparisonOperator.Equal, 2L, NativeRecordFunction.Utf16Length) },
                new[] { "name", "textAlias" }, 2);
            Assert.IsFalse(mappedLength.IsError, mappedLength.Message + mappedLength.Exception);
            Assert.AreEqual("Ada", mappedLength.Result.Single().MetaData["name"]);
            Assert.AreEqual("\U0001F600", mappedLength.Result.Single().PrepareSnapshot(actor).MetaData["textAlias"]);
            checks.Add(new { provider = "Neo4jOASIS", nativePredicate = "mapped length and unchanged snapshot field" });
            var mappedMatch = await lengthMapping.QueryAsync(actor, provider, null,
                new[] { new NativeRecordFilter("textAlias", CosmicQueryComparisonOperator.Equal, "\u0000", NativeRecordFunction.Contains) },
                new[] { "name", "textAlias" }, 2);
            Assert.IsFalse(mappedMatch.IsError, mappedMatch.Message + mappedMatch.Exception);
            Assert.AreEqual("Grace", mappedMatch.Result.Single().MetaData["name"]);
            Assert.AreEqual("abc\u0000def", mappedMatch.Result.Single().PrepareSnapshot(actor).MetaData["textAlias"]);
            checks.Add(new { provider = "Neo4jOASIS", nativePredicate = "mapped string match and unchanged snapshot" });
            var query = await mapping.QueryAsync(actor, provider, null, "tier", "Gold", new[] { "name" }, 2);
            Assert.IsFalse(query.IsError, query.Message);
            var row = query.Result.Single();
            Assert.AreEqual(NativeRecordHolonMapper.StableHolonId(actor, provider.NativeDataProviderType,
                label, imported.NativeKey), row.Id);
            Assert.AreEqual("Ada", row.MetaData["name"]);
            Assert.AreEqual(1, row.MetaData.Count);

            var snapshot = row.PrepareSnapshot(actor);
            var changed = await (await inspection.RunAsync(
                $"MATCH (item:`{label}` {{tier:'Gold'}}) SET item.name = 'Ada updated'")).ConsumeAsync();
            Assert.AreEqual(1, changed.Counters.PropertiesSet);
            var after = await mapping.QueryAsync(actor, provider, null, "tier", "Gold", new[] { "name" }, 2);
            Assert.IsFalse(after.IsError, after.Message);
            var refreshed = after.Result.Single();
            Assert.AreEqual(row.Id, refreshed.Id);
            Assert.AreEqual("Ada updated", refreshed.MetaData["name"]);
            Assert.AreEqual("Ada", snapshot.MetaData["name"]);

            var integer = await provider.QueryNativeRecordsAsync(label, "tier", 1L, new[] { "name" }, 2);
            Assert.IsFalse(integer.IsError, integer.Message);
            Assert.AreEqual("Alan", integer.Result.Single().Fields["name"]);

            await (await inspection.RunAsync($"CREATE (:`{label}` {{ tier:'Gold', name:'Second Gold' }})")).ConsumeAsync();
            var gold = await provider.QueryNativeRecordsAsync(label, "tier", "Gold", new[] { "name" }, 2);
            Assert.IsFalse(gold.IsError, gold.Message);
            Assert.AreEqual(2, gold.Result.Count);
            var conjunction = await provider.QueryNativeRecordsAsync(label,
                new[] { new KeyValuePair<string, object>("tier", "Gold"),
                    new KeyValuePair<string, object>("name", gold.Result[1].Fields["name"]) }, new[] { "name" }, 1);
            Assert.IsFalse(conjunction.IsError, conjunction.Message);
            Assert.AreEqual(gold.Result[1].NativeKey, conjunction.Result.Single().NativeKey);

            // Only this serial test permits its disposable loopback Neo4j fixture.
            named["labels"] = label;
            var identity = provider.DescribeVirtualQueryConnection(named);
            var namedMapping = new VirtualHolonMapping(Guid.NewGuid(), 1, actor, provider.NativeDataProviderType,
                label, "owned-ci-graph", identity, mapping.Fields);
            var definition = namedMapping.ToDefinitionHolon();
            definition.Version = 1;
            Assert.IsFalse(definition.MetaData.Values.OfType<string>().Any(value => value.Contains(named["password"])));
            var reopened = VirtualHolonMapping.FromDefinitionHolon(actor, definition, identity);
            var previousPrivatePolicy = NativeConnectionGuard.AllowPrivateOverride;
            try
            {
                NativeConnectionGuard.AllowPrivateOverride = true;
                var matching = await reopened.QueryAsync(actor, provider, named, "tier", "Gold", new[] { "name" }, 2);
                Assert.IsFalse(matching.IsError, matching.Message);
                CollectionAssert.AreEquivalent(new[] { "Ada updated", "Second Gold" },
                    matching.Result.Select(result => (string)result.MetaData["name"]).ToArray());
                var retargeted = new NativeConnection(named) { ["database"] = "other" };
                var refused = await reopened.QueryAsync(actor, provider, retargeted, "tier", "Gold", new[] { "name" }, 2);
                Assert.AreEqual("VIRTUAL_HOLON_CONNECTION_CHANGED", refused.ErrorCode);
            }
            finally
            {
                NativeConnectionGuard.AllowPrivateOverride = previousPrivatePolicy;
            }

            await (await inspection.RunAsync($"MATCH (n:`{label}`) WHERE n.hidden = 'private' SET n.amount = $amount, n.small = 123, n.flag = true, n.empty = '', n.big = $big, n.near = $near, n.rate = $rate, n.low = $low, n.high = $high, n.tiny = $tiny, n.zero = $zero, n.badNaN = $badNaN, n.badInf = $badInf, n.badNegativeInf = $badNegativeInf",
                new { amount = 9007199254740993L, big = new string('x', 1048580),
                    rate = 3.25d, low = -double.MaxValue, high = double.MaxValue, tiny = double.Epsilon, zero = -0d,
                    badNaN = double.NaN, badInf = double.PositiveInfinity, badNegativeInf = double.NegativeInfinity,
                    near = string.Concat(Enumerable.Repeat("A\u007F\u0080\u07FF\u0800\uFFFF😀", 62500)) })).ConsumeAsync();
            async Task<string[]> NativeProjectionSnapshot()
            {
                var records = await (await inspection.RunAsync($"MATCH (n:`{label}`) RETURN elementId(n) AS key, properties(n) AS data ORDER BY key")).ToListAsync();
                return records.Select(record => record["key"].As<string>() + ":" + Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(new SortedDictionary<string, object>(record["data"].As<IDictionary<string, object>>(), StringComparer.Ordinal),
                            new JsonSerializerOptions { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals }))))).ToArray();
            }
            var nativeBefore = await NativeProjectionSnapshot();
            await VerifyNativeDoubleProjection(provider, label,
                new NativeRecordFilter("hidden", CosmicQueryComparisonOperator.Equal, "private"), "small", true, checks);
            var projectionWatch = System.Diagnostics.Stopwatch.StartNew();
            var adaFilter = new[] { new NativeRecordFilter("hidden", CosmicQueryComparisonOperator.Equal, "private") };
            var projectionColumns = new[] {
                new NativeProjectionColumn("name", "Name", typeof(string)),
                new NativeProjectionColumn("text", "Original", typeof(string)),
                new NativeProjectionColumn("text", "Units", typeof(string), function: NativeRecordFunction.Utf16Length),
                new NativeProjectionColumn("text", "UnitsAgain", typeof(string), function: NativeRecordFunction.Utf16Length),
                new NativeProjectionColumn("empty", "EmptyUnits", typeof(string), function: NativeRecordFunction.Utf16Length)
            };
            var projection = await provider.QueryNativeProjectionRecordsAsync(label, adaFilter, projectionColumns, 1);
            Assert.IsFalse(projection.IsError, projection.Message + projection.Exception);
            var projectedRow = projection.Result.Single();
            Assert.AreEqual(imported.NativeKey, projectedRow.NativeKey);
            Assert.AreEqual("Ada updated", projectedRow.Cells[0].Value);
            Assert.AreEqual("😀", projectedRow.Cells[1].Value);
            Assert.AreEqual(2, projectedRow.Cells[2].Value);
            Assert.AreEqual(2, projectedRow.Cells[3].Value);
            Assert.AreEqual(0, projectedRow.Cells[4].Value);
            await VerifyMappedNativeProjectionCompiler(provider, lengthMapping, actor, mappedLength.Result.Single());
            checks.Add(new { provider = "Neo4jOASIS", nativeProjection = "raw/computed/repeated aliases and original element identity", mappedCompilerVerified = true, elapsedMilliseconds = projectionWatch.ElapsedMilliseconds });

            projectionWatch.Restart();
            var missingProjection = await provider.QueryNativeProjectionRecordsAsync(label, adaFilter,
                new[] { new NativeProjectionColumn("absent", "Units", typeof(string), true, NativeRecordFunction.Utf16Length) }, 1);
            Assert.IsFalse(missingProjection.IsError, missingProjection.Message);
            Assert.AreEqual(NativeProjectionValueState.Missing, missingProjection.Result.Single().Cells[0].State);
            var requiredProjection = await provider.QueryNativeProjectionRecordsAsync(label, adaFilter,
                new[] { new NativeProjectionColumn("absent", "Units", typeof(string), false, NativeRecordFunction.Utf16Length) }, 1);
            Assert.AreEqual("NATIVE_PROJECTION_DATA_INVALID", requiredProjection.ErrorCode);
            var nulProjection = await provider.QueryNativeProjectionRecordsAsync(label,
                new[] { new NativeRecordFilter("name", CosmicQueryComparisonOperator.Equal, "Grace") },
                new[] { new NativeProjectionColumn("text", "Units", typeof(string), function: NativeRecordFunction.Utf16Length) }, 1);
            Assert.IsFalse(nulProjection.IsError, nulProjection.Message);
            Assert.AreEqual(7, nulProjection.Result.Single().Cells[0].Value);
            checks.Add(new { provider = "Neo4jOASIS", nativeProjection = "native missing/nullability and embedded-NUL semantics", elapsedMilliseconds = projectionWatch.ElapsedMilliseconds });

            projectionWatch.Restart();
            var invalidProjection = await provider.QueryNativeProjectionRecordsAsync(label,
                new[] { new NativeRecordFilter("name", CosmicQueryComparisonOperator.Equal, "Alan") },
                new[] { new NativeProjectionColumn("text", "Units", typeof(string), true, NativeRecordFunction.Utf16Length) }, 1);
            Assert.AreEqual("NATIVE_PROJECTION_DATA_INVALID", invalidProjection.ErrorCode);
            Assert.IsNull(invalidProjection.Result);
            var duplicateAliases = await provider.QueryNativeProjectionRecordsAsync(label, adaFilter,
                new[] { new NativeProjectionColumn("name", "Same", typeof(string)),
                    new NativeProjectionColumn("text", "Same", typeof(string)) }, 1);
            Assert.AreEqual("NATIVE_PROJECTION_QUERY_INVALID", duplicateAliases.ErrorCode);
            checks.Add(new { provider = "Neo4jOASIS", nativeProjection = "invalid native input/aliases produce no partial results", elapsedMilliseconds = projectionWatch.ElapsedMilliseconds });

            projectionWatch.Restart();
            var exactProjection = await provider.QueryNativeProjectionRecordsAsync(label, adaFilter,
                new[] { new NativeProjectionColumn("amount", "Exact", typeof(long)),
                    new NativeProjectionColumn("small", "Narrow", typeof(int)), new NativeProjectionColumn("flag", "Flag", typeof(bool)) }, 1);
            Assert.IsFalse(exactProjection.IsError, exactProjection.Message);
            Assert.AreEqual(9007199254740993L, exactProjection.Result.Single().Cells[0].Value);
            Assert.AreEqual(123, exactProjection.Result.Single().Cells[1].Value);
            Assert.AreEqual(true, exactProjection.Result.Single().Cells[2].Value);
            var overflowingProjection = await provider.QueryNativeProjectionRecordsAsync(label, adaFilter,
                new[] { new NativeProjectionColumn("amount", "Narrow", typeof(int)) }, 1);
            Assert.AreEqual("NATIVE_PROJECTION_DATA_INVALID", overflowingProjection.ErrorCode);
            var wrongBoolean = await provider.QueryNativeProjectionRecordsAsync(label, adaFilter,
                new[] { new NativeProjectionColumn("small", "Flag", typeof(bool)) }, 1);
            Assert.AreEqual("NATIVE_PROJECTION_DATA_INVALID", wrongBoolean.ErrorCode);
            checks.Add(new { provider = "Neo4jOASIS", nativeProjection = "exact native scalar domains and checked Int32 boundary", elapsedMilliseconds = projectionWatch.ElapsedMilliseconds });

            projectionWatch.Restart();
            var nearLimit = await provider.QueryNativeProjectionRecordsAsync(label, adaFilter,
                new[] { new NativeProjectionColumn("near", "Text", typeof(string)) }, 1);
            Assert.IsFalse(nearLimit.IsError, nearLimit.Message + nearLimit.Exception);
            Assert.AreEqual(1000000, System.Text.Encoding.UTF8.GetByteCount((string)nearLimit.Result.Single().Cells[0].Value));
            var oversizedProjection = await provider.QueryNativeProjectionRecordsAsync(label, adaFilter,
                new[] { new NativeProjectionColumn("big", "Text", typeof(string)) }, 1);
            Assert.AreEqual("NATIVE_PROJECTION_PAYLOAD_LIMIT", oversizedProjection.ErrorCode);
            var excessiveWork = await provider.QueryNativeProjectionRecordsAsync(label, adaFilter,
                new[] { new NativeProjectionColumn("big", "Units", typeof(string), function: NativeRecordFunction.Utf16Length) }, 1);
            Assert.AreEqual("NATIVE_PROJECTION_RESOURCE_LIMIT", excessiveWork.ErrorCode);
            using (var cancelled = new System.Threading.CancellationTokenSource()) {
                cancelled.Cancel();
                await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => provider.QueryNativeProjectionRecordsAsync(
                    label, adaFilter, projectionColumns, 1, cancellationToken: cancelled.Token));
            }
            await CqlV1Web4NativeTests.VerifyMappedNativeHttpAsync(provider, lengthMapping, mappedLength.Result.Single());
            CollectionAssert.AreEqual(nativeBefore, await NativeProjectionSnapshot());
            checks.Add(new { provider = "Neo4jOASIS", nativeProjection = "exact UTF8 byte bound, limited computation, cancellation and unchanged source", elapsedMilliseconds = projectionWatch.ElapsedMilliseconds });
            checks.Add(new { provider = "Neo4jOASIS", nativeHttp = "mixed raw/computed and database-loaded private definition", jwt = true, discovery = "controlled owned fixture", successAttempts = 3, foreignActorAttempts = 2, globalOrderingVerified = true, orderedAttempts = 3, identityHttpVerified = true, mixedCountHttpVerified = true, mixedNumericHttpVerified = true });
        }
        finally
        {
            provider.NativeImportLabels = precedingLabels;
            await (await inspection.RunAsync($"MATCH (n:`{label}`) DETACH DELETE n")).ConsumeAsync();
        }
    }
}
