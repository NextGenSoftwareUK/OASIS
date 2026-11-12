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

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests;

[TestClass]
public sealed class HederaPublicAccountMetadataTests
{
    private static readonly string[] Keys = { "0.0.2", "0.0.3", "0.0.98" };

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
            Assert.AreEqual("?limit=1", request.RequestUri.Query, "No hidden transaction scan or historical fallback.");
            Assert.IsTrue(path.StartsWith("/api/v1/accounts/", StringComparison.Ordinal));
            string key = path[17..];
            int index = Array.IndexOf(Keys, key);
            Assert.IsTrue(index >= 0);
            var balance = new JsonObject { ["balance"] = long.MaxValue, ["timestamp"] = (100 + index) + ".123456789" };
            var row = new JsonObject { ["account"] = key, ["deleted"] = false,
                ["balance"] = balance, ["transactions"] = new JsonArray() };
            if (Fault == "identity") row["account"] = Keys[(index + 1) % Keys.Length];
            if (Fault == "deleted") row["deleted"] = true;
            if (Fault == "deleted-type") row["deleted"] = "false";
            if (Fault == "precision") balance["balance"] = "9223372036854775807";
            if (Fault == "negative") balance["balance"] = -1;
            if (Fault == "overflow") balance["balance"] = ulong.MaxValue;
            if (Fault == "timestamp") balance["timestamp"] = "1.2";
            if (Fault == "timestamp-overflow") balance["timestamp"] = "18446744073709551616.000000000";
            if (Fault == "history") row["transactions"] = new JsonArray(new JsonObject(), new JsonObject());
            if (Fault == "last" && index == 2) row["account"] = "invalid";
            result = row;
            string text = JsonSerializer.Serialize(result);
            if (Fault == "oversize") text = new string(' ', 262145);
            if (Fault == "duplicate") text = "{\"round\":0,\"round\":1}";
            return Task.FromResult(new HttpResponseMessage(Fault == "http" ? HttpStatusCode.BadGateway : HttpStatusCode.OK)
                { Content = new StringContent(text, Encoding.UTF8, "application/json") });
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("budget")]
    [DataRow("identity")] [DataRow("deleted")] [DataRow("deleted-type")] [DataRow("precision")]
    [DataRow("negative")] [DataRow("overflow")] [DataRow("timestamp")] [DataRow("timestamp-overflow")]
    [DataRow("history")] [DataRow("last")] [DataRow("oversize")] [DataRow("duplicate")] [DataRow("http")]
    [DataRow("inactive")] [DataRow("cancel")] [DataRow("empty-window-error")]
    public async Task OnlyCompleteIndexedSelectionsEscape(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new HederaPublicAccountMetadataSource(client, new Uri(HederaPublicAccountMetadataSource.PublicEndpoint), Keys, Guid.NewGuid());
        if (scenario != "inactive")
        {
            var activated = await source.ActivateQuerySourceAsync(default);
            Assert.IsFalse(activated.IsError, activated.Exception?.ToString());
        }
        native.Requests = 0;
        native.Fault = scenario == "empty-window-error" ? "last" : scenario;

        var selected = Keys;
        var pointKey = Keys[0];
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.HashgraphOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, HederaPublicAccountMetadataSource.AddressMetadata))).Result;
        if (scenario == "point")
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.HashgraphOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, HederaPublicAccountMetadataSource.AddressMetadata) == pointKey).Result;
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.HashgraphOASIS,
            new[] { new CosmicQueryTarget(ProviderType.HashgraphOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancel = new CancellationTokenSource();
        if (scenario == "cancel") cancel.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancel.Token);
        if (scenario is "normal" or "point" or "window")
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(scenario == "normal" ? 3 : 1, result.Result.Count);
            Assert.IsTrue(result.Result.All(row => row.IsPublic));
            Assert.AreEqual("9223372036854775807", result.Result[0].MetaData["HederaBalanceTinybars"]);
            Assert.AreEqual("LatestIndexed", result.Result[0].MetaData["HederaConsistency"]);
        }
        else Assert.IsTrue(result.IsError, "No partial page may escape.");
        if (scenario is "inactive" or "cancel") Assert.AreEqual(0, native.Requests);
    }

    [TestMethod]
    public void CanonicalAccountAndExplicitMainnetConfigRequired()
    {
        foreach (string key in Keys) Assert.AreEqual(key, HederaPublicAccountMetadataSource.ValidateAddress(key));
        foreach (string key in new[] { "00.0.2", "0.0.0", "0.0.18446744073709551616" })
            Assert.ThrowsException<ArgumentException>(() => HederaPublicAccountMetadataSource.ValidateAddress(key));
        using var client = new HttpClient(new Native());
        Assert.ThrowsException<ArgumentException>(() => new HederaPublicAccountMetadataSource(client,
            new Uri("https://testnet.mirrornode.hedera.com/api/v1/"), Keys, Guid.NewGuid()));
        Assert.ThrowsException<ArgumentException>(() => HederaPublicAccountMetadataSource.FromJson("{\"round\":123}"));
    }

    [TestMethod]
    [TestCategory("NativeHederaPublic")]
    public async Task RetainedNativeCatalogueRunsSharedManagerThreeAndTwo()
    {
        string? path = Environment.GetEnvironmentVariable("COSMIC_HEDERA_PUBLIC_SOURCE");
        if (string.IsNullOrEmpty(path)) Assert.Inconclusive("Explicit retained catalogue required.");
        string json = await File.ReadAllTextAsync(path!);
        using var source = HederaPublicAccountMetadataSource.FromJson(json);
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
