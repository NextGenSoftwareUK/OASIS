using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.TursoOASIS.UnitTests;

[TestClass]
public class TursoPipelineTests
{
    [DataTestMethod]
    [DataRow("""{"results":[{"type":"ok","response":{"type":"execute","result":{"cols":[],"rows":[]}}},{"type":"ok","response":{"type":"close"}}]}""", false)]
    [DataRow("""{"results":[{"type":"error","error":{"message":"statement failed","code":"SQLITE_ERROR"}},{"type":"ok","response":{"type":"close"}}]}""", true)]
    [DataRow("""{"results":[{"type":"ok","response":{"type":"execute","result":{"cols":[],"rows":[]}}},{"type":"error","error":{"message":"close failed"}}]}""", true)]
    [DataRow("""{"results":[]}""", true)]
    public async Task ActivationRequiresStatementAndCloseAcknowledgements(string body, bool expectedError)
    {
        var provider = new TursoOASIS("https://turso.invalid", "fixture", new PipelineHandler(body));
        var result = await provider.ActivateProviderAsync();

        Assert.AreEqual(expectedError, result.IsError, result.Message);
        Assert.AreEqual(!expectedError, result.Result);
    }

    private sealed class PipelineHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.AreEqual("/v2/pipeline", request.RequestUri?.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
