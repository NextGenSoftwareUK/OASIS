using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.WorldIDOASIS.UnitTests;

[TestClass]
public sealed class WorldIdProtocolCqlTests
{
    private const ProviderType Provider = ProviderType.WorldIDOASIS;
    private const string Registry = "0x0000000000ae079eb8a274cd51c0f44a9e4d67d4";
    private const string Issuer = "0x941239840f4d9668da8be76b568e836b50685d2c";
    private const string Oprf = "0x0d8b461799474207a3d223553d4d5e6609cb0c69";
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly string[] Components = { "registry", "verifier", "issuer-registry" };

    [TestMethod]
    public async Task WrongGenesisRejectsActivationBeforeContractReads()
    {
        using var transport = new ProtocolTransport(wrongGenesis: true);
        using var source = new WorldIdProtocolQuerySource(Owner, transport);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(source.IsProviderActivated);
        CollectionAssert.AreEqual(new[] { "eth_chainId", "eth_getBlockByNumber" }, transport.Methods.ToArray());
    }

    [TestMethod]
    public async Task MiswiredVerifierFailsTheCompleteQuery()
    {
        using var transport = new ProtocolTransport(wrongGenesis: false);
        using var source = new WorldIdProtocolQuerySource(Owner, transport);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.Message);
        var result = await source.ExecuteCosmicQueryAsync(Plan(source, Components[..2]));
        Assert.IsTrue(result.IsError);
        Assert.AreEqual("CQL_QUERY_FAILED", result.ErrorCode);
        Assert.IsNull(result.Result);
        Assert.IsTrue(transport.Methods.Contains("eth_call:0xc6c15ae0"));
    }

    [TestMethod, TestCategory("WorldIdProtocolCqlNative")]
    public async Task PublishedWorldIdComponentsReturnExactThreeAndTwoFinalizedRows()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_WORLD_ID_PROTOCOL_VERIFY") != "1")
        {
            Assert.Inconclusive("Enable the reviewed public World ID v4 reads explicitly.");
            return;
        }

        using var source = new WorldIdProtocolQuerySource(Owner);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.Message);
        foreach (var count in new[] { 3, 2 })
        {
            var selected = Components.Take(count).ToArray();
            var result = await source.ExecuteCosmicQueryAsync(Plan(source, selected));
            Assert.IsFalse(result.IsError, result.Exception?.Message);
            CollectionAssert.AreEquivalent(selected, result.Result.Select(row => row.ProviderUniqueStorageKey[Provider]).ToArray());
            CollectionAssert.AreEquivalent(selected.Select(component => WorldIdProtocolQuerySource.LogicalId(Owner, component)).ToArray(),
                result.Result.Select(row => row.Id).ToArray());
            Assert.AreEqual(1, result.Result.Select(row => row.MetaData["WorldIdBlockHash"].ToString()).Distinct().Count());
            Assert.IsTrue(result.Result.All(row => row.IsPublic && row.CreatedByAvatarId == Owner));
            var verifier = result.Result.Single(row => row.ProviderUniqueStorageKey[Provider] == "verifier");
            Assert.AreEqual(Registry, verifier.MetaData["WorldIdRegistry"].ToString());
            Assert.AreEqual(Issuer, verifier.MetaData["WorldIdIssuerRegistry"].ToString());
            Assert.AreEqual(Oprf, verifier.MetaData["WorldIdOprfRegistry"].ToString());
        }
    }

    private static CosmicQueryPlan<Holon> Plan(WorldIdProtocolQuerySource source, string[] components)
    {
        var ids = components.Select(component => WorldIdProtocolQuerySource.LogicalId(Owner, component)).ToArray();
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, Provider, ids).Result).Result
            .Where(row => ids.Contains(row.Id));
        Assert.IsFalse(query.IsError, query.Message);
        var plan = CosmicQueryPlanner.Plan(query.Result, Provider, new[] { new CosmicQueryTarget(Provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return plan.Result;
    }

    private sealed class ProtocolTransport(bool wrongGenesis) : HttpMessageHandler
    {
        private const string Genesis = "0x70d316d2e0973b62332ba2e9768dd7854298d7ffe77f0409ffdb8d859f2d3fa3";
        private static readonly string Finalized = "0x" + new string('1', 64);
        public ConcurrentQueue<string> Methods { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("https://worldchain-mainnet.g.alchemy.com/public", request.RequestUri!.AbsoluteUri);
            using var document = JsonDocument.Parse(await request.Content!.ReadAsByteArrayAsync(token));
            var body = document.RootElement;
            var method = body.GetProperty("method").GetString()!;
            var args = body.GetProperty("params");
            object result;
            if (method == "eth_chainId")
            {
                Methods.Enqueue(method);
                result = "0x1e0";
            }
            else if (method == "eth_getBlockByNumber")
            {
                Methods.Enqueue(method);
                result = args[0].GetString() == "0x0" ? new { hash = wrongGenesis ? Finalized : Genesis, number = "0x0" }
                    : new { hash = Finalized, number = "0x1" };
            }
            else if (method == "eth_call")
            {
                var selector = args[0].GetProperty("data").GetString()!;
                Methods.Enqueue("eth_call:" + selector);
                Assert.AreEqual(Finalized, args[1].GetProperty("blockHash").GetString());
                Assert.IsTrue(args[1].GetProperty("requireCanonical").GetBoolean());
                var value = selector switch
                {
                    "0xfdab463d" => "1",
                    "0x8e5cdd50" => "1e",
                    "0x65b64a72" => "e10",
                    "0xc6c15ae0" => "1",
                    "0x340eb98f" => Issuer[2..],
                    "0x1dfc0cea" => Oprf[2..],
                    _ => throw new AssertFailedException("Unexpected World ID selector.")
                };
                result = "0x" + value.PadLeft(64, '0');
            }
            else throw new AssertFailedException("Unexpected RPC method.");
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(new
            {
                jsonrpc = "2.0", id = body.GetProperty("id").GetInt32(), result
            }) };
        }
    }
}
