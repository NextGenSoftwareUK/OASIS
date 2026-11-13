using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.CloudflareOASIS.UnitTests;

[TestClass, DoNotParallelize]
public class CloudflareRestContractTests
{
    private const string Key = "oasis/private/v1/test";
    private sealed class Wire : HttpMessageHandler
    {
        public readonly Dictionary<string, string> Values = new(StringComparer.Ordinal);
        public readonly List<(string Method, Uri Url)> Requests = new();
        public Func<HttpRequestMessage, HttpResponseMessage?>? Override;
        public bool Disposed;
        public static HttpResponseMessage Response(string body, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.AreEqual("https", request.RequestUri!.Scheme); Assert.AreEqual("api.cloudflare.com", request.RequestUri.Host);
            Assert.AreEqual("synthetic-token", request.Headers.Authorization?.Parameter);
            Assert.AreEqual("Bearer", request.Headers.Authorization?.Scheme);
            Requests.Add((request.Method.Method, request.RequestUri));
            var replacement = Override?.Invoke(request); if (replacement != null) return replacement;
            if (request.RequestUri.AbsolutePath.EndsWith("/keys", StringComparison.Ordinal))
            {
                var query = request.RequestUri.Query.TrimStart('?').Split('&').Select(part => part.Split('=', 2)).ToDictionary(parts => parts[0], parts => Uri.UnescapeDataString(parts[1]));
                Assert.AreEqual("1000", query["limit"]);
                var keys = Values.Keys.Where(key => key.StartsWith(query["prefix"], StringComparison.Ordinal)).OrderBy(key => key, StringComparer.Ordinal).Select(key => new { name = key }).ToArray();
                return Response(JsonSerializer.Serialize(new { success = true, errors = Array.Empty<object>(), result = keys, result_info = new { count = keys.Length, cursor = "" } }));
            }
            const string marker = "/values/";
            var offset = request.RequestUri.AbsolutePath.IndexOf(marker, StringComparison.Ordinal);
            Assert.IsTrue(offset >= 0);
            var keyName = Uri.UnescapeDataString(request.RequestUri.AbsolutePath.Substring(offset + marker.Length));
            if (request.Method == HttpMethod.Get)
                return Values.TryGetValue(keyName, out var value) ? Response(value) : Response("not found", HttpStatusCode.NotFound);
            if (request.Method == HttpMethod.Put)
            {
                Assert.AreEqual("application/octet-stream", request.Content!.Headers.ContentType!.MediaType);
                Values[keyName] = await request.Content.ReadAsStringAsync(token);
            }
            else { Assert.AreEqual(HttpMethod.Delete, request.Method); Values.Remove(keyName); }
            return Response("{\"success\":true,\"errors\":[],\"result\":null}");
        }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }
    private static CloudflareRestBackend Backend(HttpClient http) => new("syntheticaccount", "syntheticnamespace", "synthetic-token", http);
    private static CloudflareOASIS Provider(HttpClient http) => new("syntheticaccount", "syntheticnamespace", "synthetic-token", http, () => new byte[32]);
    private static string Work(string mode)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        return Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent", "CloudflareOASIS", mode);
    }

    [TestMethod]
    public Task SharedAsynchronousSuiteUsesControlledHttpOnly() => RunSharedSuiteAsync(false);
    [TestMethod]
    public Task SharedSynchronousSuiteUsesControlledHttpOnly() => RunSharedSuiteAsync(true);
    private async Task RunSharedSuiteAsync(bool synchronous)
    {
        var wire = new Wire(); using var http = new HttpClient(wire); using var provider = Provider(http);
        var directory = Work(synchronous ? "sync" : "async"); Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "report.json");
        Assert.IsFalse(File.Exists(path) || File.Exists(path + ".next"), "Preserve original outcomes before rerunning controlled suite.");
        using var lease = new FileStream(Path.Combine(directory, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var checkpoint = new AtomicConformanceCheckpoint<CoreStorageConformance.Report>(path);
        var report = await CoreStorageConformance.RunAsync(provider, "CloudflareOASIS", "controlled", synchronous, checkpoint.WriteAsync,
            (id, sync) => sync ? Task.FromResult(provider.DeleteHolonSoft(id, false)) : provider.DeleteHolonSoftAsync(id, false), "soft");
        Assert.IsTrue(report.Succeeded, report.CurrentCase + ": " + report.FailureType);
        Assert.AreEqual(0, wire.Values.Count);
    }
    [TestMethod]
    public async Task PointAbsenceRequiresPriorSuccessfulNamespaceAdmission()
    {
        var wire = new Wire(); using var http = new HttpClient(wire); var backend = Backend(http);
        Assert.IsNull(await backend.GetAsync(Key));
        Assert.AreEqual(4, wire.Requests.Count(request => request.Url.AbsolutePath.EndsWith("/keys")));
        Assert.AreEqual("GET", wire.Requests.Last().Method);
        Assert.IsFalse((object)backend is IConditionalKeyValueBackend);
        Assert.IsNull(http.DefaultRequestHeaders.Authorization);
    }
    [DataTestMethod]
    [DataRow("{}")] [DataRow("{\"success\":true,\"errors\":[{}]}")]
    [DataRow("{\"success\":true,\"success\":true,\"errors\":[]}")]
    public async Task MissingOrContradictoryMutationAcknowledgementIsUnknown(string body)
    {
        var wire = new Wire(); using var http = new HttpClient(wire); var backend = Backend(http); await backend.GetAsync(Key);
        wire.Override = _ => Wire.Response(body);
        var error = await Assert.ThrowsExceptionAsync<CloudflareMutationReconciliationException>(() => backend.PutAsync(Key, "v"));
        Assert.AreEqual(Key, error.Key); Assert.AreEqual(1, wire.Requests.Count(request => request.Method == "PUT"));
    }
    [DataTestMethod, DataRow(HttpStatusCode.Unauthorized), DataRow(HttpStatusCode.Forbidden), DataRow(HttpStatusCode.NotFound)]
    public async Task NamespaceAccessFailureCannotBecomeRecordAbsence(HttpStatusCode status)
    {
        var wire = new Wire { Override = _ => Wire.Response("sensitive remote body", status) }; using var http = new HttpClient(wire);
        var error = await Assert.ThrowsExceptionAsync<CloudflareCommandRejectedException>(() => Backend(http).GetAsync(Key));
        Assert.IsFalse(error.ToString().Contains("sensitive")); Assert.AreEqual(1, wire.Requests.Count);
    }
    [TestMethod]
    public async Task ExplicitMutationRejectionIsNotAnUnknownOutcomeAndDoesNotLeakBody()
    {
        var wire = new Wire(); using var http = new HttpClient(wire); var backend = Backend(http); await backend.GetAsync(Key);
        wire.Override = _ => Wire.Response("{\"success\":false,\"errors\":[{\"message\":\"sensitive\"}]}");
        var error = await Assert.ThrowsExceptionAsync<CloudflareCommandRejectedException>(() => backend.DeleteAsync(Key));
        Assert.IsFalse(error.ToString().Contains("sensitive"));
    }
    [TestMethod]
    public async Task ServerFailureRequiresReconciliationWithoutRetry()
    {
        var wire = new Wire(); using var http = new HttpClient(wire); var backend = Backend(http); await backend.GetAsync(Key);
        wire.Override = _ => Wire.Response("sensitive", HttpStatusCode.ServiceUnavailable);
        await Assert.ThrowsExceptionAsync<CloudflareMutationReconciliationException>(() => backend.DeleteAsync(Key));
        Assert.AreEqual(1, wire.Requests.Count(request => request.Method == "DELETE"));
    }
    [TestMethod]
    public async Task EmptyPageWithCursorDoesNotEndEnumeration()
    {
        var wire = new Wire(); using var http = new HttpClient(wire); var backend = Backend(http); await backend.GetAsync(Key);
        var pages = new Queue<string>(new[] { "{\"success\":true,\"errors\":[],\"result\":[],\"result_info\":{\"cursor\":\"next+/=\"}}",
            "{\"success\":true,\"errors\":[],\"result\":[{\"name\":\"" + Key + "\"}],\"result_info\":{\"cursor\":\"\"}}" });
        wire.Override = _ => Wire.Response(pages.Dequeue());
        CollectionAssert.AreEqual(new[] { Key }, (await backend.ListKeysAsync(Key)).ToArray());
        Assert.IsTrue(wire.Requests.Last().Url.Query.Contains("cursor=next%2B%2F%3D"));
        Assert.AreEqual(0, pages.Count);
    }
    [DataTestMethod]
    [DataRow("{\"success\":true,\"errors\":[],\"result\":null}")]
    [DataRow("{\"success\":true,\"errors\":[],\"result\":[{\"name\":\"outside\"}]}")]
    [DataRow("{\"success\":true,\"errors\":[],\"result\":[],\"result_info\":{\"count\":1}}")]
    public async Task InvalidListingCannotReturnPartialSuccess(string body)
    {
        var wire = new Wire(); using var http = new HttpClient(wire); var backend = Backend(http); await backend.GetAsync(Key);
        wire.Override = _ => Wire.Response(body);
        await Assert.ThrowsExceptionAsync<System.IO.InvalidDataException>(() => backend.ListKeysAsync(Key));
    }
    [TestMethod]
    public async Task LegacyLayoutIsRefusedBeforeDirectSave()
    {
        var wire = new Wire(); wire.Values.Add("avatar:old", "preserved"); using var http = new HttpClient(wire);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Backend(http).PutAsync(Key, "v"));
        Assert.AreEqual("preserved", wire.Values["avatar:old"]); Assert.AreEqual(0, wire.Requests.Count(request => request.Method == "PUT"));
    }
    [TestMethod]
    public async Task SharedPrivatePolicyHidesIdentityAndReleasesOnlyItsOwnRecord()
    {
        var wire = new Wire(); using var http = new HttpClient(wire); using var provider = Provider(http);
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "private-person", Email = "private-person@example.invalid" };
        var saved = await provider.SaveAvatarAsync(avatar); Assert.IsFalse(saved.IsError);
        Assert.IsTrue(wire.Values.Count > 0);
        Assert.IsTrue(wire.Values.Keys.All(key => key.StartsWith("oasis/private/v1/", StringComparison.Ordinal)));
        Assert.IsFalse(wire.Values.Values.Any(value => value.Contains("private-person", StringComparison.Ordinal)));
        Assert.IsFalse((await provider.DeleteAvatarAsync(avatar.Id, false)).IsError);
        Assert.AreEqual(0, wire.Values.Count);
    }
    [TestMethod]
    public void CallerOwnedHttpClientIsNotDisposedOrReconfigured()
    {
        var wire = new Wire(); using var http = new HttpClient(wire);
        using (var provider = Provider(http)) { }
        Assert.IsFalse(wire.Disposed); Assert.IsNull(http.DefaultRequestHeaders.Authorization);
    }
    [TestMethod]
    public async Task LaterPageFailureDoesNotReturnTheEarlierPageAsComplete()
    {
        var wire = new Wire(); using var http = new HttpClient(wire); var backend = Backend(http); await backend.GetAsync(Key);
        var pages = new Queue<HttpResponseMessage>(new[] {
            Wire.Response("{\"success\":true,\"errors\":[],\"result\":[{\"name\":\"" + Key + "\"}],\"result_info\":{\"cursor\":\"next\"}}"),
            Wire.Response("unavailable", HttpStatusCode.ServiceUnavailable) });
        wire.Override = _ => pages.Dequeue();
        var failure = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => backend.ListKeysAsync(Key));
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, failure.StatusCode);
        Assert.AreEqual(0, pages.Count);
    }
    [TestMethod]
    public async Task RepeatedCursorIsAnErrorRatherThanAnInfiniteLoop()
    {
        var wire = new Wire(); using var http = new HttpClient(wire); var backend = Backend(http); await backend.GetAsync(Key);
        var before = wire.Requests.Count;
        wire.Override = _ => Wire.Response("{\"success\":true,\"errors\":[],\"result\":[],\"result_info\":{\"cursor\":\"again\"}}");
        await Assert.ThrowsExceptionAsync<System.IO.InvalidDataException>(() => backend.ListKeysAsync(Key));
        Assert.AreEqual(2, wire.Requests.Count - before);
    }
    [TestMethod]
    public async Task MissingPrivateKeyFailsBeforeNamespaceOrRecordRequests()
    {
        var wire = new Wire(); using var http = new HttpClient(wire);
        using var provider = new CloudflareOASIS("syntheticaccount", "syntheticnamespace", "synthetic-token", http, () => throw new InvalidOperationException("persisted key missing"));
        Assert.IsTrue((await provider.LoadAvatarAsync(Guid.NewGuid())).IsError);
        Assert.AreEqual(0, wire.Requests.Count);
    }
    [TestMethod]
    public async Task ForeignPhysicalKeyCannotBeDispatched()
    {
        var wire = new Wire(); using var http = new HttpClient(wire);
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => Backend(http).PutAsync("avatar:unsafe", "v"));
        Assert.AreEqual(0, wire.Requests.Count);
    }
}
