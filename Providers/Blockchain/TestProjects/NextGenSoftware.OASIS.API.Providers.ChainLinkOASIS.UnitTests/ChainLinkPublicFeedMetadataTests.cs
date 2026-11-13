using System.Net;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Xunit;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS.UnitTests;

public sealed class ChainLinkPublicFeedMetadataTests
{
    private static readonly ChainLinkPublicFeedMetadataSource.Feed[] Feeds = {
        new("0x5f4ec3df9cbd43714fe2740f5e3616155c5b8419", "ETH / USD", 8),
        new("0xf4030086522a5beea4988f8ca5b36dbc97bee88c", "BTC / USD", 8),
        new("0x2c1d072e956affc0d435cb7ac38ef18d24d9127c", "LINK / USD", 8)
    };
    private const string Block = "0x" + "1111111111111111111111111111111111111111111111111111111111111111";

    private static string Word(BigInteger value)
    {
        if (value < 0) value += BigInteger.Pow(2, 256);
        return value.ToString("x").TrimStart('0').PadLeft(64, '0');
    }

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "";
        public List<string> Calls = new();
        public List<string> FeedReads = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.Equal(HttpMethod.Post, request.Method);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var input = body.RootElement;
            string method = input.GetProperty("method").GetString()!;
            Calls.Add(method);
            long id = input.GetProperty("id").GetInt64();
            var parameters = input.GetProperty("params");
            object result;

            if (method == "eth_chainId") result = Fault == "chain" ? "0x2" : "0x1";
            else if (method == "eth_getBlockByNumber")
            {
                Assert.False(parameters[1].GetBoolean());
                string selector = parameters[0].GetString()!;
                Assert.Contains(selector, new[] { "0x0", "finalized" });
                result = selector == "0x0"
                    ? new { number = "0x0", hash = Fault == "genesis" ? Block : "0xd4e56740f876aef8c010b86a40d5f56745a118d0906a34e69aec8c0db1cb8fa3", timestamp = "0x0" }
                    : new { number = Fault == "quantity" ? "0x01" : "0x100", hash = Fault == "hash" ? "0x12" : Block, timestamp = "0x3e8" };
            }
            else
            {
                Assert.Equal("eth_call", method); // No discovery scans, signing or transaction submission.
                string address = parameters[0].GetProperty("to").GetString()!;
                var feed = Assert.Single(Feeds.Where(f => f.Address == address));
                FeedReads.Add(address);
                Assert.Equal(Block, parameters[1].GetProperty("blockHash").GetString());
                Assert.True(parameters[1].GetProperty("requireCanonical").GetBoolean());
                string data = parameters[0].GetProperty("data").GetString()!;
                if (data == "0x7284e416")
                {
                    string name = Fault == "name" || (Fault == "last" && address == Feeds[^1].Address) ? "OTHER / USD" : feed.Name;
                    string bytes = Convert.ToHexString(Encoding.UTF8.GetBytes(name)).ToLowerInvariant().PadRight(64, '0');
                    result = "0x" + Word(Fault == "offset" ? 64 : 32) + Word(name.Length) + bytes;
                    if (Fault == "padding") result = ((string)result)[..^1] + "1";
                    if (Fault == "utf8") result = ((string)result)[..130] + "ff" + ((string)result)[132..];
                }
                else if (data == "0x313ce567") result = "0x" + Word(Fault == "decimals" ? 9 : 8);
                else
                {
                    Assert.Equal("0xfeaf968c", data);
                    BigInteger round = Fault == "round-width" ? BigInteger.Pow(2, 80) : Fault == "round-zero" ? 0 : BigInteger.Parse("129127208515966896564");
                    BigInteger answer = Fault == "negative" ? -1 : Fault == "minimum" ? -BigInteger.Pow(2, 255) : BigInteger.Pow(2, 255) - 1;
                    BigInteger started = Fault == "started" ? 0 : 900;
                    BigInteger updated = Fault == "future" ? 1001 : Fault == "backward" ? 899 : 950;
                    BigInteger answered = Fault == "answered-width" ? BigInteger.Pow(2, 80) : Fault == "deprecated" ? 0 : round;
                    result = "0x" + Word(round) + Word(answer) + Word(started) + Word(updated) + Word(answered);
                    if (Fault == "truncated") result = ((string)result)[..^2];
                }
            }

            string text = JsonSerializer.Serialize(new { jsonrpc = "2.0", id, result });
            if (Fault == "id") text = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = id.ToString(), result });
            if (Fault == "rpc") text = JsonSerializer.Serialize(new { jsonrpc = "2.0", id, error = new { code = -32603 } });
            if (Fault == "duplicate") text = "{\"id\":1,\"id\":2}";
            if (Fault == "oversize") text = new string(' ', 262145);
            return new HttpResponseMessage(Fault == "http" ? HttpStatusCode.Forbidden : HttpStatusCode.OK) {
                Content = new StringContent(text, Encoding.UTF8, "application/json")
            };
        }
    }

    [Theory]
    [InlineData("normal")] [InlineData("point")] [InlineData("subset")] [InlineData("window")]
    [InlineData("negative")] [InlineData("minimum")] [InlineData("deprecated")]
    [InlineData("chain")] [InlineData("genesis")] [InlineData("hash")] [InlineData("quantity")]
    [InlineData("name")] [InlineData("last")] [InlineData("offset")] [InlineData("padding")] [InlineData("utf8")]
    [InlineData("decimals")] [InlineData("round-width")] [InlineData("answered-width")] [InlineData("round-zero")]
    [InlineData("started")] [InlineData("future")] [InlineData("backward")] [InlineData("truncated")]
    [InlineData("id")] [InlineData("rpc")] [InlineData("duplicate")] [InlineData("oversize")] [InlineData("http")]
    [InlineData("cancel")] [InlineData("budget")] [InlineData("unknown")] [InlineData("inactive")] [InlineData("empty-window-error")]
    public async Task PublishesOnlyCompleteVerifiedSelections(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new ChainLinkPublicFeedMetadataSource(client, new Uri("https://example.invalid/"), "0x1", Feeds, Guid.NewGuid());
        if (scenario != "inactive") Assert.False((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Calls.Clear(); native.FeedReads.Clear();
        native.Fault = scenario == "empty-window-error" ? "last" : scenario;
        string[] selected = scenario == "unknown" ? new[] { "0x" + new string('9', 40) }
            : (scenario == "subset" ? Feeds.Take(2) : Feeds).Select(f => f.Address).ToArray();
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ChainLinkOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, ChainLinkPublicFeedMetadataSource.AddressMetadata))).Result;
        if (scenario == "point")
        {
            string key = Feeds[0].Address;
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ChainLinkOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, ChainLinkPublicFeedMetadataSource.AddressMetadata) == key).Result;
        }
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.ChainLinkOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ChainLinkOASIS, source.QueryCapabilities) });
        Assert.False(plan.IsError, plan.Message);
        using var cancel = new CancellationTokenSource();
        if (scenario == "cancel") cancel.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancel.Token);
        if (scenario is "normal" or "point" or "subset" or "window" or "negative" or "minimum" or "deprecated")
        {
            Assert.False(result.IsError, result.Exception?.ToString());
            Assert.Equal(scenario == "point" || scenario == "window" ? 1 : scenario == "subset" ? 2 : 3, result.Result.Count);
            Assert.Equal(scenario == "negative" ? "-1" : scenario == "minimum" ? (-BigInteger.Pow(2, 255)).ToString() : (BigInteger.Pow(2, 255) - 1).ToString(), result.Result[0].MetaData["ChainLinkAnswer"]);
            Assert.Equal("129127208515966896564", result.Result[0].MetaData["ChainLinkRoundId"]);
            Assert.Equal("8", result.Result[0].MetaData["ChainLinkDecimals"]);
            Assert.All(result.Result, row => Assert.True(row.IsPublic));
            Assert.Equal(scenario == "point" ? 3 : selected.Length * 3, native.FeedReads.Count);
        }
        else
        {
            Assert.True(result.IsError, "No partial result may escape.");
            Assert.True(result.Result == null || result.Result.Count == 0);
        }
        if (scenario is "inactive" or "cancel" or "unknown") Assert.Empty(native.Calls);
    }

    [Fact]
    public async Task PublicFeedCannotSaveAndNeverSubmitsTransactions()
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new ChainLinkPublicFeedMetadataSource(client, new Uri("https://example.invalid/"), "0x1", Feeds, Guid.NewGuid());
        Assert.False((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Calls.Clear();
        Assert.True((await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), IsNewHolon = true }, saveChildren: false)).IsError);
        Assert.Empty(native.Calls);
    }

    [Theory]
    [InlineData("network")] [InlineData("credential")] [InlineData("uppercase")] [InlineData("duplicates")]
    [InlineData("owner")] [InlineData("secret")] [InlineData("descriptor-secret")] [InlineData("decimals")]
    public void RejectsInvalidOrCredentialedConfigurationBeforeNativeIo(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        if (scenario is "secret" or "descriptor-secret")
        {
            string json = scenario == "secret" ? "{\"privateKey\":\"forbidden\"}" : JsonSerializer.Serialize(new {
                schema = 1, rpcUrl = "https://example.invalid/", chainId = "0x1", sourceOwner = Guid.NewGuid(),
                feeds = new[] { new { address = Feeds[0].Address, name = Feeds[0].Name, decimals = 8, privateKey = "forbidden" } }
            });
            Assert.Throws<ArgumentException>(() => ChainLinkPublicFeedMetadataSource.FromJson(json));
        }
        else
        {
            var selected = scenario == "duplicates" ? new[] { Feeds[0], Feeds[0] } : Feeds;
            if (scenario == "uppercase") selected = new[] { Feeds[0] with { Address = Feeds[0].Address.ToUpperInvariant() } };
            if (scenario == "decimals") selected = new[] { Feeds[0] with { Decimals = 256 } };
            Assert.Throws<ArgumentException>(() => new ChainLinkPublicFeedMetadataSource(client,
                new Uri(scenario == "credential" ? "https://user:secret@example.invalid/" : "https://example.invalid/"),
                scenario == "network" ? "0x2" : "0x1", selected, scenario == "owner" ? Guid.Empty : Guid.NewGuid()));
        }
        Assert.Empty(native.Calls);
    }
}
