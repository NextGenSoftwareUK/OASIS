using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.ConvexOASIS.UnitTests;

[TestClass]
public class ConvexFunctionContractTests
{
    private sealed class Reply(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? Authorization, RequestBody;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization?.ToString();
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    [DataTestMethod]
    [DataRow("[]"), DataRow("{}"), DataRow("{\"status\":\"success\"}")]
    [DataRow("{\"status\":\"other\",\"value\":[]}"), DataRow("{\"status\":\"error\",\"errorMessage\":\"private-diagnostic\"}")]
    [DataRow("{\"status\":\"success\",\"value\":{}}"), DataRow("{\"status\":\"success\",\"value\":null}"), DataRow("{\"status\":\"success\",\"value\":[1]}")]
    public async Task InvalidOrFailedFunctionDoesNotBecomeEmptyCollection(string body)
    {
        using var http = new HttpClient(new Reply(body));
        var provider = new ConvexOASIS("https://synthetic.invalid", "synthetic-key", http);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.IsNotNull(result.Exception);
        Assert.IsNull(result.Result);
        Assert.IsFalse(result.Message.Contains("private-diagnostic"));
    }

    [TestMethod]
    public async Task FunctionErrorNeverAcknowledgesWriteSuccess()
    {
        using var http = new HttpClient(new Reply("{\"status\":\"error\",\"errorMessage\":\"private-diagnostic\"}"));
        var provider = new ConvexOASIS("https://synthetic.invalid", "synthetic-key", http);
        var result = await provider.SaveAvatarAsync(new Avatar { Id = Guid.NewGuid(), Username = "synthetic" });
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.IsSaved);
        Assert.IsNull(result.Result);
        Assert.IsFalse(result.Message.Contains("private-diagnostic"));
    }

    [DataTestMethod, DataRow(401), DataRow(403), DataRow(429), DataRow(500)]
    public async Task TransportStatusRemainsAvailable(int status)
    {
        using var http = new HttpClient(new Reply("{}", (HttpStatusCode)status));
        var provider = new ConvexOASIS("https://synthetic.invalid", "synthetic-key", http);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsInstanceOfType(result.Exception, typeof(HttpRequestException));
        Assert.AreEqual((HttpStatusCode)status, ((HttpRequestException)result.Exception!).StatusCode);
    }

    [TestMethod]
    public async Task DeployKeyUsesAdminSchemeAndExplicitJsonFormat()
    {
        using var reply = new Reply("{\"status\":\"success\",\"value\":[]}");
        using var http = new HttpClient(reply);
        var provider = new ConvexOASIS("https://synthetic.invalid", "synthetic-key", http);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsFalse(result.IsError);
        Assert.AreEqual("Convex synthetic-key", reply.Authorization);
        using var request = JsonDocument.Parse(reply.RequestBody!);
        Assert.AreEqual("json", request.RootElement.GetProperty("format").GetString());
    }

    [TestMethod]
    public async Task SuccessfulEnvelopeReturnsItsActualRows()
    {
        var id = Guid.NewGuid();
        var body = JsonSerializer.Serialize(new { status = "success", value = new[] { new { dataJson = JsonSerializer.Serialize(new { id, name = "synthetic" }), isDeleted = false } } });
        using var http = new HttpClient(new Reply(body));
        var provider = new ConvexOASIS("https://synthetic.invalid", "synthetic-key", http);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsFalse(result.IsError);
        Assert.AreEqual(id, result.Result.Single().Id);
    }
}
