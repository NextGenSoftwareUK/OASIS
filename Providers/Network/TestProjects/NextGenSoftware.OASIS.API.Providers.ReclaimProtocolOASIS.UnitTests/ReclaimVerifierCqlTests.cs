using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ReclaimProtocolOASIS.UnitTests;

[TestClass]
public sealed class ReclaimVerifierCqlTests
{
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly string[] Networks = { "base", "arbitrum", "polygon" };
    private static readonly Dictionary<string, string> Semaphores = new()
    {
        ["base"] = "0xa2bff333d2e5468cf4dc6194eb4b5ddefa2625c0",
        ["arbitrum"] = "0x9c04beb93d3c84f94a89e4102a24475e29084dbe",
        ["polygon"] = "0xf90085f5fd1a3beb8678623409b3811ecec5f6a5"
    };
    private const ProviderType Provider = ProviderType.ReclaimProtocolOASIS;

    [TestMethod]
    public async Task UnexpectedSemaphoreRejectsTheReviewedDeployment()
    {
        using var transport = new VerifierTransport(wrongGenesis: false);
        using var source = new ReclaimVerifierQuerySource(Owner, transport);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual(3, transport.Methods.Count(method => method == "eth_call"));
    }

    [TestMethod]
    public async Task WrongGenesisStopsBeforeFinalizedOrContractRead()
    {
        using var transport = new VerifierTransport(wrongGenesis: true);
        using var source = new ReclaimVerifierQuerySource(Owner, transport);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(source.IsProviderActivated);
        CollectionAssert.AreEqual(new[] { "eth_chainId", "eth_getBlockByNumber" }, transport.Methods.ToArray());
    }

    [TestMethod, TestCategory("ReclaimVerifierNative")]
    public async Task DeployedVerifiersReturnThreeAndTwoFinalizedConfigurationRows()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_RECLAIM_VERIFIER_VERIFY") != "1")
        {
            Assert.Inconclusive("Enable the reviewed public Reclaim verifier reads explicitly.");
            return;
        }

        using var source = new ReclaimVerifierQuerySource(Owner);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.Message);
        foreach (var count in new[] { 3, 2 })
        {
            var networks = Networks.Take(count).ToArray();
            var ids = networks.Select(network => ReclaimVerifierQuerySource.LogicalId(Owner, network)).ToArray();
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
                Assert.AreEqual(Semaphores[network], row.MetaData["ReclaimSemaphore"].ToString());
                Assert.IsTrue(Convert.ToInt64(row.MetaData["ReclaimCurrentEpoch"]) > 0);
                Assert.IsTrue(Convert.ToInt64(row.MetaData["ReclaimEpochDurationSeconds"]) > 0);
                Assert.AreEqual(66, row.MetaData["ReclaimBlockHash"].ToString()!.Length);
            }
        }
    }

    private sealed class VerifierTransport(bool wrongGenesis) : HttpMessageHandler
    {
        private const string Genesis = "0xf712aa9241cc24369b143cf6dce85f0902a9731e70d66818a3a5845b296c73dd";
        private static readonly string Finalized = "0x" + new string('1', 64);
        public ConcurrentQueue<string> Methods { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("https://mainnet.base.org/", request.RequestUri!.AbsoluteUri);
            using var document = JsonDocument.Parse(await request.Content!.ReadAsByteArrayAsync(token));
            var body = document.RootElement;
            var method = body.GetProperty("method").GetString()!;
            var parameters = body.GetProperty("params");
            Methods.Enqueue(method);
            object result;
            switch (method)
            {
                case "eth_chainId":
                    result = "0x2105";
                    break;
                case "eth_getBlockByNumber":
                    result = parameters[0].GetString() == "0x0" ? new { hash = wrongGenesis ? Finalized : Genesis, number = "0x0" }
                        : new { hash = Finalized, number = "0x1" };
                    break;
                case "eth_call":
                    Assert.AreEqual("0x8cdc031d5b7f148ab0435028b16c682c469cefc3", parameters[0].GetProperty("to").GetString());
                    Assert.AreEqual(Finalized, parameters[1].GetProperty("blockHash").GetString());
                    Assert.IsTrue(parameters[1].GetProperty("requireCanonical").GetBoolean());
                    var value = parameters[0].GetProperty("data").GetString() switch
                    {
                        "0x76671808" => "1",
                        "0x289989fd" => "15180",
                        "0xf7da2ac9" => "1",
                        _ => throw new AssertFailedException("Unexpected Reclaim selector.")
                    };
                    result = "0x" + value.PadLeft(64, '0');
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
