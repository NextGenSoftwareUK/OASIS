using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.LayerZeroOASIS.UnitTests;

[TestClass]
public sealed class LayerZeroV1EndpointCqlTests
{
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly string[] Networks = { "ethereum", "polygon", "arbitrum" };
    private static readonly Dictionary<string, int> ContractIds = new() { ["ethereum"] = 1, ["polygon"] = 9, ["arbitrum"] = 10 };
    private const ProviderType Provider = ProviderType.LayerZeroOASIS;

    [TestMethod]
    public async Task PublishedIdCannotSubstituteForTheDeployedContractIdentity()
    {
        using var transport = new EndpointTransport(wrongGenesis: false);
        using var source = new LayerZeroV1EndpointQuerySource(Owner, transport);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual(3, transport.Methods.Count(method => method == "eth_call"));
    }

    [TestMethod]
    public async Task WrongGenesisStopsBeforeFinalizedOrContractRead()
    {
        using var transport = new EndpointTransport(wrongGenesis: true);
        using var source = new LayerZeroV1EndpointQuerySource(Owner, transport);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(source.IsProviderActivated);
        CollectionAssert.AreEqual(new[] { "eth_chainId", "eth_getBlockByNumber" }, transport.Methods.ToArray());
    }

    [TestMethod, TestCategory("LayerZeroV1EndpointNative")]
    public async Task DeployedEndpointsReturnThreeAndTwoFinalizedConfigurationRows()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_LAYERZERO_V1_ENDPOINT_VERIFY") != "1")
        {
            Assert.Inconclusive("Enable the reviewed public LayerZero v1 endpoint reads explicitly.");
            return;
        }

        using var source = new LayerZeroV1EndpointQuerySource(Owner);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.Message);
        foreach (var count in new[] { 3, 2 })
        {
            var networks = Networks.Take(count).ToArray();
            var ids = networks.Select(network => LayerZeroV1EndpointQuerySource.LogicalId(Owner, network)).ToArray();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, Provider, ids).Result).Result
                .Where(row => ids.Contains(row.Id));
            Assert.IsFalse(query.IsError, query.Message);
            var plan = CosmicQueryPlanner.Plan(query.Result, Provider, new[] { new CosmicQueryTarget(Provider, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Exception?.Message);
            CollectionAssert.AreEquivalent(ids, result.Result.Select(row => row.Id).ToArray());
            foreach (var row in result.Result)
            {
                var network = row.ProviderUniqueStorageKey[Provider];
                Assert.IsTrue(row.IsPublic && row.CreatedByAvatarId == Owner);
                Assert.AreEqual(ContractIds[network], Convert.ToInt32(row.MetaData["LayerZeroV1ContractChainId"]));
                Assert.AreEqual(ContractIds[network] + 100, Convert.ToInt32(row.MetaData["LayerZeroV1PublishedChainId"]));
                Assert.AreEqual(66, row.MetaData["LayerZeroV1BlockHash"].ToString()!.Length);
            }
        }
    }

    private sealed class EndpointTransport(bool wrongGenesis) : HttpMessageHandler
    {
        private const string Genesis = "0xd4e56740f876aef8c010b86a40d5f56745a118d0906a34e69aec8c0db1cb8fa3";
        private static readonly string Finalized = "0x" + new string('1', 64);
        public ConcurrentQueue<string> Methods { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("https://eth.drpc.org/", request.RequestUri!.AbsoluteUri);
            using var document = JsonDocument.Parse(await request.Content!.ReadAsByteArrayAsync(token));
            var body = document.RootElement;
            var method = body.GetProperty("method").GetString()!;
            var parameters = body.GetProperty("params");
            Methods.Enqueue(method);
            object result;
            switch (method)
            {
                case "eth_chainId":
                    result = "0x1";
                    break;
                case "eth_getBlockByNumber":
                    result = parameters[0].GetString() == "0x0" ? new { hash = wrongGenesis ? Finalized : Genesis, number = "0x0" }
                        : new { hash = Finalized, number = "0x1" };
                    break;
                case "eth_call":
                    Assert.AreEqual("0x66a71dcef29a0ffbdbe3c6a460a3b5bc225cd675", parameters[0].GetProperty("to").GetString());
                    Assert.AreEqual(Finalized, parameters[1].GetProperty("blockHash").GetString());
                    Assert.IsTrue(parameters[1].GetProperty("requireCanonical").GetBoolean());
                    result = "0x" + (parameters[0].GetProperty("data").GetString() == "0x3408e470" ? "65" : "04").PadLeft(64, '0');
                    break;
                default:
                    throw new AssertFailedException("Unexpected RPC method.");
            }
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(new
            {
                jsonrpc = "2.0", id = body.GetProperty("id").GetInt32(), result
            }) };
        }
    }
}
