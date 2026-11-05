using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.IntegrationTests;

[TestClass]
public class QuestDbSymbolQueryTests
{
    [DataTestMethod]
    [DataRow("http://questdb.example/api/v1/sql/execute")]
    [DataRow("https://127.0.0.1/api/v1/sql/execute")]
    public void HostedConfigurationRejectsIneligibleEndpoint(string endpoint)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            schema = 1, endpoint, sourceOwner = Guid.NewGuid(), symbols = new[] { "BTC-USDT" }
        });
        Assert.ThrowsException<ArgumentException>(() => QuestDbSymbolQuerySource.FromJson(json));
    }

    [TestMethod]
    public async Task NativeCqlReturnsExactFullAndSubsetKeys()
    {
        var endpoint = Environment.GetEnvironmentVariable("OASIS_QUESTDB_SYMBOL_TEST_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
            Assert.Inconclusive("Start the isolated QuestDB symbol fixture and configure its endpoint.");

        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var keys = new[] { "BTC-USDT", "ETH-USDT", "SOL-USDT" };
        using var source = new QuestDbSymbolQuerySource(http, new Uri(endpoint), Guid.NewGuid(), keys);
        await VerifyExactSelections(source, keys);
    }

    [TestMethod]
    public async Task HostedProfileReturnsExactFullAndSubsetKeys()
    {
        var configurationPath = Environment.GetEnvironmentVariable("OASIS_QUESTDB_HOSTED_SOURCE_CONFIG");
        if (string.IsNullOrWhiteSpace(configurationPath))
            Assert.Inconclusive("Provide the reviewed QuestDB HTTPS source configuration file.");

        using var source = QuestDbSymbolQuerySource.FromJson(await File.ReadAllTextAsync(configurationPath));
        await VerifyExactSelections(source, new[] { "BTC-USDT", "ETH-USDT", "SOL-USDT" });
    }

    private static async Task VerifyExactSelections(QuestDbSymbolQuerySource source, string[] keys)
    {
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(activation.Result, activation.Exception?.ToString() ?? activation.Message);
        var scope = CosmicQueryScope.Named(ProviderType.QuestDBOASIS).Result;
        var targets = new[] { new CosmicQueryTarget(ProviderType.QuestDBOASIS, source.QueryCapabilities) };

        foreach (var selected in new[] { keys, new[] { keys[0], keys[2] } })
        {
            var query = CosmicQuery<Holon>.Create(scope).Where(row => selected.Contains(
                CosmicQueryMetadata.Value<string>(row, QuestDbSymbolQuerySource.SymbolMetadata)));
            Assert.IsFalse(query.IsError, query.Message);
            var plan = CosmicQueryPlanner.Plan(query.Result, ProviderType.QuestDBOASIS, targets);
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
            CollectionAssert.AreEqual(selected, result.Result.Select(row =>
                row.ProviderUniqueStorageKey[ProviderType.QuestDBOASIS]).ToArray());
        }
    }
}
