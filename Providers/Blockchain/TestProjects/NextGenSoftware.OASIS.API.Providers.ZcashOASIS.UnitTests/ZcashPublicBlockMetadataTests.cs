using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS.UnitTests;

[TestClass]
public sealed class ZcashPublicBlockMetadataTests
{
    private static readonly string[] Hashes = { new('a', 64), new('b', 64), new('c', 64) };
    private sealed class Clock : TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency => 1;
        public override long GetTimestamp() => Interlocked.Add(ref ticks, 2);
    }

    private sealed class Transport : HttpMessageHandler
    {
        public string Fault = "";
        public int Requests;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Requests++;
            Assert.AreEqual(HttpMethod.Get, request.Method);
            var query = request.RequestUri!.Query;
            int height = int.Parse(query.Split("min_height=")[1].Split('&')[0]);
            Assert.AreEqual($"?min_height={height}&max_height={height}&limit=1&order=oldest", query);
            Assert.IsTrue(height >= 0 && height <= 3, "No discovery scans permitted.");
            if (Fault == "cancel-flight") await Task.Delay(Timeout.Infinite, token);
            if (int.TryParse(Fault, out int status)) return new HttpResponseMessage((HttpStatusCode)status);
            string hash = height == 0 ? ZcashPublicBlockMetadataSource.Genesis : Hashes[height - 1];
            if (Fault == "genesis" && height == 0 || Fault == "last" && height == 3) hash = new string('d', 64);
            string json = JsonSerializer.Serialize(new {
                data = new[] { new { height = Fault == "height" ? height + 1 : height, hash,
                    timestamp = Fault == "timestamp" ? -1L : 4294967295L } },
                meta = new { network = Fault == "network" ? "testnet" : "mainnet",
                    units = new { authoritativeMonetary = "zatoshi", authoritativeEncoding = "decimal-string",
                        zatoshiPerZec = Fault == "units" ? "1" : "100000000" },
                    freshness = new { status = Fault == "unavailable" ? "unavailable" : "unknown" } }
            });
            if (Fault == "duplicate") json = json.Replace("\"network\":", "\"network\":\"mainnet\",\"network\":");
            if (Fault == "oversize") json = new string(' ', 262145);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }

    private static ZcashPublicBlockMetadataSource Source(HttpClient client) => new(client,
        new Uri("https://example.invalid/"), ZcashPublicBlockMetadataSource.Genesis,
        Hashes.Select((hash, i) => new ZcashPublicBlockReference(i + 1, hash)), Guid.NewGuid(), timeProvider: new Clock());

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("budget")]
    [DataRow("genesis")] [DataRow("last")] [DataRow("height")] [DataRow("network")]
    [DataRow("units")] [DataRow("timestamp")] [DataRow("unavailable")] [DataRow("duplicate")]
    [DataRow("oversize")] [DataRow("403")] [DataRow("429")] [DataRow("500")]
    [DataRow("empty-window-error")] [DataRow("inactive")] [DataRow("cancel")] [DataRow("unknown")]
    public async Task QueriesPublishOnlyCompleteIdentityCheckedSelections(string scenario)
    {
        var transport = new Transport();
        using var client = new HttpClient(transport);
        using var source = Source(client);
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        transport.Requests = 0;
        transport.Fault = scenario == "empty-window-error" ? "last" : scenario;
        var selected = scenario == "unknown" ? new[] { new string('e', 64) } : Hashes;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ZcashOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, ZcashPublicBlockMetadataSource.AddressMetadata))).Result;
        if (scenario == "point")
        {
            string key = Hashes[0];
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ZcashOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, ZcashPublicBlockMetadataSource.AddressMetadata) == key).Result;
        }
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.ZcashOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ZcashOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancellation = new CancellationTokenSource();
        if (scenario == "cancel") cancellation.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancellation.Token);
        if (scenario is "normal" or "point" or "window")
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(scenario == "normal" ? 3 : 1, result.Result.Count);
            Assert.AreEqual(scenario == "window" ? Hashes[1] : Hashes[0], result.Result[0].MetaData[ZcashPublicBlockMetadataSource.AddressMetadata]);
            foreach (var row in result.Result)
            {
                Assert.IsTrue(row.IsPublic);
                Assert.AreEqual("4294967295", row.MetaData["ZcashBlockTimestamp"]);
                Assert.AreEqual("unknown", row.MetaData["ZcashFreshness"]);
                Assert.AreEqual("false", row.MetaData["ZcashProofVerified"]);
            }
        }
        else Assert.IsTrue(result.IsError, "No partial or malformed source output may escape.");
        if (scenario is "inactive" or "cancel" or "unknown") Assert.AreEqual(0, transport.Requests);
        if (scenario is "403" or "429" or "500") Assert.AreEqual(1, transport.Requests, "No implicit retry.");
    }

    [TestMethod]
    public async Task WritesAreRejectedWithoutRequestsOrMutation()
    {
        var transport = new Transport();
        using var client = new HttpClient(transport);
        using var source = Source(client);
        var row = new Holon { Name = "Unchanged", Version = 7 };
        Assert.AreEqual("CQL_READ_ONLY_SOURCE", (await source.SaveHolonAsync(row)).ErrorCode);
        Assert.AreEqual("CQL_READ_ONLY_SOURCE", (await source.SaveHolonsAsync(new[] { row })).ErrorCode);
        Assert.AreEqual("CQL_READ_ONLY_SOURCE", (await source.DeleteHolonAsync(row.Id)).ErrorCode);
        Assert.AreEqual("CQL_READ_ONLY_SOURCE", (await source.DeleteHolonAsync("unknown")).ErrorCode);
        Assert.AreEqual(7, row.Version);
        Assert.AreEqual(0, transport.Requests);
    }

    [TestMethod]
    public async Task TransportCancellationPublishesNoRows()
    {
        var transport = new Transport { Fault = "cancel-flight" };
        using var client = new HttpClient(transport);
        using var source = Source(client);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var result = await source.ActivateQuerySourceAsync(cancellation.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode);
        Assert.AreEqual(1, transport.Requests);
    }
    [DataTestMethod]
    [DataRow("http://example.invalid/")]
    [DataRow("https://secret@example.invalid/")]
    [DataRow("https://example.invalid/?key=secret")]
    [DataRow("https://example.invalid/other")]
    public void InvalidEndpointsAreRejectedBeforeNetwork(string endpoint)
    {
        using var client = new HttpClient(new Transport());
        Assert.ThrowsException<ArgumentException>(() => new ZcashPublicBlockMetadataSource(client,
            new Uri(endpoint), ZcashPublicBlockMetadataSource.Genesis,
            new[] { new ZcashPublicBlockReference(1, Hashes[0]) }, Guid.NewGuid()));
    }

    [TestMethod]
    public void ConfigurationRejectsUnknownDuplicateAndInvalidIdentityFields()
    {
        string configuration = JsonSerializer.Serialize(new { schema = 1, apiUrl = "https://example.invalid/",
            genesisHash = ZcashPublicBlockMetadataSource.Genesis,
            blocks = new[] { new { height = 1, hash = Hashes[0] } }, sourceOwner = Guid.NewGuid() });
        using var valid = ZcashPublicBlockMetadataSource.FromJson(configuration);
        foreach (string invalid in new[] {
            configuration.Replace("\"schema\":1", "\"schema\":1,\"privateKey\":\"forbidden\""),
            configuration.Replace("\"schema\":1", "\"schema\":1,\"schema\":1"),
            configuration.Replace("\"height\":1", "\"height\":-1"),
            configuration.Replace(Hashes[0], Hashes[0].ToUpperInvariant()) })
        {
            bool rejected = false;
            try { using var unexpected = ZcashPublicBlockMetadataSource.FromJson(invalid); }
            catch (ArgumentException) { rejected = true; }
            catch (InvalidDataException) { rejected = true; }
            Assert.IsTrue(rejected, "Invalid explicit profile must not be silently accepted.");
        }
    }
}
