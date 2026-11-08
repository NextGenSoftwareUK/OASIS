using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SelfProtocolOASIS.UnitTests;

[TestClass]
public class SelfProtocolRegistryQueryTests
{
    private const ProviderType Provider = ProviderType.SelfProtocolOASIS;
    private static readonly Guid Owner = Guid.Parse("8dbbf7bc-8a31-4a35-8796-621f17cb5e39");
    private static readonly int[] Attestations = { 1, 3, 4 };
    private static readonly Guid[] Ids = Attestations.Select(x => SelfProtocolRegistryQuerySource.LogicalId(Owner, x)).ToArray();

    [TestMethod]
    public async Task ExactIdsResolveHubBindingsAndPreserveUnsignedRootAndBlockProvenance()
    {
        using var replies = new Replies();
        using var source = new SelfProtocolRegistryQuerySource(Owner, Attestations, replies);
        var activated = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activated.IsError, activated.Exception?.ToString());
        var full = await Read(source, Ids);
        Check(full, Ids);
        Assert.AreEqual(((BigInteger.One << 256) - 1).ToString(CultureInfo.InvariantCulture),
            full.Result.Single(x => x.Id == Ids[0]).MetaData["SelfProtocolRootDecimal"].ToString());
        Check(await Read(source, Ids.Take(2).ToArray()), Ids.Take(2).ToArray());
        Assert.AreEqual(42, replies.Requests, "Seven mandatory calls per exact observation, with no retries.");
        var before = replies.Requests;
        var unknown = await Read(source, new[] { Guid.NewGuid() });
        Assert.IsTrue(unknown.IsError); Assert.IsNull(unknown.Result);
        Assert.AreEqual(before, replies.Requests, "Unknown IDs must not reach the transport.");
        var mixed = await Read(source, new[] { Ids[0], Guid.NewGuid() });
        Assert.IsTrue(mixed.IsError); Assert.IsNull(mixed.Result);
        Assert.AreEqual(before, replies.Requests, "Validate the entire selection before dispatching its first native read.");
    }

    [DataTestMethod]
    [DataRow("wrong-chain")]
    [DataRow("wrong-genesis")]
    [DataRow("wrong-block-hash")]
    [DataRow("wrong-block-number")]
    [DataRow("duplicate-header")]
    [DataRow("empty-hub-code")]
    [DataRow("empty-registry-code")]
    [DataRow("invalid-code")]
    [DataRow("zero-registry")]
    [DataRow("noncanonical-address")]
    [DataRow("short-root")]
    [DataRow("uppercase-root")]
    [DataRow("rpc-error")]
    [DataRow("wrong-id")]
    [DataRow("duplicate-result")]
    [DataRow("http-failure")]
    [DataRow("oversized-root")]
    public async Task MandatoryIdentityCodeAndAbiFailuresRefuseActivation(string fault)
    {
        using var replies = new Replies { Fault = fault };
        using var source = new SelfProtocolRegistryQuerySource(Owner, Attestations, replies);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError, fault); Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual("CQL_SOURCE_UNAVAILABLE", result.ErrorCode);
        Assert.IsTrue(replies.Requests <= 7, "A failed mandatory stage cannot retry or continue past the root read.");
    }

    [TestMethod]
    public async Task OneFailedRegistryReadPublishesNoPartialRows()
    {
        using var replies = new Replies();
        using var source = new SelfProtocolRegistryQuerySource(Owner, Attestations, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        replies.FailRegistry = Replies.Address(3);
        var result = await Read(source, Ids);
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual(28, replies.Requests);
    }

    [TestMethod]
    public async Task InactiveAndCancelledReadsMakeNoTransportRequest()
    {
        using var replies = new Replies();
        using var source = new SelfProtocolRegistryQuerySource(Owner, Attestations, replies);
        Assert.IsTrue((await Read(source, Ids)).IsError);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.IsTrue((await source.ActivateQuerySourceAsync(cancellation.Token)).IsError);
        Assert.AreEqual(0, replies.Requests);
    }

    [TestMethod]
    public void InvalidCataloguesFailBeforeTransportConstruction()
    {
        foreach (var values in new[] { Array.Empty<int>(), new[] { 1, 1 }, new[] { 0 }, new[] { 2 }, new[] { 5 }, new[] { 1, 3, 4, 1 } })
            Assert.ThrowsException<ArgumentException>(() => new SelfProtocolRegistryQuerySource(Owner, values));
        Assert.ThrowsException<ArgumentException>(() => new SelfProtocolRegistryQuerySource(Guid.Empty, Attestations));
    }

    [TestMethod, TestCategory("SelfProtocolRegistryNative")]
    public async Task PublicRegistryCqlReturnsThreeAndTwoExactCanonicalObservations()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_SELF_PROTOCOL_VERIFY_PUBLIC") != "1")
        {
            Assert.Inconclusive("Explicitly opt in to this public read-only native schedule."); return;
        }
        using var source = SelfProtocolRegistryQuerySource.ForReviewedTeachingCatalogue(Owner);
        var activated = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activated.IsError, activated.Exception?.ToString());
        var all = await Read(source, Ids); Check(all, Ids);
        var roots = new Dictionary<string, string>
        {
            ["1"] = "16467781073809921235718380216749760257063201868872900454449846404863196687156",
            ["3"] = "15132602829901602832616170110205297154820203316036968499170345415260925905600",
            ["4"] = "17403457413196001059533281012865110387998358053288702523938180990850375925989"
        };
        foreach (var row in all.Result)
            Assert.AreEqual(roots[row.MetaData["SelfProtocolAttestationId"].ToString()], row.MetaData["SelfProtocolRootDecimal"].ToString());
        Check(await Read(source, Ids.Take(2).ToArray()), Ids.Take(2).ToArray());
    }

    private static Task<OASISResult<IReadOnlyList<IHolon>>> Read(SelfProtocolRegistryQuerySource source, Guid[] ids)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, Provider, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result;
        var plan = CosmicQueryPlanner.Plan(query, Provider, new[] { new CosmicQueryTarget(Provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return source.ExecuteCosmicQueryAsync(plan.Result);
    }

    private static void Check(OASISResult<IReadOnlyList<IHolon>> result, Guid[] expected)
    {
        Assert.IsFalse(result.IsError, result.Exception?.ToString());
        CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
        foreach (var row in result.Result)
        {
            Assert.IsTrue(row.IsPublic && row.IsActive && row.CreatedByAvatarId == Owner && !row.Children.Any());
            Assert.AreEqual(SelfProtocolRegistryQuerySource.BlockHash, row.MetaData["SelfProtocolBlockHash"].ToString());
            Assert.AreEqual(SelfProtocolRegistryQuerySource.GenesisHash, row.MetaData["SelfProtocolGenesisHash"].ToString());
            Assert.AreEqual(SelfProtocolRegistryQuerySource.Hub, row.MetaData["SelfProtocolHub"].ToString());
            Assert.AreEqual(64, row.MetaData["SelfProtocolRegistryCodeSha256"].ToString().Length);
        }
    }

    private sealed class Replies : HttpMessageHandler
    {
        public string Fault { get; init; }
        public string FailRegistry { get; set; }
        public int Requests;
        public static string Address(int value) => "0x" + value.ToString("x40", CultureInfo.InvariantCulture);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref Requests);
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("https://forno.celo.org/", request.RequestUri.AbsoluteUri);
            Assert.IsNull(request.Headers.Authorization);
            using var body = JsonDocument.Parse(await request.Content.ReadAsStringAsync(token));
            var method = body.RootElement.GetProperty("method").GetString();
            var parameters = body.RootElement.GetProperty("params");
            object result;
            var rootRead = false;
            if (method == "eth_chainId") result = Fault == "wrong-chain" ? "0x1" : "0xa4ec";
            else if (method == "eth_getBlockByNumber")
            {
                var genesis = parameters[0].GetString() == "0x0";
                Assert.AreEqual(genesis ? "0x0" : SelfProtocolRegistryQuerySource.BlockNumber, parameters[0].GetString());
                result = new { number = genesis ? "0x0" : Fault == "wrong-block-number" ? "0x1" : SelfProtocolRegistryQuerySource.BlockNumber,
                    hash = genesis ? Fault == "wrong-genesis" ? "0x" + new string('1', 64) : SelfProtocolRegistryQuerySource.GenesisHash
                        : Fault == "wrong-block-hash" ? "0x" + new string('1', 64) : SelfProtocolRegistryQuerySource.BlockHash };
            }
            else
            {
                Assert.AreEqual(SelfProtocolRegistryQuerySource.BlockHash, parameters[1].GetProperty("blockHash").GetString());
                Assert.IsTrue(parameters[1].GetProperty("requireCanonical").GetBoolean());
                if (method == "eth_getCode")
                {
                    var hub = parameters[0].GetString() == SelfProtocolRegistryQuerySource.Hub;
                    result = Fault == "invalid-code" ? "0x600X" : (hub && Fault == "empty-hub-code") || (!hub && Fault == "empty-registry-code") ? "0x" : "0x6001";
                }
                else
                {
                    Assert.AreEqual("eth_call", method);
                    var call = parameters[0]; var data = call.GetProperty("data").GetString();
                    rootRead = data == "0x4bf4dc7a";
                    if (rootRead)
                    {
                        if (call.GetProperty("to").GetString() == FailRegistry) return new(HttpStatusCode.ServiceUnavailable);
                        result = Fault == "short-root" ? "0x01" : "0x" + new string(Fault == "uppercase-root" ? 'F' : 'f', 64);
                    }
                    else
                    {
                        Assert.AreEqual(SelfProtocolRegistryQuerySource.Hub, call.GetProperty("to").GetString());
                        Assert.IsTrue(data.StartsWith("0x7ef50298", StringComparison.Ordinal));
                        var attestation = int.Parse(data[^1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        Assert.AreEqual("0x7ef50298" + attestation.ToString("x64", CultureInfo.InvariantCulture), data);
                        result = Fault == "zero-registry" ? "0x" + new string('0', 64) : Fault == "noncanonical-address" ? "0x1" + new string('0', 63)
                            : "0x" + new string('0', 24) + Address(attestation)[2..];
                    }
                }
            }
            var json = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = Fault == "wrong-id" ? 2 : 1, result });
            if (Fault == "duplicate-result") json = json[..^1] + ",\"result\":null}";
            if (Fault == "duplicate-header" && method == "eth_getBlockByNumber")
                json = json.Replace("\"number\":\"0x0\"", "\"number\":\"0x0\",\"number\":\"0x0\"");
            if (Fault == "rpc-error") json = "{\"jsonrpc\":\"2.0\",\"id\":1,\"error\":{\"code\":-32000,\"message\":\"controlled\"}}";
            if (Fault == "oversized-root" && rootRead) json += new string(' ', 2049);
            return new(Fault == "http-failure" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
                { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }
}
