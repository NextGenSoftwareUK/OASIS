using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.FirebaseOASIS.UnitTests;

[TestClass]
public class FirebaseReadFailureTests
{
    private sealed class Reply(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public Uri? LastUri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    [DataTestMethod]
    [DataRow(401, "point"), DataRow(403, "point"), DataRow(429, "point"), DataRow(500, "point")]
    [DataRow(401, "query"), DataRow(403, "query"), DataRow(429, "query"), DataRow(500, "query")]
    [DataRow(401, "list"), DataRow(403, "list"), DataRow(429, "list"), DataRow(500, "list")]
    public async Task FailedHttpReadPreservesTransportError(int status, string operation)
    {
        using var http = new HttpClient(new Reply((HttpStatusCode)status, "{\"error\":\"synthetic failure\"}"));
        var provider = new FirebaseOASIS("https://synthetic.invalid", null, http);
        Exception? cause;
        if (operation == "point") cause = (await provider.LoadAvatarAsync(Guid.NewGuid())).Exception;
        else if (operation == "query") cause = (await provider.LoadAvatarByUsernameAsync("synthetic")).Exception;
        else cause = (await provider.LoadAllHolonsAsync(loadChildren: false)).Exception;
        Assert.IsInstanceOfType(cause, typeof(HttpRequestException), "A transport failure must not become a missing record or empty collection.");
        Assert.AreEqual((HttpStatusCode)status, ((HttpRequestException)cause!).StatusCode);
    }

    [DataTestMethod]
    [DataRow("[]"), DataRow("\"wrong shape\""), DataRow("")]
    public async Task InvalidCollectionPayloadIsNotAnEmptySuccess(string body)
    {
        using var http = new HttpClient(new Reply(HttpStatusCode.OK, body));
        var provider = new FirebaseOASIS("https://synthetic.invalid", null, http);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.IsNotNull(result.Exception);
        Assert.IsNull(result.Result);
    }

    [DataTestMethod, DataRow("null"), DataRow("{}")]
    public async Task DocumentedEmptyCollectionRemainsSuccessful(string body)
    {
        using var http = new HttpClient(new Reply(HttpStatusCode.OK, body));
        var provider = new FirebaseOASIS("https://synthetic.invalid", null, http);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsFalse(result.IsError);
        Assert.IsNotNull(result.Result);
        Assert.AreEqual(0, result.Result.Count());
    }

    [TestMethod]
    public async Task QueryEncodesJsonStringBeforeUrlEncoding()
    {
        const string username = "a\"b\\c&x=1+ñ";
        using var reply = new Reply(HttpStatusCode.OK, "null");
        using var http = new HttpClient(reply);
        var provider = new FirebaseOASIS("https://synthetic.invalid", null, http);
        await provider.LoadAvatarByUsernameAsync(username);
        var query = reply.LastUri!.Query.TrimStart('?').Split('&').Select(item => item.Split('=', 2))
            .ToDictionary(item => item[0], item => Uri.UnescapeDataString(item[1]));
        Assert.AreEqual("username", System.Text.Json.JsonSerializer.Deserialize<string>(query["orderBy"]));
        Assert.AreEqual(username, System.Text.Json.JsonSerializer.Deserialize<string>(query["equalTo"]));
        Assert.AreEqual(2, query.Count);
    }
}
