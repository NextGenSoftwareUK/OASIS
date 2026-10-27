using System.Net;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

[TestClass]
public class CqlBoundedReadTests
{
    [TestMethod]
    public async Task CompletePagesPublishCanonicalRows()
    {
        using var pages = new Pages("{\"Documents\":[{\"Id\":\"first\"}]}", "{\"Documents\":[{\"Id\":\"second\"}]}");
        var rows = await CosmosDbClient.ReadBoundedPagesAsync(pages, 2, 2, 1000, default);
        Assert.AreEqual(2, rows.Count);
        StringAssert.Contains(rows[1], "second");
    }

    [TestMethod]
    public async Task RowOverflowRejectsInsteadOfTruncating()
    {
        using var pages = new Pages("{\"Documents\":[{},{}]}");
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => CosmosDbClient.ReadBoundedPagesAsync(pages, 1, 2, 1000, default));
    }

    [TestMethod]
    public async Task PageOverflowStopsBeforeNextRequest()
    {
        using var pages = new Pages("{\"Documents\":[]}", "{\"Documents\":[{}]}");
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => CosmosDbClient.ReadBoundedPagesAsync(pages, 2, 1, 1000, default));
        Assert.AreEqual(1, pages.Reads);
    }

    [TestMethod]
    public async Task CumulativeByteBudgetIncludesEmptyPages()
    {
        using var pages = new Pages("{\"Documents\":[]}", "{\"Documents\":[]}");
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => CosmosDbClient.ReadBoundedPagesAsync(pages, 2, 2, 20, default));
    }

    [TestMethod]
    public async Task CancellationPreventsFirstRequest()
    {
        using var pages = new Pages("{\"Documents\":[{}]}");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => CosmosDbClient.ReadBoundedPagesAsync(pages, 2, 2, 1000, cancellation.Token));
        Assert.AreEqual(0, pages.Reads);
    }

    [TestMethod]
    public async Task MalformedLaterPageDoesNotPublishPartialRows()
    {
        using var pages = new Pages("{\"Documents\":[{}]}", "{\"Documents\":[1]}");
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => CosmosDbClient.ReadBoundedPagesAsync(pages, 2, 2, 1000, default));
    }

    private sealed class Pages(params string[] bodies) : FeedIterator
    {
        public int Reads { get; private set; }
        public override bool HasMoreResults => Reads < bodies.Length;

        public override Task<ResponseMessage> ReadNextAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ResponseMessage(HttpStatusCode.OK)
            {
                Content = new MemoryStream(Encoding.UTF8.GetBytes(bodies[Reads++]))
            });
        }
    }
}
