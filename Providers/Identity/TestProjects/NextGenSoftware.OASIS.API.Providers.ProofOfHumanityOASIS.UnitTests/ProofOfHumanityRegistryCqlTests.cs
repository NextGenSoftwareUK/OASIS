#nullable enable
using System;
using System.Collections.Concurrent;
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

namespace NextGenSoftware.OASIS.API.Providers.ProofOfHumanityOASIS.UnitTests;

[TestClass]
public class ProofOfHumanityRegistryCqlTests
{
    private const ProviderType Provider = ProviderType.ProofOfHumanityOASIS;
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly string[] Addresses =
    {
        "0x16044e1063c08670f8653055a786b7cc2034d2b0",
        "0xa478095886659168e8812154fb0de39f103e74b2",
        "0xc5e9ddebb09cd64dfacab4011a0d5cedaf7c9bdb",
    };
    private static readonly Guid[] Ids = Addresses.Select(address => ProofOfHumanityRegistryQuerySource.LogicalId(Owner, address)).ToArray();

    [TestMethod]
    public async Task QueriesOnlySelectedKeysAtOneFinalizedBlockPerChain()
    {
        using var transport = new Rpc();
        using var source = new ProofOfHumanityRegistryQuerySource(Owner, Addresses, Addresses[0], transport);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        await CheckAsync(source, Ids, Ids, true);
        await CheckAsync(source, Ids[..2], Ids[..2], true);
        await CheckAsync(source, new[] { Guid.NewGuid() }, null, true);
        Assert.AreEqual(12, transport.Calls.Count);
        Assert.AreEqual(2, transport.Calls.Count(address => address == Addresses[2]));
    }

    [DataTestMethod]
    [DataRow("foreign")]
    [DataRow("malformed")]
    public async Task FailureOnOneChainPreventsActivation(string fault)
    {
        using var transport = new Rpc(fault);
        using var source = new ProofOfHumanityRegistryQuerySource(Owner, Addresses, Addresses[0], transport);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(source.IsProviderActivated);
    }

    [TestMethod]
    [TestCategory("ProofOfHumanityPublicCqlNative")]
    public async Task NativeMainnetsReturnThreeAndTwoReviewedRegistryObservations()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_PROOF_OF_HUMANITY_VERIFY_PUBLIC") != "1")
        {
            Assert.Inconclusive("Enable the reviewed public Proof of Humanity probe explicitly.");
            return;
        }
        using var source = new ProofOfHumanityRegistryQuerySource(Owner, Addresses, Addresses[0]);
        var active = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(active.IsError, active.Exception?.ToString());
        await CheckAsync(source, Ids, Ids, false);
        await CheckAsync(source, Ids[..2], Ids[..2], false);
    }

    private static async Task CheckAsync(ProofOfHumanityRegistryQuerySource source, Guid[] keys, Guid[]? expected, bool controlled)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, Provider, keys).Result).Result
            .Where(row => keys.Contains(row.Id)).Result;
        var plan = CosmicQueryPlanner.Plan(query, Provider, new[] { new CosmicQueryTarget(Provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var result = await source.ExecuteCosmicQueryAsync(plan.Result);
        if (expected == null)
        {
            Assert.IsTrue(result.IsError);
            Assert.IsNull(result.Result);
            return;
        }
        Assert.IsFalse(result.IsError, result.Exception?.ToString());
        CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
        foreach (var chain in new[] { "Ethereum", "Gnosis" })
        {
            var hashes = result.Result.Select(row => row.MetaData[$"ProofOfHumanity{chain}BlockHash"].ToString()).Distinct().ToArray();
            Assert.AreEqual(1, hashes.Length);
            Assert.AreEqual(66, hashes[0]!.Length);
            foreach (var row in result.Result)
            {
                Assert.IsTrue(row.IsPublic && row.CreatedByAvatarId == Owner && !row.Children.Any());
                Assert.AreEqual(row.Name, row.ProviderUniqueStorageKey[Provider]);
                Assert.IsTrue(bool.TryParse(row.MetaData[$"ProofOfHumanity{chain}Reported"].ToString(), out var reported));
                Assert.AreEqual(controlled && chain == "Gnosis", reported);
            }
        }
    }

    private sealed class Rpc(string? fault = null) : HttpMessageHandler
    {
        public readonly ConcurrentBag<string> Calls = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.IsNull(request.Headers.Authorization);
            var gnosis = request.RequestUri!.Host == "rpc.gnosischain.com";
            Assert.AreEqual(gnosis ? "rpc.gnosischain.com" : "ethereum-rpc.publicnode.com", request.RequestUri.Host);
            var hash = "0x" + new string(gnosis ? 'a' : 'b', 64);
            using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;
            var method = root.GetProperty("method").GetString();
            var parameters = root.GetProperty("params");
            object result;
            switch (method)
            {
                case "eth_chainId":
                    result = gnosis ? "0x64" : fault == "foreign" ? "0xa" : "0x1";
                    break;
                case "eth_getBlockByNumber":
                    Assert.AreEqual("finalized", parameters[0].GetString());
                    Assert.IsFalse(parameters[1].GetBoolean());
                    result = new { number = "0xa1", hash };
                    break;
                case "eth_call":
                    Assert.AreEqual(Addresses[gnosis ? 0 : 1], parameters[0].GetProperty("to").GetString()!.ToLowerInvariant());
                    var data = parameters[0].GetProperty("data").GetString()!;
                    StringAssert.StartsWith(data, "0xf72c436f" + new string('0', 24));
                    var address = "0x" + data[^40..];
                    Assert.IsTrue(Addresses.Contains(address));
                    Assert.AreEqual(hash, parameters[1].GetProperty("blockHash").GetString());
                    Assert.IsTrue(parameters[1].GetProperty("requireCanonical").GetBoolean());
                    Calls.Add(address);
                    result = "0x" + new string('0', 63) + (gnosis ? "1" : fault == "malformed" ? "2" : "0");
                    break;
                default: throw new AssertFailedException("Unexpected registry RPC method.");
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { jsonrpc = "2.0", id = 1, result })
            };
        }
    }
}
