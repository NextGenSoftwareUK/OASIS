using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jLifecycleTests
{
    public class DriverProbe : DispatchProxy
    {
        public int Verifications;
        public int Closes;
        public bool Closed;
        public Exception? VerifyFailure;
        public Exception? CloseFailure;
        public TaskCompletionSource? CloseStarted;
        public TaskCompletionSource? AllowClose;

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IDriver.VerifyConnectivityAsync))
            {
                Verifications++;
                return Closed ? Task.FromException(new ObjectDisposedException("owned driver"))
                    : VerifyFailure == null ? Task.CompletedTask : Task.FromException(VerifyFailure);
            }
            if (method.Name == nameof(IDriver.CloseAsync)) return CloseAsync();
            throw new InvalidOperationException("Unexpected driver call: " + method.Name);
        }

        private async Task CloseAsync()
        {
            Closes++;
            CloseStarted?.TrySetResult();
            if (AllowClose != null) await AllowClose.Task;
            if (CloseFailure != null) throw CloseFailure;
            Closed = true;
        }
    }

    private static (IDriver Driver, DriverProbe Probe) NewDriver()
    {
        var driver = DispatchProxy.Create<IDriver, DriverProbe>();
        return (driver, (DriverProbe)driver);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ReactivationUsesFreshVerifiedDriverAndRepeatedCloseIsIdempotent(bool synchronous)
    {
        var drivers = new List<(IDriver Driver, DriverProbe Probe)>();
        var provider = new Provider("bolt://127.0.0.1:17687", "owned", "owned", () => {
            var next = NewDriver(); drivers.Add(next); return next.Driver;
        });
        for (var cycle = 0; cycle < 3; cycle++)
        {
            var activated = synchronous ? provider.ActivateProvider() : await provider.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError, activated.Message);
            Assert.IsTrue(provider.IsProviderActivated);
            Assert.AreEqual(cycle + 1, drivers.Count);
            Assert.AreEqual(1, drivers[cycle].Probe.Verifications);
            var closed = synchronous ? provider.DeActivateProvider() : await provider.DeActivateProviderAsync();
            Assert.IsFalse(closed.IsError, closed.Message);
            Assert.IsFalse(provider.IsProviderActivated);
            Assert.IsTrue(drivers[cycle].Probe.Closed);
            Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
            Assert.AreEqual(1, drivers[cycle].Probe.Closes);
        }
    }

    [TestMethod]
    public async Task OriginalConnectivityAndCloseFailuresAreNotSuccess()
    {
        var owned = NewDriver();
        var cause = new IOException("owned connection failure");
        owned.Probe.VerifyFailure = cause;
        var provider = new Provider("bolt://127.0.0.1:17687", "owned", "owned", () => owned.Driver);
        var failed = await provider.ActivateProviderAsync();
        Assert.IsTrue(failed.IsError); Assert.AreSame(cause, failed.Exception);
        Assert.IsFalse(provider.IsProviderActivated);
        owned.Probe.VerifyFailure = null;
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        owned.Probe.CloseFailure = cause;
        var close = await provider.DeActivateProviderAsync();
        Assert.IsTrue(close.IsError); Assert.AreSame(cause, close.Exception);
        Assert.IsTrue(provider.IsProviderActivated, "Failed close cannot report successful deactivation.");
        owned.Probe.CloseFailure = null;
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }

    [TestMethod]
    public async Task ActivationWaitsForInFlightCloseBeforeCreatingAnotherDriver()
    {
        var first = NewDriver(); var second = NewDriver(); var created = 0;
        first.Probe.CloseStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        first.Probe.AllowClose = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new Provider("bolt://127.0.0.1:17687", "owned", "owned", () => ++created == 1 ? first.Driver : second.Driver);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var closing = provider.DeActivateProviderAsync();
        await first.Probe.CloseStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var activating = provider.ActivateProviderAsync();
        Assert.IsFalse(activating.IsCompleted);
        Assert.AreEqual(1, created);
        first.Probe.AllowClose.SetResult();
        Assert.IsFalse((await closing).IsError);
        Assert.IsFalse((await activating).IsError);
        Assert.AreEqual(2, created); Assert.AreEqual(1, second.Probe.Verifications);
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
