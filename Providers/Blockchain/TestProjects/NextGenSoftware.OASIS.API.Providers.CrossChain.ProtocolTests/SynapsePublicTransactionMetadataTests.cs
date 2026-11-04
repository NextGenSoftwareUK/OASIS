using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SynapseOASIS;

namespace NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests;

[TestClass]
public class SynapsePublicTransactionMetadataTests
{
    private const string Endpoint = "https://api.synapseprotocol.com/transaction-id/";
    private static readonly string[] Keys = Enumerable.Range(1, 3).Select(i => $"0x{i:x64}").ToArray();

    private sealed class Scan : HttpMessageHandler
    {
        public string Fault { get; set; } = "";
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            Assert.AreEqual(HttpMethod.Get, request.Method);
            var uri = request.RequestUri.AbsoluteUri;
            Assert.IsTrue(uri.StartsWith(Endpoint, StringComparison.Ordinal));
            var key = uri[Endpoint.Length..];
            Assert.IsTrue(Keys.Contains(key));
            var row = new
            {
                Bridge = new
                {
                    transactionId = Fault == "foreign" && key == Keys[1] ? $"0x{4:x64}" : key,
                    originChainId = Fault == "invalid-chain" ? 0 : 999,
                    destChainId = 10
                }
            };
            var status = Fault == "http" ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK;
            var body = Fault == "missing" ? "{}" : JsonSerializer.Serialize(row);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("missing")]
    [DataRow("invalid-chain")]
    [DataRow("foreign")]
    [DataRow("http")]
    public async Task ExactKeyQueryReturnsCompleteOrderedRowsOrFails(string fault)
    {
        using var explorer = new Scan();
        var owner = Guid.NewGuid();
        using var source = new SynapsePublicTransactionMetadataSource(explorer, Keys, owner);
        var activation = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        explorer.Fault = fault;
        var keys = Keys.Take(2).ToArray();
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SynapseOASIS).Result)
            .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row, SynapsePublicTransactionMetadataSource.TransactionIdMetadata))).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.SynapseOASIS,
            new[] { new CosmicQueryTarget(ProviderType.SynapseOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var result = await source.ExecuteCosmicQueryAsync(plan.Result);
        Assert.AreEqual(fault is "" or "foreign" ? 5 : 4, explorer.Calls);
        if (fault.Length != 0)
        {
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("CQL_QUERY_FAILED", result.ErrorCode);
            Assert.IsNull(result.Result);
            return;
        }
        Assert.IsFalse(result.IsError, result.Exception?.ToString());
        CollectionAssert.AreEqual(keys, result.Result.Select(row => row.ProviderUniqueStorageKey[ProviderType.SynapseOASIS]).ToArray());
        Assert.IsTrue(result.Result.All(row => row.IsPublic && row.CreatedByAvatarId == owner));
        Assert.IsTrue(result.Result.All(row => (long)row.MetaData["SynapseOriginChainId"] == 999));
    }
    [TestMethod]
    public async Task QualifiedRfqCatalogueExecutesNativeCql()
    {
        var json = Environment.GetEnvironmentVariable("OASIS_SYNAPSE_NATIVE_CQL_SOURCE");
        if (string.IsNullOrWhiteSpace(json))
            Assert.Inconclusive("An explicitly qualified RFQ catalogue is required.");

        using var source = SynapsePublicTransactionMetadataSource.FromJson(json);
        using var document = JsonDocument.Parse(json);
        var keys = document.RootElement.GetProperty("messageIds").EnumerateArray()
            .Select(value => value.GetString()).ToArray();
        Assert.AreEqual(3, keys.Length);
        var activation = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        foreach (var selected in new[] { keys, keys.Take(2).ToArray() })
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SynapseOASIS).Result)
                .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row,
                    SynapsePublicTransactionMetadataSource.TransactionIdMetadata))).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.SynapseOASIS,
                new[] { new CosmicQueryTarget(ProviderType.SynapseOASIS, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            CollectionAssert.AreEqual(selected,
                result.Result.Select(row => row.ProviderUniqueStorageKey[ProviderType.SynapseOASIS]).ToArray());
        }
    }
}
