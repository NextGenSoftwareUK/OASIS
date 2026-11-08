using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.UrbitOASIS.UnitTests;

[TestClass]
public class UrbitContactsV1ClientTests
{
    private const string Profile = "{\"nickname\":{\"type\":\"text\",\"value\":\"Native contact\"},\"bio\":{\"type\":\"text\",\"value\":\"Teaching profile\"},\"status\":{\"type\":\"text\",\"value\":\"Available\"},\"private-field\":{\"type\":\"text\",\"value\":\"Never projected\"}}";
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    {
        public List<Uri> Calls { get; } = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.IsNull(request.Content);
            Calls.Add(request.RequestUri!);
            return Task.FromResult(reply(request));
        }
    }
    private static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [TestMethod]
    public async Task CurrentTaggedProfileUsesExactPointReadAndProjectsOnlyReviewedFields()
    {
        var handler = new Handler(_ => Reply(Profile));
        using var http = new HttpClient(handler);
        using var client = new UrbitContactsV1Client(new Uri("https://teaching.invalid/"), http);
        var result = await client.ReadContactAsync("~sampel-palnet");
        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreEqual("~sampel-palnet", result.Result.RequestedShip);
        Assert.AreEqual("Native contact", result.Result.Nickname);
        Assert.AreEqual("Teaching profile", result.Result.Bio);
        Assert.AreEqual("Available", result.Result.Status);
        Assert.AreEqual(1, handler.Calls.Count);
        Assert.AreEqual("https://teaching.invalid/~/scry/contacts/v1/contact/~sampel-palnet.json", handler.Calls[0].AbsoluteUri);
        Assert.AreEqual(1, result.LoadedCount);
    }

    [DataTestMethod]
    [DataRow(401)]
    [DataRow(403)]
    [DataRow(404)]
    [DataRow(500)]
    public async Task HttpFailureNeverBecomesAnAbsentProfileOrLegacyRead(int status)
    {
        var handler = new Handler(_ => Reply("denied", (HttpStatusCode)status));
        using var http = new HttpClient(handler);
        using var client = new UrbitContactsV1Client(new Uri("https://teaching.invalid/"), http);
        var result = await client.ReadContactAsync("~zod");
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        Assert.IsInstanceOfType(result.Exception, typeof(HttpRequestException));
        Assert.AreEqual((HttpStatusCode)status, ((HttpRequestException)result.Exception!).StatusCode);
        Assert.AreEqual(1, handler.Calls.Count);
        Assert.IsFalse(handler.Calls.Any(uri => uri.AbsolutePath.Contains("contact-store")));
    }

    [DataTestMethod]
    [DataRow("bare-text")]
    [DataRow("duplicate-root")]
    [DataRow("duplicate-value")]
    [DataRow("wrong-tag")]
    [DataRow("invalid-json")]
    public async Task MalformedTaggedProfileDoesNotPublishPartialFields(string fault)
    {
        var body = fault switch {
            "bare-text" => "{\"nickname\":\"Old untagged format\"}",
            "duplicate-root" => "{\"nickname\":{},\"nickname\":{}}",
            "duplicate-value" => "{\"nickname\":{\"type\":\"text\",\"value\":\"a\",\"value\":\"b\"}}",
            "wrong-tag" => "{\"nickname\":{\"type\":\"numb\",\"value\":7}}",
            _ => "not-json"
        };
        var handler = new Handler(_ => Reply(body));
        using var http = new HttpClient(handler);
        using var client = new UrbitContactsV1Client(new Uri("https://teaching.invalid/"), http);
        var result = await client.ReadContactAsync("~zod");
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        Assert.AreEqual(1, handler.Calls.Count);
    }

    [DataTestMethod]
    [DataRow("zod")]
    [DataRow("~../x")]
    [DataRow("~ZOD")]
    [DataRow("~zod?x=1")]
    [DataRow("~zod\n")]
    public async Task UnsafeSelectorsRejectBeforeTransport(string ship)
    {
        var handler = new Handler(_ => throw new AssertFailedException("No read expected."));
        using var http = new HttpClient(handler);
        using var client = new UrbitContactsV1Client(new Uri("https://teaching.invalid/"), http);
        Assert.AreEqual("URBIT_CONTACT_INVALID_INPUT", (await client.ReadContactAsync(ship)).ErrorCode);
        Assert.AreEqual(0, handler.Calls.Count);
    }

    [DataTestMethod]
    [DataRow("http://example.org/")]
    [DataRow("https://owner:inert@node.invalid/")]
    [DataRow("https://node.invalid/prefix")]
    [DataRow("https://node.invalid/?x=y")]
    public void NonOriginOrUnauthenticatedRemoteHttpRejects(string address)
    {
        using var http = new HttpClient(new Handler(_ => Reply(Profile)));
        Assert.ThrowsException<ArgumentException>(() => new UrbitContactsV1Client(new Uri(address), http));
    }

    [TestMethod]
    public async Task PreCancellationSendsNoNativeRequest()
    {
        var handler = new Handler(_ => throw new AssertFailedException("No read expected."));
        using var http = new HttpClient(handler);
        using var client = new UrbitContactsV1Client(new Uri("http://127.0.0.1:1/"), http);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.AreEqual("URBIT_CONTACT_CANCELLED", (await client.ReadContactAsync("~zod", cancelled.Token)).ErrorCode);
        Assert.AreEqual(0, handler.Calls.Count);
    }

    [TestMethod]
    public async Task OversizedBodyFailsBeforeProfilePublication()
    {
        var handler = new Handler(_ => Reply(new string('x', 64 * 1024 + 1)));
        using var http = new HttpClient(handler);
        using var client = new UrbitContactsV1Client(new Uri("https://teaching.invalid/"), http);
        var result = await client.ReadContactAsync("~zod");
        Assert.AreEqual("URBIT_CONTACT_LIMIT", result.ErrorCode);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task TaggedNullAndMissingOptionalTextArePreserved()
    {
        using var http = new HttpClient(new Handler(_ => Reply("{\"nickname\":{\"type\":\"null\"}}")));
        using var client = new UrbitContactsV1Client(new Uri("https://teaching.invalid/"), http);
        var result = await client.ReadContactAsync("~zod");
        Assert.IsFalse(result.IsError);
        Assert.IsNull(result.Result.Nickname);
        Assert.IsNull(result.Result.Bio);
        Assert.IsNull(result.Result.Status);
    }

    [TestMethod]
    public async Task DisposingReadClientPreservesCallerOwnedHttpClient()
    {
        var handler = new Handler(_ => Reply(Profile));
        using var http = new HttpClient(handler);
        var client = new UrbitContactsV1Client(new Uri("https://teaching.invalid/"), http);
        client.Dispose();
        Assert.IsTrue((await client.ReadContactAsync("~zod")).IsError);
        Assert.AreEqual(0, handler.Calls.Count);
        using var response = await http.GetAsync("https://teaching.invalid/health");
        Assert.IsTrue(response.IsSuccessStatusCode);
    }
}
