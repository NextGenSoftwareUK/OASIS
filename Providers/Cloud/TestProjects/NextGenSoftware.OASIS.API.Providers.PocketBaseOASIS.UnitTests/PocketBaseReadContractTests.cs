using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS.UnitTests;

[TestClass]
public class PocketBaseReadContractTests
{
    private sealed class Reply(Func<int, (HttpStatusCode Status, string Body)> read, string authBody = "{\"token\":\"synthetic-token\"}") : HttpMessageHandler
    {
        public List<Uri> Reads { get; } = new();
        public string? AuthPath, Header;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            (HttpStatusCode Status, string Body) result;
            if (request.Method == HttpMethod.Post) { AuthPath = request.RequestUri!.AbsolutePath; result = (HttpStatusCode.OK, authBody); }
            else { Reads.Add(request.RequestUri!); Header = request.Headers.GetValues("Authorization").Single(); result = read(Reads.Count); }
            return Task.FromResult(new HttpResponseMessage(result.Status) { Content = new StringContent(result.Body, Encoding.UTF8, "application/json") });
        }
    }
    private static string Page(int page, int total, string id)
        => JsonSerializer.Serialize(new { page, perPage = 1, totalItems = total, totalPages = total,
            items = new[] { new { id, oasis_id = GuidFor(id).ToString("D"), is_deleted = false,
                data_json = JsonSerializer.Serialize(new { id = GuidFor(id), name = "Read contract fixture" }) } } });

    private static Guid GuidFor(string id)
        => new(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(id)).AsSpan(0, 16));

    [DataTestMethod]
    [DataRow(401, false), DataRow(403, false), DataRow(429, false), DataRow(500, false)]
    [DataRow(401, true), DataRow(403, true), DataRow(429, true), DataRow(500, true)]
    public async Task ReadFailurePreservesTransportStatus(int status, bool list)
    {
        using var http = new HttpClient(new Reply(_ => ((HttpStatusCode)status, "{}")));
        var provider = new PocketBaseOASIS("https://synthetic.invalid", "synthetic", "synthetic", http);
        var cause = list ? (await provider.LoadAllHolonsAsync(loadChildren: false)).Exception
            : (await provider.LoadAvatarAsync(Guid.NewGuid())).Exception;
        Assert.IsInstanceOfType(cause, typeof(HttpRequestException));
        Assert.AreEqual((HttpStatusCode)status, ((HttpRequestException)cause!).StatusCode);
    }

    [TestMethod]
    public async Task AllPagesAreReadInStableOrderWithCurrentSuperuserAuth()
    {
        using var reply = new Reply(n => (HttpStatusCode.OK, Page(n, 2, "record" + n)));
        using var http = new HttpClient(reply);
        var provider = new PocketBaseOASIS("https://synthetic.invalid", "synthetic", "synthetic", http);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsFalse(result.IsError);
        Assert.AreEqual(2, result.Result.Count());
        Assert.AreEqual(2, reply.Reads.Count);
        StringAssert.Contains(reply.Reads[1].Query, "page=2");
        StringAssert.Contains(reply.Reads[1].Query, "sort=id");
        Assert.AreEqual("/api/collections/_superusers/auth-with-password", reply.AuthPath);
        Assert.AreEqual("synthetic-token", reply.Header);
    }

    [TestMethod]
    public async Task FailedLaterPageNeverReturnsPartialSuccess()
    {
        using var http = new HttpClient(new Reply(n => n == 1 ? (HttpStatusCode.OK, Page(1, 2, "first")) : (HttpStatusCode.ServiceUnavailable, "{}")));
        var provider = new PocketBaseOASIS("https://synthetic.invalid", "synthetic", "synthetic", http);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        Assert.IsInstanceOfType(result.Exception, typeof(HttpRequestException));
    }

    [DataTestMethod, DataRow("{}"), DataRow("null"), DataRow("{\"items\":[]}"), DataRow("{\"page\":1,\"perPage\":1,\"totalItems\":1,\"totalPages\":1,\"items\":[]}")]
    public async Task IncompletePageDoesNotBecomeEmptySuccess(string body)
    {
        using var http = new HttpClient(new Reply(_ => (HttpStatusCode.OK, body)));
        var provider = new PocketBaseOASIS("https://synthetic.invalid", "synthetic", "synthetic", http);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.IsNotNull(result.Exception);
        Assert.IsNull(result.Result);
    }

    [DataTestMethod, DataRow("repeat-id"), DataRow("wrong-page"), DataRow("changed-total")]
    public async Task InconsistentContinuationFailsInsteadOfDuplicatingOrTruncating(string fault)
    {
        using var http = new HttpClient(new Reply(n => (HttpStatusCode.OK, n == 1 ? Page(1, 2, "first")
            : Page(fault == "wrong-page" ? 1 : 2, fault == "changed-total" ? 3 : 2, fault == "repeat-id" ? "first" : "second"))));
        var provider = new PocketBaseOASIS("https://synthetic.invalid", "synthetic", "synthetic", http);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task EmptyTokenCannotPoisonSubsequentAuthentication()
    {
        using var http = new HttpClient(new Reply(_ => (HttpStatusCode.OK, "{}"), "{\"token\":\"\"}"));
        var provider = new PocketBaseOASIS("https://synthetic.invalid", "synthetic", "synthetic", http);
        Assert.IsTrue((await provider.ActivateProviderAsync()).IsError);
        Assert.IsTrue((await provider.ActivateProviderAsync()).IsError);
    }

    [TestMethod]
    public async Task EmptyCollectionIsAValidCompletePage()
    {
        using var http = new HttpClient(new Reply(_ => (HttpStatusCode.OK, "{\"page\":1,\"perPage\":500,\"totalItems\":0,\"totalPages\":0,\"items\":[]}")));
        var provider = new PocketBaseOASIS("https://synthetic.invalid", "synthetic", "synthetic", http);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsFalse(result.IsError);
        Assert.AreEqual(0, result.Result.Count());
    }
}
