using System.Net;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class AwsCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);

    [TestMethod]
    public async Task NativeAwsCqlReadsThreeRecordsAndFiltersTwoWithoutScanning()
    {
        const string endpoint = "http://127.0.0.1:18002";
        Assert.AreEqual(endpoint, Environment.GetEnvironmentVariable("OASIS_AWS_CQL_ENDPOINT"));
        var state = Environment.GetEnvironmentVariable("OASIS_AWS_CQL_STATE")!;
        Assert.IsTrue(Path.IsPathFullyQualified(state));
        var path = Path.Combine(state, "demo-seed.json");
        var pending = path + ".pending";
        Assert.IsFalse(File.Exists(pending), "Unresolved original seed outcome must be reviewed.");
        Assert.IsTrue(File.Exists(path), "This runner qualifies retained records; it must never reseed.");
        var seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(path))!;
        Assert.AreEqual(3, seed.Ids.Distinct().Count()); Assert.AreNotEqual(Guid.Empty, seed.Actor);
        var provider = new AWSOASIS("us-east-1", "CosmicCqlLocal", "owned-local-only", endpoint) { IsVersionControlEnabled = false };
        using var native = new AmazonDynamoDBClient(new BasicAWSCredentials("CosmicCqlLocal", "owned-local-only"),
            new AmazonDynamoDBConfig { ServiceURL = endpoint, AuthenticationRegion = "us-east-1", UseHttp = true, MaxErrorRetry = 0 });
        try
        {
            var activated = await provider.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError, activated.Message + activated.Exception);
            var names = new[] { "Espresso", "Latte", "Tea" };
            var before = new Dictionary<Guid,string>();
            for (var i = 0; i < seed.Ids.Length; i++)
            {
                var id = seed.Ids[i];
                var stored = await native.GetItemAsync(new GetItemRequest { TableName = "OasisHolons", ConsistentRead = true,
                    Key = new() { ["id"] = new() { S = id.ToString("D") } } });
                Assert.AreEqual(HttpStatusCode.OK, stored.HttpStatusCode);
                using var payload = JsonDocument.Parse(stored.Item["data_json"].S);
                Assert.AreEqual(names[i], payload.RootElement.GetProperty("Name").GetString());
                before.Add(id, stored.Item["data_json"].S);
            }
            foreach (var selected in new[] { seed.Ids, seed.Ids.Take(2).ToArray() })
            {
                var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AWSOASIS).Result)
                    .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.AWSOASIS, seed.Ids).Result).Result
                    .Where(h => selected.Contains(h.Id));
                Assert.IsFalse(query.IsError, query.Message);
                var plan = CosmicQueryPlanner.Plan(query.Result, ProviderType.AWSOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.AWSOASIS, provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Message + result.Exception);
                CollectionAssert.AreEquivalent(selected, result.Result.Select(h => h.Id).ToArray());
                Assert.AreEqual(selected.Length, result.LoadedCount);
            }
            foreach (var id in seed.Ids)
            {
                var after = await native.GetItemAsync(new GetItemRequest { TableName = "OasisHolons", ConsistentRead = true,
                    Key = new() { ["id"] = new() { S = id.ToString("D") } } });
                Assert.AreEqual(before[id], after.Item["data_json"].S, "Query cannot rewrite its native records.");
            }
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
        // Keep this tiny owned seed for SDK/HTTP/browser demos; no per-run recreation.
    }
}
