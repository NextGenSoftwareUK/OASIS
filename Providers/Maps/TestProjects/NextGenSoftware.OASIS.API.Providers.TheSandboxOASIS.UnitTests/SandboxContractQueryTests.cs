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

namespace NextGenSoftware.OASIS.API.Providers.TheSandboxOASIS.UnitTests;

[TestClass]
public class SandboxContractQueryTests
{
    private const string FixtureBlockNumber = "0x5c7349f";
    private const string FixtureBlockHash = "0x63e481ab0ed2431ffa2c986b662b35480f69d46d43e847ea54b8cfd7b9dbf7af";
    private const ProviderType Provider = ProviderType.TheSandboxOASIS;
    private static readonly Guid Owner = Guid.Parse("8dbbf7bc-8a31-4a35-8796-621f17cb5e39");
    private static readonly string[] Networks = { "land", "sand", "asset" };
    private static readonly Guid[] Ids = Networks.Select(x => SandboxContractQuerySource.LogicalId(Owner, x)).ToArray();

    [TestMethod]
    public async Task ExactCataloguePreservesMetadataAndCanonicalBlockWithoutScanningHoldings()
    {
        using var replies = new Replies();
        using var source = new SandboxContractQuerySource(Owner, Networks, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        Check(await Read(source, Ids), Ids);
        Check(await Read(source, Ids.Take(2).ToArray()), Ids.Take(2).ToArray());
        Assert.AreEqual(42, replies.Requests, "Seven mandatory calls per observation; no retry or holdings query.");
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
    [DataRow("changed-name")]
    [DataRow("changed-symbol")]
    [DataRow("rpc-error")]
    [DataRow("wrong-id")]
    [DataRow("duplicate-result")]
    [DataRow("http-failure")]
    [DataRow("oversized-response")]
    public async Task IdentityCodeEnvelopeAndCanonicalAbiFailuresRefuseActivation(string fault)
    {
        using var replies = new Replies { Fault = fault };
        using var source = new SandboxContractQuerySource(Owner, Networks, replies);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError, fault); Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual("CQL_SOURCE_UNAVAILABLE", result.ErrorCode);
        Assert.IsTrue(replies.Requests <= 7, "No retry or read past the failed mandatory stage.");
    }

    [TestMethod]
    public async Task FailedNetworkPublishesNoPartialRows()
    {
        using var replies = new Replies();
        using var source = new SandboxContractQuerySource(Owner, Networks, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        replies.FailNetwork = "sand";
        var result = await Read(source, Ids);
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual(28, replies.Requests);
    }

    [TestMethod]
    public async Task InactiveAndCancelledReadsDoNotReachTransport()
    {
        using var replies = new Replies();
        using var source = new SandboxContractQuerySource(Owner, Networks, replies);
        Assert.IsTrue((await Read(source, Ids)).IsError);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.IsTrue((await source.ActivateQuerySourceAsync(cancellation.Token)).IsError);
        Assert.AreEqual(0, replies.Requests);
    }

    [TestMethod]
    public void InvalidCataloguesFailBeforeTransportConstruction()
    {
        foreach (var values in new[] { Array.Empty<string>(), new[] { "land", "land" }, new[] { "LAND" }, new[] { "unknown" }, new[] { "land", "sand", "asset", "land" } })
            Assert.ThrowsException<ArgumentException>(() => new SandboxContractQuerySource(Owner, values));
        Assert.ThrowsException<ArgumentException>(() => new SandboxContractQuerySource(Guid.Empty, Networks));
        Assert.AreNotEqual(SandboxContractQuerySource.LogicalId(Owner, "land"), SandboxContractQuerySource.LogicalId(Guid.NewGuid(), "land"));
    }

    [TestMethod, TestCategory("SandboxContractNative")]
    public async Task PublicContractCqlReturnsThreeAndTwoReviewedTokenNames()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_SANDBOX_VERIFY_PUBLIC") != "1")
        {
            Assert.Inconclusive("Explicitly opt in to this public read-only native schedule."); return;
        }
        using var source = SandboxContractQuerySource.ForReviewedTeachingCatalogue(Owner);
        var activated = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activated.IsError, activated.Exception?.ToString());
        Check(await Read(source, Ids), Ids);
        Check(await Read(source, Ids.Take(2).ToArray()), Ids.Take(2).ToArray());
    }

    private static Task<OASISResult<IReadOnlyList<IHolon>>> Read(SandboxContractQuerySource source, Guid[] ids)
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
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(row.MetaData["SandboxBlockHash"].ToString(), "^0x[0-9a-f]{64}$"));
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(row.MetaData["SandboxBlockNumber"].ToString(), "^0x[1-9a-f][0-9a-f]*$"));
            Assert.AreEqual(SandboxContractQuerySource.GenesisHash, row.MetaData["SandboxGenesisHash"].ToString());
            var key = row.MetaData["SandboxContractKey"].ToString();
            Assert.AreEqual(SandboxContractQuerySource.ContractAddress(key), row.MetaData["SandboxContractAddress"].ToString());
            Assert.AreEqual(SandboxContractQuerySource.ReviewedName(key), row.MetaData["SandboxTokenName"].ToString());
            Assert.AreEqual(SandboxContractQuerySource.ReviewedSymbol(key), row.MetaData["SandboxTokenSymbol"].ToString());
            Assert.AreEqual(key, row.ProviderUniqueStorageKey[Provider]);
            Assert.AreEqual(64, row.MetaData["SandboxContractCodeSha256"].ToString().Length);
        }
    }

    private sealed class Replies : HttpMessageHandler
    {
        public string Fault { get; init; }
        public string FailNetwork { get; set; }
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
                var finalized = parameters[0].GetString() == "finalized";
                Assert.AreEqual(genesis ? "0x0" : finalized ? "finalized" : FixtureBlockNumber, parameters[0].GetString());
                result = new { number = genesis ? "0x0" : !finalized && Fault == "wrong-block-number" ? "0x1" : FixtureBlockNumber,
                    hash = genesis ? Fault == "wrong-genesis" ? "0x" + new string('1', 64) : SandboxContractQuerySource.GenesisHash
                        : !finalized && Fault == "wrong-block-hash" ? "0x" + new string('1', 64) : FixtureBlockHash };
            }
            else
            {
                Assert.AreEqual(FixtureBlockHash, parameters[1].GetProperty("blockHash").GetString());
                Assert.IsTrue(parameters[1].GetProperty("requireCanonical").GetBoolean());
                if (method == "eth_getCode")
                {
                    Assert.IsTrue(Networks.Select(SandboxContractQuerySource.ContractAddress).Contains(parameters[0].GetString()));
                    result = Fault == "invalid-code" ? "0x600X" : Fault == "empty-code" ? "0x" : "0x6001";
                }
                else
                {
                    Assert.AreEqual("eth_call", method);
                    var call = parameters[0]; var data = call.GetProperty("data").GetString();
                    var network = Networks.Single(key => SandboxContractQuerySource.ContractAddress(key) == call.GetProperty("to").GetString());
                    Assert.IsTrue(data is "0x06fdde03" or "0x95d89b41");
                    if (network == FailNetwork && data == "0x95d89b41") return new(HttpStatusCode.ServiceUnavailable);
                    var isName = data == "0x06fdde03";
                    var text = isName ? SandboxContractQuerySource.ReviewedName(network) : SandboxContractQuerySource.ReviewedSymbol(network);
                    if ((isName && Fault == "changed-name") || (!isName && Fault == "changed-symbol")) text = "different";
                    var value = Encoding.UTF8.GetBytes(text);
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
