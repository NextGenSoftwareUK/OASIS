using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlReadTransportPoolTests
{
    private static (IDriver Driver, Neo4jLifecycleTests.DriverProbe Probe) NewDriver()
    {
        var driver = DispatchProxy.Create<IDriver, Neo4jLifecycleTests.DriverProbe>();
        return (driver, (Neo4jLifecycleTests.DriverProbe)driver);
    }

    [TestMethod]
    public async Task SequentialReadsReuseTransportAndCancellationClosesOnlyItsOwner()
    {
        var drivers = new List<(IDriver Driver, Neo4jLifecycleTests.DriverProbe Probe)>();
        IDriver Create()
        {
            var driver = NewDriver();
            drivers.Add(driver);
            return driver.Driver;
        }
        await using var pool = new NeoCqlReadTransportPool(Create, 2);
        using var cancellation = new CancellationTokenSource();
        var first = await pool.AcquireAsync(cancellation.Token);
        var other = await pool.AcquireAsync(CancellationToken.None);
        cancellation.Cancel();
        await first.DisposeAsync();
        Assert.AreEqual(1, drivers[0].Probe.Closes);
        Assert.AreEqual(0, drivers[1].Probe.Closes);
        await other.DisposeAsync();

        var reused = await pool.AcquireAsync(CancellationToken.None);
        var replaced = await pool.AcquireAsync(CancellationToken.None);
        Assert.AreSame(drivers[1].Driver, replaced.Driver);
        Assert.AreNotSame(drivers[0].Driver, reused.Driver);
        await reused.DisposeAsync();
        await replaced.DisposeAsync();
    }

    [TestMethod]
    public async Task CapacityWaitIsCancellableWithoutClosingTheActiveOwner()
    {
        var driver = NewDriver();
        await using var pool = new NeoCqlReadTransportPool(() => driver.Driver, 1);
        var active = await pool.AcquireAsync(CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var waiting = pool.AcquireAsync(cancellation.Token);
        Assert.IsFalse(waiting.IsCompleted);
        cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () => await waiting);
        Assert.AreEqual(0, driver.Probe.Closes);
        await active.DisposeAsync();
    }

    [TestMethod]
    public async Task AbortedTransportIsJoinedBeforeItsCapacityIsReused()
    {
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var driver = NewDriver();
        driver.Probe.AllowClose = closed;
        var replacement = NewDriver();
        var created = 0;
        await using var pool = new NeoCqlReadTransportPool(
            () => Interlocked.Increment(ref created) == 1 ? driver.Driver : replacement.Driver, 1);
        using var cancellation = new CancellationTokenSource();
        var active = await pool.AcquireAsync(cancellation.Token);
        cancellation.Cancel();
        var disposing = active.DisposeAsync().AsTask();
        var waiting = pool.AcquireAsync(CancellationToken.None);
        Assert.IsFalse(disposing.IsCompleted);
        Assert.IsFalse(waiting.IsCompleted);
        closed.SetResult();
        await disposing;
        var next = await waiting.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreSame(replacement.Driver, next.Driver);
        await next.DisposeAsync();
    }

    [TestMethod]
    public async Task FailedDriverCreationReturnsCapacityForTheNextRead()
    {
        var replacement = NewDriver();
        var attempts = 0;
        await using var pool = new NeoCqlReadTransportPool(() =>
            Interlocked.Increment(ref attempts) == 1
                ? throw new InvalidOperationException("creation failed")
                : replacement.Driver, 1);

        var failure = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            async () => await pool.AcquireAsync(CancellationToken.None));
        Assert.AreEqual("creation failed", failure.Message);

        // A failed constructor must not permanently consume the single slot.
        await using var next = await pool.AcquireAsync(CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreSame(replacement.Driver, next.Driver);
    }

    [TestMethod]
    public async Task FailedCloseIsReportedAndTheRetiredTransportIsNeverReused()
    {
        var broken = NewDriver();
        broken.Probe.CloseFailure = new InvalidOperationException("close failed");
        var replacement = NewDriver();
        var attempts = 0;
        await using var pool = new NeoCqlReadTransportPool(() =>
            Interlocked.Increment(ref attempts) == 1 ? broken.Driver : replacement.Driver, 1);
        using var cancellation = new CancellationTokenSource();
        var active = await pool.AcquireAsync(cancellation.Token);
        cancellation.Cancel();

        var failure = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            async () => await active.DisposeAsync());
        Assert.AreEqual("close failed", failure.Message);

        await using var next = await pool.AcquireAsync(CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreSame(replacement.Driver, next.Driver);
        Assert.AreEqual(1, broken.Probe.Closes);
    }

    [TestMethod]
    public async Task ShutdownClosesActiveTransportAndRejectsNewReads()
    {
        var driver = NewDriver();
        var pool = new NeoCqlReadTransportPool(() => driver.Driver, 1);
        var active = await pool.AcquireAsync(CancellationToken.None);
        await pool.DisposeAsync();
        await active.DisposeAsync();
        await pool.DisposeAsync();
        Assert.AreEqual(1, driver.Probe.Closes);
        await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () =>
            await pool.AcquireAsync(CancellationToken.None));
    }
}
