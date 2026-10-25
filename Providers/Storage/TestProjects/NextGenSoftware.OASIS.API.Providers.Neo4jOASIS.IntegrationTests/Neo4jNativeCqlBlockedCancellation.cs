using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [DataTestMethod]
    [DataRow(false, false, "rows")]
    [DataRow(false, true, "rows")]
    [DataRow(true, false, "rows")]
    [DataRow(true, true, "rows")]
    [DataRow(false, false, "projection")]
    [DataRow(false, true, "projection")]
    [DataRow(false, false, "scalar")]
    [DataRow(false, true, "scalar")]
    [DataRow(false, false, "grouped")]
    [DataRow(false, true, "grouped")]
    public Task NativeCqlCancellationInterruptsPausedBoltResponseBeforeRelease(bool abortOwnedDriver, bool warmConnection, string shape)
        => WithNativeGraph(async fixture =>
        {
            // The relay forwards the actual Bolt handshake, authentication and
            // schema setup. Only a later server response is deliberately held.
            await using var relay = new PausedBoltRelay();
            using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17788",
                AuthTokens.Basic("neo4j", "oasis-local-test-only"),
                config => config.WithMaxConnectionPoolSize(1));
            using var storage = GraphDatabase.Driver("bolt://127.0.0.1:17687",
                AuthTokens.Basic("neo4j", "oasis-local-test-only"));
            var provider = new Provider("bolt://127.0.0.1:17687", "neo4j",
                "oasis-local-test-only", () => storage, () => driver);
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            try
            {
                using var cancellation = new CancellationTokenSource();
                if (warmConnection) await driver.VerifyConnectivityAsync();
                relay.Arm();
                // Each public native executor must own interruption, not just row reads.
                var pending = ExecuteShapeAsync();

                async Task ExecuteShapeAsync()
                {
                    var source = CosmicQuery<Holon>.Create();
                    switch (shape)
                    {
                        case "rows":
                            CheckCancelled(await provider.ExecuteCosmicQueryAsync(
                                DescendantPlan(fixture), cancellationToken: cancellation.Token));
                            break;
                        case "projection":
                            CheckCancelled(await provider.ExecuteCosmicProjectionAsync(
                                ProjectionPlan(provider, source.Select(h => h.Name).Result),
                                cancellationToken: cancellation.Token));
                            break;
                        case "scalar":
                            CheckCancelled(await provider.ExecuteCosmicScalarAsync(
                                ScalarPlan(source.Count().Result), cancellationToken: cancellation.Token));
                            break;
                        case "grouped":
                            CheckCancelled(await provider.ExecuteCosmicGroupedAsync(
                                GroupedPlan(source.GroupBy(h => h.Version)
                                    .Select(g => new { g.Key, Count = g.Count() }).Result),
                                cancellationToken: cancellation.Token));
                            break;
                        default:
                            Assert.Fail("Unknown interruption shape: " + shape);
                            break;
                    }
                }

                static void CheckCancelled<T>(OASISResult<T> result)
                {
                    Assert.IsTrue(result.IsError);
                    Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode);
                    Assert.IsFalse(result.IsLoaded);
                    Assert.IsTrue(EqualityComparer<T>.Default.Equals(default!, result.Result),
                        "Cancellation must not publish even a partial result.");
                }
                bool interrupted;
                Task closing = Task.CompletedTask;
                try
                {
                    await relay.Paused.WaitAsync(TimeSpan.FromSeconds(10));
                    cancellation.Cancel();
                    if (abortOwnedDriver)
                    {
                        // This driver belongs exclusively to this probe, never
                        // the fixture writer or a concurrent provider request.
                        closing = driver.CloseAsync();
                        await closing.WaitAsync(TimeSpan.FromSeconds(3));
                    }
                    interrupted = await Task.WhenAny(pending, Task.Delay(1000)) == pending;
                }
                finally
                {
                    // Release the exact held bytes even on failure. Always join
                    // the native read before deactivation and fixture cleanup.
                    relay.Release();
                    await pending.WaitAsync(TimeSpan.FromSeconds(10));
                    await closing.WaitAsync(TimeSpan.FromSeconds(10));
                }
                await pending;
                Assert.IsTrue(interrupted,
                    "Caller cancellation must finish while the actual Bolt response is still held.");
            }
            finally { await provider.DeActivateProviderAsync(); }
        });

    private sealed class PausedBoltRelay : IAsyncDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 17788);
        private readonly CancellationTokenSource stop = new();
        private readonly List<Task> sessions = new();
        private readonly TaskCompletionSource paused = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Task accepting;
        private int armed;
        internal Task Paused => paused.Task;
        internal void Arm() => Interlocked.Exchange(ref armed, 1);
        internal void Release() => resume.TrySetResult();

        internal PausedBoltRelay()
        {
            listener.Start(); // A foreign listener must fail; never choose another port.
            accepting = AcceptAsync();
        }

        private async Task AcceptAsync()
        {
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync(stop.Token);
                    sessions.Add(ForwardAsync(client));
                }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        }

        private async Task ForwardAsync(TcpClient client)
        {
            using (client)
            using (var upstream = new TcpClient())
            using (var sessionStop = CancellationTokenSource.CreateLinkedTokenSource(stop.Token))
            {
                await upstream.ConnectAsync(IPAddress.Loopback, 17687, sessionStop.Token);
                var upload = PumpAsync(client.GetStream(), upstream.GetStream(), false, sessionStop.Token);
                var download = PumpAsync(upstream.GetStream(), client.GetStream(), true, sessionStop.Token);
                await Task.WhenAny(upload, download);
                sessionStop.Cancel();
                await Task.WhenAll(upload, download);
            }
        }

        private async Task PumpAsync(NetworkStream source, NetworkStream target, bool serverResponse,
            CancellationToken cancellation)
        {
            var buffer = new byte[8192];
            try
            {
                while (true)
                {
                    var count = await source.ReadAsync(buffer, cancellation);
                    if (count == 0) return;
                    if (serverResponse && Interlocked.CompareExchange(ref armed, 0, 1) == 1)
                    {
                        paused.TrySetResult();
                        await resume.Task.WaitAsync(cancellation);
                    }
                    await target.WriteAsync(buffer.AsMemory(0, count), cancellation);
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        }

        public async ValueTask DisposeAsync()
        {
            Release();
            stop.Cancel();
            listener.Stop();
            await accepting;
            await Task.WhenAll(sessions);
            stop.Dispose();
        }
    }
}
