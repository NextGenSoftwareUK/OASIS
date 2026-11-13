using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.CardanoOASIS.UnitTests;

[TestClass]
public sealed class CardanoPublicHeaderMetadataTests
{
    private static readonly string[] Keys = { new('1', 64), new('2', 64), new('3', 64) };
    private const string ExactOutput = "123456789012345678901234567890";

    private sealed class Rest : HttpMessageHandler
    {
        public string Fault = "normal";
        public int Requests;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Requests++;
            Assert.AreEqual("api.koios.rest", request.RequestUri!.Host);
            Assert.IsNull(request.Headers.Authorization);
            object result;
            if (request.RequestUri.AbsolutePath == "/api/v1/genesis")
            {
                Assert.AreEqual(HttpMethod.Get, request.Method);
                result = new[] { new { networkmagic = Fault == "chain" ? "1" : "764824073",
                    networkid = Fault == "network" ? "Preview" : "Mainnet" } };
                if (Fault == "empty-genesis") result = Array.Empty<object>();
            }
            else
            {
                Assert.AreEqual("/api/v1/block_info", request.RequestUri.AbsolutePath);
                Assert.AreEqual(HttpMethod.Post, request.Method);
                using var input = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
                Assert.AreEqual(1, input.RootElement.EnumerateObject().Count());
                var keys = input.RootElement.GetProperty("_block_hashes").EnumerateArray().Select(x => x.GetString()!).ToArray();
                if (Fault == "cancel-mid") throw new OperationCanceledException(token);
                var rows = keys.Select(key => new Dictionary<string, object?> {
                    ["hash"] = key, ["parent_hash"] = new string('0', 64),
                    ["block_height"] = (ulong)Array.IndexOf(Keys, key) + 1,
                    ["epoch_no"] = 320, ["abs_slot"] = 10000, ["epoch_slot"] = 30,
                    ["block_size"] = 1024, ["block_time"] = 1637072695,
                    ["tx_count"] = 1, ["num_confirmations"] = 9999999,
                    ["total_output"] = ExactOutput, ["total_fees"] = "0"
                }).ToList();
                if (Fault == "missing") rows.RemoveAt(rows.Count - 1);
                if (Fault == "duplicate") rows[^1] = new(rows[0]);
                if (Fault == "extra") rows.Add(new(rows[0]) { ["hash"] = new string('4', 64) });
                if (Fault == "substitute") rows[^1]["hash"] = new string('4', 64);
                if (Fault == "hash") rows[^1]["parent_hash"] = new string('A', 64);
                if (Fault == "immature") rows[^1]["num_confirmations"] = 2159;
                if (Fault == "negative") rows[^1]["block_time"] = -1;
                if (Fault == "fraction") rows[^1]["block_height"] = 1.5;
                if (Fault == "string-number") rows[^1]["block_height"] = "1";
                if (Fault == "overflow") rows[^1]["block_height"] = JsonDocument.Parse("18446744073709551616").RootElement.Clone();
                if (Fault == "lovelace-number") rows[^1]["total_output"] = 123;
                if (Fault == "lovelace-fraction") rows[^1]["total_fees"] = "1.5";
                if (Fault == "missing-required") rows[^1].Remove("tx_count");
                if (Fault == "exact") foreach (var row in rows) row["block_height"] = 9007199254740993UL;
                if (Fault == "nullable") foreach (var row in rows) row["total_fees"] = null;
                if (Fault == "absent") foreach (var row in rows) row.Remove("total_fees");
                rows.Reverse(); // Real qualification proved response order differs.
                result = rows;
            }
            string json = JsonSerializer.Serialize(result);
            if (Fault == "json-duplicate") json = "[{\"networkmagic\":\"764824073\",\"networkmagic\":\"1\",\"networkid\":\"Mainnet\"}]";
            if (Fault == "oversize") json = new string(' ', 262145);
            if (Fault == "http") return new(HttpStatusCode.TooManyRequests);
            return new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("exact")]
    [DataRow("nullable")] [DataRow("absent")] [DataRow("chain")] [DataRow("network")]
    [DataRow("empty-genesis")] [DataRow("missing")] [DataRow("duplicate")] [DataRow("extra")]
    [DataRow("substitute")] [DataRow("hash")] [DataRow("immature")] [DataRow("negative")]
    [DataRow("fraction")] [DataRow("string-number")] [DataRow("overflow")] [DataRow("missing-required")]
    [DataRow("lovelace-number")] [DataRow("lovelace-fraction")] [DataRow("json-duplicate")]
    [DataRow("oversize")] [DataRow("http")] [DataRow("inactive")] [DataRow("cancel")]
    [DataRow("cancel-mid")] [DataRow("unknown")] [DataRow("budget")] [DataRow("empty-window-error")]
    public async Task FullValidatedSelectionsPreserveExactValuesAndCapturedKeyOrder(string scenario)
    {
        var rest = new Rest(); using var client = new HttpClient(rest);
        using var source = new CardanoPublicHeaderMetadataSource(client,
            new Uri(CardanoPublicHeaderMetadataSource.PublicEndpoint), Keys, Guid.NewGuid());
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        rest.Requests = 0; rest.Fault = scenario == "empty-window-error" ? "missing-required" : scenario;
        var selected = scenario == "unknown" ? new[] { new string('4', 64) } : scenario == "point" ? Keys.Take(1).ToArray() : Keys;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.CardanoOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, CardanoPublicHeaderMetadataSource.AddressMetadata))).Result;
        if (scenario == "point")
        {
            string key = Keys[0];
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.CardanoOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, CardanoPublicHeaderMetadataSource.AddressMetadata) == key).Result;
        }
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.CardanoOASIS,
            new[] { new CosmicQueryTarget(ProviderType.CardanoOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancel = new CancellationTokenSource(); if (scenario == "cancel") cancel.Cancel();
        var loaded = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancel.Token);
        if (scenario is "normal" or "point" or "window" or "exact" or "nullable" or "absent")
        {
            Assert.IsFalse(loaded.IsError, loaded.Exception?.ToString());
            var expected = scenario == "window" ? Keys.Skip(1).Take(1).ToArray() : selected;
            CollectionAssert.AreEqual(expected, loaded.Result.Select(row => (string)row.MetaData[CardanoPublicHeaderMetadataSource.AddressMetadata]).ToArray());
            Assert.AreEqual(ExactOutput, loaded.Result[0].MetaData["CardanoTotalOutputLovelace"]);
            if (scenario == "exact") Assert.AreEqual("9007199254740993", loaded.Result[0].MetaData["CardanoBlockHeight"]);
            if (scenario == "nullable") Assert.IsNull(loaded.Result[0].MetaData["CardanoTotalFeesLovelace"]);
            if (scenario == "absent") Assert.IsFalse(loaded.Result[0].MetaData.ContainsKey("CardanoTotalFeesLovelace"));
        }
        else { Assert.IsTrue(loaded.IsError); Assert.IsNull(loaded.Result); }
        if (scenario is "inactive" or "cancel" or "unknown") Assert.AreEqual(0, rest.Requests);
        if (scenario == "http") Assert.AreEqual(1, rest.Requests, "No retry or alternate endpoint.");
    }

    [DataTestMethod]
    [DataRow("00")] [DataRow("0x123")] [DataRow("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void RejectInvalidNativeKeys(string key)
    {
        using var client = new HttpClient(new Rest());
        Assert.ThrowsException<InvalidDataException>(() => new CardanoPublicHeaderMetadataSource(client,
            new Uri(CardanoPublicHeaderMetadataSource.PublicEndpoint), new[] { key }, Guid.NewGuid()));
    }

    [TestMethod]
    public void RejectSigningFieldsAndEndpointSubstitution()
    {
        Assert.ThrowsException<ArgumentException>(() => CardanoPublicHeaderMetadataSource.FromJson("{\"privateKey\":\"forbidden\"}"));
        using var client = new HttpClient(new Rest());
        Assert.ThrowsException<ArgumentException>(() => new CardanoPublicHeaderMetadataSource(client,
            new Uri("https://example.invalid/"), Keys, Guid.NewGuid()));
    }
    [TestMethod]
    [TestCategory("NativeCardanoPublic")]
    public async Task RetainedNativeCatalogueReturnsThreeAndFilteredTwoInRequestedOrder()
    {
        string? path = Environment.GetEnvironmentVariable("COSMIC_CARDANO_PUBLIC_SOURCE");
        if (string.IsNullOrEmpty(path)) Assert.Inconclusive("Explicit retained source catalogue required.");
        string json = await File.ReadAllTextAsync(path!);
        using var configuration = JsonDocument.Parse(json);
        var keys = configuration.RootElement.GetProperty("blockHashes").EnumerateArray().Select(row => row.GetString()!).ToArray();
        Assert.AreEqual(3, keys.Length);
        using var source = CardanoPublicHeaderMetadataSource.FromJson(json);
        var activated = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activated.IsError, activated.Exception?.ToString());
        foreach (int count in new[] { 3, 2 })
        {
            var selected = keys.Take(count).ToArray();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.CardanoOASIS).Result)
                .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, CardanoPublicHeaderMetadataSource.AddressMetadata))).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.CardanoOASIS,
                new[] { new CosmicQueryTarget(ProviderType.CardanoOASIS, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var loaded = await source.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(loaded.IsError, loaded.Exception?.ToString());
            Assert.AreEqual(count, loaded.Result.Count);
            CollectionAssert.AreEqual(selected, loaded.Result.Select(row =>
                (string)row.MetaData[CardanoPublicHeaderMetadataSource.AddressMetadata]).ToArray());
            Assert.IsTrue(loaded.Result.All(row => row.IsPublic && ulong.Parse(
                (string)row.MetaData["CardanoReportedConfirmations"]) >= CardanoPublicHeaderMetadataSource.MinimumReportedConfirmations));
        }
    }

}
