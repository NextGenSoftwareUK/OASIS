using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.AkashOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.CqlTests;

[TestClass]
public class AkashPublicProviderMetadataTests
{
    private static readonly string[] Addresses = {
        "akash1qqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqq7cdc78",
        "akash1lllllllllllllllllllllllllllllllljfvhed",
        "akash1u5cdg7k3gl43mukca4aeultuz8x2j68mgwn28e" };

    [DataTestMethod]
    [DataRow("full")]
    [DataRow("point")]
    [DataRow("pair")]
    [DataRow("page")]
    [DataRow("unknown")]
    [DataRow("inactive")]
    [DataRow("cancel")]
    [DataRow("budget")]
    [DataRow("native-error")]
    [DataRow("empty-page-error")]
    public async Task SelectionPreservesProvenanceOrFailsWithoutPartialRows(string scenario)
    {
        using var handler = new Native();
        using var client = new HttpClient(handler);
        var owner = Guid.NewGuid();
        using var source = new AkashPublicProviderMetadataSource(client, new Uri("https://example.invalid/"), scenario == "unknown" ? Addresses[..2] : Addresses, owner);
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        handler.Starts.Clear(); handler.Paths.Clear();
        if (scenario is "native-error" or "empty-page-error") handler.Fault = true;
        var selected = scenario == "pair" ? Addresses.Take(2).ToArray() : scenario == "unknown" ? new[] { Addresses[2] } : Addresses;
        var scope = CosmicQueryScope.Named(ProviderType.AkashOASIS).Result;
        var firstOwner = Addresses[0];
        var query = CosmicQuery<Holon>.Create(scope).Where(h => selected.Contains(CosmicQueryMetadata.Value<string>(h, "AkashProviderAddress"))).Result;
        if (scenario == "point") query = CosmicQuery<Holon>.Create(scope)
            .Where(h => CosmicQueryMetadata.Value<string>(h, "AkashProviderAddress") == firstOwner).Result;
        if (scenario == "page") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-page-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.AkashOASIS,
            new[] { new CosmicQueryTarget(ProviderType.AkashOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancel = new CancellationTokenSource();
        if (scenario == "cancel") cancel.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancel.Token);
        if (scenario is "full" or "point" or "pair" or "page")
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(scenario is "point" or "page" ? 1 : scenario == "pair" ? 2 : 3, result.Result.Count);
            foreach (var row in result.Result)
            {
                Assert.IsTrue(row.IsPublic); Assert.AreEqual(owner, row.CreatedByAvatarId);
                Assert.AreEqual("Provider", row.Name);
                Assert.AreEqual("https://127.0.0.1:8443/", row.MetaData["AkashProviderHostUri"]);
                Assert.AreEqual("true", row.MetaData["AkashProviderOnline"]);
                Assert.AreEqual("false", row.MetaData["AkashConsensusVerified"]);
                Assert.AreEqual("indexer-reported-provider-metadata", row.MetaData["AkashObservationPolicy"]);
                Assert.IsFalse(row.MetaData.ContainsKey("email"));
                Assert.IsFalse(row.MetaData.ContainsKey("attributes"));
                Assert.AreEqual(row.MetaData["AkashProviderAddress"], row.ProviderUniqueStorageKey[ProviderType.AkashOASIS]);
                Assert.AreEqual(NativeRecordHolonMapper.StableHolonId(owner, ProviderType.AkashOASIS,
                    "akash-public-providers:https://example.invalid/", row.MetaData["AkashProviderAddress"].ToString()), row.Id);
                Assert.AreEqual(0, row.Children.Count());
            }
        }
        else
        {
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Result == null || result.Result.Count == 0);
        }
        if (scenario is "unknown" or "inactive" or "cancel") Assert.AreEqual(0, handler.Starts.Count);
        if (scenario == "empty-page-error") Assert.AreEqual(3, handler.Starts.Count);
    }

    [TestMethod]
    public async Task UnsupportedFiltersAndWritesDoNotReachNativeTransport()
    {
        using var handler = new Native();
        using var client = new HttpClient(handler);
        using var source = new AkashPublicProviderMetadataSource(client, new Uri("https://example.invalid/"), Addresses, Guid.NewGuid());
        var firstOwner = Addresses[0];
        var queryResult = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AkashOASIS).Result)
            .Where(h => h.Name == firstOwner);
        Assert.IsFalse(queryResult.IsError, queryResult.Message);
        var query = queryResult.Result;
        Assert.IsTrue(CosmicQueryPlanner.Plan(query, ProviderType.AkashOASIS,
            new[] { new CosmicQueryTarget(ProviderType.AkashOASIS, source.QueryCapabilities) }).IsError);
        Assert.IsTrue((await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), IsNewHolon = true }, saveChildren: false)).IsError);
        Assert.IsTrue((await source.DeleteHolonAsync(Guid.NewGuid())).IsError);
        Assert.AreEqual(0, handler.Starts.Count);
    }

    [DataTestMethod]
    [DataRow("duplicate")]
    [DataRow("unknown")]
    [DataRow("empty-owner")]
    [DataRow("uppercase")]
    [DataRow("duplicate-name")]
    [DataRow("credentials")]
    public void ConfigurationRejectsAmbiguityAndUnsupportedIdentity(string scenario)
    {
        var fields = new Dictionary<string, object> { ["schema"] = 1, ["apiUrl"] = "https://example.invalid/",
            ["providerOwners"] = Addresses, ["sourceOwner"] = Guid.NewGuid() };
        if (scenario == "unknown") fields["apiKey"] = "inert";
        if (scenario == "empty-owner") fields["sourceOwner"] = Guid.Empty;
        if (scenario == "uppercase") fields["providerOwners"] = new[] { Addresses[0].ToUpperInvariant() };
        if (scenario == "duplicate-name") fields["providerOwners"] = new[] { Addresses[0], Addresses[0] };
        if (scenario == "credentials") fields["apiUrl"] = "https://user:inert@example.invalid/";
        var json = JsonSerializer.Serialize(fields);
        if (scenario == "duplicate") json = json.Insert(1, "\"schema\":1,");
        try
        {
            using var source = AkashPublicProviderMetadataSource.FromJson(json);
            Assert.Fail("Invalid source was accepted.");
        }
        catch (Exception error) when (error is ArgumentException or InvalidDataException) { }
    }

    private sealed class Native : HttpMessageHandler
    {
        public bool Fault;
        public List<string> Paths = new();
        public List<long> Starts = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("example.invalid", request.RequestUri!.Host);
            var path = request.RequestUri.AbsolutePath;
            Assert.IsTrue(path.StartsWith("/v1/providers/", StringComparison.Ordinal));
            var key = path["/v1/providers/".Length..];
            Assert.IsTrue(Addresses.Contains(key));
            Paths.Add(path); Starts.Add(System.Diagnostics.Stopwatch.GetTimestamp());
            var row = new { owner = key, name = "Provider", hostUri = "https://127.0.0.1:8443/", isOnline = true,
                email = "excluded@example.invalid", attributes = new[] { "not-projected" } };
            return Task.FromResult(new HttpResponseMessage(Fault && Paths.Count == 3
                ? System.Net.HttpStatusCode.ServiceUnavailable : System.Net.HttpStatusCode.OK) {
                Content = new StringContent(JsonSerializer.Serialize(row), System.Text.Encoding.UTF8, "application/json") });
        }
    }

}
