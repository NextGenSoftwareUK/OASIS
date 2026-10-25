using System.Diagnostics;
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
        => RunPausedReadAsync(abortOwnedDriver, warmConnection, shape, deadlineExpired: false);

    [DataTestMethod]
    [DataRow(false, "rows")]
    [DataRow(true, "rows")]
    [DataRow(false, "projection")]
    [DataRow(true, "projection")]
    [DataRow(false, "scalar")]
    [DataRow(true, "scalar")]
    [DataRow(false, "grouped")]
    [DataRow(true, "grouped")]
    public Task NativeCqlDeadlineInterruptsPausedBoltResponseBeforeRelease(bool warmConnection, string shape)
        => RunPausedReadAsync(false, warmConnection, shape, deadlineExpired: true);

    private static Task RunPausedReadAsync(bool abortOwnedDriver, bool warmConnection, string shape, bool deadlineExpired)
        => WithNativeGraph(async fixture =>
        {
            // Cold cases hold the initial handshake; warm cases hold a later
            // response. Both use the real server and preserve the held bytes.
            await using var relay = new PausedBoltRelay();
            IDriver CreateReadDriver() => GraphDatabase.Driver("bolt://127.0.0.1:17788",
                AuthTokens.Basic("neo4j", "oasis-local-test-only"),
                config => config.WithMaxConnectionPoolSize(1));
            using var driver = CreateReadDriver();
            // Transfer the prepared cold/warm driver exactly once. The pool owns
            // cancellation teardown; each later factory call creates a new owned
            // transport instead of returning the disposed prepared instance.
            IDriver? preparedDriver = driver;
            IDriver NextReadDriver() => Interlocked.Exchange(ref preparedDriver, null) ?? CreateReadDriver();
            using var storage = GraphDatabase.Driver("bolt://127.0.0.1:17687",
                AuthTokens.Basic("neo4j", "oasis-local-test-only"));
            var provider = new Provider("bolt://127.0.0.1:17687", "neo4j",
                "oasis-local-test-only", () => storage, NextReadDriver);
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            try
            {
                using var cancellation = new CancellationTokenSource();
                if (warmConnection) await driver.VerifyConnectivityAsync();
                relay.Arm();
                // Each public native executor must own interruption, not just row reads.
                var elapsed = Stopwatch.StartNew();
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
                        case "join":
                            CheckCancelled(await provider.ExecuteCosmicJoinAsync(
                                JoinPlan(source.Join(source,h=>h.Name,h=>h.Name).Select((l,r)=>l.Name).Result),
                                new CosmicJoinExecutionLimits(10,10,100,100,10000,10000,10000),cancellation.Token));
                            break;
                        case "composition":
                            CheckCancelled(await provider.ExecuteCosmicProjectionCompositionAsync(
                                CompositionPlan(source.Select(h => h.Name).Take(1).Result),
                                new CosmicProjectionCompositionExecutionLimits(10, 10000, 100, 10000, 10, 10000), cancellation.Token));
                            break;
                        default:
                            Assert.Fail("Unknown interruption shape: " + shape);
                            break;
                    }
                }

                void CheckCancelled<T>(OASISResult<T> result)
                {
                    Assert.IsTrue(result.IsError);
                    Assert.AreEqual(deadlineExpired ? "CQL_QUERY_TIMEOUT" : "CQL_QUERY_CANCELLED", result.ErrorCode);
                    if (deadlineExpired)
                    {
                        // Exercise the shipping 30-second budget, with no injected
                        // shortened timeout and no caller cancellation.
                        Assert.IsFalse(cancellation.IsCancellationRequested);
                        Assert.IsTrue(elapsed.Elapsed >= TimeSpan.FromSeconds(29));
                    }
                    Assert.IsFalse(result.IsLoaded);
                    Assert.IsTrue(EqualityComparer<T>.Default.Equals(default!, result.Result),
                        "Cancellation must not publish even a partial result.");
                }
                bool interrupted;
                Task closing = Task.CompletedTask;
                try
                {
                    await relay.Paused.WaitAsync(TimeSpan.FromSeconds(10));
                    if (!deadlineExpired) cancellation.Cancel();
                    if (abortOwnedDriver)
                    {
                        // This driver belongs exclusively to this probe, never
                        // the fixture writer or a concurrent provider request.
                        closing = driver.CloseAsync();
                        await closing.WaitAsync(TimeSpan.FromSeconds(3));
                    }
                    var maximumWait = deadlineExpired ? TimeSpan.FromSeconds(33) : TimeSpan.FromSeconds(1);
                    interrupted = await Task.WhenAny(pending, Task.Delay(maximumWait)) == pending;
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
                    "Cancellation or the native deadline must finish while the actual Bolt response is still held.");
                if (shape == "composition")
                {
                    // The same activated provider must admit a fresh read after
                    // its interrupted transport lease has been joined/released.
                    var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(1).Result;
                    var fresh = await provider.ExecuteCosmicProjectionCompositionAsync(CompositionPlan(query),
                        new CosmicProjectionCompositionExecutionLimits(10, 10000, 100, 10000, 10, 10000));
                    Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception);
                    var detached = CosmicProjectionCompositionResults.Materialize(query, fresh.Result);
                    Assert.IsFalse(detached.IsError, detached.Message);
                    Assert.AreEqual(1, detached.Result.Count);
                }
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
