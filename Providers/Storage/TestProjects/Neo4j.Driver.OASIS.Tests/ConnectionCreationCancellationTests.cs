using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;

namespace Neo4j.Driver.Tests;

[TestClass]
public sealed class ConnectionCreationCancellationTests
{
    [TestMethod]
    public void SourceDriverPreservesShippingPublicIdentity()
        => Assert.AreEqual(
            "Neo4j.Driver, Version=4.3.2.2, Culture=neutral, PublicKeyToken=b646bc66d277ac07",
            typeof(GraphDatabase).Assembly.GetName().FullName);

    private sealed class HeldResolver : IHostResolver
    {
        internal readonly TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource<IPAddress[]> Addresses = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IPAddress[] Resolve(string hostname) => throw new InvalidOperationException("Synchronous DNS must not run.");

        public Task<IPAddress[]> ResolveAsync(string hostname, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            return Addresses.Task.WaitAsync(cancellationToken);
        }
    }

    [TestMethod]
    public async Task ClosedOwnerCannotStartResolutionOrCreateAnotherSocket()
    {
        var resolver = new HeldResolver();
        var client = new TcpSocketClient(new SocketSettings
        {
            HostResolver = resolver,
            EncryptionManager = new EncryptionManager(),
            ConnectionTimeout = TimeSpan.FromSeconds(30)
        });
        await client.DisconnectAsync();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            await client.ConnectAsync(new Uri("bolt://held.invalid:17687")));
        Assert.IsFalse(resolver.Started.Task.IsCompleted);
    }

    [TestMethod]
    public async Task CancelledCacheWaitDoesNotInterruptTheCurrentResolverOrPoisonReuse()
    {
        var resolver = new HeldResolver();
        var cache = new CachingHostResolver(resolver, 30);
        using var firstCancellation = new CancellationTokenSource();
        using var waitingCancellation = new CancellationTokenSource();
        var first = cache.ResolveAsync("first.invalid", firstCancellation.Token);
        await resolver.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var waiting = cache.ResolveAsync("second.invalid", waitingCancellation.Token);
        waitingCancellation.Cancel();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            await waiting.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.IsFalse(first.IsCompleted);
        firstCancellation.Cancel();
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            await first.WaitAsync(TimeSpan.FromSeconds(2)));

        resolver.Addresses.TrySetResult(new[] { IPAddress.Loopback });
        var fresh = await cache.ResolveAsync("fresh.invalid").WaitAsync(TimeSpan.FromSeconds(2));
        CollectionAssert.AreEqual(new[] { IPAddress.Loopback }, fresh);
    }

    [TestMethod]
    public async Task CancelledReadCannotReturnEvenCachedAddresses()
    {
        var resolver = new HeldResolver();
        resolver.Addresses.SetResult(new[] { IPAddress.Loopback });
        var cache = new CachingHostResolver(resolver, 30);
        await cache.ResolveAsync("cached.invalid");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            await cache.ResolveAsync("cached.invalid", cancellation.Token));
    }

    [TestMethod]
    public async Task DisconnectInterruptsTlsHandshakeBeforeServerResponds()
    {
        var listener = new TcpListener(IPAddress.Loopback, 17789);
        listener.Start(); // Foreign ownership must fail, never select another port.
        var resolver = new HeldResolver();
        resolver.Addresses.SetResult(new[] { IPAddress.Loopback });
        var client = new TcpSocketClient(new SocketSettings
        {
            HostResolver = resolver,
            EncryptionManager = new EncryptionManager(useTls: true, trustManager: null),
            ConnectionTimeout = TimeSpan.FromSeconds(30)
        });
        var connecting = client.ConnectAsync(new Uri("bolt+ssc://held.invalid:17789"));
        TcpClient? accepted = null;
        bool interrupted;
        try
        {
            accepted = await listener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(2));
            using var readBudget = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var hello = new byte[1];
            Assert.AreEqual(1, await accepted.GetStream().ReadAsync(hello, readBudget.Token));
            Assert.AreEqual(22, hello[0], "The real TLS ClientHello must precede interruption.");
            await client.DisconnectAsync();
            interrupted = await Task.WhenAny(connecting, Task.Delay(1000)) == connecting;
        }
        finally
        {
            accepted?.Dispose();
            listener.Stop();
            await client.DisconnectAsync();
            try { await connecting.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (Exception cause) when (cause is IOException or OperationCanceledException or SecurityException or ObjectDisposedException) { }
        }
        Assert.IsTrue(interrupted, "Closing a connection must join a held TLS authentication await.");
        Assert.IsFalse(connecting.IsCompletedSuccessfully);
    }

    [TestMethod]
    public async Task DisconnectInterruptsResolutionBeforeResolverRelease()
    {
        var resolver = new HeldResolver();
        var client = new TcpSocketClient(new SocketSettings
        {
            HostResolver = resolver,
            EncryptionManager = new EncryptionManager(),
            ConnectionTimeout = TimeSpan.FromSeconds(30)
        });
        var connecting = client.ConnectAsync(new Uri("bolt://held.invalid:17687"));
        bool interrupted;
        try
        {
            await resolver.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await client.DisconnectAsync();
            interrupted = await Task.WhenAny(connecting, Task.Delay(1000)) == connecting;
        }
        finally
        {
            // No real DNS, socket or test service is created. Join the original
            // operation even when the interruption assertion would fail.
            resolver.Addresses.TrySetResult(Array.Empty<IPAddress>());
            try { await connecting.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (Exception cause) when (cause is IOException or OperationCanceledException) { }
            await client.DisconnectAsync();
        }

        Assert.IsTrue(interrupted, "Connection shutdown must own an in-flight DNS resolution.");
    }
}
