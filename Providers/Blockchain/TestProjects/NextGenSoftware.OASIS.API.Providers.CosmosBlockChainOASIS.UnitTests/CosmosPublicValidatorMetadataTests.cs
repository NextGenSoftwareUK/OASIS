using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass]
public sealed class CosmosPublicValidatorMetadataTests
{
    private static readonly string[] Keys = {
        "cosmosvaloper1qphf0ferqcch0jca9hlqfm3x0eds3dpkcvpafp",
        "cosmosvaloper1qr6sk28w4r6kqsg0737wzgu05505t4glezetwn",
        "cosmosvaloper1q9p73lx07tjqc34vs8jrsu5pg3q4ha534uqv4w" };

    private sealed class Rest : HttpMessageHandler
    {
        public string Fault = "";
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Requests++;
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.IsNull(request.Content);
            var path = request.RequestUri!.AbsolutePath;
            object body;
            if (path.EndsWith("/blocks/latest"))
            {
                Assert.IsFalse(request.Headers.Contains("x-cosmos-block-height"));
                body = new { block_id = new { hash = Convert.ToBase64String(new byte[32]) },
                    block = new { header = new { chain_id = Fault == "chain" ? "other" : "cosmoshub-4", height = "9007199254740994" } } };
            }
            else
            {
                Assert.IsFalse(path.Contains('?'));
                Assert.AreEqual("9007199254740993", request.Headers.GetValues("x-cosmos-block-height").Single());
                string key = path.Split('/').Last();
                Assert.IsTrue(Keys.Contains(key), "No validator scans permitted.");
                body = new { validator = new { operator_address = Fault == "identity" ? Keys[1] : key,
                    tokens = Fault == "last" && key == Keys[2] ? "01" : "9007199254740993123456789",
                    status = "BOND_STATUS_BONDED", jailed = false, description = new { moniker = "Example validator" } } };
            }
            var json = Fault == "duplicate" ? "{\"validator\":{},\"validator\":{}}" : JsonSerializer.Serialize(body);
            if (Fault == "oversize") json = new string(' ', 262145);
            var response = new HttpResponseMessage(Fault == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK) {
                Content = new StringContent(json, Encoding.UTF8, "application/json") };
            if (Fault != "missing-height") response.Headers.Add("x-cosmos-block-height", Fault == "height" ? "1" : "9007199254740993");
            return Task.FromResult(response);
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("budget")]
    [DataRow("chain")] [DataRow("height")] [DataRow("missing-height")] [DataRow("identity")]
    [DataRow("last")] [DataRow("empty-window-error")] [DataRow("duplicate")]
    [DataRow("oversize")] [DataRow("http")] [DataRow("inactive")] [DataRow("cancel")]
    public async Task QueryRequiresCompleteHeightPinnedNativeSelection(string scenario)
    {
        var rest = new Rest(); using var client = new HttpClient(rest);
        using var source = new CosmosPublicValidatorMetadataSource(client, new Uri("https://example.invalid/"), "cosmoshub-4", Keys, Guid.NewGuid());
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        rest.Requests = 0; rest.Fault = scenario == "empty-window-error" ? "last" : scenario;
        var selected = Keys;
        var point = Keys[0];
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.CosmosBlockChainOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, CosmosPublicValidatorMetadataSource.AddressMetadata))).Result;
        if (scenario == "point") query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.CosmosBlockChainOASIS).Result)
            .Where(row => CosmicQueryMetadata.Value<string>(row, CosmosPublicValidatorMetadataSource.AddressMetadata) == point).Result;
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.CosmosBlockChainOASIS,
            new[] { new CosmicQueryTarget(ProviderType.CosmosBlockChainOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancel = new CancellationTokenSource();
        if (scenario == "cancel") cancel.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancel.Token);
        if (scenario is "normal" or "point" or "window")
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(scenario == "normal" ? 3 : 1, result.Result.Count);
            foreach (var row in result.Result)
            {
                Assert.IsTrue(row.IsPublic);
                Assert.AreEqual("9007199254740993", row.MetaData["CosmosStateHeight"]);
                Assert.AreEqual("9007199254740993123456789", row.MetaData["CosmosValidatorTokens"]);
            }
            if (scenario == "window") Assert.AreEqual(Keys[1], result.Result[0].MetaData[CosmosPublicValidatorMetadataSource.AddressMetadata]);
        }
        else Assert.IsTrue(result.IsError, "Malformed/partial native state must never become success.");
        if (scenario is "inactive" or "cancel") Assert.AreEqual(0, rest.Requests);
    }

    [TestMethod]
    public async Task PublishedSharedWalkthroughExecutesThroughHolonManager()
    {
        using var client = new HttpClient(new Rest());
        using var source = new CosmosPublicValidatorMetadataSource(client,
            new Uri("https://example.invalid/"), "cosmoshub-4", Keys, Guid.NewGuid());
        Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA {
            OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2 } };
        var registry = new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null, dna);
        var manager = new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(null, dna, registry);
        using var output = new StringWriter();
        var actor = Guid.NewGuid();
        var full = await CosmicCqlDemo.NativeKeySetWalkthrough.RunAsync(manager, source, Keys, actor, output);
        var subset = await CosmicCqlDemo.NativeKeySetWalkthrough.RunAsync(manager, source, Keys.Take(2), actor, output);
        Assert.AreEqual(3, full.Count);
        Assert.AreEqual(2, subset.Count);
        CollectionAssert.AreEqual(full.Take(2).Select(row => row.Id).ToArray(), subset.Select(row => row.Id).ToArray());
    }

    [TestMethod]
    public void InvalidCatalogueAndSecretConfigurationFailBeforeNetwork()
    {
        using var client = new HttpClient(new Rest());
        Assert.ThrowsException<ArgumentException>(() => new CosmosPublicValidatorMetadataSource(client,
            new Uri("https://example.invalid/"), "cosmoshub-4", new[] { Keys[0][..^1] + "q" }, Guid.NewGuid()));
        Assert.ThrowsException<ArgumentException>(() => CosmosPublicValidatorMetadataSource.FromJson("{\"privateKey\":\"forbidden\"}"));
    }

    [TestMethod]
    [TestCategory("NativeCosmosPublic")]
    public async Task ConfiguredRealEndpointReturnsThreeAndFilteredTwo()
    {
        string? path = Environment.GetEnvironmentVariable("COSMIC_COSMOS_PUBLIC_SOURCE");
        if (string.IsNullOrEmpty(path)) Assert.Inconclusive("Explicit retained source configuration required.");
        using var output = new StringWriter();
        var result = await CosmicCqlDemo.CosmosPublicWalkthrough.RunAsync(path!, output);
        Assert.AreEqual((3, 2), result);
        StringAssert.Contains(output.ToString(), "Pinned state height:");
    }
}
