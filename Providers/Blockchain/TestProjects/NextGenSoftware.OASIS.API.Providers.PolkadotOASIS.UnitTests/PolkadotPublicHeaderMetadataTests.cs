using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass]
public sealed class PolkadotPublicHeaderMetadataTests
{
    // Golden SCALE/BLAKE2b vectors from the independent Python stdlib probe.
    // Cover all four compact-number widths, not hashes produced by this adapter.
    private static readonly string[] Hashes = { "0xb351db3a6478757a0f95616be99c54de19f726167ac9d6a94cfa7f69d7e7a4a5",
        "0x98bcfdb65efd76ff15c26c38540579e185b0e89709bf5c0a24e4a570223186da",
        "0xcbbe61722e403437c91da6cc80d4593d4b4931df6fb18d997932ad1008f595e5",
        "0x86a38edde87064c356294b26506ad382b3ec20a4225229e0bd5aa3f8724713fc" };
    private static readonly uint[] Numbers = { 1, 64, 16384, 1073741824 };
    private static object Header(int index, bool tamper = false) => new {
        parentHash = "0x" + new string('0', 64), number = "0x" + Numbers[index].ToString("x"),
        stateRoot = "0x" + new string(tamper ? '3' : '1', 64),
        extrinsicsRoot = "0x" + new string('2', 64), digest = new { logs = Array.Empty<string>() } };

    private sealed class Rpc : HttpMessageHandler
    {
        public string Fault = "";
        public int Requests;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Requests++;
            Assert.AreEqual(HttpMethod.Post, request.Method);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var root = body.RootElement; string method = root.GetProperty("method").GetString()!;
            var args = root.GetProperty("params"); object result;
            if (method == "chain_getFinalizedHead") result = Hashes[Fault == "future" ? 0 : 3];
            else if (method == "chain_getHeader")
            {
                int index = Array.IndexOf(Hashes, args[0].GetString());
                Assert.IsTrue(index >= 0, "No source scans permitted.");
                result = Header(index, Fault == "hash" || (Fault == "last" && index == 2));
            }
            else if (method == "chain_getBlockHash")
            {
                uint number = args[0].GetUInt32();
                result = number == 0 ? Fault == "genesis" ? Hashes[0] : PolkadotPublicHeaderMetadataSource.Genesis
                    : Fault == "canonical" ? Hashes[3] : Hashes[Array.IndexOf(Numbers, number)];
            }
            else throw new AssertFailedException("No writes, signing, runtime downloads or storage scans.");
            string json = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = Fault == "id" ? 2 : 1, result });
            if (Fault == "oversize") json = new string(' ',262145);
            if (Fault == "duplicate") json = "{\"id\":1,\"id\":1}";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json,Encoding.UTF8,"application/json") };
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("budget")]
    [DataRow("genesis")] [DataRow("hash")] [DataRow("last")] [DataRow("canonical")]
    [DataRow("future")] [DataRow("id")] [DataRow("oversize")] [DataRow("duplicate")]
    [DataRow("empty-window-error")] [DataRow("inactive")] [DataRow("cancel")] [DataRow("unknown")]
    public async Task OnlyCompleteCanonicalHashVerifiedSelectionsBecomeVisible(string scenario)
    {
        var rpc = new Rpc(); using var client = new HttpClient(rpc);
        using var source = new PolkadotPublicHeaderMetadataSource(client,new Uri("https://example.invalid/"),
            PolkadotPublicHeaderMetadataSource.Genesis,Hashes.Take(3),Guid.NewGuid());
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        rpc.Requests = 0; rpc.Fault = scenario == "empty-window-error" ? "last" : scenario;
        var selected = scenario == "unknown" ? new[] { "0x" + new string('9',64) } : Hashes.Take(3).ToArray();
        var point = Hashes[0];
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.PolkadotOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row,PolkadotPublicHeaderMetadataSource.AddressMetadata))).Result;
        if (scenario == "point") query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.PolkadotOASIS).Result)
            .Where(row => CosmicQueryMetadata.Value<string>(row,PolkadotPublicHeaderMetadataSource.AddressMetadata) == point).Result;
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query,ProviderType.PolkadotOASIS,
            new[] { new CosmicQueryTarget(ProviderType.PolkadotOASIS,source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError,plan.Message);
        using var cancel = new CancellationTokenSource(); if (scenario == "cancel") cancel.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result,scenario == "budget" ? 2 : 100,cancel.Token);
        if (scenario is "normal" or "point" or "window")
        {
            Assert.IsFalse(result.IsError,result.Exception?.ToString());
            Assert.AreEqual(scenario == "normal" ? 3 : 1,result.Result.Count);
            Assert.IsTrue(result.Result.All(row => row.IsPublic));
            Assert.AreEqual(scenario == "window" ? Hashes[1] : Hashes[0], result.Result[0].MetaData[PolkadotPublicHeaderMetadataSource.AddressMetadata]);
        }
        else Assert.IsTrue(result.IsError,"No malformed or partial native output may escape.");
        if (scenario is "inactive" or "cancel" or "unknown") Assert.AreEqual(0,rpc.Requests);
    }

    [TestMethod]
    public void SourceRejectsSecretsAndNonCanonicalHashes()
    {
        Assert.ThrowsException<ArgumentException>(() => PolkadotPublicHeaderMetadataSource.FromJson("{\"privateKey\":\"forbidden\"}"));
        using var client = new HttpClient(new Rpc());
        Assert.ThrowsException<InvalidDataException>(() => new PolkadotPublicHeaderMetadataSource(client,
            new Uri("https://example.invalid/"),PolkadotPublicHeaderMetadataSource.Genesis,new[] { "0xAB" },Guid.NewGuid()));
    }

    [TestMethod]
    [TestCategory("NativePolkadotPublic")]
    public async Task ExistingConfiguredSourceReturnsThreeAndFilteredTwo()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_POLKADOT_PUBLIC_SOURCE");
        if (string.IsNullOrEmpty(path)) Assert.Inconclusive("Explicit retained native catalogue required.");
        using var output = new StringWriter();
        var result = await CosmicCqlDemo.PolkadotPublicWalkthrough.RunAsync(path!, output);
        Assert.AreEqual((3,2),result);
        StringAssert.Contains(output.ToString(),"Verified content hash:");
    }
}
