using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.AppwriteOASIS.UnitTests;

[TestClass]
public class AppwriteDocumentBackendTests
{
    private sealed class Reply(Func<HttpRequestMessage, (HttpStatusCode Status, string Body)> response) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var reply = response(request);
            return Task.FromResult(new HttpResponseMessage(reply.Status) { Content = new StringContent(reply.Body, Encoding.UTF8, "application/json") });
        }
    }

    private static AppwriteDocumentBackend Backend(Reply reply)
        => new("https://synthetic.invalid/v1", "project", "secret", "db", "coll", new HttpClient(reply));

    private static string Doc(string key, string value) => JsonSerializer.Serialize(new Dictionary<string, string>
        { ["$id"] = AppwriteDocumentBackend.DocumentId(key), ["key"] = key, ["value"] = value });

    [TestMethod]
    public void DocumentIdsAreValidAppwriteIdsAndDistinct()
    {
        var a = AppwriteDocumentBackend.DocumentId("oasis/holon/a");
        Assert.AreEqual(36, a.Length);
        StringAssert.Matches(a, new System.Text.RegularExpressions.Regex("^k[0-9a-f]{35}$"));
        Assert.AreNotEqual(a, AppwriteDocumentBackend.DocumentId("oasis/holon/A"));
        Assert.ThrowsException<ArgumentException>(() => AppwriteDocumentBackend.DocumentId(new string('k', 256)));
    }

    [TestMethod]
    public async Task ReadSendsProjectAndKeyHeadersAndVerifiesIdentity()
    {
        var reply = new Reply(_ => (HttpStatusCode.OK, Doc("oasis/holon/a", "v")));
        Assert.AreEqual("v", await Backend(reply).GetAsync("oasis/holon/a"));
        var request = reply.Requests.Single();
        Assert.AreEqual("project", request.Headers.GetValues("X-Appwrite-Project").Single());
        Assert.AreEqual("secret", request.Headers.GetValues("X-Appwrite-Key").Single());
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => Backend(reply).GetAsync("oasis/holon/b"));
    }

    [TestMethod]
    public async Task OnlyMissingDocumentIsAbsence()
    {
        var missing = new Reply(_ => (HttpStatusCode.NotFound, "{\"type\":\"document_not_found\"}"));
        Assert.IsNull(await Backend(missing).GetAsync("oasis/holon/a"));
        await Backend(missing).DeleteAsync("oasis/holon/a");

        var noCollection = new Reply(_ => (HttpStatusCode.NotFound, "{\"type\":\"collection_not_found\"}"));
        var error = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => Backend(noCollection).GetAsync("oasis/holon/a"));
        Assert.AreEqual(HttpStatusCode.NotFound, error.StatusCode);
        await Assert.ThrowsExceptionAsync<HttpRequestException>(() => Backend(noCollection).DeleteAsync("oasis/holon/a"));
    }

    [DataTestMethod, DataRow(401), DataRow(403), DataRow(429), DataRow(500)]
    public async Task FailuresPreserveTransportStatus(int status)
    {
        var reply = new Reply(_ => ((HttpStatusCode)status, "{}"));
        Assert.AreEqual((HttpStatusCode)status, (await Assert.ThrowsExceptionAsync<HttpRequestException>(() => Backend(reply).GetAsync("oasis/a"))).StatusCode);
        Assert.AreEqual((HttpStatusCode)status, (await Assert.ThrowsExceptionAsync<HttpRequestException>(() => Backend(reply).ListKeysAsync("oasis/"))).StatusCode);
        Assert.AreEqual((HttpStatusCode)status, (await Assert.ThrowsExceptionAsync<HttpRequestException>(() => Backend(reply).PutAsync("oasis/a", "v"))).StatusCode);
    }

    [TestMethod]
    public async Task PutIsNativeUpsertAndVerifiesTheAcknowledgement()
    {
        var reply = new Reply(_ => (HttpStatusCode.OK, Doc("oasis/a", "v")));
        await Backend(reply).PutAsync("oasis/a", "v");
        Assert.AreEqual(HttpMethod.Put, reply.Requests.Single().Method);
        StringAssert.EndsWith(reply.Requests.Single().RequestUri!.AbsolutePath, "/databases/db/collections/coll/documents/" + AppwriteDocumentBackend.DocumentId("oasis/a"));
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => Backend(reply).PutAsync("oasis/a", "other"));
    }

    [TestMethod]
    public async Task ListingFollowsCursorPagesAndKeepsOnlyCaseSensitivePrefixMatches()
    {
        var page1 = Enumerable.Range(0, 100).Select(i => $"oasis/holon/{i:D3}").ToList();
        var reply = new Reply(request =>
        {
            var query = Uri.UnescapeDataString(request.RequestUri!.Query);
            var keys = query.Contains("cursorAfter") ? new List<string> { "oasis/holon/x", "OASIS/HOLON/y" } : page1;
            var body = JsonSerializer.Serialize(new { total = 102, documents = keys.Select(k => new Dictionary<string, string> { ["$id"] = AppwriteDocumentBackend.DocumentId(k), ["key"] = k }) });
            return (HttpStatusCode.OK, body);
        });
        var listed = await Backend(reply).ListKeysAsync("oasis/holon/");
        Assert.AreEqual(101, listed.Count);
        Assert.AreEqual("oasis/holon/x", listed[^1]);
        Assert.AreEqual(2, reply.Requests.Count);
        StringAssert.Contains(Uri.UnescapeDataString(reply.Requests[1].RequestUri!.Query), AppwriteDocumentBackend.DocumentId(page1[^1]));
    }

    [DataTestMethod, DataRow("{}"), DataRow("{\"documents\":{}}"), DataRow("{\"documents\":[{\"key\":\"oasis/a\"}]}")]
    public async Task MalformedListingIsAnError(string body)
    {
        var reply = new Reply(_ => (HttpStatusCode.OK, body));
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => Backend(reply).ListKeysAsync("oasis/"));
    }

    [TestMethod]
    public async Task VerifyRequiresAvailableKeyAndValueAttributes()
    {
        var ready = new Reply(_ => (HttpStatusCode.OK, "{\"attributes\":[{\"key\":\"key\",\"type\":\"string\",\"status\":\"available\"},{\"key\":\"value\",\"type\":\"string\",\"status\":\"available\"}]}"));
        await Backend(ready).VerifyAsync();
        var processing = new Reply(_ => (HttpStatusCode.OK, "{\"attributes\":[{\"key\":\"key\",\"type\":\"string\",\"status\":\"available\"},{\"key\":\"value\",\"type\":\"string\",\"status\":\"processing\"}]}"));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Backend(processing).VerifyAsync());
    }
}
