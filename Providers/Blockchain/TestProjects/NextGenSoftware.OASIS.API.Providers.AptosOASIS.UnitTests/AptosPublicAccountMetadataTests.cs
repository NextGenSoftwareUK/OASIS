using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS.UnitTests;

[TestClass]
public sealed class AptosPublicAccountMetadataTests
{
    private static readonly string[] Keys = new[] { 1, 3, 4 }
        .Select(value => "0x" + value.ToString("x64")).ToArray();

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "normal";
        public int Requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Requests++;
            Assert.AreEqual(HttpMethod.Get, request.Method);
            var path = request.RequestUri!.AbsolutePath;
            object result;
            if (path == "/v1/")
                result = new { chain_id = Fault == "body-network" ? 2 : 1,
                    ledger_version = Fault == "body-version" ? "99" : "100", oldest_ledger_version = "0" };
            else
            {
                Assert.AreEqual("?ledger_version=100", request.RequestUri.Query, "Every account must use the selected version.");
                string key = path[13..];
                int index = Array.IndexOf(Keys, key);
                Assert.IsTrue(index >= 0);
                var row = new JsonObject {
                    ["sequence_number"] = "18446744073709551615",
                    ["authentication_key"] = "0x" + new string('a',64)
                };
                if (Fault == "precision") row["sequence_number"] = 123;
                if (Fault == "negative") row["sequence_number"] = "-1";
                if (Fault == "overflow") row["sequence_number"] = "18446744073709551616";
                if (Fault == "padding") row["sequence_number"] = "01";
                if (Fault == "authentication") row["authentication_key"] = "0x1";
                if (Fault == "extra") row["extra"] = "unexpected";
                if (Fault == "last" && index == 2) row["sequence_number"] = "invalid";
                result = row;
            }
            string text = JsonSerializer.Serialize(result);
            if (Fault == "oversize") text = new string(' ', 262145);
            if (Fault == "duplicate") text = "{\"round\":0,\"round\":1}";
            var response = new HttpResponseMessage(Fault == "http" ? HttpStatusCode.Gone : HttpStatusCode.OK)
                { Content = new StringContent(text, Encoding.UTF8, "application/json") };
            if (Fault != "missing-header") response.Headers.Add("X-APTOS-CHAIN-ID", Fault == "network" ? "2" : "1");
            response.Headers.Add("X-APTOS-LEDGER-VERSION", Fault == "future" && path != "/v1/" ? "99" : "100");
            response.Headers.Add("X-APTOS-LEDGER-OLDEST-VERSION", Fault == "pruned" && path != "/v1/" ? "101" : "0");
            if (Fault == "duplicate-header") response.Headers.Add("X-APTOS-CHAIN-ID", "1");
            return Task.FromResult(response);
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("budget")]
    [DataRow("network")] [DataRow("body-network")] [DataRow("body-version")]
    [DataRow("future")] [DataRow("pruned")] [DataRow("authentication")] [DataRow("extra")]
    [DataRow("precision")] [DataRow("negative")] [DataRow("overflow")] [DataRow("padding")]
    [DataRow("last")] [DataRow("oversize")] [DataRow("duplicate")] [DataRow("http")]
    [DataRow("missing-header")] [DataRow("duplicate-header")]
    [DataRow("inactive")] [DataRow("cancel")] [DataRow("empty-window-error")]
    public async Task OnlyCompleteCurrentStateSelectionsEscape(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new AptosPublicAccountMetadataSource(client, new Uri("https://aptos.invalid/v1/"), Keys, Guid.NewGuid());
        if (scenario != "inactive")
        {
            var activated = await source.ActivateQuerySourceAsync(default);
            Assert.IsFalse(activated.IsError, activated.Exception?.ToString());
        }
        native.Requests = 0;
        native.Fault = scenario == "empty-window-error" ? "last" : scenario;

        var selected = Keys;
        var pointKey = Keys[0];
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AptosOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, AptosPublicAccountMetadataSource.AddressMetadata))).Result;
        if (scenario == "point")
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AptosOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, AptosPublicAccountMetadataSource.AddressMetadata) == pointKey).Result;
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.AptosOASIS,
            new[] { new CosmicQueryTarget(ProviderType.AptosOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancel = new CancellationTokenSource();
        if (scenario == "cancel") cancel.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancel.Token);
        if (scenario is "normal" or "point" or "window")
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(scenario == "normal" ? 3 : 1, result.Result.Count);
            Assert.IsTrue(result.Result.All(row => row.IsPublic));
            Assert.AreEqual("18446744073709551615", result.Result[0].MetaData["AptosSequenceNumber"]);
            Assert.AreEqual("PerQueryLedgerVersion", result.Result[0].MetaData["AptosConsistency"]);
            Assert.IsTrue(result.Result.All(row => (string)row.MetaData["AptosLedgerVersion"] == "100"));
        }
        else Assert.IsTrue(result.IsError, "No partial page may escape.");
        if (scenario is "inactive" or "cancel") Assert.AreEqual(0, native.Requests);
    }

    [TestMethod]
    public void CanonicalAddressAndExplicitCurrentConfigRequired()
    {
        foreach (string key in Keys) Assert.AreEqual(key, AptosPublicAccountMetadataSource.ValidateAddress(key));
        foreach (string key in new[] { Keys[0].ToUpperInvariant(), Keys[0] + "=", new string('A', 58) })
            Assert.ThrowsException<ArgumentException>(() => AptosPublicAccountMetadataSource.ValidateAddress(key));
        Assert.ThrowsException<ArgumentException>(() => AptosPublicAccountMetadataSource.FromJson("{\"round\":123}"));
    }

    [TestMethod]
    [TestCategory("NativeAptosPublic")]
    public async Task RetainedNativeCatalogueRunsSharedManagerThreeAndTwo()
    {
        string? path = Environment.GetEnvironmentVariable("COSMIC_APTOS_PUBLIC_SOURCE");
        if (string.IsNullOrEmpty(path)) Assert.Inconclusive("Explicit retained catalogue required.");
        string json = await File.ReadAllTextAsync(path!);
        using var source = AptosPublicAccountMetadataSource.FromJson(json);
        using var configuration = JsonDocument.Parse(json);
        var keys = configuration.RootElement.GetProperty("accounts").EnumerateArray().Select(row => row.GetString()!).ToArray();
        Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        var manager = new HolonManager(null, dna, new ProviderManager(null, dna));
        using var output = new StringWriter();
        var actor = Guid.NewGuid();
        var full = await CosmicCqlDemo.NativeKeySetWalkthrough.RunAsync(manager, source, keys, actor, output);
        var subset = await CosmicCqlDemo.NativeKeySetWalkthrough.RunAsync(manager, source, keys.Take(2), actor, output);
        Assert.AreEqual(3, full.Count);
        Assert.AreEqual(2, subset.Count);
    }
}
