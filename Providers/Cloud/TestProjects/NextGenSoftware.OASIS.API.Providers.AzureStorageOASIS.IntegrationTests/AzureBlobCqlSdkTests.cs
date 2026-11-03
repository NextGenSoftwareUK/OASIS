using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

[TestClass]
public class AzureBlobCqlSdkTests
{
    private static CosmicQueryPlan<Holon> Plan(AzureStorageOASIS provider, Guid id)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AzureStorageOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.AzureStorageOASIS, new[] { id }).Result).Result;
        var result = CosmicQueryPlanner.Plan(query, ProviderType.AzureStorageOASIS,
            new[] { new CosmicQueryTarget(ProviderType.AzureStorageOASIS, provider.QueryCapabilities) });
        Assert.IsFalse(result.IsError, result.Message);
        return result.Result;
    }

    [DataTestMethod]
    [DataRow("record")]
    [DataRow("absent")]
    [DataRow("container")]
    [DataRow("identity")]
    [DataRow("deleted")]
    [DataRow("budget")]
    public async Task OfficialSdkReadsOnlySelectedKeyAndPreservesResponseBoundaries(string mode)
    {
        var id = Guid.NewGuid();
        var row = new Holon { Id = mode == "identity" ? Guid.NewGuid() : id,
            Name = "controlled synthetic blob", DeletedDate = mode == "deleted" ? DateTime.UtcNow : DateTime.MinValue };
        await using var server = new BlobReply(mode, OasisJson.Serialize(row));
        var provider = server.Provider();
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, id));
        await server.Requested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        StringAssert.EndsWith(server.RequestPath, "/holons/" + id.ToString("D"));

        if (mode is "container" or "identity" or "budget")
        {
            Assert.IsTrue(result.IsError);
            Assert.AreEqual(mode == "budget" ? "CQL_QUERY_LIMIT" : "CQL_QUERY_FAILED", result.ErrorCode);
            Assert.IsNull(result.Result, "No partial page may escape an SDK or integrity failure.");
        }
        else
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
            Assert.AreEqual(mode == "record" ? 1 : 0, result.Result.Count);
            if (mode == "record") Assert.AreEqual(id, result.Result.Single().Id);
        }
    }

    [TestMethod]
    public async Task CallerCancellationStopsLiveSdkReadWithoutClassifyingProviderOutage()
    {
        await using var server = new BlobReply("hold", "");
        var provider = server.Provider();
        using var cancellation = new CancellationTokenSource();
        var running = provider.ExecuteCosmicQueryAsync(Plan(provider, Guid.NewGuid()), cancellationToken: cancellation.Token);
        await server.Requested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        var result = await running.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode);
        Assert.IsNull(result.Result);
        Assert.IsFalse(provider.IsProviderTransportFailure(result.Exception));
        Assert.IsTrue(provider.IsProviderTransportFailure(new TaskCanceledException("Controlled SDK-only deadline")));
    }

    // Controlled HTTP responses exercise the official signed Azure SDK pipeline.
    // They are not emulator/native-service evidence and never provision storage.
    private sealed class BlobReply : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _serving;
        private readonly string _mode, _json;
        private readonly int _port;
        internal readonly TaskCompletionSource<bool> Requested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal string RequestPath = "";

        internal BlobReply(string mode, string json)
        {
            _mode = mode; _json = json;
            _listener.Start();
            _port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            Console.WriteLine($"Owned controlled Azure SDK listener 127.0.0.1:{_port}, mode={mode}");
            _serving = Serve();
        }

        internal AzureStorageOASIS Provider()
            => new($"DefaultEndpointsProtocol=http;AccountName=controlled;AccountKey={Convert.ToBase64String(new byte[32])};BlobEndpoint=http://127.0.0.1:{_port}/controlled",
                "holons", "avatars", "details") { IsProviderActivated = true };

        private async Task Serve()
        {
            try
            {
                using var peer = await _listener.AcceptTcpClientAsync(_stop.Token);
                await using var stream = peer.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
                var first = await reader.ReadLineAsync(_stop.Token);
                RequestPath = first!.Split(' ')[1];
                while (!string.IsNullOrEmpty(await reader.ReadLineAsync(_stop.Token))) { }
                Requested.TrySetResult(true);
                if (_mode == "hold") { await Task.Delay(Timeout.Infinite, _stop.Token); return; }

                var missing = _mode is "absent" or "container";
                var error = _mode == "container" ? "ContainerNotFound" : "BlobNotFound";
                var payload = Encoding.UTF8.GetBytes(missing ? $"<Error><Code>{error}</Code><Message>controlled absence</Message></Error>" : _json);
                var length = _mode == "budget" ? 4 * 1024 * 1024 + 1 : payload.Length;
                var headers = $"HTTP/1.1 {(missing ? "404 Not Found" : "200 OK")}\r\nContent-Length: {length}\r\nContent-Type: {(missing ? "application/xml" : "application/json")}\r\nConnection: close\r\nETag: \"controlled\"\r\nLast-Modified: Tue, 01 Jan 2030 00:00:00 GMT\r\nx-ms-blob-type: BlockBlob\r\n";
                if (missing) headers += $"x-ms-error-code: {error}\r\n";
                await stream.WriteAsync(Encoding.ASCII.GetBytes(headers + "\r\n"), _stop.Token);
                if (_mode != "budget") await stream.WriteAsync(payload, _stop.Token);
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        }

        public async ValueTask DisposeAsync()
        {
            _stop.Cancel(); _listener.Stop();
            await _serving.WaitAsync(TimeSpan.FromSeconds(5));
            _stop.Dispose();
        }
    }
}
