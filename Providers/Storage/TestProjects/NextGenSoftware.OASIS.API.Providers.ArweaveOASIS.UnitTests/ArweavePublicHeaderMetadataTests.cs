using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS.UnitTests;

[TestClass]
public sealed class ArweavePublicHeaderMetadataTests
{
    private static readonly string[] Keys = { new('A', 64), new('B', 64), new('C', 64) };
    private const string Difficulty = "115792088374597902074750511579343425068641803109251942518159264612597601665024";

    private sealed class Gateway : HttpMessageHandler
    {
        public string Fault = "normal";
        public int Requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Requests++;
            Assert.AreEqual("arweave.net", request.RequestUri!.Host);
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.IsNull(request.Headers.Authorization);
            Assert.AreEqual("2", request.Headers.GetValues("X-Block-Format").Single());

            object result;
            if (request.RequestUri.AbsolutePath == "/info")
                result = new { network = Fault == "network" ? "other" : "arweave.N.1" };
            else
            {
                string key = request.RequestUri.Segments.Last();
                Assert.IsTrue(Keys.Contains(key));
                if (Fault == "cancel-mid") throw new OperationCanceledException(token);
                var header = new Dictionary<string, object?> {
                    ["indep_hash"] = key, ["previous_block"] = Keys[0],
                    ["height"] = 269512, ["timestamp"] = 1567052949,
                    ["last_retarget"] = 1567052114, ["reward_pool"] = 0,
                    ["weave_size"] = 21080508475UL, ["block_size"] = 991723,
                    ["diff"] = Difficulty, ["cumulative_diff"] = "616416144",
                    ["txs"] = new[] { new string('A', 43) }
                };
                if (Fault == "wrong-hash") header["indep_hash"] = new string('D', 64);
                if (Fault == "bad-parent") header["previous_block"] = "bad";
                if (Fault == "early") header["height"] = 269509;
                if (Fault == "late") header["height"] = 422250;
                if (Fault == "negative") header["block_size"] = -1;
                if (Fault == "fraction") header["height"] = 269512.5;
                if (Fault == "string-number") header["height"] = "269512";
                if (Fault == "overflow") header["weave_size"] = JsonDocument.Parse("18446744073709551616").RootElement.Clone();
                if (Fault == "exact") header["weave_size"] = 9007199254740993UL;
                if (Fault == "difficulty-number") header["diff"] = 1;
                if (Fault == "difficulty-leading-zero") header["diff"] = "01";
                if (Fault == "difficulty-negative") header["diff"] = "-1";
                if (Fault == "difficulty-oversize") header["diff"] = new string('1', 257);
                if (Fault == "missing") header.Remove("timestamp");
                if (Fault == "null") header["diff"] = null;
                if (Fault == "tx-array") header["txs"] = "bad";
                if (Fault == "tx-invalid") header["txs"] = new[] { "bad" };
                if (Fault == "tx-noncanonical") header["txs"] = new[] { new string('B', 43) };
                if (Fault == "tx-duplicate") header["txs"] = new[] { new string('A', 43), new string('A', 43) };
                // Even a bad final selected row invalidates a requested empty page.
                if (Fault == "bad-last" && key == Keys[^1]) header.Remove("timestamp");
                result = header;
            }

            string json = JsonSerializer.Serialize(result);
            if (Fault == "duplicate-json") json = "{\"network\":\"arweave.N.1\",\"network\":\"other\"}";
            if (Fault == "oversize") json = new string(' ', 262145);
            if (Fault == "http") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    private static CosmicQueryPlan<Holon> Plan(ArweavePublicHeaderMetadataSource source, string[] selected, string mode)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ArweaveOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row,
                ArweavePublicHeaderMetadataSource.AddressMetadata))).Result;

        if (mode == "point")
        {
            string key = selected[0];
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ArweaveOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row,
                    ArweavePublicHeaderMetadataSource.AddressMetadata) == key).Result;
        }
        if (mode == "window") query = query.Skip(1).Result.Take(1).Result;
        if (mode == "bad-last") query = query.Take(0).Result;

        var plan = CosmicQueryPlanner.Plan(query, ProviderType.ArweaveOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ArweaveOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return plan.Result;
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("exact")]
    [DataRow("network")] [DataRow("wrong-hash")] [DataRow("bad-parent")]
    [DataRow("early")] [DataRow("late")] [DataRow("negative")] [DataRow("fraction")]
    [DataRow("string-number")] [DataRow("overflow")] [DataRow("difficulty-number")]
    [DataRow("difficulty-leading-zero")] [DataRow("difficulty-negative")]
    [DataRow("difficulty-oversize")] [DataRow("missing")] [DataRow("null")]
    [DataRow("tx-array")] [DataRow("tx-invalid")] [DataRow("tx-noncanonical")]
    [DataRow("tx-duplicate")] [DataRow("duplicate-json")] [DataRow("oversize")]
    [DataRow("http")] [DataRow("inactive")] [DataRow("cancel")]
    [DataRow("cancel-mid")] [DataRow("unknown")] [DataRow("budget")] [DataRow("bad-last")]
    public async Task ExactEraAndCompleteSelectionAreRequiredBeforePaging(string scenario)
    {
        var gateway = new Gateway();
        using var client = new HttpClient(gateway);
        using var source = new ArweavePublicHeaderMetadataSource(client,
            new Uri(ArweavePublicHeaderMetadataSource.PublicEndpoint), Keys, Guid.NewGuid());

        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        gateway.Requests = 0;
        gateway.Fault = scenario;
        var selected = scenario == "unknown" ? new[] { new string('D', 64) }
            : scenario == "point" ? Keys.Take(1).ToArray() : Keys;
        using var cancel = new CancellationTokenSource();
        if (scenario == "cancel") cancel.Cancel();

        var loaded = await source.ExecuteCosmicQueryAsync(Plan(source, selected, scenario),
            scenario == "budget" ? 2 : 100, cancel.Token);

        if (scenario is "normal" or "point" or "window" or "exact")
        {
            Assert.IsFalse(loaded.IsError, loaded.Exception?.ToString());
            var expected = scenario == "window" ? Keys.Skip(1).Take(1).ToArray() : selected;
            CollectionAssert.AreEqual(expected, loaded.Result.Select(row =>
                (string)row.MetaData[ArweavePublicHeaderMetadataSource.AddressMetadata]).ToArray());
            Assert.AreEqual(Difficulty, loaded.Result[0].MetaData["ArweaveDifficulty"]);
            Assert.AreEqual(scenario == "exact" ? "9007199254740993" : "21080508475",
                loaded.Result[0].MetaData["ArweaveWeaveSize"]);
        }
        else
        {
            Assert.IsTrue(loaded.IsError);
            Assert.IsNull(loaded.Result);
        }
        if (scenario is "inactive" or "cancel" or "unknown") Assert.AreEqual(0, gateway.Requests);
        if (scenario == "http") Assert.AreEqual(1, gateway.Requests, "No retry or alternate gateway.");
    }

    [TestMethod]
    public void SigningFieldsAndEndpointSubstitutionAreRejected()
    {
        Assert.ThrowsException<ArgumentException>(() => ArweavePublicHeaderMetadataSource.FromJson("{\"privateKey\":\"forbidden\"}"));
        using var client = new HttpClient(new Gateway());
        Assert.ThrowsException<ArgumentException>(() => new ArweavePublicHeaderMetadataSource(client,
            new Uri("https://example.invalid/"), Keys, Guid.NewGuid()));
        Assert.ThrowsException<InvalidDataException>(() => new ArweavePublicHeaderMetadataSource(client,
            new Uri(ArweavePublicHeaderMetadataSource.PublicEndpoint), new[] { "invalid" }, Guid.NewGuid()));
    }

    [TestMethod]
    [TestCategory("NativeArweavePublic")]
    public async Task NativeExplicitCatalogueReturnsThreeAndTwo()
    {
        string? path = Environment.GetEnvironmentVariable("COSMIC_ARWEAVE_PUBLIC_SOURCE");
        if (string.IsNullOrEmpty(path)) Assert.Inconclusive("Explicit retained catalogue required.");
        string json = await File.ReadAllTextAsync(path!);
        using var configuration = JsonDocument.Parse(json);
        var keys = configuration.RootElement.GetProperty("blockHashes").EnumerateArray().Select(x => x.GetString()!).ToArray();
        Assert.AreEqual(3, keys.Length);
        using var source = ArweavePublicHeaderMetadataSource.FromJson(json);
        var active = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(active.IsError, active.Exception?.ToString());

        foreach (int count in new[] { 3, 2 })
        {
            var selected = keys.Take(count).ToArray();
            var loaded = await source.ExecuteCosmicQueryAsync(Plan(source, selected, "normal"));
            Assert.IsFalse(loaded.IsError, loaded.Exception?.ToString());
            CollectionAssert.AreEqual(selected, loaded.Result.Select(row =>
                (string)row.MetaData[ArweavePublicHeaderMetadataSource.AddressMetadata]).ToArray());
        }
    }
}
