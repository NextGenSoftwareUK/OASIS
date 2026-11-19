using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.AppwriteOASIS.UnitTests;

[TestClass]
public class AppwriteReadFailureTests
{
    private sealed class Reply(Func<int, (HttpStatusCode Status, string Body)> response) : HttpMessageHandler
    {
        public int Count { get; private set; }
        public Uri? LastUri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUri = request.RequestUri;
            var reply = response(++Count);
            return Task.FromResult(new HttpResponseMessage(reply.Status) { Content = new StringContent(reply.Body, Encoding.UTF8, "application/json") });
        }
    }

    [DataTestMethod]
    [DataRow(401, false), DataRow(403, false), DataRow(429, false), DataRow(500, false)]
    [DataRow(401, true), DataRow(403, true), DataRow(429, true), DataRow(500, true)]
    public async Task FailedReadPreservesTransportStatus(int status, bool list)
    {
        using var http = new HttpClient(new Reply(_ => ((HttpStatusCode)status, "{}")));
        var provider = new AppwriteOASIS("https://synthetic.invalid", "synthetic", "synthetic", "synthetic", http);
        Exception? cause = list ? (await provider.LoadAllHolonsAsync(loadChildren: false)).Exception
            : (await provider.LoadAvatarAsync(Guid.NewGuid())).Exception;
        Assert.IsInstanceOfType(cause, typeof(HttpRequestException));
        Assert.AreEqual((HttpStatusCode)status, ((HttpRequestException)cause!).StatusCode);
    }

    [TestMethod]
    public async Task FailedLaterPageDoesNotReturnPartialSuccess()
    {
        using var http = new HttpClient(new Reply(n => n == 1
            ? (HttpStatusCode.OK, "{\"documents\":[{\"$id\":\"synthetic\",\"data_json\":\"{}\"}],\"total\":2}")
            : (HttpStatusCode.ServiceUnavailable, "{}")));
        var provider = new AppwriteOASIS("https://synthetic.invalid", "synthetic", "synthetic", "synthetic", http);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        Assert.IsInstanceOfType(result.Exception, typeof(HttpRequestException));
    }

    [DataTestMethod, DataRow("{}"), DataRow("null"), DataRow("{\"documents\":[],\"total\":1}")]
    public async Task IncompleteEnvelopeDoesNotBecomeEmptySuccess(string body)
    {
        using var http = new HttpClient(new Reply(_ => (HttpStatusCode.OK, body)));
        var provider = new AppwriteOASIS("https://synthetic.invalid", "synthetic", "synthetic", "synthetic", http);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.IsNotNull(result.Exception);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task CompleteEmptyCollectionRemainsSuccessful()
    {
        using var http = new HttpClient(new Reply(_ => (HttpStatusCode.OK, "{\"documents\":[],\"total\":0}")));
        var provider = new AppwriteOASIS("https://synthetic.invalid", "synthetic", "synthetic", "synthetic", http);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsFalse(result.IsError);
        Assert.IsNotNull(result.Result);
        Assert.AreEqual(0, result.Result.Count());
    }

    [TestMethod]
    public async Task BooleanSelectorCarriesACompleteJsonValueArray()
    {
        using var reply = new Reply(_ => (HttpStatusCode.OK, "{\"documents\":[],\"total\":0}"));
        using var http = new HttpClient(reply);
        var provider = new AppwriteOASIS("https://synthetic.invalid", "synthetic", "synthetic", "synthetic", http);
        await provider.LoadAllHolonsAsync(loadChildren: false);
        var query = reply.LastUri!.Query.TrimStart('?').Split('&').Where(x => x.StartsWith("queries[]="))
            .Select(x => Uri.UnescapeDataString(x.Split('=', 2)[1])).Single();
        using var values = System.Text.Json.JsonDocument.Parse(query[(query.IndexOf(',') + 1)..^1]);
        Assert.AreEqual(System.Text.Json.JsonValueKind.Array, values.RootElement.ValueKind);
        Assert.AreEqual(1, values.RootElement.GetArrayLength());
        Assert.IsFalse(values.RootElement[0].GetBoolean());
    }
}
