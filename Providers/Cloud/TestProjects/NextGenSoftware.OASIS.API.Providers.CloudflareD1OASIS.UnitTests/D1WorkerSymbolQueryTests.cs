using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.CloudflareD1OASIS.UnitTests;

[TestClass]
public class D1WorkerSymbolQueryTests
{
    [TestMethod]
    public async Task NativeCqlReturnsExactFullAndSubsetKeys()
    {
        var endpoint = Environment.GetEnvironmentVariable("OASIS_D1_SYMBOL_TEST_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
            Assert.Inconclusive("Start the isolated D1 Worker fixture and configure its endpoint.");

        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var keys = new[] { "BTC-USDT", "ETH-USDT", "SOL-USDT" };
        var source = new D1WorkerSymbolQuerySource(http, new Uri(endpoint), Guid.NewGuid(), keys);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(activation.Result, activation.Exception?.ToString() ?? activation.Message);
        var scope = CosmicQueryScope.Named(ProviderType.CloudflareD1OASIS).Result;
        var targets = new[] { new CosmicQueryTarget(ProviderType.CloudflareD1OASIS, source.QueryCapabilities) };

        foreach (var selected in new[] { keys, new[] { keys[0], keys[2] } })
        {
            var query = CosmicQuery<Holon>.Create(scope).Where(row => selected.Contains(
                CosmicQueryMetadata.Value<string>(row, D1WorkerSymbolQuerySource.SymbolMetadata)));
            Assert.IsFalse(query.IsError, query.Message);
            var plan = CosmicQueryPlanner.Plan(query.Result, ProviderType.CloudflareD1OASIS, targets);
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
            CollectionAssert.AreEqual(selected, result.Result.Select(row =>
                row.ProviderUniqueStorageKey[ProviderType.CloudflareD1OASIS]).ToArray());
        }
    }

    [DataTestMethod]
    [DataRow("""{"symbols":["BTC-USDT","ETH-USDT"]}""")]
    [DataRow("""{"symbols":["BTC-USDT","BTC-USDT"]}""")]
    [DataRow("""{"symbols":["BTC-USDT","SOL-USDT"],"symbols":["BTC-USDT","SOL-USDT"]}""")]
    public async Task UnexpectedWorkerRowsFailWithoutReturningPartialResults(string body)
    {
        using var http = new HttpClient(new SymbolResponse(body));
        var keys = new[] { "BTC-USDT", "SOL-USDT" };
        var provider = ProviderType.CloudflareD1OASIS;
        var source = new D1WorkerSymbolQuerySource(http, new Uri("https://d1.example/symbols"), Guid.NewGuid(), keys);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(activation.IsError);
        Assert.AreEqual("CQL_SOURCE_UNAVAILABLE", activation.ErrorCode);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row, D1WorkerSymbolQuerySource.SymbolMetadata)));
        var plan = CosmicQueryPlanner.Plan(query.Result, provider,
            new[] { new CosmicQueryTarget(provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);

        var result = await source.ExecuteCosmicQueryAsync(plan.Result);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual("CQL_QUERY_FAILED", result.ErrorCode);
        Assert.IsNull(result.Result);
    }

    private sealed class SymbolResponse(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(body) });
    }
}
