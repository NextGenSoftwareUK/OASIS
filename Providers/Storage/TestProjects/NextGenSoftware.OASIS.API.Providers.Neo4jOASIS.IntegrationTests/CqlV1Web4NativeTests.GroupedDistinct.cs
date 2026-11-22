using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task GroupedDistinctUsesRealNativeProviderAndCompleteInputProof(string providerName)
        => RunGroupedDistinctNativeFixture(providerName, false);

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task GroupedDistinctPrimitiveDomainsUseRealNativeProvider(string providerName)
        => RunGroupedDistinctNativeFixture(providerName, true);

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task GroupedDistinctRequiredAndCanonicalDomainsUseRealNativeProvider(string providerName)
        => RunGroupedDistinctNativeFixture(providerName, false, requiredDomains: true);

    private async Task RunGroupedDistinctNativeFixture(string providerName, bool primitiveDomains, bool mixedHttp = false,
        bool requiredDomains = false, bool singlePerformance = false, bool sharedInputs = false, bool guidDomains = false, bool mixedGuidHttp = false, bool guidRowsHttp = false, bool predicateCrossingHttp = false, bool guidFilterHttp = false, bool int32FilterNative = false, bool int32FilterHttp = false, bool utcDateFilterNative = false, bool sessionJourney = false, bool statelessJourney = false, bool statelessInsertJourney = false, bool parentReferenceJourney = false, bool childrenReferenceJourney = false, bool readOnlyJourney = false, bool findJourney = false, bool syncJourney = false, bool statelessLifetimeJourney = false, bool statelessLinqJourney = false, bool updateJourney = false, bool lifecycleJourney = false, bool inspectionJourney = false, bool whereJourney = false, bool unfilteredMappingHttp = false, bool dateKeysHttp = false, bool snapshotPreparation = false, bool rangeRegistration = false, bool statelessGetJourney = false, bool setJourney = false, bool setHttpJourney = false, bool projectedSetJourney = false)
    {
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        const string mongoUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(mongoUri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var authority = Enum.Parse<ProviderType>(providerName);
        var database = "cosmic_distinct_" + Guid.NewGuid().ToString("N");
        var workRoot = Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!);
        var work = Path.Combine(workRoot, database);
        Assert.IsFalse(Directory.Exists(work));
        Directory.CreateDirectory(work);
        var sqlite = Path.Combine(work, "distinct.sqlite");
        IOASISStorageProvider provider = authority switch
        {
            ProviderType.MongoDBOASIS => new MongoProvider(mongoUri, database) { IsVersionControlEnabled = false },
            ProviderType.SQLLiteDBOASIS => new SqliteProvider("Data Source=" + sqlite + ";Pooling=False") { IsVersionControlEnabled = false },
            ProviderType.Neo4jOASIS => new Neo4jOASIS("bolt://127.0.0.1:17687", "neo4j", "oasis-local-test-only") { IsVersionControlEnabled = false },
            _ => throw new InvalidOperationException("Only isolated native providers are admitted.")
        };
        var ids = new List<Guid>();
        var complete = false; var active = false; var checks = 0;
        var queryElapsed = new Dictionary<string, long>();
        Dictionary<string, object>? singleProfile = null;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var mongo = authority == ProviderType.MongoDBOASIS ? new MongoClient(mongoUri) : null;
        if (mongo != null)
        {
            using var names = await mongo.ListDatabaseNamesAsync();
            Assert.IsFalse((await names.ToListAsync()).Contains(database));
        }
        try
        {
            var activated = await provider.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError, activated.Message + activated.Exception); active = true;
            var adapter = (ICosmicQueryGroupedStorageProvider)provider;
            async Task<Guid[]> Seed(params (string Key, object? Value)[] inputs)
            {
                var selected = new List<Guid>();
                foreach (var input in inputs)
                {
                    var id = Guid.NewGuid(); ids.Add(id); selected.Add(id);
                    var saved = await provider.SaveHolonAsync(new Holon { Id = id, Name = "distinct owned",
                        IsActive = true, MetaData = new() { ["category"] = input.Key, ["stock"] = input.Value! } },
                        saveChildren: false);
                    Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                }
                return selected.ToArray();
            }
            async Task<NextGenSoftware.OASIS.Common.OASISResult<CosmicGroupedExecution<CosmicProjectionRecord>>> Run<T>(
                Guid[] selected, Expression<Func<Holon, T>> selector, int maximumGroups = 1000, bool emptyPage = false,
                Action<CosmicGroupedSelection<Holon, string, CosmicProjectionRecord>>? observeQuery = null)
            {
                var grouping = CosmicQuery<Holon>.Create().Where(h => selected.Contains(h.Id))
                    .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"),
                        CosmicGroupingOptions.Create(maximumGroups).Result).Result;
                var intent = grouping.SelectColumns(new[]
                {
                    CosmicGroupedBinding<Holon, string>.Key(grouping, "Category", 0).Result,
                    CosmicGroupedBinding<Holon, string>.CountValues(grouping, "Present", selector).Result,
                    CosmicGroupedBinding<Holon, string>.CountValues(grouping, "Unique", selector, true).Result
                });
                Assert.IsFalse(intent.IsError, intent.Message);
                var query = emptyPage ? intent.Result.Take(0).Result : intent.Result;
                var planned = CosmicGroupedPlanner.Plan(query, authority,
                    new[] { new CosmicGroupedTarget(authority, adapter.QueryCapabilities, adapter.GroupedCapabilities) });
                Assert.IsFalse(planned.IsError, planned.Message);
                observeQuery?.Invoke(planned.Result.Query);
                return await CosmicGroupedExecutor.ExecuteAsync(planned.Result,
                    new[] { (ICosmicQueryStorageProvider)provider });
            }
            if (projectedSetJourney)
                checks=await VerifyNativeProjectedSetJourney(provider,ids,queryElapsed);
            else if (setHttpJourney)
                checks=await VerifyNativeSetHttpJourney(provider,ids,queryElapsed);
            else if (setJourney)
                checks=await VerifyNativeSetJourney(provider,ids,queryElapsed);
            else if (statelessGetJourney)
                checks=await VerifyStatelessGetJourney(provider,ids,queryElapsed);
            else if (rangeRegistration)
                checks=await VerifyRangeRegistration(provider,ids,queryElapsed);
            else if (snapshotPreparation)
            {
                checks=await VerifyGroupedDistinctMixedNativeHttpProvider(provider,work,database,mongo,ids,
                    queryTimings:queryElapsed,snapshotPreparation:true);
                Assert.AreEqual(1,ids.Count,"Only one owned snapshot may be written.");
            }
            else if (whereJourney)
                checks = await VerifyWhereJourney(provider, ids, queryElapsed);
            else if (lifecycleJourney)
                checks = await VerifyLifecycleJourney(provider, ids, queryElapsed);
            else if (updateJourney)
                checks = await VerifyUpdateJourney(provider, ids, queryElapsed);
            else if (statelessLinqJourney)
                checks = await VerifyStatelessLinqJourney(provider, ids, queryElapsed);
            else if (statelessLifetimeJourney)
                checks = await VerifyStatelessLifetimeJourney(provider, ids, queryElapsed);
            else if (syncJourney)
                checks = await VerifySyncJourney(provider, ids, queryElapsed);
            else if (findJourney)
                checks = await VerifyFindJourney(provider, ids, queryElapsed);
            else if (inspectionJourney)
                checks = await VerifyInspectionJourney(provider, ids, queryElapsed);
            else if (readOnlyJourney)
                checks = await VerifyReadOnlyJourney(provider, ids, queryElapsed);
            else if (childrenReferenceJourney)
                checks = await VerifyChildrenReferenceJourney(provider, ids, queryElapsed);
            else if (parentReferenceJourney)
                checks = await VerifyParentReferenceJourney(provider, ids, queryElapsed);
            else if (statelessInsertJourney)
                checks = await VerifyStatelessInsertJourney(provider, ids, queryElapsed);
            else if (statelessJourney)
                checks = await VerifyStatelessJourney(provider, ids, queryElapsed);
            else if (sessionJourney)
                checks = await VerifySessionJourney(provider, ids, queryElapsed);
            else if (utcDateFilterNative)
            {
                checks = await VerifyNativeUtcDates(provider, work, database, mongo, queryElapsed);
                Assert.AreEqual(0, ids.Count, "Native date queries must not write a holon.");
            }
            else if (int32FilterNative)
            {
                checks = await VerifyNativeInt32Filters(provider, work, database, mongo, queryElapsed);
                Assert.AreEqual(0, ids.Count, "Native projection must not write a holon.");
            }
            else if (predicateCrossingHttp)
            {
                checks = await VerifyGroupedDistinctMixedNativeHttpProvider(provider, work, database, mongo, ids,
                    queryTimings: queryElapsed, predicateCrossing: true);
                Assert.AreEqual(4, ids.Count);
            }
            else if (mixedGuidHttp || guidRowsHttp || guidFilterHttp || int32FilterHttp || unfilteredMappingHttp || dateKeysHttp)
            {
                checks = await VerifyGroupedDistinctMixedNativeHttpProvider(provider, work, database, mongo, ids,
                    guidCounts: true, queryTimings: queryElapsed, guidRows: guidRowsHttp || guidFilterHttp, guidFilters: guidFilterHttp, int32Http: int32FilterHttp, unfiltered: unfilteredMappingHttp, dateKeys: dateKeysHttp);
                Assert.AreEqual(5, ids.Count, "Four stored writes and one virtual-only cleanup identity.");
            }
            else if (guidDomains)
            {
                checks = await VerifyGroupedGuidDomains(provider, authority, mongo, database, ids, queryElapsed);
            }
            else if (sharedInputs)
            {
                Assert.AreEqual(ProviderType.Neo4jOASIS, authority);
                var selected = await Seed(("a", 2), ("a", 2), ("a", 4), ("a", null));
                checks = await VerifyNeoGroupedSharedInputs(provider, selected);
                Assert.AreEqual(4, ids.Count);
            }
            else if (singlePerformance)
            {
                Assert.AreEqual(ProviderType.Neo4jOASIS, authority);
                var negativeZero = BitConverter.Int32BitsToSingle(int.MinValue);
                var first = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(1f) + 1);
                var second = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(1f) + 2);
                Assert.AreNotEqual(first, second);
                var selected = await Seed(("a", 0f), ("a", negativeZero), ("a", first), ("a", second), ("a", second));
                singleProfile = await ProfileNeoSingleGroupedDistinct(
                    observer => Run(selected, h => CosmicQueryMetadata.Value<float>(h, "stock"), observeQuery: observer));
                checks = 3;
                Assert.AreEqual(5, ids.Count);
            }
            else if (mixedHttp)
            {
                checks = await VerifyGroupedDistinctMixedNativeHttpProvider(provider, work, database, mongo, ids);
                Assert.AreEqual(4, ids.Count);
            }
            else if (requiredDomains)
            {
                async Task Check<T>(string domain, (string Key, object? Value)[] values,
                    Expression<Func<Holon, T>> selector, long unique)
                {
                    var selected = await Seed(values);
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var result = await Run(selected, selector);
                    queryElapsed.Add(domain, watch.ElapsedMilliseconds);
                    Assert.IsFalse(result.IsError, result.Message + result.Exception);
                    var row = result.Result.Providers.Single().Rows.Single();
                    Assert.AreEqual((long)values.Length, row["Present"], domain);
                    Assert.AreEqual(unique, row["Unique"], domain);
                    checks++;
                }
                await Check("RequiredBoolean", new (string, object?)[] {
                    ("a", true), ("a", false), ("a", false) },
                    h => CosmicQueryMetadata.Value<bool>(h, "stock"), 2);
                await Check("RequiredInt32", new (string, object?)[] {
                    ("a", int.MinValue), ("a", int.MaxValue), ("a", int.MaxValue) },
                    h => CosmicQueryMetadata.Value<int>(h, "stock"), 2);
                await Check("RequiredInt64", new (string, object?)[] {
                    ("a", 9007199254740992L), ("a", 9007199254740993L), ("a", 9007199254740993L) },
                    h => CosmicQueryMetadata.Value<long>(h, "stock"), 2);
                var negativeSingleZero = BitConverter.Int32BitsToSingle(int.MinValue);
                var negativeDoubleZero = BitConverter.Int64BitsToDouble(long.MinValue);
                var first = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(1f) + 1);
                var second = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(1f) + 2);
                Assert.AreEqual(int.MinValue, BitConverter.SingleToInt32Bits(negativeSingleZero));
                Assert.AreEqual(long.MinValue, BitConverter.DoubleToInt64Bits(negativeDoubleZero));
                Assert.AreNotEqual(first, second);
                await Check("RequiredSingle", new (string, object?)[] {
                    ("a", 0f), ("a", negativeSingleZero), ("a", first), ("a", second), ("a", second) },
                    h => CosmicQueryMetadata.Value<float>(h, "stock"), 3);
                await Check("RequiredDouble", new (string, object?)[] {
                    ("a", 0d), ("a", negativeDoubleZero), ("a", double.Epsilon), ("a", -double.Epsilon),
                    ("a", double.MaxValue), ("a", double.MaxValue), ("a", -double.MaxValue) },
                    h => CosmicQueryMetadata.Value<double>(h, "stock"), 5);
                var canonical = await Seed(("a", null), ("a", null));
                async Task CheckCanonical<T>(string domain, Expression<Func<Holon, T>> selector)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var result = await Run(canonical, selector);
                    queryElapsed.Add(domain, watch.ElapsedMilliseconds);
                    Assert.IsFalse(result.IsError, result.Message + result.Exception);
                    var row = result.Result.Providers.Single().Rows.Single();
                    Assert.AreEqual(2L, row["Present"], domain);
                    Assert.AreEqual(1L, row["Unique"], domain);
                    checks++;
                }
                await CheckCanonical("CanonicalName", h => h.Name);
                await CheckCanonical("CanonicalIsActive", h => h.IsActive);
                await CheckCanonical("CanonicalVersion", h => h.Version);
                Assert.AreEqual(23, ids.Count);
                Assert.AreEqual(8, checks);
            }
            else if (primitiveDomains)
            {
                async Task Check<T>(string domain, (string Key, object? Value)[] values,
                    Expression<Func<Holon, T>> selector, long present, long unique)
                {
                    var selected = await Seed(values);
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var result = await Run(selected, selector);
                    queryElapsed.Add(domain, watch.ElapsedMilliseconds);
                    Assert.IsFalse(result.IsError, result.Message + result.Exception);
                    var rows = result.Result.Providers.Single().Rows;
                    Assert.AreEqual(2, rows.Count);
                    var populated = rows.Single(row => (string)row["Category"] == "a");
                    var empty = rows.Single(row => (string)row["Category"] == "b");
                    Assert.AreEqual(present, populated["Present"], domain);
                    Assert.AreEqual(unique, populated["Unique"], domain);
                    Assert.AreEqual(0L, empty["Present"], domain);
                    Assert.AreEqual(0L, empty["Unique"], domain);
                    checks++;
                }
                await Check("Boolean", new (string, object?)[] {
                    ("a", true), ("a", false), ("a", true), ("b", null) },
                    h => CosmicQueryMetadata.Value<bool?>(h, "stock"), 3, 2);
                await Check("Int32", new (string, object?)[] {
                    ("a", int.MinValue), ("a", int.MaxValue), ("a", int.MaxValue), ("b", null) },
                    h => CosmicQueryMetadata.Value<int?>(h, "stock"), 3, 2);
                var negativeSingleZero = BitConverter.Int32BitsToSingle(int.MinValue);
                var negativeDoubleZero = BitConverter.Int64BitsToDouble(long.MinValue);
                Assert.AreEqual(int.MinValue, BitConverter.SingleToInt32Bits(negativeSingleZero));
                Assert.AreEqual(long.MinValue, BitConverter.DoubleToInt64Bits(negativeDoubleZero));
                var first = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(1f) + 1);
                var second = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(1f) + 2);
                Assert.AreNotEqual(first, second);
                await Check("Single", new (string, object?)[] {
                    ("a", 0f), ("a", negativeSingleZero), ("a", first), ("a", second),
                    ("a", second), ("b", null) },
                    h => CosmicQueryMetadata.Value<float?>(h, "stock"), 5, 3);
                await Check("Double", new (string, object?)[] {
                    ("a", 0d), ("a", negativeDoubleZero), ("a", double.Epsilon), ("a", -double.Epsilon),
                    ("a", double.MaxValue), ("a", double.MaxValue), ("a", -double.MaxValue), ("b", null) },
                    h => CosmicQueryMetadata.Value<double?>(h, "stock"), 7, 5);
                Assert.AreEqual(22, ids.Count);
            }
            else
            {
                var strings = await Seed(("a", "A"), ("a", "a"), ("a", "λ"), ("a", "λ"),
                    ("a", "x\0y"), ("a", "x"), ("a", null));
                var stringResult = await Run(strings, h => CosmicQueryMetadata.Value<string>(h, "stock"));
                Assert.IsFalse(stringResult.IsError, stringResult.Message + stringResult.Exception);
                var stringRow = stringResult.Result.Providers.Single().Rows.Single();
                Assert.AreEqual(6L, stringRow["Present"]); Assert.AreEqual(5L, stringRow["Unique"]); checks++;
                var longs = await Seed(("a", 9007199254740992L), ("a", 9007199254740993L),
                    ("a", 9007199254740993L), ("b", null));
                var integerResult = await Run(longs, h => CosmicQueryMetadata.Value<long?>(h, "stock"));
                Assert.IsFalse(integerResult.IsError, integerResult.Message + integerResult.Exception);
                var rows = integerResult.Result.Providers.Single().Rows;
                Assert.AreEqual(2, rows.Count);
                Assert.AreEqual(2L, rows.Single(row => (string)row["Category"] == "a")["Unique"]);
                Assert.AreEqual(0L, rows.Single(row => (string)row["Category"] == "b")["Unique"]); checks++;
                var invalid = await Seed(("z", "invalid integer"));
                var invalidResult = await Run(invalid.Concat(longs).ToArray(),
                    h => CosmicQueryMetadata.Value<long?>(h, "stock"), emptyPage: true);
                Assert.IsTrue(invalidResult.IsError); Assert.AreEqual("CQL_DATA_INVALID", invalidResult.ErrorCode); checks++;
                var excess = await Run(longs, h => CosmicQueryMetadata.Value<long?>(h, "stock"), 1, true);
                Assert.IsTrue(excess.IsError); Assert.AreEqual("CQL_QUERY_LIMIT", excess.ErrorCode); checks++;
                Assert.AreEqual(12, ids.Count);
            }
            complete = true;
        }
        finally
        {
            if (active)
            {
                var stopped = await provider.DeActivateProviderAsync();
                Assert.IsFalse(stopped.IsError, stopped.Message + stopped.Exception);
            }
            if (mongo != null)
            {
                Assert.IsTrue(database.StartsWith("cosmic_distinct_", StringComparison.Ordinal));
                await mongo.DropDatabaseAsync(database);
                using var names = await mongo.ListDatabaseNamesAsync();
                Assert.IsFalse((await names.ToListAsync()).Contains(database));
            }
            if (authority == ProviderType.Neo4jOASIS)
            {
                using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
                await using var session = driver.AsyncSession();
                var owned = ids.Select(id => id.ToString("D")).ToArray();
                await (await session.RunAsync("MATCH (h:Holon) WHERE h.Id IN $owned DETACH DELETE h", new { owned })).ConsumeAsync();
                await (await session.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $owned DETACH DELETE v", new { owned })).ConsumeAsync();
                var remaining = await (await session.RunAsync(
                    "MATCH (n) WHERE (n:Holon AND n.Id IN $owned) OR (n:OasisRecordVersion AND n.Kind='Holon' AND n.RecordId IN $owned) RETURN count(n) AS n",
                    new { owned })).SingleAsync();
                Assert.AreEqual(0L, remaining["n"].As<long>());
            }
            if (provider is IDisposable disposable) disposable.Dispose();
            Assert.AreEqual(workRoot, Path.GetDirectoryName(Path.GetFullPath(work)));
            Directory.Delete(work, true); Assert.IsFalse(Directory.Exists(work));
            if (complete)
            {
                var root = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_CQL_MONGO_ENTITY_PROJECTION_ROOT")!);
                var report = Path.Combine(root, "TestResults/Neo4jCurrent/" + providerName
                    + (projectedSetJourney ? "-projected-set-native.json" : setHttpJourney ? "-set-http-native.json" : setJourney ? "-set-native.json" : statelessGetJourney ? "-stateless-get-native.json" : rangeRegistration ? "-session-range-native.json" : snapshotPreparation ? "-unfiltered-snapshot-native.json" : dateKeysHttp ? "-mapped-date-keys-native.json" : unfilteredMappingHttp ? "-unfiltered-mapping-native.json" : whereJourney ? "-boolean-where-native.json" : inspectionJourney ? "-inspection-native-journey.json" : lifecycleJourney ? "-lifecycle-native-journey.json" : updateJourney ? "-update-native-journey.json" : statelessLinqJourney ? "-stateless-linq-native-journey.json" : statelessLifetimeJourney ? "-stateless-lifetime-native-journey.json" : syncJourney ? "-sync-native-journey.json" : findJourney ? "-find-native-journey.json" : readOnlyJourney ? "-read-only-native-journey.json" : childrenReferenceJourney ? "-children-reference-native-journey.json" : parentReferenceJourney ? "-parent-reference-native-journey.json" : statelessInsertJourney ? "-stateless-insert-native-journey.json" : statelessJourney ? "-stateless-native-journey.json" : sessionJourney ? "-session-native-journey.json" : utcDateFilterNative ? "-utc-date-filter-native.json" : int32FilterHttp ? "-int32-filter-http-native.json" : int32FilterNative ? "-int32-filter-native.json" : guidFilterHttp ? "-guid-filter-http-native.json" : predicateCrossingHttp ? "-predicate-crossing-http-native.json" : guidRowsHttp ? "-guid-rows-http-native.json" : mixedGuidHttp ? "-grouped-guid-http-native.json" : guidDomains ? "-grouped-guid-native.json" : sharedInputs ? "-grouped-shared-inputs.json" : singlePerformance ? "-grouped-distinct-single-profile.json" : requiredDomains ? "-grouped-distinct-required-native.json" : mixedHttp ? "-grouped-distinct-http-native.json" : primitiveDomains ? "-grouped-distinct-primitives-native.json" : "-grouped-distinct-native.json"));
                if (projectedSetJourney)
                    await File.WriteAllTextAsync(report,JsonSerializer.Serialize(new {
                        provider=providerName,checks,ownedCleanupIds=ids.Count,successfulHolonWrites=4,
                        queryElapsedMilliseconds=queryElapsed,nativeProviderVerified=true,
                        allFourSetOperationsVerified=true,concatMultiplicityVerified=true,independentWindowsVerified=true,
                        selectedTupleEqualityVerified=true,hiddenInputColumnsExcludedVerified=true,
                        foreignRowsHiddenVerified=true,inputLimitBeforeEmptyOutputVerified=true,
                        ordinaryReadPreservedVerified=true,exactCleanupVerified=true }));
                else if (setHttpJourney)
                    await File.WriteAllTextAsync(report,JsonSerializer.Serialize(new {
                        provider=providerName,checks,ownedCleanupIds=ids.Count,successfulHolonWrites=1,
                        queryElapsedMilliseconds=queryElapsed,nativeProviderVerified=true,realHttpVerified=true,
                        concatMultiplicityVerified=true,foreignRowsHiddenVerified=true,anonymousRefusedVerified=true,
                        usageSettledVerified=true,oneNativeAttemptPerReadVerified=true,exactCleanupVerified=true }));
                else if (setJourney)
                    await File.WriteAllTextAsync(report,JsonSerializer.Serialize(new {
                        provider=providerName,checks,ownedCleanupIds=ids.Count,successfulHolonWrites=4,
                        queryElapsedMilliseconds=queryElapsed,nativeProviderVerified=true,
                        allFourSetOperationsVerified=true,concatMultiplicityVerified=true,independentWindowsVerified=true,
                        foreignRowsHiddenVerified=true,inputLimitBeforeEmptyOutputVerified=true,
                        ordinaryReadPreservedVerified=true,exactCleanupVerified=true }));
                else if (statelessGetJourney)
                    await File.WriteAllTextAsync(report,JsonSerializer.Serialize(new {
                        provider=providerName,checks,ownedCleanupIds=ids.Count,successfulHolonWrites=2,
                        queryElapsedMilliseconds=queryElapsed,nativeProviderVerified=true,
                        freshInstancesVerified=true,persistedUpdateVisibleVerified=true,syncAsyncAgreementVerified=true,
                        absentAndForeignHiddenVerified=true,cancellationAndLifetimeVerified=true,exactCleanupVerified=true }));
                else if (rangeRegistration)
                    await File.WriteAllTextAsync(report,JsonSerializer.Serialize(new {
                        provider=providerName,checks,ownedCleanupIds=ids.Count,successfulHolonWrites=6,
                        queryElapsedMilliseconds=queryElapsed,nativeProviderVerified=true,
                        registrationDoesNotWriteVerified=true,explicitOwnedSaveVerified=true,
                        attachBaselineAndUpdateVerified=true,foreignPrivateReadExcludedVerified=true,
                        explicitUpdateRangeVerified=true,hardRemoveRangeVerified=true,
                        exactCleanupVerified=true }));
                else if (snapshotPreparation)
                    await File.WriteAllTextAsync(report,JsonSerializer.Serialize(new {
                        provider=providerName,checks,successfulHolonWrites=1,ownedCleanupIds=ids.Count,
                        rawNativeRecords=2,queryElapsedMilliseconds=queryElapsed,
                        nativeProviderVerified=true,emptySnapshotPredicateVerified=true,
                        originalFieldsVerified=true,privateStableSnapshotSavedVerified=true,
                        foreignOwnerRejectedVerified=true,nativeSourceUnchangedVerified=true,
                        exactCleanupVerified=true}));
                else if (dateKeysHttp)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider=providerName, checks,
                        successfulHolonWrites=4,ownedCleanupIds=ids.Count,rawNativeRecords=2,
                        successfulHttpQueries=4,foreignOwnerRequests=1,queryElapsedMilliseconds=queryElapsed,
                        nativeProviderVerified=true,realHttpVerified=true,emptyNativePredicateVerified=true,
                        completeIdentityUnionVerified=true,globalPagingVerified=true,foreignOwnerRejectedVerified=true,
                        nativeSourceUnchangedVerified=true,noNativeOnlyHolonMaterializedVerified=true,exactCleanupVerified=true,
                        exactUtcTickKeysVerified=true,nullKeyGroupVerified=true,staleCachedKeyExcludedVerified=true }));
                else if (unfilteredMappingHttp)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        successfulHolonWrites = 4, ownedCleanupIds = ids.Count, rawNativeRecords = 2,
                        successfulHttpQueries = 2, foreignOwnerRequests = 1, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, realHttpVerified = true, emptyNativePredicateVerified = true,
                        completeIdentityUnionVerified = true, globalPagingVerified = true,
                        foreignOwnerRejectedVerified = true, nativeSourceUnchangedVerified = true,
                        noNativeOnlyHolonMaterializedVerified = true, exactCleanupVerified = true }));
                else if (whereJourney)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        ownedCleanupIds = ids.Count, successfulHolonWrites = 5, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, typedWhereCompiledVerified = true,
                        nestedBooleanRelationshipsVerified = true, nativePageVerified = true,
                        nativeCountVerified = true, nativeProjectionVerified = true,
                        foreignActorIsolationVerified = true, exactCleanupVerified = true }));
                else if (inspectionJourney)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        ownedCleanupIds = ids.Count, successfulHolonWrites = 3, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, defaultReadOnlyMaterializationVerified = true,
                        existingModePreservedVerified = true, explicitAddAttachMutableVerified = true,
                        immutableEntryStateVerified = true, pendingIntentInspectionVerified = true,
                        readOnlyNoFlushVerified = true, disposedInspectionRefusedVerified = true,
                        exactCleanupVerified = true }));
                else if (lifecycleJourney)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        ownedCleanupIds = ids.Count, successfulHolonWrites = 3, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, fluentModelConstraintsVerified = true, preDispatchValidationVerified = true, reentrancyRefusedVerified = true,
                        committedCallbackFailureRetainsOutcomeVerified = true, noOpSkipsCallbacksVerified = true,
                        ownedWriteRefusalSkipsCallbackVerified = true, cancellationAndIdentityRefusedVerified = true,
                        exactCleanupVerified = true }));
                else if (updateJourney)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        ownedCleanupIds = ids.Count, successfulHolonWrites = 3, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, detachedValuesPersistedVerified = true, forcedTrackedUpdateVerified = true,
                        staleAndForeignRefusedVerified = true, revisionlessBatchRefusedVerified = true,
                        reloadAndReadOnlyCancelIntentVerified = true, nativeSurvivorVerified = true, exactCleanupVerified = true }));
                else if (statelessLinqJourney)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        ownedCleanupIds = ids.Count, successfulHolonWrites = 2, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, freshStatelessRowsVerified = true, nativeProjectionAndTerminalsVerified = true,
                        trackingRefusedVerified = true, foreignRowsHiddenVerified = true, managedIdentityAndNoFlushVerified = true,
                        closedExecutionRefusedVerified = true, exactCleanupVerified = true }));
                else if (statelessLifetimeJourney)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        ownedCleanupIds = ids.Count, successfulHolonWrites = 3, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, busyDisposeRefusedVerified = true, closedBeforeEnumerationVerified = true,
                        borrowedProviderPreservedVerified = true, freshSessionWriteVerified = true, idempotentDisposeVerified = true,
                        hardDeleteAbsenceVerified = true, exactCleanupVerified = true }));
                else if (syncJourney)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        ownedCleanupIds = ids.Count, successfulHolonWrites = 8, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, managedSyncVerified = true, statelessSyncVerified = true,
                        asyncNativeAgreementVerified = true, generatedAcknowledgementVerified = true,
                        staleSyncRefusedVerified = true, hardDeleteAbsenceVerified = true, exactCleanupVerified = true }));
                else if (findJourney)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        ownedCleanupIds = ids.Count, successfulHolonWrites = 2, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, canonicalCacheAndPendingValuesVerified = true, freshAuthorizationVerified = true,
                        readsDoNotFlushVerified = true, negativeCacheAbsentVerified = true, detachLoadsFreshVerified = true,
                        localKeyGuardVerified = true, exactCleanupVerified = true }));
                else if (readOnlyJourney)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        ownedCleanupIds = ids.Count, successfulHolonWrites = 4, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, readOnlyUpdatesSuppressedVerified = true, mutableBaselineVerified = true,
                        laterMutableWriteVerified = true, reloadRetainsModeVerified = true, explicitDeletionVerified = true,
                        detachClearFlagsRemovedVerified = true, exactCleanupVerified = true }));
                else if (childrenReferenceJourney)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        ownedCleanupIds = ids.Count, successfulHolonWrites = 4, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, canonicalChildrenVerified = true, pendingValuesPreservedVerified = true,
                        freshSourceAuthorizationVerified = true, privateChildrenHiddenVerified = true,
                        readsDoNotFlushVerified = true, partialReplacementRefusedVerified = true, exactCleanupVerified = true }));
                else if (parentReferenceJourney)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        ownedCleanupIds = ids.Count, successfulHolonWrites = 3, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, canonicalParentVerified = true, pendingValuesPreservedVerified = true,
                        freshSourceAuthorizationVerified = true, privateTargetHiddenVerified = true,
                        readsDoNotFlushVerified = true, freshDetachedParentVerified = true, exactCleanupVerified = true }));
                else if (statelessInsertJourney)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        ownedCleanupIds = ids.Count, successfulHolonWrites = 3, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, generatedIdsVerified = true, createOnlyVerified = true,
                        wholeBatchAuthorizationVerified = true, failedIdentitiesUnpublishedVerified = true,
                        existingRecordsUnchangedVerified = true, parentPreservedVerified = true, exactCleanupVerified = true }));
                else if (statelessJourney)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        ownedCleanupIds = ids.Count, successfulHolonWrites = 8, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, explicitRevisionsVerified = true, freshIdentityVerified = true,
                        localEditsNotFlushedVerified = true, mixedCreateUpdateVerified = true,
                        staleBatchAtomicityVerified = true, actorIsolationVerified = true,
                        wholeBatchDeleteAuthorizationVerified = true, hardRecordsAbsentVerified = true,
                        exactCleanupVerified = true }));
                else if (sessionJourney)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        ownedCleanupIds = ids.Count, successfulHolonWrites = 8, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, ownedBatchVerified = true, identityResolutionVerified = true,
                        pendingValuesPreservedVerified = true, stateTransitionsVerified = true,
                        clearReloadVerified = true, actorIsolationVerified = true, exactCleanupVerified = true,
                        noTrackingVerified = true, databaseValuesVerified = true, explicitReloadVerified = true, standardLinqVerified = true, linqTrackingModesVerified = true, deferredDeletionVerified = true, removalCancellationVerified = true, wholeBatchDeleteAuthorizationVerified = true, softRecordRetainedVerified = true, hardRecordsAbsentVerified = true, parentBoundBeforeWriteVerified = true, unsupportedMixNotCommittedVerified = true }));
                else if (utcDateFilterNative)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        rawNativeRecords = providerName == "MongoDBOASIS" ? 17 : providerName == "Neo4jOASIS" ? 18 : 15,
                        nativeQueries = 16, foreignOwnerRequests = 1, successfulHolonWrites = 0,
                        queryElapsedMilliseconds = queryElapsed, nativeProviderVerified = true,
                        actualApiCompilerVerified = true, typedUtcDateVerified = true, exact100nsVerified = true,
                        nativeTemporalVerified = providerName != "SQLLiteDBOASIS",
                        nullabilityVerified = true, invalidCalendarAndPrecisionExcludedVerified = true,
                        nativeSourceUnchangedVerified = true, noHolonMaterializedVerified = true, exactCleanupVerified = true }));
                else if (int32FilterHttp)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        successfulHolonWrites = 4, ownedCleanupIds = ids.Count, rawNativeRecords = 3,
                        successfulHttpQueries = 4, foreignOwnerRequests = 1, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, realHttpVerified = true, typedInt32Verified = true,
                        staleCacheExcludedVerified = true, globalPagingVerified = true,
                        nativeSourceUnchangedVerified = true, storedCacheUnchangedVerified = true,
                        usageSettledVerified = true, noReadMaterializationVerified = true, exactCleanupVerified = true }));
                else if (int32FilterNative)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        rawNativeRecords = 8, nativeQueries = 8, foreignOwnerRequests = 1, successfulHolonWrites = 0,
                        queryElapsedMilliseconds = queryElapsed, nativeProviderVerified = true,
                        actualApiCompilerVerified = true, typedInt32Verified = true, wideIntegersExcludedVerified = true,
                        invalidAndNullExcludedVerified = true, nativeSourceUnchangedVerified = true,
                        noHolonMaterializedVerified = true, exactCleanupVerified = true }));
                else if (guidFilterHttp)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        successfulHolonWrites = 4, ownedCleanupIds = ids.Count, rawNativeRecords = providerName == "MongoDBOASIS" ? 3 : 2,
                        successfulHttpQueries = 4, foreignOwnerRequests = 1, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, realHttpVerified = true, typedGuidFiltersVerified = true,
                        equalityAndInequalityVerified = true, staleCacheCrossingVerified = true,
                        nativeSourceUnchangedVerified = true, storedCacheUnchangedVerified = true,
                        usageSettledVerified = true, exactCleanupVerified = true }));
                else if (predicateCrossingHttp)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        successfulHolonWrites = 4, ownedCleanupIds = ids.Count, rawNativeRecords = 2,
                        successfulHttpQueries = 2, foreignOwnerRequests = 1, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, realHttpVerified = true, stalePredicateExcluded = true,
                        livePredicateIncluded = true, foreignOwnerRefused = true,
                        nativeSourceUnchangedVerified = true, storedCacheUnchangedVerified = true,
                        usageSettledVerified = true, exactCleanupVerified = true }));
                else if (guidRowsHttp)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        successfulHolonWrites = 4, ownedCleanupIds = ids.Count, rawNativeRecords = 2,
                        successfulHttpQueries = 3, foreignOwnerRequests = 1, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, realHttpVerified = true, typedGuidRowsVerified = true,
                        globalAscendingGuidPageVerified = true, globalDescendingGuidPageVerified = true,
                        virtualOnlyRemainsUnstoredVerified = true, provenanceVerified = true,
                        staleCacheReconciledVerified = true, foreignOwnerRefused = true,
                        nativeSourceUnchangedVerified = true, storedCacheUnchangedVerified = true,
                        usageSettledVerified = true, exactCleanupVerified = true }));
                else if (mixedGuidHttp)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        successfulHolonWrites = 4, ownedCleanupIds = ids.Count, rawNativeRecords = 2, successfulHttpQueries = 2,
                        foreignOwnerRequests = 1, queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, realHttpVerified = true, storedVirtualSameGroupVerified = true,
                        guidNormalizedIdentityVerified = true, virtualOnlyRemainsUnstoredVerified = true,
                        distinctCountVerified = true, havingPagingVerified = true,
                        staleCacheReconciledVerified = true, foreignOwnerRefused = true,
                        nativeSourceUnchangedVerified = true, storedCacheUnchangedVerified = true,
                        usageSettledVerified = true, exactCleanupVerified = true }));
                else if (guidDomains)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        successfulHolonWrites = ids.Count, mongoBinaryRepresentations = mongo == null ? 0 : 2,
                        queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, mixedRepresentationsVerified = true,
                        canonicalGuidVerified = true, requiredGuidVerified = true, missingNullVerified = true,
                        offPageInvalidRefused = true, exactCleanupVerified = true }));
                else if (sharedInputs)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        successfulHolonWrites = ids.Count, nativeProviderVerified = true,
                        independentReducersVerified = true, canonicalMetadataSeparatedVerified = true,
                        ordinalMetadataPathsVerified = true, nullableRequiredSeparatedVerified = true,
                        offPageInvalidRefused = true, exactCleanupVerified = true }));
                else if (singlePerformance)
                {
                    Assert.IsNotNull(singleProfile);
                    singleProfile["provider"] = providerName;
                    singleProfile["checks"] = checks;
                    singleProfile["successfulHolonWrites"] = ids.Count;
                    singleProfile["exactCleanupVerified"] = true;
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(singleProfile));
                }
                else if (mixedHttp)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        successfulHolonWrites = ids.Count, rawNativeRecords = 2, successfulHttpQueries = 2,
                        foreignOwnerRequests = 1, elapsedMilliseconds = timer.ElapsedMilliseconds,
                        nativeProviderVerified = true, realHttpVerified = true, storedVirtualSameGroupVerified = true,
                        distinctCountVerified = true, havingPagingVerified = true, staleCacheReconciledVerified = true,
                        foreignOwnerRefused = true, nativeSourceUnchangedVerified = true, storedCacheUnchangedVerified = true,
                        usageSettledVerified = true, exactCleanupVerified = true }));
                else if (requiredDomains)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        successfulHolonWrites = ids.Count, elapsedMilliseconds = timer.ElapsedMilliseconds,
                        queryElapsedMilliseconds = queryElapsed, nativeProviderVerified = true,
                        requiredMetadataVerified = true, canonicalStringVerified = true, canonicalBooleanVerified = true,
                        canonicalInt32Verified = true, adjacentInt64Verified = true, adjacentSingleVerified = true,
                        signedZeroVerified = true, subnormalDoubleVerified = true, exactCleanupVerified = true }));
                else if (primitiveDomains)
                    await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                        successfulHolonWrites = ids.Count, elapsedMilliseconds = timer.ElapsedMilliseconds,
                        queryElapsedMilliseconds = queryElapsed,
                        nativeProviderVerified = true, booleanVerified = true, int32Verified = true,
                        singleVerified = true, doubleVerified = true, adjacentSingleVerified = true,
                        signedZeroVerified = true, subnormalDoubleVerified = true,
                        allNullVerified = true, exactCleanupVerified = true }));
                else
                await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                    successfulHolonWrites = ids.Count, elapsedMilliseconds = timer.ElapsedMilliseconds,
                    nativeProviderVerified = true, nonNullCountVerified = true, ordinalUnicodeNullVerified = true,
                    adjacentInt64Verified = true, allNullVerified = true, offPageInvalidRefused = true,
                    excessGroupRefused = true, exactCleanupVerified = true }));
            }
        }
    }
}
