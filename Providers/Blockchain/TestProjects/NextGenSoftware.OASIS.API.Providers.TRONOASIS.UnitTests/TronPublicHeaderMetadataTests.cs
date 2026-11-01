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

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronPublicFactAttribute : FactAttribute
{
    public TronPublicFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COSMIC_TRON_PUBLIC_SOURCE")))
            Skip = "Explicit retained public TRON catalogue required.";
    }
}

public class TronPublicHeaderMetadataTests
{
    private static readonly string[] Keys = { "1", "2", "3" };

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "normal";
        public int Requests;
        public bool Gated;
        public int InFlight, Maximum, Started;
        public TaskCompletionSource<bool> Reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Interlocked.Increment(ref Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            bool batch = request.RequestUri!.AbsoluteUri.EndsWith("getblockbylimitnext", StringComparison.Ordinal);
            long height;
            long end;
            if (batch)
            {
                height = body.RootElement.GetProperty("startNum").GetInt64();
                end = body.RootElement.GetProperty("endNum").GetInt64();
                Assert.InRange(end - height, 2, 8);
            }
            else
            {
                Assert.Equal(TronPublicHeaderMetadataSource.PublicEndpoint, request.RequestUri.AbsoluteUri);
                Assert.False(body.RootElement.GetProperty("detail").GetBoolean());
                height = long.Parse(body.RootElement.GetProperty("id_or_num").GetString()!, CultureInfo.InvariantCulture);
                end = height == long.MaxValue ? height : height + 1;
            }
            if (Gated && height != 0)
            {
                int active = Interlocked.Increment(ref InFlight);
                int observed;
                do { observed = Maximum; if (observed >= active) break; }
                while (Interlocked.CompareExchange(ref Maximum, active, observed) != observed);
                if (Interlocked.Increment(ref Started) == 3) Reached.TrySetResult(true);
                try { await Release.Task.WaitAsync(token); }
                finally { Interlocked.Decrement(ref InFlight); }
            }

            JsonObject Make(long height)
            {
                var raw = new JsonObject();
                var result = new JsonObject { ["blockID"] = height == 0 ? TronPublicHeaderMetadataSource.PublicGenesis
                : height.ToString("x16") + new string('a', 48), ["block_header"] = new JsonObject { ["raw_data"] = raw } };
            if (height == 0)
            {
                if (Fault == "genesis") result["blockID"] = new string('0', 64);
                if (Fault == "genesis-number") raw["number"] = 1;
            }
            else
            {
                raw["number"] = height; raw["timestamp"] = long.MaxValue;
                raw["parentHash"] = TronPublicHeaderMetadataSource.PublicGenesis;
                raw["txTrieRoot"] = new string('0', 64); raw["witness_address"] = "41" + new string('a', 40);
                if (Fault == "identity") raw["number"] = height + 1;
                if (Fault == "id-height") result["blockID"] = (height + 1).ToString("x16") + new string('a', 48);
                if (Fault == "precision") raw["timestamp"] = long.MaxValue.ToString(CultureInfo.InvariantCulture);
                if (Fault == "overflow") raw["timestamp"] = (ulong)long.MaxValue + 1;
                if (Fault == "negative") raw["timestamp"] = -1;
                if (Fault == "fraction") raw["timestamp"] = 1.5;
                if (Fault == "boolean") raw["timestamp"] = true;
                if (Fault == "parent") raw["parentHash"] = "bad";
                if (Fault == "root") raw["txTrieRoot"] = "bad";
                if (Fault == "witness") raw["witness_address"] = "bad";
                if (Fault == "missing") raw.Remove("timestamp");
                if (Fault == "last" && height == 3) raw.Remove("number");
                if (Fault == "null") result["block_header"] = null;
            }
                return result;
            }
            JsonObject result;
            if (batch)
            {
                var blocks = new JsonArray();
                for (long value = height; value < end; value++) blocks.Add(Make(value));
                if (Fault == "range-missing") blocks.RemoveAt(blocks.Count - 1);
                if (Fault == "range-extra") blocks.Add(Make(end));
                if (Fault == "range-order") { var swap = blocks[0]!.DeepClone(); blocks[0] = blocks[1]!.DeepClone(); blocks[1] = swap; }
                result = new JsonObject { ["block"] = blocks };
            }
            else result = Make(height);
            if (Fault == "error") result["Error"] = "";
            if (Fault == "transactions") result["transactions"] = new JsonArray();
            string text = result.ToJsonString();
            if (Fault == "duplicate") text = "{\"blockID\":1,\"blockID\":2}";
            if (Fault == "oversize") text = new string(' ', 65537);
            return new HttpResponseMessage(Fault == "http" ? HttpStatusCode.BadGateway : HttpStatusCode.OK)
                { Content = new StringContent(text, Encoding.UTF8, "application/json") };
        }
    }

    private static CosmicQueryPlan<Holon> Plan(TronPublicHeaderMetadataSource source, string[] keys)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.TRONOASIS).Result)
            .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row, TronPublicHeaderMetadataSource.AddressMetadata))).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.TRONOASIS,
            new[] { new CosmicQueryTarget(ProviderType.TRONOASIS, source.QueryCapabilities) });
        Assert.False(plan.IsError, plan.Message);
        return plan.Result;
    }

    [Theory]
    [InlineData("normal")] [InlineData("point")] [InlineData("window")] [InlineData("budget")]
    [InlineData("genesis")] [InlineData("genesis-number")] [InlineData("identity")] [InlineData("id-height")]
    [InlineData("precision")] [InlineData("overflow")] [InlineData("negative")] [InlineData("fraction")] [InlineData("boolean")]
    [InlineData("parent")] [InlineData("root")] [InlineData("witness")] [InlineData("missing")] [InlineData("last")]
    [InlineData("null")] [InlineData("error")] [InlineData("transactions")] [InlineData("duplicate")]
    [InlineData("oversize")] [InlineData("http")] [InlineData("inactive")] [InlineData("cancel")]
    [InlineData("range-missing")] [InlineData("range-extra")] [InlineData("range-order")]
    [InlineData("off-catalogue")] [InlineData("empty-window-error")]
    public async Task OnlyCompleteNativeSelectionsEscape(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new TronPublicHeaderMetadataSource(client, new Uri(TronPublicHeaderMetadataSource.PublicEndpoint), Keys, Guid.NewGuid());
        if (scenario != "inactive") Assert.False((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Requests = 0; native.Fault = scenario == "empty-window-error" ? "last" : scenario;
        string[] selected = scenario == "off-catalogue" ? new[] { "9" } : Keys;
        var plan = Plan(source, selected);
        if (scenario is "point" or "window" or "empty-window-error")
        {
            var query = plan.Query;
            if (scenario == "point")
            {
                string key = Keys[0];
                query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.TRONOASIS).Result)
                    .Where(row => CosmicQueryMetadata.Value<string>(row, TronPublicHeaderMetadataSource.AddressMetadata) == key).Result;
            }
            if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
            if (scenario == "empty-window-error") query = query.Take(0).Result;
            plan = CosmicQueryPlanner.Plan(query, ProviderType.TRONOASIS,
                new[] { new CosmicQueryTarget(ProviderType.TRONOASIS, source.QueryCapabilities) }).Result;
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
                Assert.Equal("9223372036854775807", row.MetaData["TronTimestampMilliseconds"]);
                Assert.Equal("SolidityNodeSolidified", row.MetaData["TronConsistency"]);
            });
        }
        else Assert.True(result.IsError, "A partial page must never escape.");
        if (scenario is "inactive" or "cancel" or "off-catalogue") Assert.Equal(0, native.Requests);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OverlapIsBoundedAndCancellationDrainsAllReads(bool cancelInFlight)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        var keys = Enumerable.Range(1, 6).Select(value => (value * 2 - 1).ToString(CultureInfo.InvariantCulture)).ToArray();
        using var source = new TronPublicHeaderMetadataSource(client, new Uri(TronPublicHeaderMetadataSource.PublicEndpoint), keys, Guid.NewGuid());
        Assert.False((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Gated = true;
        using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var pending = source.ExecuteCosmicQueryAsync(Plan(source, keys), cancellationToken: cancel.Token);
        try { await native.Reached.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
        finally { if (cancelInFlight) cancel.Cancel(); else native.Release.TrySetResult(true); }
        var result = await pending;
        Assert.Equal(3, native.Maximum);
        Assert.Equal(0, native.InFlight);
        if (cancelInFlight) Assert.True(result.IsError);
        else
        {
            Assert.False(result.IsError, result.Exception?.ToString());
            Assert.Equal(keys, result.Result.Select(row => (string)row.MetaData[TronPublicHeaderMetadataSource.AddressMetadata]));
        }
    }

    [Fact]
    public void ExplicitCanonicalConfigurationRequired()
    {
        foreach (string key in new[] { "0", "01", "+1", "1.0", "9223372036854775808" })
            Assert.Throws<ArgumentException>(() => TronPublicHeaderMetadataSource.ValidateAddress(key));
        using var client = new HttpClient(new Native());
        Assert.Throws<ArgumentException>(() => new TronPublicHeaderMetadataSource(client, new Uri("https://api.shasta.trongrid.io/walletsolidity/getblock"), Keys, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new TronPublicHeaderMetadataSource(client, new Uri(TronPublicHeaderMetadataSource.PublicEndpoint), new[] { "1", "1" }, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => TronPublicHeaderMetadataSource.FromJson("{\"signer\":123}"));
    }

    [TronPublicFact]
    public async Task RetainedNativeCatalogueRunsSharedManagerThreeAndTwo()
    {
        string json = await File.ReadAllTextAsync(Environment.GetEnvironmentVariable("COSMIC_TRON_PUBLIC_SOURCE")!);
        using var source = TronPublicHeaderMetadataSource.FromJson(json);
        using var config = JsonDocument.Parse(json);
        var keys = config.RootElement.GetProperty("blockHeights").EnumerateArray().Select(row => row.GetString()!).ToArray();
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
