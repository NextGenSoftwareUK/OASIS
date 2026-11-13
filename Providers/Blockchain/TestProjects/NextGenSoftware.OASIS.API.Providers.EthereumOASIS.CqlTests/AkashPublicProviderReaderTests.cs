using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.AkashOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.CqlTests;

[TestClass]
public class AkashPublicProviderReaderTests
{
    private static readonly string[] Owners = { "akash1qqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqq7cdc78",
        "akash1lllllllllllllllllllllllllllllllljfvhed", "akash1u5cdg7k3gl43mukca4aeultuz8x2j68mgwn28e" };

    [DataTestMethod]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(4096)]
    public async Task DiscoveryStopsAtThreeCompleteRecordsWithoutReadingCatalogueTail(int chunkSize)
    {
        // More than the old whole-body budget follows these three records. It is
        // not part of the selected sample and must not be downloaded or certified.
        var prefix = "[" + string.Join(",", Owners.Select(owner =>
            JsonSerializer.Serialize(new { owner, name = "Café ☕" }))) + ",";
        using var body = new ObservedStream(prefix + new string(' ', 2097153), chunkSize);
        using var handler = new StreamResponse(body);
        using var client = new HttpClient(handler);

        var keys = await new AkashPublicProviderReader(client, new Uri("https://example.invalid/"))
            .DiscoverThreeAsync(default);

        CollectionAssert.AreEqual(Owners.Order(StringComparer.Ordinal).ToArray(), keys.ToArray());
        Assert.AreEqual(1, handler.Requests);
        Assert.IsTrue(body.BytesRead <= Encoding.UTF8.GetByteCount(prefix) + 4096);
        Assert.IsTrue(body.Disposed, "The unused HTTP response stream must be closed.");
    }

    [DataTestMethod]
    [DataRow("wrong-root")]
    [DataRow("missing-owner")]
    [DataRow("duplicate-field")]
    [DataRow("oversized-record")]
    [DataRow("total-budget")]
    [DataRow("truncated")]
    public async Task StreamingDiscoveryRejectsInvalidOrUnboundedSelectedPrefix(string fault)
    {
        var json = fault switch
        {
            "wrong-root" => "{}",
            "missing-owner" => "[{}]",
            "duplicate-field" => "[{\"owner\":\"" + Owners[0] + "\",\"owner\":\"" + Owners[1] + "\"}]",
            "oversized-record" => "[{\"owner\":\"" + Owners[0] + "\",\"label\":\"" + new string('x', 262145) + "\"}]",
            "total-budget" => "[" + new string(' ', 2097153),
            _ => "[{\"owner\":\"" + Owners[0]
        };
        using var body = new ObservedStream(json, 4096);
        using var handler = new StreamResponse(body);
        using var client = new HttpClient(handler);
        try
        {
            await new AkashPublicProviderReader(client, new Uri("https://example.invalid/"))
                .DiscoverThreeAsync(default);
            Assert.Fail("Invalid discovery prefix accepted.");
        }
        catch (Exception error) when (error is InvalidDataException or JsonException) { }

        Assert.AreEqual(1, handler.Requests, "No retries or point reads after discovery failure.");
        Assert.IsTrue(body.BytesRead <= 2097152);
        Assert.IsTrue(body.Disposed);
    }

    private sealed class StreamResponse(Stream body) : HttpMessageHandler
    {
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Requests++;
            Assert.AreEqual("https://example.invalid/v1/providers", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(body) });
        }
    }

    private sealed class ObservedStream(string text, int chunkSize) : MemoryStream(Encoding.UTF8.GetBytes(text))
    {
        public long BytesRead;
        public bool Disposed;
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var count = await base.ReadAsync(buffer[..Math.Min(chunkSize, buffer.Length)], cancellationToken);
            BytesRead += count;
            return count;
        }
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    [TestMethod]
    public async Task DiscoveryFreezesThreeUniqueOwnersAndPointReadsOnlyTheirMetadata()
    {
        using var handler = new Native(); using var client = new HttpClient(handler);
        var reader = new AkashPublicProviderReader(client, new Uri("https://example.invalid/"));
        var keys = await reader.DiscoverThreeAsync(default);
        CollectionAssert.AreEqual(Owners.Order(StringComparer.Ordinal).ToArray(), keys.ToArray());
        var rows = await reader.ReadAsync(keys, default);
        CollectionAssert.AreEqual(keys.ToArray(), rows.Select(row => row.Owner).ToArray());
        Assert.AreEqual(4, handler.Starts.Count);
        Assert.IsTrue(handler.Paths.All(path => path.StartsWith("/v1/providers", StringComparison.Ordinal)));
        for (var i = 1; i < handler.Starts.Count; i++)
            Assert.IsTrue(Stopwatch.GetElapsedTime(handler.Starts[i - 1], handler.Starts[i]) >= TimeSpan.FromMilliseconds(950));
        // The descriptive private URL is never contacted or resolved.
        Assert.IsTrue(rows.All(row => row.HostUri == "https://127.0.0.1:8443/"));
    }

    [DataTestMethod]
    [DataRow("duplicate")]
    [DataRow("short")]
    [DataRow("bad-owner")]
    public async Task InvalidDiscoveryDoesNotProduceACatalogue(string fault)
    {
        using var handler = new Native { Fault = fault }; using var client = new HttpClient(handler);
        try
        {
            await new AkashPublicProviderReader(client, new Uri("https://example.invalid/")).DiscoverThreeAsync(default);
            Assert.Fail("Invalid discovery accepted.");
        }
        catch (Exception error) when (error is InvalidDataException or ArgumentException) { }
        Assert.AreEqual(1, handler.Paths.Count);
    }

    [DataTestMethod]
    [DataRow("last-http")]
    [DataRow("mismatch")]
    [DataRow("oversize")]
    public async Task FailedPointSelectionNeverReturnsPartialRowsOrRetries(string fault)
    {
        using var handler = new Native { Fault = fault }; using var client = new HttpClient(handler);
        try
        {
            await new AkashPublicProviderReader(client, new Uri("https://example.invalid/")).ReadAsync(Owners, default);
            Assert.Fail("Failed selection accepted.");
        }
        catch (Exception error) when (error is InvalidDataException or HttpRequestException) { }
        Assert.AreEqual(fault == "last-http" ? 3 : 1, handler.Paths.Count);
    }

    [TestMethod]
    public async Task InvalidOrCancelledSelectionDoesNotDispatch()
    {
        using var handler = new Native(); using var client = new HttpClient(handler);
        var reader = new AkashPublicProviderReader(client, new Uri("https://example.invalid/"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => reader.ReadAsync(new[] { "bad-owner" }, default));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => reader.ReadAsync(new[] { Owners[0], Owners[0] }, default));
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => reader.ReadAsync(Owners, cancellation.Token));
        Assert.AreEqual(0, handler.Paths.Count);
    }

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "";
        public List<long> Starts = new();
        public List<string> Paths = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("example.invalid", request.RequestUri!.Host); Assert.IsNull(request.Content);
            Assert.IsNull(request.Headers.Authorization); Assert.IsFalse(request.Headers.Contains("Cookie"));
            var path = request.RequestUri.AbsolutePath; Paths.Add(path); Starts.Add(Stopwatch.GetTimestamp());
            object Row(string owner) => new { owner, name = "Provider", hostUri = "https://127.0.0.1:8443/", isOnline = true };
            string json;
            if (path == "/v1/providers")
            {
                var keys = Fault == "duplicate" ? new[] { Owners[0], Owners[0], Owners[1] } :
                    Fault == "short" ? Owners[..2] : Fault == "bad-owner" ? new[] { Owners[0], Owners[1], "bad" } : Owners;
                json = JsonSerializer.Serialize(keys.Select(Row));
            }
            else
            {
                var owner = path["/v1/providers/".Length..]; Assert.IsTrue(Owners.Contains(owner));
                json = Fault == "oversize" ? new string(' ', 262145) : JsonSerializer.Serialize(Row(Fault == "mismatch" ? "wrong-owner" : owner));
            }
            return Task.FromResult(new HttpResponseMessage(Fault == "last-http" && Paths.Count == 3 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK) {
                Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
}
