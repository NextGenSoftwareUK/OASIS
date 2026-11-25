using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AxelarOASIS;
using NextGenSoftware.OASIS.API.Providers.ChainflipOASIS;
using NextGenSoftware.OASIS.API.Providers.ChainlinkFunctionsOASIS;
using NextGenSoftware.OASIS.API.Providers.deBridgeOASIS;
using NextGenSoftware.OASIS.API.Providers.HyperlaneOASIS;
using NextGenSoftware.OASIS.API.Providers.LayerZeroV2OASIS;
using NextGenSoftware.OASIS.Providers.Shared.CrossChain;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.Providers.CrossChainTrackers.CapabilityTests;

/// <summary>
/// capability-v1 against the real public tracker APIs. Each probe reads one real transfer straight from the API, then
/// loads the same transfer through the shipping provider by its source transaction and requires them to agree. A guard
/// on every request permits only the probe's fixed HTTPS host and read paths, without credentials.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class CrossChainTrackerCapabilityTests
{
    /// <summary>Real HTTP, restricted to one public host and the given read paths; counts round trips.</summary>
    private sealed class PublicReadGuard(string host, params (HttpMethod Method, string PathPrefix)[] allowed) : DelegatingHandler(new HttpClientHandler { AllowAutoRedirect = false })
    {
        public int Requests;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var uri = request.RequestUri!;
            if (uri.Scheme != "https" || uri.Host != host || uri.Port != 443 || !string.IsNullOrEmpty(uri.UserInfo) || request.Headers.Authorization != null
                || !allowed.Any(a => a.Method == request.Method && uri.AbsolutePath.StartsWith(a.PathPrefix, StringComparison.Ordinal)))
                throw new InvalidOperationException($"The public capability fixture refuses {request.Method} {uri.Host}{uri.AbsolutePath}.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            Interlocked.Increment(ref Requests);
            return await base.SendAsync(request, deadline.Token);
        }
    }

    private static string ReportDirectory()
    {
        if (Environment.GetEnvironmentVariable("OASIS_TRACKER_PUBLIC_LIVE") != "1")
            Assert.Inconclusive("Requires OASIS_TRACKER_PUBLIC_LIVE=1 (real public tracker APIs).");
        var directory = Environment.GetEnvironmentVariable("OASIS_CAPABILITY_REPORTS");
        Assert.IsFalse(string.IsNullOrWhiteSpace(directory), "OASIS_CAPABILITY_REPORTS is required.");
        Directory.CreateDirectory(directory!);
        return directory!;
    }

    private static async Task RunAsync(IOASISProvider provider, string name, Func<Task<NonStorageConformance.Probe>> probe)
    {
        var path = Path.Combine(ReportDirectory(), name + ".json");
        Assert.IsFalse(File.Exists(path) || File.Exists(path + ".next"), "Preserve the original capability report before another run.");
        var report = await NonStorageConformance.RunAsync(provider, name, "native", probe, async checkpoint =>
        {
            await File.WriteAllTextAsync(path + ".next", JsonSerializer.Serialize(checkpoint));
            File.Move(path + ".next", path, overwrite: true);
        });
        if (!report.Succeeded && report.CurrentCase == "activate")
        {
            var activation = await provider.ActivateProviderAsync();
            Console.WriteLine($"Diagnose(activate): error={activation.IsError} {activation.Message}");
        }
        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType}");
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpClient client, HttpMethod method, string url, string? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body != null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd("NextGenSoftware-OASIS-CapabilityTests/1.0");
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static void RequireTransfer(OASISResultList loaded, Func<CrossChainTransfer, bool> matches, string protocol)
    {
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.IsTrue(loaded.Result!.Any(t => t.Protocol == protocol && matches(t)), $"The provider did not return the native {protocol} transfer.");
    }

    private sealed record OASISResultList(bool IsError, string? Message, IReadOnlyList<CrossChainTransfer>? Result);

    private static async Task<OASISResultList> Load(Task<NextGenSoftware.OASIS.Common.OASISResult<IReadOnlyList<CrossChainTransfer>>> call)
    {
        var result = await call.WaitAsync(TimeSpan.FromSeconds(60));
        return new OASISResultList(result.IsError, result.Message, result.Result);
    }

    private static bool Same(string? a, string? b) => a != null && b != null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    [TestMethod]
    public async Task AxelarReadsARealGmpMessageBySourceTransaction()
    {
        const string host = "api.axelarscan.io";
        var guard = new PublicReadGuard(host, (HttpMethod.Post, "/gmp/searchGMP"));
        using var native = new HttpClient(new PublicReadGuard(host, (HttpMethod.Post, "/gmp/searchGMP")));
        using var provider = new AxelarOASIS("https://" + host, guard);
        await RunAsync(provider, "AxelarOASIS", async () =>
        {
            using var latest = await ReadJsonAsync(native, HttpMethod.Post, $"https://{host}/gmp/searchGMP", "{\"size\":1}");
            var hash = latest.RootElement.GetProperty("data")[0].GetProperty("call").GetProperty("transactionHash").GetString();
            var before = guard.Requests;
            RequireTransfer(await Load(provider.GetTransfersBySourceTxAsync(hash!)), t => Same(t.SourceTxHash, hash), "axelar");
            return new NonStorageConformance.Probe(host, "GmpSearchBySourceTx", guard.Requests - before, true, true);
        });
    }

    [TestMethod]
    public async Task HyperlaneReadsARealMessageBySourceTransaction()
    {
        const string host = "explorer4.hasura.app";
        var guard = new PublicReadGuard(host, (HttpMethod.Post, "/v1/graphql"));
        using var native = new HttpClient(new PublicReadGuard(host, (HttpMethod.Post, "/v1/graphql")));
        var provider = new HyperlaneOASIS($"https://{host}/v1/graphql", guard);
        await RunAsync(provider, "HyperlaneOASIS", async () =>
        {
            var query = JsonSerializer.Serialize(new { query = "{ message_view(limit: 1) { origin_tx_hash } }" });
            using var latest = await ReadJsonAsync(native, HttpMethod.Post, $"https://{host}/v1/graphql", query);
            var bytea = latest.RootElement.GetProperty("data").GetProperty("message_view")[0].GetProperty("origin_tx_hash").GetString()!;
            var hash = "0x" + bytea.TrimStart('\\').TrimStart('x');
            var before = guard.Requests;
            RequireTransfer(await Load(provider.GetTransfersBySourceTxAsync(hash)), t => Same(t.SourceTxHash, hash), "hyperlane");
            return new NonStorageConformance.Probe(host, "MessageViewBySourceTx", guard.Requests - before, true, true);
        });
    }

    [TestMethod]
    public async Task LayerZeroReadsARealMessageBySourceTransaction()
    {
        const string host = "scan.layerzero-api.com";
        var guard = new PublicReadGuard(host, (HttpMethod.Get, "/v1/messages/"));
        using var native = new HttpClient(new PublicReadGuard(host, (HttpMethod.Get, "/v1/messages/latest")));
        var provider = new LayerZeroV2OASIS($"https://{host}/v1", guard);
        await RunAsync(provider, "LayerZeroV2OASIS", async () =>
        {
            using var latest = await ReadJsonAsync(native, HttpMethod.Get, $"https://{host}/v1/messages/latest?limit=1");
            var hash = latest.RootElement.GetProperty("data")[0].GetProperty("source").GetProperty("tx").GetProperty("txHash").GetString();
            var before = guard.Requests;
            RequireTransfer(await Load(provider.GetTransfersBySourceTxAsync(hash!)), t => Same(t.SourceTxHash, hash), "layerzero-v2");
            return new NonStorageConformance.Probe(host, "MessagesBySourceTx", guard.Requests - before, true, true);
        });
    }

    [TestMethod]
    public async Task DeBridgeReadsARealOrderBySourceTransaction()
    {
        const string host = "stats-api.dln.trade";
        var guard = new PublicReadGuard(host, (HttpMethod.Post, "/api/Orders/filteredList"), (HttpMethod.Get, "/api/Transaction/"), (HttpMethod.Get, "/api/Orders/"));
        using var native = new HttpClient(new PublicReadGuard(host, (HttpMethod.Post, "/api/Orders/filteredList")));
        var provider = new deBridgeOASIS("https://" + host, guard);
        await RunAsync(provider, "deBridgeOASIS", async () =>
        {
            using var latest = await ReadJsonAsync(native, HttpMethod.Post, $"https://{host}/api/Orders/filteredList", "{\"skip\":0,\"take\":1}");
            var order = latest.RootElement.GetProperty("orders")[0];
            var hash = order.GetProperty("createEventTransactionHash").GetProperty("stringValue").GetString();
            var id = order.GetProperty("orderId").GetProperty("stringValue").GetString();
            var before = guard.Requests;
            RequireTransfer(await Load(provider.GetTransfersBySourceTxAsync(hash!)), t => Same(t.Id, id), "debridge");
            return new NonStorageConformance.Probe(host, "OrdersBySourceTx", guard.Requests - before, true, true);
        });
    }

    [TestMethod]
    public async Task ChainflipReadsTheFirstSwapById()
    {
        const string host = "chainflip-swap.chainflip.io";
        var guard = new PublicReadGuard(host, (HttpMethod.Get, "/healthcheck"), (HttpMethod.Get, "/v2/swaps/"));
        using var native = new HttpClient(new PublicReadGuard(host, (HttpMethod.Get, "/v2/swaps/")));
        var provider = new ChainflipOASIS("https://" + host, guard);
        await RunAsync(provider, "ChainflipOASIS", async () =>
        {
            // Swap 1 is the network's first swap: settled history, so the comparison is stable.
            using var expected = await ReadJsonAsync(native, HttpMethod.Get, $"https://{host}/v2/swaps/1");
            var destination = expected.RootElement.GetProperty("destAddress").GetString();
            var before = guard.Requests;
            RequireTransfer(await Load(provider.GetTransfersBySourceTxAsync("1")), t => t.Id == "1" && Same(t.Recipient, destination), "chainflip");
            return new NonStorageConformance.Probe(host, "SwapById", guard.Requests - before, true, true);
        });
    }

    [TestMethod]
    public async Task ChainlinkFunctionsReadsSubscriptionOneFromTheRouter()
    {
        const string host = "ethereum-rpc.publicnode.com";
        var guard = new PublicReadGuard(host, (HttpMethod.Post, "/"));
        using var native = new HttpClient(new PublicReadGuard(host, (HttpMethod.Post, "/")));
        var provider = new ChainlinkFunctionsOASIS($"https://{host}", ChainlinkFunctionsOASIS.MainnetRouter, guard);
        await RunAsync(provider, "ChainlinkFunctionsOASIS", async () =>
        {
            // getSubscription(1) read directly; subscription 1 is the router's first subscription.
            var call = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, method = "eth_call",
                @params = new object[] { new { to = ChainlinkFunctionsOASIS.MainnetRouter, data = "0xa47c7696" + 1.ToString("x64") }, "latest" } });
            using var raw = await ReadJsonAsync(native, HttpMethod.Post, $"https://{host}/", call);
            var hex = raw.RootElement.GetProperty("result").GetString()![2..];
            var owner = "0x" + hex.Substring(2 * 64 + 24, 40); // tuple offset word, balance word, then the owner word
            var before = guard.Requests;
            var loaded = await provider.LoadAvatarByProviderKeyAsync("1").WaitAsync(TimeSpan.FromSeconds(60));
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.IsTrue(Same(loaded.Result.MetaData["owner"]?.ToString(), owner), "The provider's subscription owner differs from the router's.");
            return new NonStorageConformance.Probe(host, "RouterGetSubscription", guard.Requests - before, true, true);
        });
    }
}
