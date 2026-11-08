using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
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

namespace NextGenSoftware.OASIS.API.Providers.CivicOASIS.UnitTests;

[TestClass]
public class CivicNetworkQueryTests
{
    private const ProviderType Provider = ProviderType.CivicOASIS;
    private static readonly Guid Owner = Guid.Parse("8dbbf7bc-8a31-4a35-8796-621f17cb5e39");
    private static readonly int[] Networks = { 1, 4, 10 };
    private static readonly Guid[] Ids = Networks.Select(x => CivicNetworkQuerySource.LogicalId(Owner, x)).ToArray();

    [TestMethod]
    public async Task ExactCataloguePreservesNamesAndCanonicalBlockWithoutScanningPasses()
    {
        using var replies = new Replies();
        using var source = new CivicNetworkQuerySource(Owner, Networks, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        Check(await Read(source, Ids), Ids);
        Check(await Read(source, Ids.Take(2).ToArray()), Ids.Take(2).ToArray());
        Assert.AreEqual(30, replies.Requests, "Five mandatory calls per observation; no retry or personal-token query.");
        var before = replies.Requests;
        foreach (var selection in new[] { new[] { Guid.NewGuid() }, new[] { Ids[0], Guid.NewGuid() } })
        {
            var result = await Read(source, selection);
            Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
            Assert.AreEqual(before, replies.Requests, "Preflight the whole catalogue before native dispatch.");
        }
    }

    [DataTestMethod]
    [DataRow("wrong-chain")]
    [DataRow("wrong-genesis")]
    [DataRow("wrong-block-hash")]
    [DataRow("wrong-block-number")]
    [DataRow("duplicate-header")]
    [DataRow("empty-code")]
    [DataRow("invalid-code")]
    [DataRow("wrong-offset")]
    [DataRow("huge-length")]
    [DataRow("nonzero-padding")]
    [DataRow("trailing-data")]
    [DataRow("invalid-utf8")]
    [DataRow("odd-hex")]
    [DataRow("uppercase-hex")]
    [DataRow("short-frame")]
    [DataRow("changed-network")]
    [DataRow("rpc-error")]
    [DataRow("wrong-id")]
    [DataRow("duplicate-result")]
    [DataRow("http-failure")]
    [DataRow("oversized-response")]
    public async Task IdentityCodeEnvelopeAndCanonicalAbiFailuresRefuseActivation(string fault)
    {
        using var replies = new Replies { Fault = fault };
        using var source = new CivicNetworkQuerySource(Owner, Networks, replies);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError, fault); Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual("CQL_SOURCE_UNAVAILABLE", result.ErrorCode);
        Assert.IsTrue(replies.Requests <= 5, "No retry or read past the failed mandatory stage.");
    }

    [TestMethod]
    public async Task FailedNetworkPublishesNoPartialRows()
    {
        using var replies = new Replies();
        using var source = new CivicNetworkQuerySource(Owner, Networks, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        replies.FailNetwork = 4;
        var result = await Read(source, Ids);
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual(20, replies.Requests);
    }

    [TestMethod]
    public async Task InactiveAndCancelledReadsDoNotReachTransport()
    {
        using var replies = new Replies();
        using var source = new CivicNetworkQuerySource(Owner, Networks, replies);
        Assert.IsTrue((await Read(source, Ids)).IsError);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.IsTrue((await source.ActivateQuerySourceAsync(cancellation.Token)).IsError);
        Assert.AreEqual(0, replies.Requests);
    }

    [TestMethod]
    public void InvalidCataloguesFailBeforeTransportConstruction()
    {
        foreach (var values in new[] { Array.Empty<int>(), new[] { 1, 1 }, new[] { 0 }, new[] { 2 }, new[] { 1, 4, 10, 1 } })
            Assert.ThrowsException<ArgumentException>(() => new CivicNetworkQuerySource(Owner, values));
        Assert.ThrowsException<ArgumentException>(() => new CivicNetworkQuerySource(Guid.Empty, Networks));
        Assert.AreNotEqual(CivicNetworkQuerySource.LogicalId(Owner, 1), CivicNetworkQuerySource.LogicalId(Guid.NewGuid(), 1));
    }

    [TestMethod, TestCategory("CivicNetworkNative")]
    public async Task PublicNetworkCqlReturnsThreeAndTwoReviewedRegistryNames()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_CIVIC_VERIFY_PUBLIC") != "1")
        {
            Assert.Inconclusive("Explicitly opt in to this public read-only native schedule."); return;
        }
        using var source = CivicNetworkQuerySource.ForReviewedTeachingCatalogue(Owner);
        var activated = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activated.IsError, activated.Exception?.ToString());
        Check(await Read(source, Ids), Ids);
        Check(await Read(source, Ids.Take(2).ToArray()), Ids.Take(2).ToArray());
    }

    private static Task<OASISResult<IReadOnlyList<IHolon>>> Read(CivicNetworkQuerySource source, Guid[] ids)
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
            Assert.AreEqual(CivicNetworkQuerySource.BlockHash, row.MetaData["CivicBlockHash"].ToString());
            Assert.AreEqual(CivicNetworkQuerySource.GenesisHash, row.MetaData["CivicGenesisHash"].ToString());
            Assert.AreEqual(CivicNetworkQuerySource.GatewayToken, row.MetaData["CivicGatewayToken"].ToString());
            var key = int.Parse(row.MetaData["CivicNetworkId"].ToString(), CultureInfo.InvariantCulture);
            Assert.AreEqual(CivicNetworkQuerySource.ReviewedNetworkName(key), row.MetaData["CivicNetworkName"].ToString());
            Assert.AreEqual(key.ToString(CultureInfo.InvariantCulture), row.ProviderUniqueStorageKey[Provider]);
            Assert.AreEqual(64, row.MetaData["CivicGatewayCodeSha256"].ToString().Length);
        }
    }

    private sealed class Replies : HttpMessageHandler
    {
        public string Fault { get; init; }
        public int FailNetwork { get; set; }
        public int Requests;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref Requests);
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("https://polygon-bor-rpc.publicnode.com/", request.RequestUri.AbsoluteUri);
            Assert.IsNull(request.Headers.Authorization);
            using var body = JsonDocument.Parse(await request.Content.ReadAsStringAsync(token));
            var method = body.RootElement.GetProperty("method").GetString();
            var parameters = body.RootElement.GetProperty("params");
            object result;
            if (method == "eth_chainId") result = Fault == "wrong-chain" ? "0x1" : "0x89";
            else if (method == "eth_getBlockByNumber")
            {
                var genesis = parameters[0].GetString() == "0x0";
                Assert.AreEqual(genesis ? "0x0" : CivicNetworkQuerySource.BlockNumber, parameters[0].GetString());
                result = new { number = genesis ? "0x0" : Fault == "wrong-block-number" ? "0x1" : CivicNetworkQuerySource.BlockNumber,
                    hash = genesis ? Fault == "wrong-genesis" ? "0x" + new string('1', 64) : CivicNetworkQuerySource.GenesisHash
                        : Fault == "wrong-block-hash" ? "0x" + new string('1', 64) : CivicNetworkQuerySource.BlockHash };
            }
            else
            {
                Assert.AreEqual(CivicNetworkQuerySource.BlockHash, parameters[1].GetProperty("blockHash").GetString());
                Assert.IsTrue(parameters[1].GetProperty("requireCanonical").GetBoolean());
                if (method == "eth_getCode")
                {
                    Assert.AreEqual(CivicNetworkQuerySource.GatewayToken, parameters[0].GetString());
                    result = Fault == "invalid-code" ? "0x600X" : Fault == "empty-code" ? "0x" : "0x6001";
                }
                else
                {
                    Assert.AreEqual("eth_call", method);
                    var call = parameters[0]; var data = call.GetProperty("data").GetString();
                    Assert.AreEqual(CivicNetworkQuerySource.GatewayToken, call.GetProperty("to").GetString());
                    var network = int.Parse(data[^2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    Assert.AreEqual("0x95ea5717" + network.ToString("x64", CultureInfo.InvariantCulture), data);
                    if (network == FailNetwork) return new(HttpStatusCode.ServiceUnavailable);
                    var value = Encoding.UTF8.GetBytes(Fault == "changed-network" ? "different" : CivicNetworkQuerySource.ReviewedNetworkName(network));
                    var frame = new byte[64 + ((value.Length + 31) / 32) * 32];
                    frame[31] = 32; frame[63] = (byte)value.Length; value.CopyTo(frame, 64);
                    if (Fault == "wrong-offset") frame[31] = 64;
                    if (Fault == "huge-length") Array.Fill(frame, (byte)255, 32, 32);
                    if (Fault == "nonzero-padding") frame[^1] = 1;
                    if (Fault == "invalid-utf8") frame[64] = 255;
                    if (Fault == "trailing-data") Array.Resize(ref frame, frame.Length + 32);
                    var hex = Convert.ToHexStringLower(frame);
                    if (Fault == "uppercase-hex") hex = hex.ToUpperInvariant();
                    result = Fault == "short-frame" ? "0x00" : "0x" + hex + (Fault == "odd-hex" ? "0" : "");
                }
            }
            var json = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = Fault == "wrong-id" ? 2 : 1, result });
            if (Fault == "duplicate-result") json = json[..^1] + ",\"result\":null}";
            if (Fault == "duplicate-header" && method == "eth_getBlockByNumber")
                json = json.Replace("\"number\":\"0x0\"", "\"number\":\"0x0\",\"number\":\"0x0\"");
            if (Fault == "rpc-error") json = "{\"jsonrpc\":\"2.0\",\"id\":1,\"error\":{\"code\":-32000,\"message\":\"controlled\"}}";
            if (Fault == "oversized-response" && method == "eth_call") json += new string(' ', 4096);
            return new(Fault == "http-failure" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
                { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }
}
