using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.StarknetOASIS.UnitTests;

[TestClass]
public sealed class StarknetPublicHeaderMetadataTests
{
    private static readonly string[] Keys = { "0x1", "0x2", "0x3" };

    private sealed class Rpc : HttpMessageHandler
    {
        public string Fault = "normal";
        public int Requests;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Requests++;
            Assert.AreEqual(StarknetPublicHeaderMetadataSource.PublicEndpoint, request.RequestUri!.AbsoluteUri);
            Assert.IsNull(request.Headers.Authorization);
            using var doc = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            string method = doc.RootElement.GetProperty("method").GetString()!;
            object result;
            if (method == "starknet_specVersion") result = Fault == "version" ? "0.0.0" : StarknetPublicHeaderMetadataSource.RpcVersion;
            else if (method == "starknet_chainId") result = Fault == "chain" ? "0x0" : StarknetPublicHeaderMetadataSource.ChainId;
            else if (method == "starknet_getBlockWithTxHashes")
            {
                var selection = doc.RootElement.GetProperty("params")[0];
                bool genesis = selection.TryGetProperty("block_number", out var number);
                if (genesis) Assert.AreEqual(0, number.GetInt32());
                string key = genesis ? StarknetPublicHeaderMetadataSource.Genesis : selection.GetProperty("block_hash").GetString()!;
                var header = new Dictionary<string, object> {
                    ["status"] = "ACCEPTED_ON_L1", ["block_hash"] = key,
                    ["parent_hash"] = "0x0", ["new_root"] = "0x4", ["sequencer_address"] = "0x0",
                    ["block_number"] = genesis ? 0UL : ulong.Parse(key[2..]), ["timestamp"] = 1637072695UL
                };
                if (genesis && Fault == "genesis") header["block_hash"] = "0x9";
                if (!genesis)
                {
                    if (Fault == "identity" || Fault == "last" && key == Keys[2]) header["block_hash"] = "0x9";
                    if (Fault == "status") header["status"] = "ACCEPTED_ON_L2";
                    if (Fault == "field") header["new_root"] = "0x800000000000011000000000000000000000000000000000000000000000001";
                    if (Fault == "negative") header["timestamp"] = -1;
                    if (Fault == "fraction") header["block_number"] = 1.5;
                    if (Fault == "string-number") header["block_number"] = "1";
                    if (Fault == "overflow") header["timestamp"] = JsonDocument.Parse("18446744073709551616").RootElement.Clone();
                    if (Fault == "missing") header.Remove("sequencer_address");
                    if (Fault == "exact") header["timestamp"] = 9007199254740993UL;
                }
                result = header;
            }
            else throw new AssertFailedException("Only explicit anonymous protocol/chain/header reads permitted.");
            string json = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = Fault == "id" ? 2 : 1, result });
            if (Fault == "duplicate") json = "{\"id\":1,\"id\":1}";
            if (Fault == "oversize") json = new string(' ', 262145);
            if (Fault == "http") return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("exact")]
    [DataRow("version")] [DataRow("chain")] [DataRow("genesis")] [DataRow("identity")]
    [DataRow("last")] [DataRow("status")] [DataRow("field")] [DataRow("negative")]
    [DataRow("fraction")] [DataRow("string-number")] [DataRow("overflow")] [DataRow("missing")]
    [DataRow("id")] [DataRow("duplicate")] [DataRow("oversize")] [DataRow("http")]
    [DataRow("inactive")] [DataRow("cancel")] [DataRow("unknown")] [DataRow("budget")]
    [DataRow("empty-window-error")]
    public async Task CompleteExactNativeSelectionsOrExplicitError(string scenario)
    {
        var rpc = new Rpc();
        using var client = new HttpClient(rpc);
        using var source = new StarknetPublicHeaderMetadataSource(client,
            new Uri(StarknetPublicHeaderMetadataSource.PublicEndpoint), StarknetPublicHeaderMetadataSource.Genesis, Keys, Guid.NewGuid());
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        rpc.Requests = 0;
        rpc.Fault = scenario == "empty-window-error" ? "last" : scenario;
        var selected = scenario == "unknown" ? new[] { "0x9" } : Keys;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.StarknetOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, StarknetPublicHeaderMetadataSource.AddressMetadata))).Result;
        if (scenario == "point") query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.StarknetOASIS).Result)
            .Where(row => CosmicQueryMetadata.Value<string>(row, StarknetPublicHeaderMetadataSource.AddressMetadata) == "0x1").Result;
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var planned = CosmicQueryPlanner.Plan(query, ProviderType.StarknetOASIS,
            new[] { new CosmicQueryTarget(ProviderType.StarknetOASIS, source.QueryCapabilities) });
        Assert.IsFalse(planned.IsError, planned.Message);
        using var cancel = new CancellationTokenSource();
        if (scenario == "cancel") cancel.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(planned.Result, scenario == "budget" ? 2 : 100, cancel.Token);
        if (scenario is "normal" or "point" or "window" or "exact")
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(scenario is "point" or "window" ? 1 : 3, result.Result.Count);
            Assert.AreEqual(scenario == "window" ? "0x2" : "0x1", result.Result[0].MetaData[StarknetPublicHeaderMetadataSource.AddressMetadata]);
            Assert.IsTrue(result.Result.All(row => row.IsPublic && row.ProviderUniqueStorageKey[ProviderType.StarknetOASIS] != null));
            if (scenario == "exact") Assert.AreEqual("9007199254740993", result.Result[0].MetaData["StarknetTimestamp"]);
        }
        else
        {
            Assert.IsTrue(result.IsError, "No partial native rows may become visible.");
            Assert.IsNull(result.Result);
        }
        if (scenario is "inactive" or "cancel" or "unknown") Assert.AreEqual(0, rpc.Requests);
    }

    [DataTestMethod]
    [DataRow("0x00")] [DataRow("0xAB")] [DataRow("-1")] [DataRow("0x")]
    [DataRow("0x800000000000011000000000000000000000000000000000000000000000001")]
    public void RejectNoncanonicalOrOutOfFieldKeys(string key)
    {
        using var client = new HttpClient(new Rpc());
        Assert.ThrowsException<InvalidDataException>(() => new StarknetPublicHeaderMetadataSource(client,
            new Uri(StarknetPublicHeaderMetadataSource.PublicEndpoint), StarknetPublicHeaderMetadataSource.Genesis,
            new[] { key }, Guid.NewGuid()));
    }

    [TestMethod]
    public void ConfigurationRejectsSecretFieldsAndEndpointRedirection()
    {
        Assert.ThrowsException<ArgumentException>(() => StarknetPublicHeaderMetadataSource.FromJson("{\"privateKey\":\"forbidden\"}"));
        using var client = new HttpClient(new Rpc());
        Assert.ThrowsException<ArgumentException>(() => new StarknetPublicHeaderMetadataSource(client,
            new Uri("https://example.invalid/"), StarknetPublicHeaderMetadataSource.Genesis, Keys, Guid.NewGuid()));
    }
    [TestMethod]
    [TestCategory("NativeStarknetPublic")]
    public async Task RetainedNativeCatalogueReturnsThreeAndFilteredTwo()
    {
        string? path = Environment.GetEnvironmentVariable("COSMIC_STARKNET_PUBLIC_SOURCE");
        if (string.IsNullOrEmpty(path)) Assert.Inconclusive("Explicit retained public source required.");
        string json = await File.ReadAllTextAsync(path!);
        using var configuration = JsonDocument.Parse(json);
        var keys = configuration.RootElement.GetProperty("blockHashes").EnumerateArray().Select(row => row.GetString()!).ToArray();
        Assert.AreEqual(3, keys.Length);
        using var source = StarknetPublicHeaderMetadataSource.FromJson(json);
        var activated = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activated.IsError, activated.Exception?.ToString());
        foreach (int count in new[] { 3, 2 })
        {
            var selected = keys.Take(count).ToArray();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.StarknetOASIS).Result)
                .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, StarknetPublicHeaderMetadataSource.AddressMetadata))).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.StarknetOASIS,
                new[] { new CosmicQueryTarget(ProviderType.StarknetOASIS, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var loaded = await source.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(loaded.IsError, loaded.Exception?.ToString());
            Assert.AreEqual(count, loaded.Result.Count);
            CollectionAssert.AreEquivalent(selected, loaded.Result.Select(row =>
                (string)row.MetaData[StarknetPublicHeaderMetadataSource.AddressMetadata]).ToArray());
            Assert.IsTrue(loaded.Result.All(row => (string)row.MetaData["StarknetReportedStatus"] == "ACCEPTED_ON_L1"));
        }
    }

}
