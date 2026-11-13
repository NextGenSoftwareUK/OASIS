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
using NextGenSoftware.OASIS.API.Providers.HyperlaneOASIS;

namespace NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests;

[TestClass]
public class HyperlanePublicMessageMetadataTests
{
    private const string Endpoint = "https://explorer4.hasura.app/v1/graphql";
    private static readonly string[] Keys = Enumerable.Range(1, 3).Select(i => $"0x{i:x64}").ToArray();

    private sealed class Explorer : HttpMessageHandler
    {
        public string Fault { get; set; } = "";
        public int Calls { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual(Endpoint, request.RequestUri.AbsoluteUri);
            using var payload = JsonDocument.Parse(await request.Content.ReadAsStringAsync(token));
            var query = payload.RootElement.GetProperty("query").GetString();
            Assert.IsTrue(query.Contains("msg_id:{_in:"));
            Assert.IsFalse(query.Contains("message_body"));
            var selected = Keys.Where(key => query.Contains(key[2..])).Reverse().ToArray();
            if (Fault == "missing") selected = selected.Skip(1).ToArray();
            if (Fault == "duplicate") selected[0] = selected[1];
            if (Fault == "foreign") selected[0] = $"0x{4:x64}";
            var rows = selected.Select(key => new
            {
                msg_id = @"\x" + key[2..],
                origin_domain_id = 1,
                destination_domain_id = 10,
                is_delivered = true
            }).ToArray();
            string body = Fault == "graphql"
                ? """{"errors":[{"message":"time limit exceeded"}]}"""
                : JsonSerializer.Serialize(new { data = new { message_view = rows } });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        }
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("missing")]
    [DataRow("duplicate")]
    [DataRow("foreign")]
    [DataRow("graphql")]
    public async Task ExactKeyQueryReturnsCompleteOrderedRowsOrFails(string fault)
    {
        using var explorer = new Explorer();
        using var client = new HttpClient(explorer);
        var owner = Guid.NewGuid();
        using var source = new HyperlanePublicMessageMetadataSource(client, new Uri(Endpoint), Keys, owner);
        var activation = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        explorer.Fault = fault;
        var keys = Keys.Take(2).ToArray();
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.HyperlaneOASIS).Result)
            .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row, HyperlanePublicMessageMetadataSource.MessageIdMetadata))).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.HyperlaneOASIS,
            new[] { new CosmicQueryTarget(ProviderType.HyperlaneOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var result = await source.ExecuteCosmicQueryAsync(plan.Result);
        Assert.AreEqual(2, explorer.Calls);
        if (fault.Length != 0)
        {
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("CQL_QUERY_FAILED", result.ErrorCode);
            Assert.IsNull(result.Result);
            return;
        }
        Assert.IsFalse(result.IsError, result.Exception?.ToString());
        CollectionAssert.AreEqual(keys, result.Result.Select(row => row.ProviderUniqueStorageKey[ProviderType.HyperlaneOASIS]).ToArray());
        Assert.IsTrue(result.Result.All(row => row.IsPublic && row.CreatedByAvatarId == owner));
    }
    [TestMethod]
    public async Task QualifiedExplorerCatalogueExecutesNativeCql()
    {
        var json = Environment.GetEnvironmentVariable("OASIS_HYPERLANE_NATIVE_CQL_SOURCE");
        if (string.IsNullOrWhiteSpace(json))
            Assert.Inconclusive("An explicitly qualified Explorer catalogue is required.");

        using var source = HyperlanePublicMessageMetadataSource.FromJson(json);
        using var document = JsonDocument.Parse(json);
        var keys = document.RootElement.GetProperty("messageIds").EnumerateArray()
            .Select(value => value.GetString()).ToArray();
        Assert.AreEqual(3, keys.Length);
        var activation = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        foreach (var selected in new[] { keys, keys.Take(2).ToArray() })
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.HyperlaneOASIS).Result)
                .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row,
                    HyperlanePublicMessageMetadataSource.MessageIdMetadata))).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.HyperlaneOASIS,
                new[] { new CosmicQueryTarget(ProviderType.HyperlaneOASIS, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            CollectionAssert.AreEqual(selected,
                result.Result.Select(row => row.ProviderUniqueStorageKey[ProviderType.HyperlaneOASIS]).ToArray());
        }
    }
}
