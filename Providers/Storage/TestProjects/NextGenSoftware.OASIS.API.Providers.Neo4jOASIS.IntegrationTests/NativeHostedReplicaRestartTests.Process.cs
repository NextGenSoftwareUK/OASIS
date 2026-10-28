extern alias ShippingWeb4;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Worker = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive.HolonReplicationDeliveryHostedService;
using Neo = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using Sqlite = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class NativeHostedReplicaRestartTests
{
    [TestMethod]
    public Task ShippingWorkerRecoversSameObligationAfterAbruptReceiverCommitProcessExit()
        => VerifyShippingWorkerRestart(true);

    public class ExitAfterReplicaCommit : DispatchProxy, IBoundCommittedHolonReplicaStorageProvider, ICosmicStorageIdentityProvider
    {
        public Sqlite Native = null!;
        public Guid ExpectedOperation;
        public Guid ExpectedActor;
        public string Boundary = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            try { return method!.Invoke(Native, args); }
            catch (TargetInvocationException error) when (error.InnerException != null)
            { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
        public Task<OASISResult<CosmicStorageIdentity>> CaptureStorageIdentityAsync(CancellationToken cancellationToken = default)
            => Native.CaptureStorageIdentityAsync(cancellationToken);
        public OASISResult<IEnumerable<IHolon>> ApplyBoundCommittedHolonReplica(CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage)
            => throw new InvalidOperationException("The shipping worker must use the async bound receiver.");
        public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Automatic delivery must remain bound to native storage.");
        public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
            => throw new InvalidOperationException("The shipping delivery worker uses the async receiver contract.");
        public async Task<OASISResult<IEnumerable<IHolon>>> ApplyBoundCommittedHolonReplicaAsync(
            CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken = default)
        {
            Assert.AreEqual(ExpectedOperation, request.OperationId);
            Assert.AreEqual(ExpectedActor, request.ActorId);
            var committed = await Native.ApplyBoundCommittedHolonReplicaAsync(request, expectedStorage, cancellationToken);
            Assert.IsFalse(committed.IsError, committed.Message + committed.Exception);
            var root = committed.Result.Single();
            using (var file = new FileStream(Boundary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(file, new { pid = Environment.ProcessId,
                    operation = request.OperationId, actor = request.ActorId, id = root.Id, revision = root.VersionId });
                file.Flush(true);
            }
            // An actual abrupt process exit: no managed finally/acknowledgement and
            // no FailFast dump. The parent owns and joins the vstest process tree.
            Process.GetCurrentProcess().Kill();
            throw new InvalidOperationException("Owned crash test host unexpectedly survived termination.");
        }
    }

    [TestMethod]
    public async Task OwnedReplicaCommitCrashChild()
    {
        Assert.AreEqual("receiver-commit-exit", Environment.GetEnvironmentVariable("OASIS_REPLICA_CRASH_CHILD"), "Child-only test requires explicit parent admission.");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        var state = Path.Combine(root.FullName, "LocalState/COSMIC/OwnedRoutingCurrent");
        using var intent = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "hosted-restart.pending.json")));
        var path = intent.RootElement.GetProperty("target").GetString()!;
        Assert.AreEqual(Path.Combine(root.FullName, "TestResults/Neo4jCurrent/hosted-restart-target.db"), path);
        Assert.IsTrue(File.Exists(path));
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var source = new Neo(uri, "neo4j", "oasis-local-test-only");
        using var native = new Sqlite($"Data Source={path};Pooling=False");
        Assert.IsFalse((await source.ActivateProviderAsync()).IsError);
        Assert.IsFalse((await native.ActivateProviderAsync()).IsError);
        var proxy = DispatchProxy.Create<IOASISStorageProvider, ExitAfterReplicaCommit>();
        var boundary = (ExitAfterReplicaCommit)proxy;
        boundary.Native = native;
        boundary.ExpectedActor = intent.RootElement.GetProperty("actor").GetGuid();
        boundary.ExpectedOperation = intent.RootElement.GetProperty("operation").GetGuid();
        boundary.Boundary = Path.Combine(state, "hosted-restart-committed.json");
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        var registry = new ProviderManager(null, dna);
        registry.RegisterProvider(source);
        registry.RegisterProvider(proxy);
        var logger = new PassLogger();
        using var worker = new Worker(logger, () => registry, () => true);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await worker.StartAsync(deadline.Token);
        await logger.Completed.Task.WaitAsync(deadline.Token);
        Assert.Fail("The receiver must terminate before a completed worker pass or source acknowledgement.");
    }

    private static async Task CrashAfterNativeCommit(string root, string boundary)
    {
        var work = Path.Combine(root, "TestResults/OwnedRoutingCurrent");
        var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet/dotnet.exe"))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("vstest");
        start.ArgumentList.Add(typeof(NativeHostedReplicaRestartTests).Assembly.Location);
        start.ArgumentList.Add("/ResultsDirectory:" + work);
        start.ArgumentList.Add("/Logger:trx;LogFileName=replica-crash-child.trx");
        start.ArgumentList.Add("/TestCaseFilter:FullyQualifiedName=" + typeof(NativeHostedReplicaRestartTests).FullName + ".OwnedReplicaCommitCrashChild");
        start.Environment.Clear();
        foreach (var name in new[] { "SystemRoot", "WINDIR", "PATH", "USERPROFILE", "APPDATA", "LOCALAPPDATA", "ProgramFiles", "ProgramFiles(x86)", "ProgramData", "COMSPEC", "SYSTEMDRIVE", "HOMEDRIVE", "HOMEPATH", "DOTNET_ROOT", "DOTNET_CLI_HOME", "OASIS_OWNED_NEO4J_URI" })
            if (Environment.GetEnvironmentVariable(name) is { } value) start.Environment[name] = value;
        var temp = Path.Combine(work, "child-tmp");
        Directory.CreateDirectory(temp);
        start.Environment["TEMP"] = temp;
        start.Environment["TMP"] = temp;
        start.Environment["OASIS_REPLICA_CRASH_CHILD"] = "receiver-commit-exit";
        using var child = Process.Start(start)!;
        var stdout = child.StandardOutput.ReadToEndAsync();
        var stderr = child.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try { await child.WaitForExitAsync(timeout.Token); }
        finally
        {
            if (!child.HasExited) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(); }
            await File.WriteAllTextAsync(Path.Combine(work, "replica-crash-child.log"), await stdout + await stderr);
        }
        Assert.AreNotEqual(0, child.ExitCode, "The child host must exit without completing the source outcome.");
        using var committed = JsonDocument.Parse(await File.ReadAllTextAsync(boundary));
        Assert.AreNotEqual(Environment.ProcessId, committed.RootElement.GetProperty("pid").GetInt32());
        Assert.IsTrue(committed.RootElement.GetProperty("pid").GetInt32() > 0);
    }
}
