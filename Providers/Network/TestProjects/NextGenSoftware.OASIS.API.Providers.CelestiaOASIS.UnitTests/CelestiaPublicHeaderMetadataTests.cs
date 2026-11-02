using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.CelestiaOASIS.UnitTests;

[TestClass]
public class CelestiaPublicHeaderMetadataTests
{
    private static readonly string[] Heights = { "15521793", "15521794", "18446744073709551615" };
    private static Dictionary<string, string> Pins() => Heights.ToDictionary(h => h, _ => new string('B', 64));

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "";
        public List<string> Calls = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("/", request.RequestUri!.AbsolutePath);
            Assert.IsNull(request.Headers.Authorization);
            Assert.IsFalse(request.Headers.Contains("Cookie"));
            using var q = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.AreEqual("header.GetByHeight", q.RootElement.GetProperty("method").GetString());
            Assert.AreEqual(1, q.RootElement.GetProperty("id").GetInt32());
            Assert.AreEqual("2.0", q.RootElement.GetProperty("jsonrpc").GetString());
            var parameters = q.RootElement.GetProperty("params");
            Assert.AreEqual(1, parameters.GetArrayLength());
            var height = parameters[0].GetUInt64().ToString(System.Globalization.CultureInfo.InvariantCulture);
            Assert.IsTrue(Heights.Contains(height)); Calls.Add(height);
            var header = new Dictionary<string, object>
            {
                ["chain_id"] = Fault == "network" ? "mocha-4" : "celestia",
                ["height"] = Fault == "height" ? "1" : height,
                ["time"] = Fault == "time" ? "invalid" : "2026-11-02T00:00:00.123456789Z",
                ["data_hash"] = Fault == "hash-case" ? new string('a', 64) : new string('A', 64),
                ["last_block_id"] = new { hash = new string('C', 64) }
            };
            if (Fault == "height-number") header["height"] = ulong.Parse(height);
            var roots = new[] { Convert.ToBase64String(new byte[90]), Convert.ToBase64String(new byte[90]) };
            var result = new Dictionary<string, object>
            {
                ["header"] = header,
                ["commit"] = new { height = Fault == "commit-height" ? "1" : height,
                    block_id = new { hash = new string(Fault == "pin" ? 'D' : 'B', 64) } },
                ["dah"] = new { row_roots = roots,
                    column_roots = Fault == "dimensions" ? roots.Take(1).ToArray() : Fault == "root" ? new[] { "!", "!" } : roots }
            };
            if (Fault == "missing") result.Remove("commit");
            var envelope = new Dictionary<string, object> { ["jsonrpc"] = Fault == "version" ? "1.0" : "2.0",
                ["id"] = Fault == "rpc-id" ? 2 : 1, ["result"] = result };
            if (Fault == "rpc-error") envelope["error"] = new { code = -32603, message = "unavailable" };
            string text = JsonSerializer.Serialize(envelope);
            if (Fault == "duplicate") text = "{\"jsonrpc\":\"2.0\",\"id\":1,\"id\":1}";
            if (Fault == "oversize") text = new string(' ', 1048577);
            bool failed = Fault == "http" || Fault == "last" && height == Heights.Last();
            return new HttpResponseMessage(failed ? HttpStatusCode.NotFound : HttpStatusCode.OK)
            {
                Content = new StringContent(text, Encoding.UTF8, "application/json")
            };
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("subset")] [DataRow("window")]
    [DataRow("network")] [DataRow("height")] [DataRow("height-number")] [DataRow("commit-height")]
    [DataRow("pin")] [DataRow("hash-case")] [DataRow("time")] [DataRow("dimensions")] [DataRow("root")]
    [DataRow("missing")] [DataRow("version")] [DataRow("rpc-id")] [DataRow("rpc-error")]
    [DataRow("duplicate")] [DataRow("oversize")] [DataRow("http")] [DataRow("last")]
    [DataRow("empty-window-error")] [DataRow("budget")] [DataRow("unknown")] [DataRow("cancel")] [DataRow("inactive")]
    public async Task ExactPinnedSelectionPreservesUint64AndFailsAtomically(string scenario)
    {
        var native = new Native(); using var client = new HttpClient(native); var owner = Guid.NewGuid();
        using var source = new CelestiaPublicHeaderMetadataSource(client, new Uri("https://example.invalid/"), Pins(), owner);
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Calls.Clear(); native.Fault = scenario == "empty-window-error" ? "last" : scenario;
        var keys = scenario == "subset" ? Heights.Take(2).ToArray() : scenario == "unknown" ? new[] { "99" } : Heights;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.CelestiaOASIS).Result)
            .Where(h => keys.Contains(CosmicQueryMetadata.Value<string>(h, CelestiaPublicHeaderMetadataSource.AddressMetadata))).Result;
        if (scenario == "point")
        {
            string key = Heights[0];
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.CelestiaOASIS).Result)
                .Where(h => CosmicQueryMetadata.Value<string>(h, CelestiaPublicHeaderMetadataSource.AddressMetadata) == key).Result;
        }
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.CelestiaOASIS,
            new[] { new CosmicQueryTarget(ProviderType.CelestiaOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancelled = new CancellationTokenSource(); if (scenario == "cancel") cancelled.Cancel();
        var response = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancelled.Token);
        if (scenario is "normal" or "point" or "subset" or "window")
        {
            Assert.IsFalse(response.IsError, response.Exception?.ToString());
            Assert.AreEqual(scenario is "point" or "window" ? 1 : scenario == "subset" ? 2 : 3, response.Result.Count);
            CollectionAssert.AreEqual(scenario == "point" ? Heights.Take(1).ToArray() : keys, native.Calls.ToArray());
            foreach (var row in response.Result)
            {
                Assert.IsTrue(row.IsPublic); Assert.AreEqual(owner, row.CreatedByAvatarId);
                Assert.AreEqual("node-reported-pinned-header", row.MetaData["CelestiaObservationPolicy"]);
                Assert.AreEqual("false", row.MetaData["CelestiaConsensusVerified"]);
                Assert.AreEqual("false", row.MetaData["CelestiaDataAvailabilitySampled"]);
                Assert.AreEqual(row.Name, row.MetaData["CelestiaHeight"]);
                Assert.AreEqual(0, row.Children.Count());
            }
            if (scenario == "normal") Assert.AreEqual(ulong.MaxValue.ToString(), response.Result.Last().MetaData["CelestiaHeight"]);
        }
        else { Assert.IsTrue(response.IsError); Assert.IsTrue(response.Result == null || response.Result.Count == 0); }
        if (scenario is "inactive" or "cancel" or "unknown") Assert.AreEqual(0, native.Calls.Count);
        if (scenario is "last" or "empty-window-error") Assert.AreEqual(3, native.Calls.Count);
    }

    [TestMethod]
    public async Task UnsupportedContentQueriesAndWritesNeverDispatch()
    {
        var native = new Native(); using var client = new HttpClient(native);
        using var source = new CelestiaPublicHeaderMetadataSource(client, new Uri("https://example.invalid/"), Pins(), Guid.NewGuid());
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.CelestiaOASIS).Result).Where(h => h.Name == "anything").Result;
        Assert.IsTrue(CosmicQueryPlanner.Plan(query, ProviderType.CelestiaOASIS,
            new[] { new CosmicQueryTarget(ProviderType.CelestiaOASIS, source.QueryCapabilities) }).IsError);
        Assert.IsTrue((await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), IsNewHolon = true }, saveChildren: false)).IsError);
        Assert.IsTrue((await source.DeleteHolonAsync(Guid.NewGuid())).IsError);
        Assert.AreEqual(0, native.Calls.Count);
    }

    [DataTestMethod]
    [DataRow("schema")] [DataRow("chain")] [DataRow("extra")] [DataRow("empty")] [DataRow("height")]
    [DataRow("zero")] [DataRow("overflow")] [DataRow("hash")] [DataRow("duplicate")]
    public void ConfigurationRejectsInvalidOrAmbiguousNativeCatalogue(string fault)
    {
        var headers = Pins();
        if (fault == "empty") headers.Clear();
        if (fault is "height" or "zero" or "overflow") headers = new() { [fault == "height" ? "01" : fault == "zero" ? "0" : "18446744073709551616"] = new string('B', 64) };
        if (fault == "hash") headers[Heights[0]] = new string('b', 64);
        var config = new Dictionary<string, object> { ["schema"] = fault == "schema" ? 2 : 1,
            ["rpcUrl"] = "https://example.invalid/", ["chainId"] = fault == "chain" ? "mocha-4" : "celestia",
            ["headers"] = headers, ["sourceOwner"] = Guid.NewGuid() };
        if (fault == "extra") config["token"] = "forbidden";
        string json = JsonSerializer.Serialize(config);
        if (fault == "duplicate") json = json.Replace("\"schema\":1", "\"schema\":1,\"schema\":1");
        try { using var source = CelestiaPublicHeaderMetadataSource.FromJson(json); Assert.Fail("Invalid source accepted."); }
        catch (Exception e) when (e is ArgumentException or InvalidDataException) { }
    }
}
