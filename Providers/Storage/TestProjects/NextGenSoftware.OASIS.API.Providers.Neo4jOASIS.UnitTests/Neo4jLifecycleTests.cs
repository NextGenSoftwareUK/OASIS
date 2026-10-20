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
        public Func<IAsyncSession>? SessionFactory;
        public int Closes;
        public bool Closed;
        public Exception? VerifyFailure;
        public Exception? CloseFailure;
        public Exception? SchemaFailure;
        public long ConstraintKinds = 7;
        public int SchemaRuns;
        public int SchemaCreates;
        public bool InstallAfterCreates;
        public Exception? SchemaWriteFailure;
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
            if (method.Name == nameof(IDriver.AsyncSession))
            {
                if(SessionFactory!=null)return SessionFactory();
                var session=DispatchProxy.Create<IAsyncSession,SchemaSessionProbe>();
                ((SchemaSessionProbe)session).Owner=this;return session;
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

    public class SchemaSessionProbe : DispatchProxy
    {
        public DriverProbe Owner=null!;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name=="Dispose")return null;
            if(method.Name=="RunAsync")
            {
                Owner.SchemaRuns++;
                if(Owner.SchemaFailure!=null)return Task.FromException<IResultCursor>(Owner.SchemaFailure);
                if(args![0] is string query && query.StartsWith("CREATE CONSTRAINT",StringComparison.Ordinal))
                {
                    if(Owner.SchemaWriteFailure!=null)return Task.FromException<IResultCursor>(Owner.SchemaWriteFailure);
                    Owner.SchemaCreates++;
                    if(Owner.InstallAfterCreates && Owner.SchemaCreates==7)Owner.ConstraintKinds=7;
                }

                var cursor=DispatchProxy.Create<IResultCursor,SchemaCursorProbe>();
                ((SchemaCursorProbe)cursor).Count=Owner.ConstraintKinds;
                return Task.FromResult(cursor);
            }
            throw new InvalidOperationException("Unexpected schema session call: "+method.Name);
        }
    }
    public class SchemaCursorProbe : DispatchProxy
    {
        public long Count;private int reads;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name=="ConsumeAsync")return Task.FromResult<IResultSummary>(null!);
            if(method.Name=="FetchAsync")return Task.FromResult(reads++==0);
            if(method.Name=="get_Current")
            {
                var record=DispatchProxy.Create<IRecord,SchemaRecordProbe>();((SchemaRecordProbe)record).Count=Count;return record;
            }
            throw new InvalidOperationException("Unexpected schema cursor call: "+method.Name);
        }
    }
    public class SchemaRecordProbe : DispatchProxy
    {
        public long Count;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
            => method!.Name=="get_Item" && (string)args![0]! == "count" ? Count
                : throw new InvalidOperationException("Unexpected schema record call: "+method.Name);
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
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ActivationRequiresTheNativeSchemaAndPreservesFailure(bool synchronous)
    {
        var owned=NewDriver();var cause=new IOException("owned schema failure");owned.Probe.SchemaFailure=cause;
        var provider=new Provider("bolt://127.0.0.1:17687","owned","owned",()=>owned.Driver);
        var failed=synchronous ? provider.ActivateProvider() : await provider.ActivateProviderAsync();
        Assert.IsTrue(failed.IsError);Assert.AreSame(cause,failed.Exception);Assert.IsFalse(provider.IsProviderActivated);
        owned.Probe.SchemaFailure=null;owned.Probe.ConstraintKinds=1;
        var incomplete=synchronous ? provider.ActivateProvider() : await provider.ActivateProviderAsync();
        Assert.IsTrue(incomplete.IsError);Assert.IsInstanceOfType(incomplete.Exception,typeof(InvalidDataException));Assert.IsFalse(provider.IsProviderActivated);
        owned.Probe.ConstraintKinds=7;
        var active=synchronous ? provider.ActivateProvider() : await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError,active.Message);Assert.IsTrue(provider.IsProviderActivated);
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task SchemaBootstrapIsVerifiedAndExistingSchemaNeedsNoDdlPrivilege(bool synchronous)
    {
        var owned=NewDriver();owned.Probe.ConstraintKinds=0;owned.Probe.InstallAfterCreates=true;
        var provider=new Provider("bolt://127.0.0.1:17687","owned","owned",()=>owned.Driver);
        var initial=synchronous ? provider.ActivateProvider() : await provider.ActivateProviderAsync();
        Assert.IsFalse(initial.IsError,initial.Message);Assert.AreEqual(7,owned.Probe.SchemaCreates);
        owned.Probe.SchemaWriteFailure=new UnauthorizedAccessException("DDL privilege is not available after provisioning");
        var existing=synchronous ? provider.ActivateProvider() : await provider.ActivateProviderAsync();
        Assert.IsFalse(existing.IsError,existing.Message);Assert.AreEqual(7,owned.Probe.SchemaCreates);
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
