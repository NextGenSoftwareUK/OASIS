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

    private static async Task<Dictionary<string, object>> ProfileNeoSingleGroupedDistinct(
        Func<Action<CosmicGroupedSelection<Holon, string, CosmicProjectionRecord>>,
            Task<OASISResult<CosmicGroupedExecution<CosmicProjectionRecord>>>> execute)
    {
        CosmicGroupedSelection<Holon, string, CosmicProjectionRecord>? actualQuery = null;
        var timings = new Dictionary<string, long>();
        // Observe the actual admitted query; no second query specification or extra compilation in the timed call.
        foreach (var label in new[] { "coldServer", "warm1", "warm2" })
        {
            var timer = Stopwatch.StartNew();
            var result = await execute(query => actualQuery = query);
            timings.Add(label, timer.ElapsedMilliseconds);
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Providers.Single().Rows.Single();
            Assert.AreEqual("a", row["Category"]);
            Assert.AreEqual(5L, row["Present"]);
            Assert.AreEqual(3L, row["Unique"]);
        }

        Assert.IsNotNull(actualQuery);
        var compile = Stopwatch.StartNew();
        var command = Neo4jOASIS.CompileCqlGrouped(actualQuery, 1000);
        compile.Stop();
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
            ["compileTimingPhase"] = "after-three-provider-queries",
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
