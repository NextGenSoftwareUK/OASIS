using System.Net;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Source = NextGenSoftware.OASIS.API.Providers.ENSOffchainOASIS.EnsOffchainPublicNameSource;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlEnsOffchainPublicNameTests
{
    private static readonly Dictionary<string, string> Profiles = new() {
        ["brantly.eth"] = "0x983110309620d911731ac0932219af06091b6744",
        ["nick.eth"] = "0xb8c2c29ee19d8307cb7255e1cd9cbde883a267d5",
        ["vitalik.eth"] = "0xd8da6bf26964af9d7eed9e03e53415d37aa96045" };

    private sealed class Web3Bio : HttpMessageHandler
    {
        public string Fault = "";
        public readonly List<string> Paths = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("api.web3.bio", request.RequestUri!.Host);
            var path = request.RequestUri.AbsolutePath; Paths.Add(path);
            var name = path["/ns/ens/".Length..];
            object body = new { address = Profiles[name], identity = Fault == "identity" ? "other.eth" : name,
                platform = "ens", displayName = name, avatar = (string?)null };
            var status = Fault == "redirect" ? HttpStatusCode.Found : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(status) {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") });
        }
    }

    [Theory]
    [InlineData("normal")] [InlineData("identity")] [InlineData("redirect")] [InlineData("unknown")]
    public async Task EnrolledNamesReturnExactProfilesAndAnythingElseFails(string scenario)
    {
        var http = new Web3Bio(); using var client = new HttpClient(http);
        using var source = new Source(client, Source.Origin, Guid.NewGuid(), Profiles.Keys);
        Assert.False((await source.ActivateQuerySourceAsync(default)).IsError);
        http.Paths.Clear(); http.Fault = scenario;
        var selected = scenario == "unknown" ? new[] { "unlisted.eth" } : Profiles.Keys.Take(2).ToArray();
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ENSOffchainOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, Source.NameMetadata))).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.ENSOffchainOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ENSOffchainOASIS, source.QueryCapabilities) });
        Assert.False(plan.IsError, plan.Message);
        var response = await source.ExecuteCosmicQueryAsync(plan.Result, 100, default);
        if (scenario != "normal")
        {
            Assert.True(response.IsError);
            Assert.Equal(scenario == "unknown" ? "CQL_INVALID_SOURCE_KEY" : "CQL_QUERY_FAILED", response.ErrorCode);
            return;
        }
        Assert.False(response.IsError, response.Message);
        Assert.Equal(new[] { "brantly.eth", "nick.eth" }, response.Result.Select(row => (string)row.MetaData[Source.NameMetadata]));
        Assert.All(response.Result, row => {
            Assert.True(row.IsPublic);
            Assert.Equal(Profiles[(string)row.MetaData[Source.NameMetadata]], row.MetaData[Source.AddressMetadata]); });
        Assert.Equal(new[] { "/ns/ens/brantly.eth", "/ns/ens/nick.eth" }, http.Paths);
    }

    [Fact]
    public void ConfigurationIsExactAndRejectsOtherOriginsAndCredentials()
    {
        string Json(object value) => JsonSerializer.Serialize(value);
        var names = Profiles.Keys.ToArray();
        using (Source.FromJson(Json(new { schema = 1, origin = "https://api.web3.bio/", sourceOwner = Guid.NewGuid(), names }))) { }
        Assert.Throws<ArgumentException>(() => Source.FromJson(Json(new { schema = 1, origin = "https://evil.example/", sourceOwner = Guid.NewGuid(), names })));
        Assert.Throws<ArgumentException>(() => Source.FromJson(Json(new { schema = 1, origin = "https://api.web3.bio/", sourceOwner = Guid.NewGuid(), names, apiKey = "forbidden-test" })));
        Assert.Throws<ArgumentException>(() => Source.FromJson(Json(new { schema = 1, origin = "https://api.web3.bio/", sourceOwner = Guid.NewGuid(), names = new[] { "Vitalik.ETH" } })));
    }
}
