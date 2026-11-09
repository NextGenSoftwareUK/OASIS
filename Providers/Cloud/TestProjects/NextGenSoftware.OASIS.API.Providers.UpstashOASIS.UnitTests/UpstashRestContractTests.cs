using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.UpstashOASIS.UnitTests;

[TestClass]
public class UpstashRestContractTests
{
    private const string Key = "oasis/private/v1/test";
    private sealed class Wire : HttpMessageHandler
    {
        public Func<JsonElement, HttpResponseMessage> Reply = _ => Response("{\"result\":null}");
        public List<string[]> Commands = new();
        public List<string?> Tokens = new();
        public bool Legacy;
        public bool Disposed;
        public Func<JsonElement, HttpResponseMessage>? ScanReply;
        public Func<JsonElement, Task<HttpResponseMessage>>? AsyncReply;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.AreEqual("Bearer", request.Headers.Authorization?.Scheme);
            Assert.AreEqual("synthetic-token", request.Headers.Authorization?.Parameter);
            Assert.AreEqual("https://synthetic.upstash.io/", request.RequestUri!.AbsoluteUri);
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var command = json.RootElement;
            Commands.Add(command.EnumerateArray().Select(value => value.ToString()).ToArray());
            Tokens.Add(request.Headers.TryGetValues("upstash-sync-token", out var values) ? values.Single() : null);
            if (command[0].GetString() == "SCAN")
                return ScanReply?.Invoke(command) ?? Response(Legacy ? "{\"result\":[\"0\",[\"avatar:legacy\"]]}" : "{\"result\":[\"0\",[]]}");
            if (AsyncReply != null) return await AsyncReply(command);
            return Reply(command);
        }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }
    private static HttpResponseMessage Response(string body, bool token = true)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        if (token) response.Headers.Add("upstash-sync-token", "native-checkpoint");
        return response;
    }
    private static UpstashRestBackend Backend(Wire wire, out HttpClient http)
    { http = new HttpClient(wire); return new UpstashRestBackend("https://synthetic.upstash.io", "synthetic-token", http); }

    [DataTestMethod]
    [DataRow("{}")] [DataRow("{\"result\":5}")] [DataRow("{\"result\":null,\"result\":\"OK\"}")]
    public async Task MalformedReadsCannotBecomeMissingRecords(string body)
    {
        var wire = new Wire { Reply = _ => Response(body) }; var backend = Backend(wire, out var http); using (http)
            await Assert.ThrowsExceptionAsync<System.IO.InvalidDataException>(() => backend.GetAsync(Key));
    }
    [TestMethod]
    public async Task ExplicitNullIsAbsenceAndStringObservationUsesSharedCodec()
    {
        var wire = new Wire(); var backend = Backend(wire, out var http); using (http)
        {
            Assert.IsNull(await backend.ReadObservedAsync(Key));
            wire.Reply = _ => Response("{\"result\":\"ðŸŒ³\"}");
            var observed = await backend.ReadObservedAsync(Key);
            Assert.AreEqual("ðŸŒ³", observed!.Value);
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("ðŸŒ³"), RedisStateComparison.Expected(Key, observed.Revision));
        }
    }
    [DataTestMethod]
    [DataRow("{}")] [DataRow("{\"result\":false}")] [DataRow("{\"result\":\"wrong\"}")]
    public async Task MissingMutationAcknowledgementsRequireReconciliation(string body)
    {
        var wire = new Wire { Reply = _ => Response(body) }; var backend = Backend(wire, out var http); using (http)
        {
            var error = await Assert.ThrowsExceptionAsync<UpstashMutationReconciliationException>(() => backend.PutAsync(Key, "v"));
            Assert.AreEqual(Key, error.Key);
            Assert.AreEqual(1, wire.Commands.Count(command => command[0] == "SET"));
        }
    }
    [TestMethod]
    public async Task MutationWithoutSyncTokenCannotClaimCausalSuccess()
    {
        var wire = new Wire { Reply = _ => Response("{\"result\":\"OK\"}", false) }; var backend = Backend(wire, out var http); using (http)
            await Assert.ThrowsExceptionAsync<UpstashMutationReconciliationException>(() => backend.PutAsync(Key, "v"));
    }
    [TestMethod]
    public async Task NativeCreateConflictIsNotAnUnknownMutation()
    {
        var wire = new Wire(); var backend = Backend(wire, out var http); using (http)
            await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(() => backend.CreateOnlyAsync(Key, "v"));
    }
    [TestMethod]
    public async Task ConditionalOperationsUseNativeSharedScriptsAndReportExactKey()
    {
        var wire = new Wire { Reply = _ => Response("{\"result\":0}") }; var backend = Backend(wire, out var http); using (http)
        {
            var revision = RedisStateComparison.Observe(Key, Encoding.UTF8.GetBytes("old"));
            await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(() => backend.ReplaceUnchangedAsync(Key, "new", revision));
            CollectionAssert.AreEqual(new[] { "EVAL", RedisStateComparison.ReplaceScript, "1", Key, "old", "new" }, wire.Commands.Last());
            wire.Reply = _ => Response("{\"result\":\"1\"}");
            var error = await Assert.ThrowsExceptionAsync<UpstashMutationReconciliationException>(() => backend.DeleteUnchangedAsync(Key, revision));
            Assert.AreEqual(Key, error.Key);
        }
    }
    [TestMethod]
    public async Task SyncTokenIsForwardedWithoutChangingCallerDefaultHeaders()
    {
        var wire = new Wire { Reply = _ => Response("{\"result\":\"OK\"}") }; var backend = Backend(wire, out var http); using (http)
        {
            await backend.PutAsync(Key, "v");
            wire.Reply = _ => Response("{\"result\":\"v\"}"); await backend.GetAsync(Key);
            Assert.AreEqual("native-checkpoint", wire.Tokens.Last()); Assert.IsNull(http.DefaultRequestHeaders.Authorization);
        }
    }
    [TestMethod]
    public async Task LegacyLayoutRefusesDirectMutationAndCachesFailure()
    {
        var wire = new Wire { Legacy = true }; var backend = Backend(wire, out var http); using (http)
        {
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.PutAsync(Key, "v"));
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.PutAsync(Key, "v"));
            Assert.AreEqual(1, wire.Commands.Count); Assert.AreEqual("SCAN", wire.Commands.Single()[0]);
        }
    }
    [TestMethod]
    public async Task WrongKeyObservationIsRejectedBeforeHttp()
    {
        var wire = new Wire(); var backend = Backend(wire, out var http); using (http)
        {
            var revision = RedisStateComparison.Observe(Key + "other", new byte[] { 1 });
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => backend.DeleteUnchangedAsync(Key, revision));
            Assert.AreEqual(0, wire.Commands.Count);
        }
    }
    [TestMethod]
    public async Task HttpFailuresAndNativeErrorsNeverBecomeSuccessOrLeakResponseBody()
    {
        var wire = new Wire { Reply = _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("sensitive") } };
        var backend = Backend(wire, out var http); using (http)
        {
            var unknown = await Assert.ThrowsExceptionAsync<UpstashMutationReconciliationException>(() => backend.PutAsync(Key, "v"));
            Assert.IsFalse(unknown.ToString().Contains("sensitive"));
            wire.Reply = _ => Response("{\"error\":\"sensitive\"}");
            var rejected = await Assert.ThrowsExceptionAsync<UpstashCommandRejectedException>(() => backend.GetAsync(Key));
            Assert.IsFalse(rejected.ToString().Contains("sensitive"));
        }
    }
    [TestMethod]
    public void DisposingProviderPreservesCallerOwnedClient()
    {
        var wire = new Wire(); using var http = new HttpClient(wire);
        using (var provider = new UpstashOASIS("https://synthetic.upstash.io", "synthetic-token", http, () => new byte[32])) { }
        Assert.IsFalse(wire.Disposed);
    }
    [TestMethod]
    public async Task PaginationFollowsNativeCursorAndDeduplicatesScanRepeats()
    {
        var wire = new Wire(); var backend = Backend(wire, out var http); using (http)
        {
            await backend.GetAsync(Key); // Finish legacy admission before installing listing pages.
            var pages = new Queue<string>(new[] {
                "{\"result\":[\"7\",[\"" + Key + "b\",\"" + Key + "a\"]]}",
                "{\"result\":[\"0\",[\"" + Key + "a\"]]}" });
            wire.ScanReply = _ => Response(pages.Dequeue());
            CollectionAssert.AreEqual(new[] { Key + "a", Key + "b" }, (await backend.ListKeysAsync(Key)).ToArray());
            Assert.AreEqual("7", wire.Commands.Last()[1]); Assert.AreEqual(0, pages.Count);
        }
    }
    [DataTestMethod]
    [DataRow("{\"result\":[\"00\",[]]}")]
    [DataRow("{\"result\":[\"0\",[\"foreign\"]]}")]
    [DataRow("{\"result\":[0,[]]}")]
    public async Task MalformedOrOutOfScopeScanPagesCannotBecomeEmptySuccess(string body)
    {
        var wire = new Wire(); var backend = Backend(wire, out var http); using (http)
        {
            await backend.GetAsync(Key); wire.ScanReply = _ => Response(body);
            await Assert.ThrowsExceptionAsync<System.IO.InvalidDataException>(() => backend.ListKeysAsync(Key));
        }
    }
    [TestMethod]
    public async Task RepeatedCursorCannotLoopForever()
    {
        var wire = new Wire(); var backend = Backend(wire, out var http); using (http)
        {
            await backend.GetAsync(Key); wire.ScanReply = _ => Response("{\"result\":[\"7\",[]]}");
            await Assert.ThrowsExceptionAsync<System.IO.InvalidDataException>(() => backend.ListKeysAsync(Key));
            Assert.AreEqual(2, wire.Commands.Count(command => command[0] == "SCAN" && command[3] == Key + "*"));
        }
    }
    [TestMethod]
    public async Task ConcurrentRequestsCannotRegressTheCausalSession()
    {
        var wire = new Wire(); var backend = Backend(wire, out var http); using (http)
        {
            await backend.GetAsync(Key);
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            wire.AsyncReply = async _ => { entered.TrySetResult(); await release.Task; var response = Response("{\"result\":null}", false); response.Headers.Add("upstash-sync-token", "after-first"); return response; };
            var first = backend.GetAsync(Key); await entered.Task;
            var second = backend.GetAsync(Key);
            Assert.AreEqual(2, wire.Commands.Count(command => command[0] == "GET"), "Second concurrent request must wait for the first response token.");
            release.SetResult(); await Task.WhenAll(first, second);
            Assert.AreEqual("after-first", wire.Tokens.Last());
        }
    }
    [TestMethod]
    public async Task CancellationBeforeDispatchDoesNotSendOrClaimAnUnknownMutation()
    {
        var wire = new Wire(); var backend = Backend(wire, out var http); using (http)
        {
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => backend.PutAsync(Key, "v", cancellation.Token));
            Assert.AreEqual(0, wire.Commands.Count);
        }
    }
    [TestMethod]
    public async Task MissingStorageKeyRefusesProviderAccessBeforeTransport()
    {
        var wire = new Wire(); using var http = new HttpClient(wire);
        using var provider = new UpstashOASIS("https://synthetic.upstash.io", "synthetic-token", http, () => throw new InvalidOperationException("missing persisted key"));
        var result = await provider.LoadAvatarAsync(Guid.NewGuid());
        Assert.IsTrue(result.IsError); Assert.AreEqual(0, wire.Commands.Count);
    }
}
