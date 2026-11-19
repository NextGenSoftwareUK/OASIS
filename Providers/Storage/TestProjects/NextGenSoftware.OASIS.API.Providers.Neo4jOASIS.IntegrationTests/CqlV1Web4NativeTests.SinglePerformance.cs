using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [TestMethod]
    public Task NeoSingleGroupedDistinctColdWarmAndServerProfile()
        => RunGroupedDistinctNativeFixture("Neo4jOASIS", false, singlePerformance: true);

    private static async Task<Dictionary<string, object>> ProfileNeoSingleGroupedDistinct(Guid[] selected,
        Func<Task<OASISResult<CosmicGroupedExecution<CosmicProjectionRecord>>>> execute)
    {
        var grouping = CosmicQuery<Holon>.Create().Where(h => selected.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"),
                CosmicGroupingOptions.Create(1000).Result).Result;
        var intent = grouping.SelectColumns(new[] {
            CosmicGroupedBinding<Holon, string>.Key(grouping, "Category", 0).Result,
            CosmicGroupedBinding<Holon, string>.CountValues(grouping, "Present",
                h => CosmicQueryMetadata.Value<float>(h, "stock")).Result,
            CosmicGroupedBinding<Holon, string>.CountValues(grouping, "Unique",
                h => CosmicQueryMetadata.Value<float>(h, "stock"), true).Result
        });
        Assert.IsFalse(intent.IsError, intent.Message);
        var compile = Stopwatch.StartNew();
        var command = Neo4jOASIS.CompileCqlGrouped(intent.Result, 1000);
        compile.Stop();
        var timings = new Dictionary<string, long>();
        // Compile above is client-only: the first actual server execution remains cold.
        foreach (var label in new[] { "coldServer", "warm1", "warm2" })
        {
            var timer = Stopwatch.StartNew();
            var result = await execute();
            timings.Add(label, timer.ElapsedMilliseconds);
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Providers.Single().Rows.Single();
            Assert.AreEqual("a", row["Category"]);
            Assert.AreEqual(5L, row["Present"]);
            Assert.AreEqual(3L, row["Unique"]);
        }

        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687",
            AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var session = driver.AsyncSession(config => config.WithDefaultAccessMode(AccessMode.Read));
        using var transaction = await session.BeginTransactionAsync(config => config.WithTimeout(TimeSpan.FromSeconds(30)));
        var profileTimer = Stopwatch.StartNew();
        var cursor = await transaction.RunAsync("PROFILE " + command.Query, command.Parameters);
        var rows = await cursor.ToListAsync();
        var summary = await cursor.ConsumeAsync();
        profileTimer.Stop();
        Assert.AreEqual(1, rows.Count);
        Assert.IsTrue(summary.HasProfile);
        Assert.IsNotNull(summary.Profile);
        Assert.IsFalse(summary.Counters.ContainsUpdates);
        Assert.IsTrue(summary.ResultAvailableAfter.TotalMilliseconds >= 0);
        Assert.IsTrue(summary.ResultConsumedAfter.TotalMilliseconds >= 0);
        var operators = new List<object>();
        var stack = new Stack<IProfiledPlan>();
        stack.Push(summary.Profile);
        while (stack.Count != 0)
        {
            var node = stack.Pop();
            Assert.IsTrue(operators.Count < 4096, "Bound the diagnostic plan report.");
            operators.Add(new { operation = node.OperatorType, dbHits = node.DbHits,
                records = node.Records, timeRaw = node.Time });
            foreach (var child in node.Children) stack.Push(child);
        }
        Assert.IsTrue(operators.Count > 0);
        await transaction.RollbackAsync();
        return new Dictionary<string, object> {
            ["queryElapsedMilliseconds"] = timings,
            ["compileMilliseconds"] = compile.ElapsedMilliseconds,
            ["queryUtf8Bytes"] = Encoding.UTF8.GetByteCount(command.Query),
            ["querySha256"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(command.Query))).ToLowerInvariant(),
            ["parameterCount"] = command.Parameters.Count,
            ["profileMilliseconds"] = profileTimer.ElapsedMilliseconds,
            ["serverAvailableMilliseconds"] = summary.ResultAvailableAfter.TotalMilliseconds,
            ["serverConsumedMilliseconds"] = summary.ResultConsumedAfter.TotalMilliseconds,
            ["operators"] = operators,
            ["nativeProviderVerified"] = true,
            ["identicalIntentVerified"] = true,
            ["coldWarmResultsVerified"] = true,
            ["serverProfileVerified"] = true,
            ["profileUpdates"] = false
        };
    }
}
