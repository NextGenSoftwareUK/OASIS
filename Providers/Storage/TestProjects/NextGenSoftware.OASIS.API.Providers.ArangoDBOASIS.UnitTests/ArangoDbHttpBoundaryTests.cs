using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.ArangoDBOASIS.UnitTests;

[TestClass]
public class ArangoDbHttpBoundaryTests
{
    private sealed class Handler(HttpStatusCode status, int errorNumber) : HttpMessageHandler
    {
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Requests++;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { error = true, errorNum = errorNumber }))
            });
        }
    }

    private static ArangoDBOASIS Provider(Handler handler)
    {
        var provider = new ArangoDBOASIS("http://127.0.0.1:1");
        var field = typeof(ArangoDBOASIS).GetField("_http", BindingFlags.Instance | BindingFlags.NonPublic)!;
        ((HttpClient)field.GetValue(provider)!).Dispose();
        field.SetValue(provider, new HttpClient(handler));
        return provider;
    }

    [DataTestMethod]
    [DataRow(401, 11)]
    [DataRow(403, 11)]
    [DataRow(500, 4)]
    [DataRow(409, 1200)]
    public async Task FailedSchemaCreationCannotActivateOrLeaveStaleState(int status, int number)
    {
        var handler = new Handler((HttpStatusCode)status, number);
        var provider = Provider(handler);
        provider.IsProviderActivated = true;
        var result = await provider.ActivateProviderAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual(1, handler.Requests);
    }

    [TestMethod]
    public async Task OnlyDocumentedDuplicateNameAdmitsExistingSchema()
    {
        var handler = new Handler(HttpStatusCode.Conflict, 1207);
        var provider = Provider(handler);
        var result = await provider.ActivateProviderAsync();
        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsTrue(provider.IsProviderActivated);
        Assert.AreEqual(4, handler.Requests);
        await provider.DeActivateProviderAsync();
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [DataTestMethod]
    [DataRow(403, 11)]
    [DataRow(500, 4)]
    [DataRow(404, 1203)]
    public async Task ReadFailureIsNotReportedAsMissingDocument(int status, int number)
    {
        var provider = Provider(new Handler((HttpStatusCode)status, number));
        var result = await provider.LoadHolonAsync(Guid.NewGuid(), loadChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.IsInstanceOfType(result.Exception, typeof(HttpRequestException));
    }
}
