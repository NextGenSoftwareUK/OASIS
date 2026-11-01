using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS.UnitTests;

public sealed class ElrondPublicFactAttribute : FactAttribute
{
    public ElrondPublicFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COSMIC_ELROND_PUBLIC_SOURCE")))
            Skip = "Explicit retained public ELROND catalogue required.";
    }
}

public class ElrondPublicAccountMetadataTests
{
    private static readonly string[] Keys = {
        "erd1qqqqqqqqqqqqqqqpqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqplllst77y4l",
        "erd1rf4hv70arudgzus0ymnnsnc4pml0jkywg2xjvzslg0mz4nn2tg7q7k0t6p",
        "erd18mv2z6r2ksn4rfmm52tmhkc6x5tz6achmynvxftq4ay927029qqqmqpzfw" };

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "normal";
        public int Requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Interlocked.Increment(ref Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("api.multiversx.com", request.RequestUri!.Host);
            string text;
            if (request.RequestUri.AbsolutePath == "/dapp/config")
            {
                text = JsonSerializer.Serialize(new { id = Fault == "network-name" ? "devnet" : "mainnet",
                    chainId = Fault == "network" ? "D" : "1", egldDenomination = Fault == "denomination" ? "4" : "18", decimals = "4" });
            }
            else
            {
                Assert.Equal("/accounts", request.RequestUri.AbsolutePath);
                var parameters = request.RequestUri.Query.TrimStart('?').Split('&').Select(part => part.Split('=', 2))
                    .ToDictionary(part => part[0], part => Uri.UnescapeDataString(part[1]));
                string[] keys = parameters["addresses"].Split(',');
                Assert.Equal("0", parameters["from"]);
                Assert.Equal(keys.Length.ToString(CultureInfo.InvariantCulture), parameters["size"]);
                Assert.Equal("address,balance,nonce,shard", parameters["fields"]);
                var accounts = new JsonArray();
                foreach (string key in keys)
                {
                    Assert.Contains(key, Keys);
                    var row = new JsonObject { ["address"] = key, ["balance"] = "13901850058901293848051868",
                        ["nonce"] = 9007199254740991L, ["shard"] = key == Keys[0] ? uint.MaxValue : 0U };
                    if (Fault == "identity") row["address"] = ElrondKeys.Address(Enumerable.Repeat((byte)9, 32).ToArray());
                    if (Fault == "checksum") row["address"] = key.Substring(0, key.Length - 1) + "q";
                    if (Fault == "balance-number") row["balance"] = 123;
                    if (Fault == "balance-leading-zero") row["balance"] = "01";
                    if (Fault == "balance-negative") row["balance"] = "-1";
                    if (Fault == "nonce-overflow") row["nonce"] = 9007199254740992L;
                    if (Fault == "nonce-string") row["nonce"] = "1";
                    if (Fault == "nonce-fraction") row["nonce"] = 0.5;
                    if (Fault == "nonce-boolean") row["nonce"] = true;
                    if (Fault == "nonce-negative") row["nonce"] = -1;
                    if (Fault == "shard") row["shard"] = 3;
                    if (Fault == "shard-boolean") row["shard"] = true;
                    if (Fault == "shard-string") row["shard"] = "0";
                    if (Fault == "last" && key == Keys[2]) row.Remove("nonce");
                    accounts.Add(row);
                }
                if (Fault == "missing") accounts.RemoveAt(accounts.Count - 1);
                if (Fault == "extra") accounts.Add(accounts[0]!.DeepClone());
                if (Fault == "duplicate") accounts[accounts.Count - 1] = accounts[0]!.DeepClone();
                if (Fault == "null") accounts[accounts.Count - 1] = null;
                // The API need not preserve request order; mapping must restore it.
                text = new JsonArray(accounts.Reverse().Select(row => row?.DeepClone()).ToArray()).ToJsonString();
            }
            if (Fault == "duplicate-json") text = "{\"id\":1,\"id\":2}";
            if (Fault == "oversize") text = new string(' ', 65537);
            return Task.FromResult(new HttpResponseMessage(Fault == "http" ? HttpStatusCode.BadGateway : HttpStatusCode.OK)
                { Content = new StringContent(text, Encoding.UTF8, "application/json") });
        }
    }

    private static CosmicQueryPlan<Holon> Plan(ElrondPublicAccountMetadataSource source, string[] keys)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ElrondOASIS).Result)
            .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row, ElrondPublicAccountMetadataSource.AddressMetadata))).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.ElrondOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ElrondOASIS, source.QueryCapabilities) });
        Assert.False(plan.IsError, plan.Message);
        return plan.Result;
    }

    [Theory]
    [InlineData("normal")] [InlineData("point")] [InlineData("window")] [InlineData("budget")]
    [InlineData("network")] [InlineData("network-name")] [InlineData("denomination")]
    [InlineData("identity")] [InlineData("checksum")] [InlineData("missing")] [InlineData("extra")] [InlineData("duplicate")] [InlineData("null")]
    [InlineData("balance-number")] [InlineData("balance-leading-zero")] [InlineData("balance-negative")]
    [InlineData("nonce-overflow")] [InlineData("nonce-string")] [InlineData("nonce-fraction")] [InlineData("nonce-boolean")] [InlineData("nonce-negative")]
    [InlineData("shard")] [InlineData("shard-boolean")] [InlineData("shard-string")]
    [InlineData("last")] [InlineData("duplicate-json")] [InlineData("oversize")] [InlineData("http")]
    [InlineData("inactive")] [InlineData("cancel")] [InlineData("off-catalogue")] [InlineData("empty-window-error")]
    public async Task OnlyCompleteNativeSelectionsEscape(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new ElrondPublicAccountMetadataSource(client, new Uri(ElrondPublicAccountMetadataSource.PublicEndpoint), Keys, Guid.NewGuid());
        if (scenario != "inactive") Assert.False((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Requests = 0; native.Fault = scenario == "empty-window-error" ? "last" : scenario;
        string[] selected = scenario == "off-catalogue" ? new[] { ElrondKeys.Address(Enumerable.Repeat((byte)9, 32).ToArray()) } : Keys;
        var plan = Plan(source, selected);
        if (scenario is "point" or "window" or "empty-window-error")
        {
            var query = plan.Query;
            if (scenario == "point")
            {
                string key = Keys[0];
                query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ElrondOASIS).Result)
                    .Where(row => CosmicQueryMetadata.Value<string>(row, ElrondPublicAccountMetadataSource.AddressMetadata) == key).Result;
            }
            if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
            if (scenario == "empty-window-error") query = query.Take(0).Result;
            plan = CosmicQueryPlanner.Plan(query, ProviderType.ElrondOASIS,
                new[] { new CosmicQueryTarget(ProviderType.ElrondOASIS, source.QueryCapabilities) }).Result;
        }
        using var cancel = new CancellationTokenSource();
        if (scenario == "cancel") cancel.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan, scenario == "budget" ? 2 : 100, cancel.Token);
        if (scenario is "normal" or "point" or "window")
        {
            Assert.False(result.IsError, result.Exception?.ToString());
            Assert.Equal(scenario == "normal" ? 3 : 1, result.Result.Count);
            Assert.All(result.Result, row => {
                Assert.True(row.IsPublic);
                Assert.Equal("13901850058901293848051868", row.MetaData["ElrondBalanceBaseUnits"]);
                Assert.Equal("9007199254740991", row.MetaData["ElrondNonce"]);
                Assert.Equal("LatestIndexed", row.MetaData["ElrondConsistency"]);
            });
        }
        else Assert.True(result.IsError, "A partial page must never escape.");
        if (scenario is "inactive" or "cancel" or "off-catalogue") Assert.Equal(0, native.Requests);
    }

    [Fact]
    public void ExplicitCanonicalConfigurationRequired()
    {
        foreach (string key in new[] { "bad", Keys[0].ToUpperInvariant(), Keys[0][..^1] + "q" })
            Assert.ThrowsAny<Exception>(() => ElrondPublicAccountMetadataSource.ValidateAddress(key));
        using var client = new HttpClient(new Native());
        Assert.Throws<ArgumentException>(() => new ElrondPublicAccountMetadataSource(client, new Uri("https://devnet-api.multiversx.com/"), Keys, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new ElrondPublicAccountMetadataSource(client, new Uri(ElrondPublicAccountMetadataSource.PublicEndpoint), new[] { Keys[0], Keys[0] }, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => ElrondPublicAccountMetadataSource.FromJson("{\"signer\":123}"));
    }

    [ElrondPublicFact]
    public async Task RetainedNativeCatalogueRunsSharedManagerThreeAndTwo()
    {
        string json = await File.ReadAllTextAsync(Environment.GetEnvironmentVariable("COSMIC_ELROND_PUBLIC_SOURCE")!);
        using var source = ElrondPublicAccountMetadataSource.FromJson(json);
        using var config = JsonDocument.Parse(json);
        var keys = config.RootElement.GetProperty("addresses").EnumerateArray().Select(row => row.GetString()!).ToArray();
        var activated = await source.ActivateQuerySourceAsync(default);
        Assert.False(activated.IsError, activated.Exception?.ToString());
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        var manager = new HolonManager(null, dna, new ProviderManager(null, dna));
        using var output = new StringWriter();
        var actor = Guid.NewGuid();
        Assert.Equal(3, (await CosmicCqlDemo.NativeKeySetWalkthrough.RunAsync(manager, source, keys, actor, output)).Count);
        Assert.Equal(2, (await CosmicCqlDemo.NativeKeySetWalkthrough.RunAsync(manager, source, keys.Take(2), actor, output)).Count);
    }
}
