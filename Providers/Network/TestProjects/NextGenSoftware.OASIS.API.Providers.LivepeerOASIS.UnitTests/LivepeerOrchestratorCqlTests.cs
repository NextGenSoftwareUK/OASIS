using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.LivepeerOASIS.UnitTests;

[TestClass]
public sealed class LivepeerOrchestratorCqlTests
{
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly string[] Addresses =
    {
        "0x525419ff5707190389bfb5c87c375d710f5fcb0e",
        "0xac2e50c8f7ac0f82923a7df9d9903f6ec4741919",
        "0x4f4758f7167b18e1f5b3c1a7575e3eb584894dbc"
    };
    private const ProviderType Provider = ProviderType.LivepeerOASIS;

    [TestMethod]
    public async Task WrongChainRejectsActivationBeforeAnyContractCall()
    {
        using var transport = new ContractTransport("0x1");
        using var source = new LivepeerOrchestratorQuerySource(Owner, Addresses, Addresses[0], transport);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(source.IsProviderActivated);
        CollectionAssert.AreEqual(new[] { "eth_chainId" }, transport.Methods);
    }

    [TestMethod]
    public async Task InvalidSecondContractResultNeverPublishesPartialRows()
    {
        using var transport = new ContractTransport("0xa4b1", malformedThirdCall: true);
        using var source = new LivepeerOrchestratorQuerySource(Owner, Addresses, Addresses[0], transport);
        var activated = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activated.IsError, activated.Exception?.Message);
        var result = await source.ExecuteCosmicQueryAsync(Plan(source, Addresses[..2]));
        Assert.IsTrue(result.IsError);
        Assert.AreEqual("CQL_QUERY_FAILED", result.ErrorCode);
        Assert.IsNull(result.Result);
        Assert.AreEqual(3, transport.Methods.Count(method => method == "eth_call"));
    }

    [TestMethod, TestCategory("LivepeerOrchestratorCqlNative")]
    public async Task OfficialBondingManagerReturnsThreeAndTwoExactFinalizedRows()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_LIVEPEER_ORCHESTRATOR_VERIFY") != "1")
        {
            Assert.Inconclusive("Enable the reviewed public Livepeer reads explicitly.");
            return;
        }

        using var source = new LivepeerOrchestratorQuerySource(Owner, Addresses, Addresses[0]);
        var activated = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activated.IsError, activated.Exception?.Message);
        foreach (var count in new[] { 3, 2 })
        {
            var selected = Addresses.Take(count).ToArray();
            var result = await source.ExecuteCosmicQueryAsync(Plan(source, selected));
            Assert.IsFalse(result.IsError, result.Exception?.Message);
            CollectionAssert.AreEquivalent(selected, result.Result.Select(row =>
                row.ProviderUniqueStorageKey[Provider]).ToArray());
            Assert.AreEqual(1, result.Result.Select(row => row.MetaData["LivepeerArbitrumBlockHash"].ToString()).Distinct().Count());
            Assert.IsTrue(result.Result.All(row => row.IsPublic && row.CreatedByAvatarId == Owner &&
                bool.TryParse(row.MetaData["LivepeerOrchestratorActive"].ToString(), out _)));
        }
    }

    private static CosmicQueryPlan<Holon> Plan(LivepeerOrchestratorQuerySource source, string[] addresses)
    {
        var ids = addresses.Select(address => LivepeerOrchestratorQuerySource.LogicalId(Owner, address)).ToArray();
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, Provider, ids).Result).Result
            .Where(row => ids.Contains(row.Id));
        Assert.IsFalse(query.IsError, query.Message);
        var plan = CosmicQueryPlanner.Plan(query.Result, Provider, new[] { new CosmicQueryTarget(Provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return plan.Result;
    }

    private sealed class ContractTransport(string chain, bool malformedThirdCall = false) : HttpMessageHandler
    {
        private const string Genesis = "0x7ee576b35482195fc49205cec9af72ce14f003b9ae69f6ba0faef4514be8b442";
        private static readonly string FinalizedHash = "0x" + new string('1', 64);
        private int calls;
        public List<string> Methods { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("https://arb1.arbitrum.io/rpc", request.RequestUri!.AbsoluteUri);
            using var document = JsonDocument.Parse(await request.Content!.ReadAsByteArrayAsync(token));
            var body = document.RootElement;
            var method = body.GetProperty("method").GetString()!;
            var parameters = body.GetProperty("params");
            Methods.Add(method);
            object result;
            switch (method)
            {
                case "eth_chainId":
                    result = chain;
                    break;
                case "eth_getBlockByNumber":
                    result = parameters[0].GetString() == "0x0" ? new { hash = Genesis, number = "0x0" }
                        : new { hash = FinalizedHash, number = "0x1" };
                    break;
                case "eth_call":
                    Assert.AreEqual("0x35bcf3c30594191d53231e4ff333e8a770453e40", parameters[0].GetProperty("to").GetString());
                    Assert.IsTrue(parameters[0].GetProperty("data").GetString()!.StartsWith("0x08802374", StringComparison.Ordinal));
                    Assert.AreEqual(FinalizedHash, parameters[1].GetProperty("blockHash").GetString());
                    Assert.IsTrue(parameters[1].GetProperty("requireCanonical").GetBoolean());
                    calls++;
                    result = "0x" + new string('0', 63) + (malformedThirdCall && calls == 3 ? "2" : "1");
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
