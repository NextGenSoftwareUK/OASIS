using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS.UnitTests;

[TestClass]
[DoNotParallelize]
public sealed class TonPublicHeaderMetadataTests
{
    private static string Hash(byte value) => Convert.ToBase64String(Enumerable.Repeat(value, 32).ToArray());
    private static readonly TonPublicHeaderReference[] Blocks = Enumerable.Range(0, 3)
        .Select(index => new TonPublicHeaderReference((uint)(10 + index), Hash((byte)(index + 1)), Hash((byte)(index + 4)))).ToArray();

    private sealed class Gateway : HttpMessageHandler
    {
        public string Fault = "normal";
        public int Requests;
        public readonly List<long> Starts = new();
        public readonly List<long> Completions = new();
        public bool SlowFirst;

        private HttpResponseMessage Complete(HttpResponseMessage response)
        {
            Completions.Add(System.Diagnostics.Stopwatch.GetTimestamp());
            return response;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Requests++;
            Starts.Add(System.Diagnostics.Stopwatch.GetTimestamp());
            if (SlowFirst && Requests == 1) await Task.Delay(1300, token);
            Assert.AreEqual("toncenter.com", request.RequestUri!.Host);
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.IsNull(request.Headers.Authorization);
            if (Fault == "http") return Complete(new HttpResponseMessage(HttpStatusCode.TooManyRequests));
            if (Fault is "http-detail" or "http-long")
                return Complete(new HttpResponseMessage(HttpStatusCode.InternalServerError) {
                    Content = new StringContent(Fault == "http-detail"
                        ? "{\"error\":\"controlled native error\"}"
                        : new string('x', 5000) + "END_MARKER")
                });

            object result;
            if (request.RequestUri.AbsolutePath.EndsWith("getMasterchainInfo"))
                result = new Dictionary<string, object> {
                    ["@type"] = "blocks.masterchainInfo", ["init"] = new Dictionary<string, object> {
                        ["@type"] = "ton.blockIdExt", ["workchain"] = -1, ["shard"] = "0", ["seqno"] = 0,
                        ["root_hash"] = TonPublicHeaderReference.GenesisRootHash,
                        ["file_hash"] = TonPublicHeaderReference.GenesisFileHash
                    }
                };
            else
            {
                var query = request.RequestUri.Query.TrimStart('?').Split('&').Select(value => value.Split('=', 2))
                    .ToDictionary(value => value[0], value => Uri.UnescapeDataString(value[1]));
                var block = Blocks.Single(value => value.Sequence.ToString() == query["seqno"]);
                Assert.AreEqual(block.RootHash, query["root_hash"]);
                Assert.AreEqual(block.FileHash, query["file_hash"]);
                Assert.AreEqual("-1", query["workchain"]);
                Assert.AreEqual(TonPublicHeaderReference.MasterchainShard, query["shard"]);
                object Id(uint sequence, string root, string file) => new Dictionary<string, object> {
                    ["@type"] = "ton.blockIdExt", ["workchain"] = -1,
                    ["shard"] = TonPublicHeaderReference.MasterchainShard, ["seqno"] = sequence,
                    ["root_hash"] = root, ["file_hash"] = file
                };
                var header = new Dictionary<string, object> {
                    ["@type"] = "blocks.header", ["global_id"] = -239,
                    ["id"] = Id(block.Sequence, Fault == "wrong-root" ? Hash(9) : block.RootHash, block.FileHash),
                    ["prev_blocks"] = new[] { Id(block.Sequence - 1, Hash(8), Hash(9)) },
                    ["start_lt"] = "9007199254740993", ["end_lt"] = "9007199254740994",
                    ["version"] = 0, ["gen_utime"] = 1614503606, ["validator_list_hash_short"] = 1,
                    ["catchain_seqno"] = 1, ["min_ref_mc_seqno"] = 9, ["prev_key_block_seqno"] = 8,
                    ["after_merge"] = false, ["after_split"] = false, ["before_split"] = false,
                    ["want_merge"] = true, ["want_split"] = false, ["is_key_block"] = false
                };
                if (Fault == "bad-last" && block.Sequence == 12) header["end_lt"] = "0";
                result = header;
            }

            string json = JsonSerializer.Serialize(new { ok = true, result });
            if (Fault == "protocol") json = "{\"ok\":false,\"result\":{}}";
            if (Fault == "oversize") json = new string(' ', 262145);
            return Complete(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    [DataTestMethod]
    [DataRow("http")] [DataRow("http-detail")] [DataRow("http-long")]
    public async Task HttpFailurePreservesStatusRouteAndBoundedOperatorDetailWithoutRetry(string fault)
    {
        var gateway = new Gateway { Fault = fault };
        using var client = new HttpClient(gateway);
        using var source = new TonPublicHeaderMetadataSource(client,
            new Uri(TonPublicHeaderMetadataSource.PublicEndpoint), Blocks, Guid.NewGuid());

        var activated = await source.ActivateQuerySourceAsync(default);
        Assert.IsTrue(activated.IsError);
        var error = activated.Exception as HttpRequestException;
        Assert.IsNotNull(error);
        Assert.AreEqual(fault == "http" ? HttpStatusCode.TooManyRequests : HttpStatusCode.InternalServerError, error.StatusCode);
        StringAssert.Contains(error.Message, "https://toncenter.com/api/v2/getMasterchainInfo");
        Assert.AreEqual(1, gateway.Requests, "Error observation must not replay the request.");
        if (fault == "http-detail") StringAssert.Contains(error.Message, "controlled native error");
        if (fault == "http-long")
        {
            StringAssert.Contains(error.Message, "[truncated to4096bytes]");
            Assert.IsFalse(error.Message.Contains("END_MARKER"));
            Assert.IsTrue(error.Message.Length < 5000);
        }
    }

    [TestMethod]
    public async Task SlowResponseDoesNotConsumeNextAdmissionInterval()
    {
        // Connection setup and server latency happen after dispatch starts.
        // Waiting from that start can allow the next request immediately after
        // a slow response, despite a nominal one-request-per-second gate.
        var gateway = new Gateway { SlowFirst = true };
        using var client = new HttpClient(gateway);
        using var source = new TonPublicHeaderMetadataSource(client,
            new Uri(TonPublicHeaderMetadataSource.PublicEndpoint), Blocks, Guid.NewGuid());

        var activated = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activated.IsError, activated.Exception?.ToString());
        Assert.AreEqual(4, gateway.Starts.Count);
        Assert.AreEqual(4, gateway.Completions.Count);
        for (int index = 1; index < gateway.Starts.Count; index++)
            Assert.IsTrue(System.Diagnostics.Stopwatch.GetElapsedTime(
                gateway.Completions[index - 1], gateway.Starts[index]).TotalMilliseconds >= 1000,
                "A slow completed request must not consume the next anonymous admission interval.");
    }

    private static CosmicQueryPlan<Holon> Plan(TonPublicHeaderMetadataSource source, string[] keys, string mode)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.TONOASIS).Result)
            .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row, TonPublicHeaderMetadataSource.AddressMetadata))).Result;
        if (mode == "window") query = query.Skip(1).Result.Take(1).Result;
        if (mode == "bad-last") query = query.Take(0).Result;
        if (mode == "point")
        {
            string key = keys[0];
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.TONOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, TonPublicHeaderMetadataSource.AddressMetadata) == key).Result;
        }
        var planned = CosmicQueryPlanner.Plan(query, ProviderType.TONOASIS,
            new[] { new CosmicQueryTarget(ProviderType.TONOASIS, source.QueryCapabilities) });
        Assert.IsFalse(planned.IsError, planned.Message);
        return planned.Result;
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("bad-last")]
    [DataRow("budget")] [DataRow("unknown")] [DataRow("cancel")] [DataRow("inactive")]
    [DataRow("wrong-root")] [DataRow("http")] [DataRow("protocol")] [DataRow("oversize")]
    public async Task BoundedSelectionsValidateEverythingBeforePagingAndRespectAdmission(string scenario)
    {
        var gateway = new Gateway();
        using var client = new HttpClient(gateway);
        using var source = new TonPublicHeaderMetadataSource(client,
            new Uri(TonPublicHeaderMetadataSource.PublicEndpoint), Blocks, Guid.NewGuid());
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        gateway.Requests = 0;
        gateway.Starts.Clear();
        gateway.Fault = scenario;
        var keys = scenario == "unknown" ? new[] { Hash(9) } : Blocks.Select(value => value.RootHash).ToArray();
        using var cancellation = new CancellationTokenSource();
        if (scenario == "cancel") cancellation.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(Plan(source, keys, scenario),
            scenario == "budget" ? 2 : 100, cancellation.Token);

        if (scenario is "normal" or "point" or "window")
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            var expected = scenario == "window" ? keys.Skip(1).Take(1) : scenario == "point" ? keys.Take(1) : keys;
            CollectionAssert.AreEqual(expected.ToArray(), result.Result.Select(row =>
                (string)row.MetaData[TonPublicHeaderMetadataSource.AddressMetadata]).ToArray());
            Assert.AreEqual("9007199254740993", result.Result[0].MetaData["TonStartLogicalTime"]);
            Assert.IsTrue(result.Result.All(row => row.IsPublic));
        }
        else { Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); }

        if (scenario is "unknown" or "cancel" or "inactive") Assert.AreEqual(0, gateway.Requests);
        if (scenario == "http") Assert.AreEqual(1, gateway.Requests, "No retry or alternate endpoint.");
        foreach (var pair in gateway.Starts.Zip(gateway.Starts.Skip(1)))
            Assert.IsTrue(System.Diagnostics.Stopwatch.GetElapsedTime(pair.First, pair.Second).TotalMilliseconds >= 1000);
    }

    [TestMethod]
    [TestCategory("NativeTonPublic")]
    public async Task ExplicitRealCatalogueReturnsThreeAndTwo()
    {
        string? path = Environment.GetEnvironmentVariable("COSMIC_TON_PUBLIC_SOURCE");
        if (string.IsNullOrEmpty(path)) Assert.Inconclusive("Explicit retained source required.");
        string json = await File.ReadAllTextAsync(path!);
        using var configuration = JsonDocument.Parse(json);
        var keys = configuration.RootElement.GetProperty("blocks").EnumerateArray()
            .Select(value => value.GetProperty("rootHash").GetString()!).ToArray();
        Assert.AreEqual(3, keys.Length);
        using var source = TonPublicHeaderMetadataSource.FromJson(json);
        var active = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(active.IsError, active.Exception?.ToString());
        foreach (int count in new[] { 3, 2 })
        {
            var selected = keys.Take(count).ToArray();
            var loaded = await source.ExecuteCosmicQueryAsync(Plan(source, selected, "normal"));
            Assert.IsFalse(loaded.IsError, loaded.Exception?.ToString());
            CollectionAssert.AreEqual(selected, loaded.Result.Select(row =>
                (string)row.MetaData[TonPublicHeaderMetadataSource.AddressMetadata]).ToArray());
        }
    }
}
