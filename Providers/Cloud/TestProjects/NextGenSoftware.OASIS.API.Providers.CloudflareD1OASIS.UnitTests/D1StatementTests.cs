using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.CloudflareD1OASIS.UnitTests;

[TestClass]
public class D1StatementTests
{
    [DataTestMethod]
    [DataRow("""{"success":true,"result":[{"success":true,"results":[]}]}""", false)]
    [DataRow("""{"success":true,"result":[{"success":false,"results":[]}]}""", true)]
    [DataRow("""{"success":true,"result":[]}""", true)]
    [DataRow("""{"success":true,"result":[{"success":true}]}""", true)]
    public async Task ActivationRequiresStatementAcknowledgement(string body, bool expectedError)
    {
        var provider = new CloudflareD1OASIS("account", "database", "fixture", new ResponseHandler(body));
        var result = await provider.ActivateProviderAsync();
        Assert.AreEqual(expectedError, result.IsError, result.Message);
        Assert.AreEqual(!expectedError, result.Result);
    }

    private sealed class ResponseHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
    }
}
