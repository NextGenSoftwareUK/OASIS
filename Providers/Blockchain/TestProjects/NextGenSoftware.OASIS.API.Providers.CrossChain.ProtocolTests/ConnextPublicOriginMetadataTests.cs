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
using NextGenSoftware.OASIS.API.Providers.ConnextOASIS;

namespace NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests;

[TestClass]
public class ConnextPublicOriginMetadataTests
{
    private const string Endpoint = "https://eth.blockscout.com/api/v2/transactions/";
    private static readonly string[] Keys = Enumerable.Range(1, 3).Select(i => $"0x{i:x64}").ToArray();

    private static readonly ConnextOriginEventClient.Identity[] Events = Keys.Select((key, index) =>
        new ConnextOriginEventClient.Identity(key, $"0x{index + 10:x64}", $"0x{index + 20:x64}", 123, index)).ToArray();

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
            Assert.IsTrue(uri.EndsWith("/logs", StringComparison.Ordinal));
            var tx = uri[Endpoint.Length..^5];
            var identity = Events.Single(value => value.TransactionHash == tx);
            var key = identity.TransferId;
            var row = new
            {
                address = new { hash = ConnextOriginEventClient.ContractAddress },
                transaction_hash = tx,
                block_hash = Fault == "wrong-block" ? $"0x{99:x64}" : identity.BlockHash,
                block_number = identity.BlockNumber,
                index = identity.LogIndex,
                topics = new[] { "0xed8e6ba697dd65259e5ce532ac08ff06d1a3607bcec58f8f0937fe36a5666c54",
                    Fault == "foreign" && key == Keys[1] ? $"0x{4:x64}" : key, key, key }
            };
            var status = Fault == "http" ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK;
            var items = Fault == "missing" ? Array.Empty<object>() : new object[] { row };
            var body = JsonSerializer.Serialize(new { items, next_page_params = (object)null });
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("missing")]
    [DataRow("wrong-block")]
    [DataRow("foreign")]
    [DataRow("http")]
    public async Task ExactKeyQueryReturnsCompleteOrderedRowsOrFails(string fault)
    {
        using var explorer = new Scan();
        using var client = new HttpClient(explorer);
        var owner = Guid.NewGuid();
        using var source = new ConnextPublicOriginMetadataSource(client, Events, owner);
        var activation = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        explorer.Fault = fault;
        var keys = Keys.Take(2).ToArray();
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ConnextOASIS).Result)
            .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row, ConnextPublicOriginMetadataSource.TransferIdMetadata))).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.ConnextOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ConnextOASIS, source.QueryCapabilities) });
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
        CollectionAssert.AreEqual(keys, result.Result.Select(row => row.ProviderUniqueStorageKey[ProviderType.ConnextOASIS]).ToArray());
        Assert.IsTrue(result.Result.All(row => row.IsPublic && row.CreatedByAvatarId == owner));
        Assert.IsTrue(result.Result.All(row => (long)row.MetaData["ConnextOriginChainId"] == 1));
    }
    [TestMethod]
    public async Task QualifiedOriginCatalogueExecutesNativeCql()
    {
        var json = Environment.GetEnvironmentVariable("OASIS_CONNEXT_NATIVE_CQL_SOURCE");
        if (string.IsNullOrWhiteSpace(json))
            Assert.Inconclusive("An explicitly qualified Connext origin catalogue is required.");

        using var source = ConnextPublicOriginMetadataSource.FromJson(json);
        using var document = JsonDocument.Parse(json);
        var keys = document.RootElement.GetProperty("events").EnumerateArray()
            .Select(value => value.GetProperty("transferId").GetString()).ToArray();
        Assert.AreEqual(3, keys.Length);
        var activation = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        foreach (var selected in new[] { keys, keys.Take(2).ToArray() })
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ConnextOASIS).Result)
                .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row,
                    ConnextPublicOriginMetadataSource.TransferIdMetadata))).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.ConnextOASIS,
                new[] { new CosmicQueryTarget(ProviderType.ConnextOASIS, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            CollectionAssert.AreEqual(selected,
                result.Result.Select(row => row.ProviderUniqueStorageKey[ProviderType.ConnextOASIS]).ToArray());
        }
    }
}
