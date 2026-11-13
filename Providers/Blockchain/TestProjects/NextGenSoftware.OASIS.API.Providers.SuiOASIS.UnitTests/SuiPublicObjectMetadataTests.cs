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

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass]
public sealed class SuiPublicObjectMetadataTests
{
    private static readonly string[] Keys = new[] { 5, 6, 7 }.Select(value => "0x" + value.ToString("x64")).ToArray();

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "normal";
        public int Requests;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Requests++;
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual(SuiPublicObjectMetadataSource.PublicEndpoint, request.RequestUri!.AbsoluteUri);
            using var input = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            string query = input.RootElement.GetProperty("query").GetString()!;
            Assert.IsTrue(query.StartsWith("query(", StringComparison.Ordinal));
            Assert.IsFalse(query.Contains("mutation", StringComparison.Ordinal));
            StringAssert.Contains(query, "view:checkpoint");
            var selected = input.RootElement.GetProperty("variables").EnumerateObject().Select(row => row.Value.GetString()!).ToArray();
            var rows = new JsonObject();
            for (int index = 0; index < selected.Length; index++)
            {
                string key = selected[index];
                Assert.IsTrue(Keys.Contains(key));
                Assert.IsFalse(query.Contains(key, StringComparison.Ordinal), "Object IDs must be variables, never interpolated GraphQL.");
                var row = new JsonObject { ["address"] = key, ["version"] = 9007199254740991L,
                    ["digest"] = SuiPublicObjectMetadataSource.PublicGenesis };
                if (Fault == "identity") row["address"] = Keys[(Array.IndexOf(Keys, key) + 1) % Keys.Length];
                if (Fault == "precision") row["version"] = "9007199254740991";
                if (Fault == "overflow") row["version"] = 9007199254740992L;
                if (Fault == "negative") row["version"] = -1;
                if (Fault == "fraction") row["version"] = 0.5;
                if (Fault == "boolean") row["version"] = true;
                if (Fault == "digest") row["digest"] = new string('0', 44);
                if (Fault == "width") row["digest"] = new string('1', 31);
                if (Fault == "last" && index == 2) row["address"] = "invalid";
                rows["r" + index] = row;
            }
            if (Fault == "missing") rows.Remove("r2");
            if (Fault == "extra") rows["r9"] = new JsonObject();
            if (Fault == "null") rows["r2"] = null;
            var view = new JsonObject { ["sequenceNumber"] = 9007199254740991L,
                ["digest"] = SuiPublicObjectMetadataSource.PublicGenesis, ["query"] = rows };
            if (Fault == "checkpoint-overflow") view["sequenceNumber"] = 9007199254740992L;
            if (Fault == "checkpoint-digest") view["digest"] = "invalid";
            var data = new JsonObject { ["chainIdentifier"] = Fault == "network" ? new string('1', 32) : SuiPublicObjectMetadataSource.PublicGenesis,
                ["genesis"] = new JsonObject { ["digest"] = Fault == "genesis" ? new string('1', 32) : SuiPublicObjectMetadataSource.PublicGenesis },
                ["view"] = view };
            var response = new JsonObject { ["data"] = data };
            if (Fault == "partial") response["errors"] = new JsonArray(new JsonObject { ["message"] = "failed" });
            if (Fault == "empty-errors") response["errors"] = new JsonArray();
            string text = response.ToJsonString();
            if (Fault == "oversize") text = new string(' ', 262145);
            if (Fault == "duplicate") text = "{\"data\":{},\"data\":{}}";
            return new HttpResponseMessage(Fault == "http" ? HttpStatusCode.BadGateway : HttpStatusCode.OK)
                { Content = new StringContent(text, Encoding.UTF8, "application/json") };
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("budget")]
    [DataRow("network")] [DataRow("genesis")] [DataRow("identity")]
    [DataRow("precision")] [DataRow("overflow")] [DataRow("negative")] [DataRow("fraction")] [DataRow("boolean")]
    [DataRow("digest")] [DataRow("width")] [DataRow("checkpoint-overflow")] [DataRow("checkpoint-digest")]
    [DataRow("missing")] [DataRow("extra")] [DataRow("null")] [DataRow("last")]
    [DataRow("partial")] [DataRow("empty-errors")] [DataRow("oversize")] [DataRow("duplicate")] [DataRow("http")]
    [DataRow("inactive")] [DataRow("cancel")] [DataRow("empty-window-error")]
    public async Task OnlyCompleteCheckpointSelectionsEscape(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new SuiPublicObjectMetadataSource(client, new Uri(SuiPublicObjectMetadataSource.PublicEndpoint), Keys, Guid.NewGuid());
        if (scenario != "inactive")
        {
            var activated = await source.ActivateQuerySourceAsync(default);
            Assert.IsFalse(activated.IsError, activated.Exception?.ToString());
        }
        native.Requests = 0;
        native.Fault = scenario == "empty-window-error" ? "last" : scenario;

        var selected = Keys;
        var pointKey = Keys[0];
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SuiOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, SuiPublicObjectMetadataSource.AddressMetadata))).Result;
        if (scenario == "point")
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SuiOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, SuiPublicObjectMetadataSource.AddressMetadata) == pointKey).Result;
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.SuiOASIS,
            new[] { new CosmicQueryTarget(ProviderType.SuiOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancel = new CancellationTokenSource();
        if (scenario == "cancel") cancel.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancel.Token);
        if (scenario is "normal" or "point" or "window")
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(scenario == "normal" ? 3 : 1, result.Result.Count);
            Assert.IsTrue(result.Result.All(row => row.IsPublic));
            Assert.AreEqual("9007199254740991", result.Result[0].MetaData["SuiObjectVersion"]);
            Assert.AreEqual("PerQueryCheckpoint", result.Result[0].MetaData["SuiConsistency"]);
        }
        else Assert.IsTrue(result.IsError, "No partial page may escape.");
        if (scenario is "inactive" or "cancel") Assert.AreEqual(0, native.Requests);
    }

    [TestMethod]
    public void CanonicalAccountAndExplicitMainnetConfigRequired()
    {
        foreach (string key in Keys) Assert.AreEqual(key, SuiPublicObjectMetadataSource.ValidateAddress(key));
        foreach (string key in new[] { "0x5", Keys[0].ToUpperInvariant(), "0x" + new string('A', 64) })
            Assert.ThrowsException<ArgumentException>(() => SuiPublicObjectMetadataSource.ValidateAddress(key));
        using var client = new HttpClient(new Native());
        Assert.ThrowsException<ArgumentException>(() => new SuiPublicObjectMetadataSource(client,
            new Uri("https://graphql.testnet.sui.io/graphql"), Keys, Guid.NewGuid()));
        Assert.ThrowsException<ArgumentException>(() => SuiPublicObjectMetadataSource.FromJson("{\"round\":123}"));
    }

    [TestMethod]
    [TestCategory("NativeSuiPublic")]
    public async Task RetainedNativeCatalogueRunsSharedManagerThreeAndTwo()
    {
        string? path = Environment.GetEnvironmentVariable("COSMIC_SUI_PUBLIC_SOURCE");
        if (string.IsNullOrEmpty(path)) Assert.Inconclusive("Explicit retained catalogue required.");
        string json = await File.ReadAllTextAsync(path!);
        using var source = SuiPublicObjectMetadataSource.FromJson(json);
        using var configuration = JsonDocument.Parse(json);
        var keys = configuration.RootElement.GetProperty("objects").EnumerateArray().Select(row => row.GetString()!).ToArray();
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
