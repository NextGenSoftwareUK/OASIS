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
using NextGenSoftware.OASIS.API.Providers.StargateOASIS;

namespace NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests;

[TestClass]
public class StargatePublicMessageMetadataTests
{
    private const string Endpoint = "https://scan.layerzero-api.com/v1/messages/guid/";
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
                guid = Fault == "foreign" && key == Keys[1] ? $"0x{4:x64}" : key,
                pathway = new
                {
                    srcEid = 30110,
                    dstEid = Fault == "foreign-endpoint" && key == Keys[1] ? 30102 : 30101,
                    nonce = ulong.MaxValue,
                    receiver = new { address = Fault == "foreign-receiver" && key == Keys[1]
                        ? "0x0000000000000000000000000000000000000001"
                        : "0x6d6620efa72948c5f68a3c8646d58c00d3f4a980" }
                },
                status = new { name = "DELIVERED" }
            };
            var rows = Fault == "missing" ? Array.Empty<object>() :
                Fault == "duplicate" ? new object[] { row, row } : new object[] { row };
            var status = Fault == "http" ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(status)
            { Content = new StringContent(JsonSerializer.Serialize(new { data = rows })) });
        }
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("missing")]
    [DataRow("duplicate")]
    [DataRow("foreign")]
    [DataRow("foreign-receiver")]
    [DataRow("foreign-endpoint")]
    [DataRow("http")]
    public async Task ExactKeyQueryReturnsCompleteOrderedRowsOrFails(string fault)
    {
        using var explorer = new Scan();
        using var client = new HttpClient(explorer);
        var owner = Guid.NewGuid();
        using var source = new StargatePublicMessageMetadataSource(client, Keys, owner);
        var activation = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        explorer.Fault = fault;
        var keys = Keys.Take(2).ToArray();
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.StargateOASIS).Result)
            .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row, StargatePublicMessageMetadataSource.MessageIdMetadata))).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.StargateOASIS,
            new[] { new CosmicQueryTarget(ProviderType.StargateOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var result = await source.ExecuteCosmicQueryAsync(plan.Result);
        Assert.AreEqual(fault is "" or "foreign" or "foreign-receiver" or "foreign-endpoint" ? 5 : 4, explorer.Calls);
        if (fault.Length != 0)
        {
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("CQL_QUERY_FAILED", result.ErrorCode);
            Assert.IsNull(result.Result);
            return;
        }
        Assert.IsFalse(result.IsError, result.Exception?.ToString());
        CollectionAssert.AreEqual(keys, result.Result.Select(row => row.ProviderUniqueStorageKey[ProviderType.StargateOASIS]).ToArray());
        Assert.IsTrue(result.Result.All(row => row.IsPublic && row.CreatedByAvatarId == owner));
    }
    [TestMethod]
    public async Task QualifiedScanCatalogueExecutesNativeCql()
    {
        var json = Environment.GetEnvironmentVariable("OASIS_STARGATE_NATIVE_CQL_SOURCE");
        if (string.IsNullOrWhiteSpace(json))
            Assert.Inconclusive("An explicitly qualified Scan catalogue is required.");

        using var source = StargatePublicMessageMetadataSource.FromJson(json);
        using var document = JsonDocument.Parse(json);
        var keys = document.RootElement.GetProperty("messageIds").EnumerateArray()
            .Select(value => value.GetString()).ToArray();
        Assert.AreEqual(3, keys.Length);
        var activation = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        foreach (var selected in new[] { keys, keys.Take(2).ToArray() })
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.StargateOASIS).Result)
                .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row,
                    StargatePublicMessageMetadataSource.MessageIdMetadata))).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.StargateOASIS,
                new[] { new CosmicQueryTarget(ProviderType.StargateOASIS, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            CollectionAssert.AreEqual(selected,
                result.Result.Select(row => row.ProviderUniqueStorageKey[ProviderType.StargateOASIS]).ToArray());
        }
    }
}
