using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.IntegrationTests;

[TestClass]
public class QuestDbSymbolQueryTests
{
    [TestMethod]
    public async Task NativeCqlReturnsExactFullAndSubsetKeys()
    {
        var endpoint = Environment.GetEnvironmentVariable("OASIS_QUESTDB_SYMBOL_TEST_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
            Assert.Inconclusive("Start the isolated QuestDB symbol fixture and configure its endpoint.");

        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var keys = new[] { "BTC-USDT", "ETH-USDT", "SOL-USDT" };
        var source = new QuestDbSymbolQuerySource(http, new Uri(endpoint), Guid.NewGuid(), keys);
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
