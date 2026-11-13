using System.Net;
using System.Numerics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.UnitTests;

[TestClass]
public sealed class RadixPublicResourceMetadataTests
{
    private static readonly string[] Keys = { "resource_rdx1tknxxxxxxxxxradxrdxxxxxxxxx009923554798xxxxxxxxxradxrd", "resource_rdx1nfxxxxxxxxxxsecpsgxxxxxxxxx004638826440xxxxxxxxxsecpsg", "resource_rdx1nfxxxxxxxxxxed25sgxxxxxxxxx002236757237xxxxxxxxxed25sg" };
    private const string OutsideKey = "resource_rdx1nfxxxxxxxxxxpkgwnrxxxxxxxxx002558553505xxxxxxxxxpkgwnr";

    private static string MaximumSupply => (((BigInteger.One << 191) - 1) / BigInteger.Pow(10, 18)).ToString(CultureInfo.InvariantCulture) + "." + (((BigInteger.One << 191) - 1) % BigInteger.Pow(10, 18)).ToString("D18", CultureInfo.InvariantCulture);

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "normal";
        public int Requests;
        public long State = long.MaxValue - 1;
        public bool BlockEntity;
        public TaskCompletionSource<bool> Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private JsonObject Context(bool entity) => new() {
            ["network"] = Fault == "ledger-network" ? "stokenet" : "mainnet",
            ["state_version"] = Fault == "zero-state" ? 0 : entity && Fault == "state" ? State - 1 : State,
            ["epoch"] = entity && Fault == "epoch" ? 8 : 7,
            ["round"] = entity && Fault == "round" ? 10 : 9,
            ["proposer_round_timestamp"] = Fault == "timestamp" ? "invalid" : entity && Fault == "timestamp-mismatch" ? "2026-11-01T00:00:01.123Z" : "2026-11-01T00:00:00.123Z" };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Interlocked.Increment(ref Requests);
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("mainnet.radixdlt.com", request.RequestUri!.Host);
            using var input = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            JsonObject response;
            switch (request.RequestUri.AbsolutePath)
            {
                case "/status/network-configuration":
                    response = new JsonObject { ["network_name"] = Fault == "network-name" ? "stokenet" : "mainnet", ["network_id"] = Fault == "network" ? 2 : 1 };
                    break;
                case "/status/gateway-status":
                    response = new JsonObject { ["ledger_state"] = Context(false) };
                    if (Fault == "integer-string") response["ledger_state"]!["epoch"] = "7";
                    if (Fault == "integer-overflow") response["ledger_state"]!["epoch"] = (ulong)long.MaxValue + 1;
                    if (Fault == "integer-negative") response["ledger_state"]!["epoch"] = -1;
                    if (Fault == "integer-fraction") response["ledger_state"]!["epoch"] = 0.5;
                    if (Fault == "integer-boolean") response["ledger_state"]!["epoch"] = true;
                    break;
                case "/state/entity/details":
                    Assert.AreEqual(State, input.RootElement.GetProperty("at_ledger_state").GetProperty("state_version").GetInt64());
                    CollectionAssert.AreEqual(new[] { "name", "symbol", "description" }, input.RootElement.GetProperty("opt_ins").GetProperty("explicit_metadata").EnumerateArray().Select(x => x.GetString()!).ToArray());
                    if (BlockEntity) { Entered.TrySetResult(true); await Task.Delay(Timeout.Infinite, token); }
                    var keys = input.RootElement.GetProperty("addresses").EnumerateArray().Select(x => x.GetString()!).ToArray();
                    var items = new JsonArray();
                    foreach (string key in keys.Reverse())
                    {
                        CollectionAssert.Contains(Keys, key);
                        bool fungible = key == Keys[0];
                        var labels = new JsonArray(new JsonObject { ["key"] = "name", ["value"] = new JsonObject { ["typed"] = new JsonObject { ["type"] = "String", ["value"] = "Public resource label" } }, ["last_updated_at_state_version"] = 1 });
                        var metadata = new JsonObject { ["total_count"] = 1, ["items"] = labels };
                        var details = new JsonObject { ["type"] = fungible ? "FungibleResource" : "NonFungibleResource", ["total_supply"] = fungible ? MaximumSupply : "0" };
                        var row = new JsonObject { ["address"] = key, ["details"] = details, ["explicit_metadata"] = metadata };
                        if (Fault == "identity") row["address"] = OutsideKey;
                        if (Fault == "checksum") row["address"] = key[..^1] + "q";
                        if (Fault == "kind") details["type"] = fungible ? "NonFungibleResource" : "FungibleResource";
                        if (Fault == "supply-number") details["total_supply"] = 1;
                        if (Fault == "supply-overflow") details["total_supply"] = "3138550867693340381917894711603833208051.177722232017256448";
                        if (Fault == "supply-fraction") details["total_supply"] = "0.0000000000000000001";
                        if (Fault == "supply-negative") details["total_supply"] = "-1";
                        if (Fault == "supply-leading-zero") details["total_supply"] = "01";
                        if (Fault == "supply-exponent") details["total_supply"] = "1e3";
                        if (Fault == "last" && key == Keys[2]) details.Remove("total_supply");
                        if (Fault == "metadata-partial") metadata["total_count"] = 2;
                        if (Fault == "metadata-cursor") metadata["next_cursor"] = "more";
                        if (Fault == "metadata-future") labels[0]!["last_updated_at_state_version"] = long.MaxValue;
                        if (Fault == "metadata-type") labels[0]!["value"]!["typed"]!["type"] = "U64";
                        if (Fault == "metadata-value") labels[0]!["value"]!["typed"]!["value"] = 1;
                        if (Fault == "metadata-key") labels[0]!["key"] = "unexpected";
                        if (Fault == "metadata-size") labels[0]!["value"]!["typed"]!["value"] = new string('x', 4097);
                        if (Fault == "metadata-duplicate") { labels.Add(labels[0]!.DeepClone()); metadata["total_count"] = 2; }
                        if (Fault == "metadata-absent") { labels.Clear(); metadata["total_count"] = 0; }
                        items.Add(row);
                    }
                    if (Fault == "missing") items.RemoveAt(items.Count - 1);
                    if (Fault == "extra") items.Add(items[0]!.DeepClone());
                    if (Fault == "duplicate") items[items.Count - 1] = items[0]!.DeepClone();
                    if (Fault == "null") items[items.Count - 1] = null;
                    response = new JsonObject { ["ledger_state"] = Context(true), ["items"] = items };
                    break;
                default: throw new AssertFailedException("Only declared read endpoints may be contacted.");
            }
            string text = response.ToJsonString();
            if (Fault == "oversize") text = new string(' ', 262145);
            if (Fault == "duplicate-json") text = "{\"network_id\":1,\"network_id\":1}";
            return new HttpResponseMessage(Fault == "http" ? HttpStatusCode.BadGateway : HttpStatusCode.OK) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("budget")] [DataRow("metadata-absent")]
    [DataRow("network")] [DataRow("network-name")] [DataRow("ledger-network")] [DataRow("zero-state")]
    [DataRow("state")] [DataRow("epoch")] [DataRow("round")] [DataRow("timestamp")] [DataRow("timestamp-mismatch")]
    [DataRow("integer-string")] [DataRow("integer-overflow")] [DataRow("integer-negative")] [DataRow("integer-fraction")] [DataRow("integer-boolean")]
    [DataRow("identity")] [DataRow("checksum")] [DataRow("kind")] [DataRow("missing")] [DataRow("extra")] [DataRow("duplicate")] [DataRow("null")]
    [DataRow("supply-number")] [DataRow("supply-overflow")] [DataRow("supply-fraction")] [DataRow("supply-negative")] [DataRow("supply-leading-zero")] [DataRow("supply-exponent")]
    [DataRow("metadata-partial")] [DataRow("metadata-cursor")] [DataRow("metadata-future")] [DataRow("metadata-type")] [DataRow("metadata-value")] [DataRow("metadata-key")] [DataRow("metadata-size")] [DataRow("metadata-duplicate")]
    [DataRow("last")] [DataRow("oversize")] [DataRow("duplicate-json")] [DataRow("http")]
    [DataRow("inactive")] [DataRow("cancel")] [DataRow("empty-window-error")]
    public async Task OnlyCompleteLedgerSelectionsEscape(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new RadixPublicResourceMetadataSource(client, new Uri(RadixPublicResourceMetadataSource.PublicEndpoint), Keys, Guid.NewGuid());
        if (scenario != "inactive")
        {
            var activated = await source.ActivateQuerySourceAsync(default);
            Assert.IsFalse(activated.IsError, activated.Exception?.ToString());
        }
        native.Requests = 0;
        native.Fault = scenario == "empty-window-error" ? "last" : scenario;

        var selected = Keys;
        var pointKey = Keys[0];
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.RadixOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, RadixPublicResourceMetadataSource.AddressMetadata))).Result;
        if (scenario == "point")
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.RadixOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, RadixPublicResourceMetadataSource.AddressMetadata) == pointKey).Result;
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.RadixOASIS,
            new[] { new CosmicQueryTarget(ProviderType.RadixOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancel = new CancellationTokenSource();
        if (scenario == "cancel") cancel.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancel.Token);
        if (scenario is "normal" or "point" or "window" or "metadata-absent")
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(scenario is "normal" or "metadata-absent" ? 3 : 1, result.Result.Count);
            Assert.IsTrue(result.Result.All(row => row.IsPublic));
            Assert.AreEqual((long.MaxValue - 1).ToString(CultureInfo.InvariantCulture), result.Result[0].MetaData["RadixLedgerStateVersion"]);
            Assert.AreEqual("PerQueryLedgerState", result.Result[0].MetaData["RadixConsistency"]);
            foreach (var row in result.Result)
                Assert.AreEqual(row.MetaData["RadixResourceType"].ToString() == "FungibleResource" ? MaximumSupply : "0", row.MetaData["RadixTotalSupply"]);
            if (scenario == "metadata-absent") Assert.IsFalse(result.Result[0].MetaData.ContainsKey("RadixResourceName"));
        }
        else Assert.IsTrue(result.IsError, "No partial page may escape.");
        if (scenario is "inactive" or "cancel") Assert.AreEqual(0, native.Requests);
    }

    [TestMethod]
    public void CanonicalResourceAndExplicitMainnetConfigRequired()
    {
        foreach (string key in Keys) Assert.AreEqual(key, RadixPublicResourceMetadataSource.ValidateAddress(key));
        foreach (string key in new[] { "bad", Keys[0].ToUpperInvariant(), Keys[0][..^1] + "q" })
            Assert.ThrowsException<ArgumentException>(() => RadixPublicResourceMetadataSource.ValidateAddress(key));
        using var client = new HttpClient(new Native());
        Assert.ThrowsException<ArgumentException>(() => new RadixPublicResourceMetadataSource(client, new Uri("https://stokenet.radixdlt.com/"), Keys, Guid.NewGuid()));
        Assert.ThrowsException<ArgumentException>(() => RadixPublicResourceMetadataSource.FromJson("{\"round\":123}"));
    }

    [TestMethod]
    public async Task InflightCancellationDrainsTheNativeRead()
    {
        var native = new Native(); using var client = new HttpClient(native);
        using var source = new RadixPublicResourceMetadataSource(client, new Uri(RadixPublicResourceMetadataSource.PublicEndpoint), Keys, Guid.NewGuid());
        Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        native.BlockEntity = true; using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var selected = Keys;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.RadixOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, RadixPublicResourceMetadataSource.AddressMetadata))).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.RadixOASIS, new[] { new CosmicQueryTarget(ProviderType.RadixOASIS, source.QueryCapabilities) }).Result;
        var pending = source.ExecuteCosmicQueryAsync(plan, cancellationToken: cancel.Token);
        try { await native.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); } finally { cancel.Cancel(); }
        var result = await pending; Assert.IsTrue(result.IsError); Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode);
    }

    [TestMethod]
    [TestCategory("NativeRadixPublic")]
    public async Task RetainedNativeCatalogueRunsSharedManagerThreeAndTwo()
    {
        string? path = Environment.GetEnvironmentVariable("COSMIC_RADIX_PUBLIC_SOURCE");
        if (string.IsNullOrEmpty(path)) Assert.Inconclusive("Explicit retained catalogue required.");
        string json = await File.ReadAllTextAsync(path!);
        using var source = RadixPublicResourceMetadataSource.FromJson(json);
        using var configuration = JsonDocument.Parse(json);
        var keys = configuration.RootElement.GetProperty("resources").EnumerateArray().Select(row => row.GetString()!).ToArray();
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
