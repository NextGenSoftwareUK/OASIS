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
    public async Task GroupedDistinctUsesRealNativeProviderAndCompleteInputProof(string providerName)
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
                Guid[] selected, Expression<Func<Holon, T>> selector, int maximumGroups = 1000, bool emptyPage = false)
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
                return await CosmicGroupedExecutor.ExecuteAsync(planned.Result,
                    new[] { (ICosmicQueryStorageProvider)provider });
            }
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
                var report = Path.Combine(root, "TestResults/Neo4jCurrent/" + providerName + "-grouped-distinct-native.json");
                await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { provider = providerName, checks,
                    successfulHolonWrites = ids.Count, elapsedMilliseconds = timer.ElapsedMilliseconds,
                    nativeProviderVerified = true, nonNullCountVerified = true, ordinalUnicodeNullVerified = true,
                    adjacentInt64Verified = true, allNullVerified = true, offPageInvalidRefused = true,
                    excessGroupRefused = true, exactCleanupVerified = true }));
            }
        }
    }
}
