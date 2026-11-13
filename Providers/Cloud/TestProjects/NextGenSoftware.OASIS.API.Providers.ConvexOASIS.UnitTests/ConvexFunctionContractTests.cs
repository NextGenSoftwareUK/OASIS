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
        public bool MissingReadBeforeWrite;
        public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Authorization = request.Headers.Authorization?.ToString();
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var parsed = JsonDocument.Parse(RequestBody);
            var path = parsed.RootElement.GetProperty("path").GetString();
            if (path == "oasis/records:verify")
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"status\":\"success\",\"value\":{\"protocol\":\"oasis-private-records-v1\"}}") };
            if (MissingReadBeforeWrite && path == "oasis/records:get")
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"status\":\"success\",\"value\":null}") };
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
        var provider = new ConvexOASIS("https://synthetic.invalid", "synthetic-key", http, () => new byte[32]);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.IsNotNull(result.Exception);
        Assert.IsNull(result.Result);
        Assert.IsFalse(result.Message.Contains("private-diagnostic"));
    }

    [TestMethod]
    public async Task FunctionErrorNeverAcknowledgesWriteSuccess()
    {
        using var http = new HttpClient(new Reply("{\"status\":\"error\",\"errorMessage\":\"private-diagnostic\"}") { MissingReadBeforeWrite = true });
        var provider = new ConvexOASIS("https://synthetic.invalid", "synthetic-key", http, () => new byte[32]);
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
        var provider = new ConvexOASIS("https://synthetic.invalid", "synthetic-key", http, () => new byte[32]);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsInstanceOfType(result.Exception, typeof(HttpRequestException));
        Assert.AreEqual((HttpStatusCode)status, ((HttpRequestException)result.Exception!).StatusCode);
    }

    [TestMethod]
    public async Task DeployKeyUsesAdminSchemeAndExplicitJsonFormat()
    {
        using var reply = new Reply("{\"status\":\"success\",\"value\":{\"keys\":[],\"complete\":true,\"cursor\":null}}");
        using var http = new HttpClient(reply);
        var provider = new ConvexOASIS("https://synthetic.invalid", "synthetic-key", http, () => new byte[32]);
        var result = await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsFalse(result.IsError);
        Assert.AreEqual("Convex synthetic-key", reply.Authorization);
        using var request = JsonDocument.Parse(reply.RequestBody!);
        Assert.AreEqual("json", request.RootElement.GetProperty("format").GetString());
    }

    [DataTestMethod, DataRow("{}", 200), DataRow("{\"status\":\"success\",\"value\":null}", 200), DataRow("{}", 500)]
    public async Task UnacknowledgedMutationRequiresReconciliation(string body, int status)
    {
        using var reply = new Reply(body, (HttpStatusCode)status) { MissingReadBeforeWrite = true };
        using var http = new HttpClient(reply);
        using var provider = new ConvexOASIS("https://synthetic.invalid", "synthetic-key", http, () => new byte[32]);
        var result = await provider.SaveAvatarAsync(new Avatar { Id = Guid.NewGuid(), Username = "synthetic" });
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.IsSaved);
        Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
    }

    [TestMethod]
    public async Task ArchivedWalletCredentialsAreRejectedBeforeTransport()
    {
        using var reply = new Reply("{}");
        using var http = new HttpClient(reply);
        using var provider = new ConvexOASIS("https://synthetic.invalid", "synthetic-key", http, () => new byte[32]);
        var result = await provider.LoadAvatarByPublicKeyAsync("synthetic-key", 1);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(0, reply.Calls);
    }

    [TestMethod]
    public async Task SuccessfulObservationRetainsItsValueAndNativeRevision()
    {
        using var http = new HttpClient(new Reply("{\"status\":\"success\",\"value\":{\"value\":\"synthetic-payload\",\"revision\":\"native-revision\"}}"));
        var backend = new ConvexRecordBackend("https://synthetic.invalid", "synthetic-key", http);
        var observed = await backend.ReadObservedAsync("synthetic-key");
        Assert.IsNotNull(observed);
        Assert.AreEqual("synthetic-payload", observed.Value);
        Assert.AreEqual("native-revision", observed.Revision);
    }
}
