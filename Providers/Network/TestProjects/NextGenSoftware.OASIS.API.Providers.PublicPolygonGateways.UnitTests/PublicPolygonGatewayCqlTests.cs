using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.QuickNodeOASIS;
using NextGenSoftware.OASIS.API.Providers.TenderlyOASIS;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.PublicPolygonGateways.UnitTests;

[TestClass]
public sealed class PublicPolygonGatewayCqlTests
{
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly string[] Addresses =
    {
        "0x0000000000000000000000000000000000000000",
        "0x000000000000000000000000000000000000dead",
        "0x0000000000000000000000000000000000001010"
    };

    [DataTestMethod]
    [DataRow(ProviderType.TenderlyOASIS, "https://tenderly.rpc.polygon.community/")]
    [DataRow(ProviderType.QuickNodeOASIS, "https://rpc-mainnet.matic.quiknode.pro/")]
    public async Task OnlyTheOfficialOriginAndPolygonChainCanActivate(ProviderType provider, string origin)
    {
        using var transport = new ForeignChain(origin);
        using var client = new HttpClient(transport);
        using var source = Create(provider, client);
        var result = await source.ActivateQuerySourceAsync(default);
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual(1, transport.Requests);
    }

    [DataTestMethod, TestCategory("PublicPolygonGatewaysCqlNative")]
    [DataRow(ProviderType.TenderlyOASIS, "TenderlyPolygon")]
    [DataRow(ProviderType.QuickNodeOASIS, "QuickNodePolygon")]
    public async Task OfficialPolygonGatewayReturnsThreeAndTwoFinalizedRows(ProviderType provider, string prefix)
    {
        if (Environment.GetEnvironmentVariable("COSMIC_PUBLIC_POLYGON_GATEWAYS_VERIFY") != "1")
        {
            Assert.Inconclusive("Enable the reviewed public Polygon gateway queries explicitly.");
            return;
        }

        using var source = Create(provider);
        var activated = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activated.IsError, activated.Exception?.Message);
        await Check(source, provider, prefix, Addresses);
        await Check(source, provider, prefix, Addresses[..2]);
    }

    private static EvmPublicAccountMetadataSource Create(ProviderType provider, HttpClient? client = null) =>
        provider switch
        {
            ProviderType.TenderlyOASIS => client is null
                ? new TenderlyPublicAccountMetadataSource(Addresses, Owner)
                : new TenderlyPublicAccountMetadataSource(client, Addresses, Owner),
            ProviderType.QuickNodeOASIS => client is null
                ? new QuickNodePublicAccountMetadataSource(Addresses, Owner)
                : new QuickNodePublicAccountMetadataSource(client, Addresses, Owner),
            _ => throw new ArgumentException("An explicitly reviewed gateway is required.", nameof(provider))
        };

    private static async Task Check(EvmPublicAccountMetadataSource source, ProviderType provider, string prefix, string[] addresses)
    {
        var metadataKey = source.QueryCapabilities.StringKeySetLookup.MetadataKey;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Owner).Result).Result
            .Where(row => addresses.Contains(CosmicQueryMetadata.Value<string>(row, metadataKey)));
        Assert.IsFalse(query.IsError, query.Message);
        var plan = CosmicQueryPlanner.Plan(query.Result, provider, new[] { new CosmicQueryTarget(provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var result = await source.ExecuteCosmicQueryAsync(plan.Result);
        Assert.IsFalse(result.IsError, result.Exception?.Message);
        CollectionAssert.AreEquivalent(addresses, result.Result.Select(row => row.ProviderUniqueStorageKey[provider]).ToArray());
        Assert.AreEqual(1, result.Result.Select(row => row.MetaData[prefix + "BlockHash"].ToString()).Distinct().Count());
        foreach (var row in result.Result)
        {
            Assert.IsTrue(row.IsPublic && row.CreatedByAvatarId == Owner);
            Assert.AreEqual("0x89", row.MetaData[prefix + "ChainId"].ToString());
            Assert.AreEqual("finalized", row.MetaData[prefix + "Commitment"].ToString());
        }
    }

    private sealed class ForeignChain(string origin) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Requests++;
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual(origin, request.RequestUri!.AbsoluteUri);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsByteArrayAsync(token));
            Assert.AreEqual("eth_chainId", body.RootElement.GetProperty("method").GetString());
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(new
            {
                jsonrpc = "2.0", id = body.RootElement.GetProperty("id").Clone(), result = "0x1"
            }) };
        }
    }
}
