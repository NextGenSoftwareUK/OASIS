using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jLifecycleTests
{
    private const int RequiredConstraintKinds = 8;
    public class DriverProbe : DispatchProxy
    {
        public int Verifications;
        public Func<IAsyncSession>? SessionFactory;
        public int Closes;
        public bool Closed;
        public Exception? VerifyFailure;
        public Exception? CloseFailure;
        public Exception? SchemaFailure;
        public long ConstraintKinds = RequiredConstraintKinds;
        public int SchemaRuns;
        public int SchemaCreates;
        public bool TargetIndexExists = true;
        public bool TargetIndexOnline = true;
        public bool TargetIndexShapeValid = true;
        public bool InstallTargetIndex = true;
        public string TargetIndexName = "oasis_cosmic_outbox_target_order";
        public int TargetIndexCreates;
        public int TargetIndexWaits;
        public Exception? TargetIndexWaitFailure;
        public bool InstallAfterCreates;
        public Exception? SchemaWriteFailure;
        public Exception? CqlUpgradeFailure;
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
            if(method.Name=="BeginTransactionAsync")
            {
                var transaction=DispatchProxy.Create<IAsyncTransaction,UpgradeTransactionProbe>();
                ((UpgradeTransactionProbe)transaction).Owner=Owner;
                return Task.FromResult(transaction);
            }
            if(method.Name=="RunAsync")
            {
                Owner.SchemaRuns++;
                if(Owner.SchemaFailure!=null)return Task.FromException<IResultCursor>(Owner.SchemaFailure);
                if(args![0] is string query && query.StartsWith("CREATE CONSTRAINT",StringComparison.Ordinal))
                {
                    if(Owner.SchemaWriteFailure!=null)return Task.FromException<IResultCursor>(Owner.SchemaWriteFailure);
                    Owner.SchemaCreates++;
                    if(Owner.InstallAfterCreates && Owner.SchemaCreates==RequiredConstraintKinds)Owner.ConstraintKinds=RequiredConstraintKinds;
                }

                if (args![0] is string indexQuery && indexQuery.StartsWith("CREATE RANGE INDEX", StringComparison.Ordinal))
                {
                    if (Owner.SchemaWriteFailure != null) return Task.FromException<IResultCursor>(Owner.SchemaWriteFailure);
                    Owner.TargetIndexCreates++;
                    if (Owner.InstallTargetIndex) Owner.TargetIndexExists = true;
                }
                if (args[0] is string awaitQuery && awaitQuery.StartsWith("CALL db.awaitIndex", StringComparison.Ordinal))
                {
                    Owner.TargetIndexWaits++;
                    if (Owner.TargetIndexWaitFailure != null) return Task.FromException<IResultCursor>(Owner.TargetIndexWaitFailure);
                    Owner.TargetIndexOnline = Owner.TargetIndexExists;
                }
                var cursor=DispatchProxy.Create<IResultCursor,SchemaCursorProbe>();
                var probe = (SchemaCursorProbe)cursor;
                probe.Count=Owner.ConstraintKinds;
                probe.Name=Owner.TargetIndexName;
                if (args[0] is string show && show.StartsWith("SHOW INDEXES", StringComparison.Ordinal))
                {
                    var exists = Owner.TargetIndexExists && Owner.TargetIndexShapeValid;
                    probe.Count = exists && Owner.TargetIndexOnline ? 1 : 0;
                    probe.Empty = show.Contains("RETURN name", StringComparison.Ordinal) && !exists;
                }
                return Task.FromResult(cursor);
            }
            throw new InvalidOperationException("Unexpected schema session call: "+method.Name);
        }
    }
    public class UpgradeTransactionProbe : DispatchProxy
    {
        public DriverProbe Owner=null!;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name is "Dispose")return null;
            if(method.Name is "CommitAsync" or "RollbackAsync")return Task.CompletedTask;
            if(method.Name=="RunAsync")
            {
                if(Owner.CqlUpgradeFailure!=null)return Task.FromException<IResultCursor>(Owner.CqlUpgradeFailure);
                var cursor=DispatchProxy.Create<IResultCursor,SchemaCursorProbe>();
                ((SchemaCursorProbe)cursor).Empty=true;
                return Task.FromResult(cursor);
            }
            throw new InvalidOperationException("Unexpected upgrade transaction call: "+method.Name);
        }
    }
    public class SchemaCursorProbe : DispatchProxy
    {
        public string Name = "oasis_cosmic_outbox_target_order";
        public long Count;public bool Empty;private int reads;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name=="ConsumeAsync")return Task.FromResult<IResultSummary>(null!);
            if(method.Name=="FetchAsync")return Task.FromResult(!Empty && reads++==0);
            if(method.Name=="get_Current")
            {
                var record=DispatchProxy.Create<IRecord,SchemaRecordProbe>();((SchemaRecordProbe)record).Count=Count;((SchemaRecordProbe)record).Name=Name;return record;
            }
            throw new InvalidOperationException("Unexpected schema cursor call: "+method.Name);
        }
    }
    public class SchemaRecordProbe : DispatchProxy
    {
        public long Count;
        public string Name = "oasis_cosmic_outbox_target_order";
        protected override object? Invoke(MethodInfo? method,object?[]? args)
            => method!.Name=="get_Item" ? (string)args![0]! switch {
                "count" => Count, "name" => Name,
                _ => throw new InvalidOperationException("Unexpected schema field.") }
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

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ProjectionUpgradeFailurePreventsActivation(bool synchronous)
    {
        var owned=NewDriver();
        var cause=new System.IO.InvalidDataException("owned corrupt historical query projection");
        owned.Probe.CqlUpgradeFailure=cause;
        var provider=new Provider("bolt://127.0.0.1:17687","owned","owned",()=>owned.Driver);
        var result=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();
        Assert.IsTrue(result.IsError);Assert.AreSame(cause,result.Exception);
        Assert.IsFalse(provider.IsProviderActivated);
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
        owned.Probe.ConstraintKinds=RequiredConstraintKinds;
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
        Assert.IsFalse(initial.IsError,initial.Message);Assert.AreEqual(RequiredConstraintKinds,owned.Probe.SchemaCreates);
        owned.Probe.SchemaWriteFailure=new UnauthorizedAccessException("DDL privilege is not available after provisioning");
        var existing=synchronous ? provider.ActivateProvider() : await provider.ActivateProviderAsync();
        Assert.IsFalse(existing.IsError,existing.Message);Assert.AreEqual(RequiredConstraintKinds,owned.Probe.SchemaCreates);
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task SevenPriorConstraintsCannotSatisfyTheDestinationCatalogueGate(bool synchronous)
    {
        var owned=NewDriver();owned.Probe.ConstraintKinds=7;
        var provider=new Provider("bolt://127.0.0.1:17687","owned","owned",()=>owned.Driver);
        var result=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();
        Assert.IsTrue(result.IsError);Assert.IsFalse(provider.IsProviderActivated);
        Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
        Assert.AreEqual(RequiredConstraintKinds,owned.Probe.SchemaCreates);
    }

}
