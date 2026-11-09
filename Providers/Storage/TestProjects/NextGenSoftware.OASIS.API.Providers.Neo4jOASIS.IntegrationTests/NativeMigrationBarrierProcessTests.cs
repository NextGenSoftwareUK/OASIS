using System.Diagnostics;
using System.Text.Json;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Neo = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using Sqlite = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
public class NativeMigrationBarrierProcessTests
{
    private const string ContextFile = "barrier-host-context.json";
    internal static async Task VerifyProcessRestartAsync(string root, HolonReplicationMigrationAuthorizationContext context)
    {
        var state = Path.Combine(root, "LocalState/COSMIC/Neo4jCurrent");
        var path = Path.Combine(state, ContextFile);
        Assert.IsFalse(File.Exists(path), "Preserve any original host approval configuration.");
        using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(file, new { format = "owned-barrier-host-configuration-v1",
                context.MigrationId, context.OperatorId, context.SourceStorage, context.OriginalTargetStorage,
                context.OriginalSourceId, context.OriginalTargetId, replacementTarget = context.ReplacementRoute.TargetStorage,
                replacementPolicy = context.ReplacementRoute.PolicyBinding, context.PreviewDigest,
                context.AuthorizationPolicyBinding, context.ContextDigest,
                notBefore = DateTimeOffset.UtcNow.AddMinutes(-1), expiresAt = DateTimeOffset.UtcNow.AddMinutes(5) });
            file.Flush(true);
        }
        await RunChild(root, "acquire-crash", false);
        using var first = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "barrier-process-acquired.json")));
        Assert.AreEqual(context.MigrationId, first.RootElement.GetProperty("migration").GetGuid());
        Assert.AreEqual(context.ContextDigest, first.RootElement.GetProperty("contextDigest").GetString());
        var firstPid = first.RootElement.GetProperty("pid").GetInt32(); Assert.AreNotEqual(Environment.ProcessId, firstPid);
        await RunChild(root, "resume", true);
        using var second = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "barrier-process-resumed.json")));
        Assert.AreEqual(context.MigrationId, second.RootElement.GetProperty("migration").GetGuid());
        Assert.AreEqual(1, second.RootElement.GetProperty("heldBefore").GetInt32());
        Assert.AreEqual(1, second.RootElement.GetProperty("absentBefore").GetInt32());
        Assert.AreEqual(1, second.RootElement.GetProperty("acquired").GetInt32());
        Assert.AreNotEqual(firstPid, second.RootElement.GetProperty("pid").GetInt32());
        Assert.AreNotEqual(Environment.ProcessId, second.RootElement.GetProperty("pid").GetInt32());
    }
    private sealed class ExitAfterAcquire(IHolonMigrationFenceStorageProvider native, string state,
        HolonReplicationMigrationAuthorizationContext context) : IHolonMigrationFenceStorageProvider
    {
        public async Task<OASISResult<HolonMigrationFenceObservation>> AcquireHolonMigrationFenceAsync(HolonMigrationFenceRequest request, CancellationToken token = default)
        {
            var committed = await native.AcquireHolonMigrationFenceAsync(request, token);
            Assert.IsFalse(committed.IsError, committed.Message + committed.Exception);
            Assert.AreEqual(HolonMigrationFenceState.Held, committed.Result.State);
            using (var file = new FileStream(Path.Combine(state, "barrier-process-acquired.json"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(file, new { pid = Environment.ProcessId, migration = context.MigrationId,
                    contextDigest = context.ContextDigest, scope = request.Fingerprint }); file.Flush(true);
            }
            // Actual abrupt test-host termination after native commit, before its
            // coordinator receives an acknowledgement. No managed finally or dump.
            Process.GetCurrentProcess().Kill();
            throw new InvalidOperationException("Owned acquisition crash child survived termination.");
        }
        public Task<OASISResult<HolonMigrationFenceObservation>> ReadHolonMigrationFenceAsync(HolonMigrationFenceRequest request, CancellationToken token = default)
            => native.ReadHolonMigrationFenceAsync(request, token);
        public Task<OASISResult<HolonMigrationFenceObservation>> CancelHolonMigrationFenceAsync(HolonMigrationFenceRequest request, Guid owner, CancellationToken token = default)
            => throw new InvalidOperationException("Process recovery must never compensate uncertain scopes.");
    }
    [TestMethod]
    public async Task OwnedBarrierRecoveryChild()
    {
        var mode = Environment.GetEnvironmentVariable("OASIS_BARRIER_PROCESS_MODE");
        Assert.IsTrue(mode is "acquire-crash" or "resume", "Child-only acceptance requires explicit parent admission.");
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Run-ReusableNeo4jTests.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        var state = Path.Combine(root.FullName, "LocalState/COSMIC/Neo4jCurrent");
        using var configuration = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, ContextFile)));
        var c = configuration.RootElement;
        Assert.AreEqual("owned-barrier-host-configuration-v1", c.GetProperty("format").GetString());
        CosmicStorageIdentity Storage(string key)
        {
            var s = c.GetProperty(key);
            return new((ProviderType)s.GetProperty("Authority").GetInt32(), s.GetProperty("EndpointFingerprint").GetString()!, s.GetProperty("StoreId").GetString()!);
        }
        var sourceStorage = Storage("SourceStorage"); var targetStorage = Storage("OriginalTargetStorage");
        var replacementStorage = Storage("replacementTarget");
        Assert.AreEqual(ProviderType.Neo4jOASIS, sourceStorage.Authority);
        Assert.AreEqual(ProviderType.SQLLiteDBOASIS, targetStorage.Authority);
        Assert.AreEqual(targetStorage.BindingFingerprint, replacementStorage.BindingFingerprint);
        var context = new HolonReplicationMigrationAuthorizationContext(c.GetProperty("MigrationId").GetGuid(), c.GetProperty("OperatorId").GetGuid(),
            sourceStorage, targetStorage, c.GetProperty("OriginalSourceId").GetString()!, c.GetProperty("OriginalTargetId").GetString()!,
            new HolonReplicationRouteBinding(sourceStorage, replacementStorage, c.GetProperty("replacementPolicy").GetString()!),
            c.GetProperty("PreviewDigest").GetString()!, c.GetProperty("AuthorizationPolicyBinding").GetString()!);
        Assert.AreEqual(c.GetProperty("ContextDigest").GetString(), context.ContextDigest);
        var policy = new ScopedHolonReplicationMigrationPolicy(context.AuthorizationPolicyBinding, new[] {
            new HolonReplicationMigrationGrant(context, c.GetProperty("notBefore").GetDateTimeOffset(), c.GetProperty("expiresAt").GetDateTimeOffset()) });
        var database = Path.Combine(state, "source-receipt-target.db"); Assert.IsTrue(File.Exists(database));
        var source = new Neo(uri, "neo4j", "oasis-local-test-only");
        using var target = new Sqlite($"Data Source={database};Pooling=False");
        try
        {
            Assert.IsFalse((await source.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await target.ActivateProviderAsync()).IsError);
            var store = new FileHolonMigrationBarrierPlanStore(Path.Combine(state, "barrier-plans"));
            var persisted = await store.ReadAsync(context); Assert.IsFalse(persisted.IsError, persisted.Message + persisted.Exception);
            var participants = new[] { new HolonMigrationBarrierParticipant(sourceStorage, source), new HolonMigrationBarrierParticipant(targetStorage, target) }
                .OrderBy(p => p.Storage.BindingFingerprint, StringComparer.Ordinal).ToArray();
            var held = 0; var absent = 0;
            foreach (var scope in persisted.Result.Scopes)
            {
                var provider = participants.Single(p => p.Storage.BindingFingerprint == scope.ExpectedStorage.BindingFingerprint).Provider;
                var observed = await provider.ReadHolonMigrationFenceAsync(scope); Assert.IsFalse(observed.IsError, observed.Message);
                if (observed.Result.State == HolonMigrationFenceState.Held) held++;
                if (observed.Result.State == HolonMigrationFenceState.Absent) absent++;
            }
            if (mode == "acquire-crash")
            {
                Assert.AreEqual(0, held); Assert.AreEqual(2, absent);
                participants[0] = participants[0] with { Provider = new ExitAfterAcquire(participants[0].Provider, state, context) };
            }
            else { Assert.AreEqual(1, held); Assert.AreEqual(1, absent); }
            var recovered = await HolonReplicationMigrationBarrierCoordinator.ResumePersistedAsync(context, store, policy, participants);
            Assert.IsFalse(recovered.IsError, recovered.Message + recovered.Exception);
            Assert.AreEqual("resume", mode, "Crash child must terminate before returning an acquisition acknowledgement.");
            Assert.IsTrue(recovered.Result.AllBarriersHeld); Assert.AreEqual(1, recovered.SavedCount);
            using (var file = new FileStream(Path.Combine(state, "barrier-process-resumed.json"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(file, new { pid = Environment.ProcessId, migration = context.MigrationId,
                    heldBefore = held, absentBefore = absent, acquired = recovered.SavedCount }); file.Flush(true);
            }
        }
        finally { Assert.IsFalse((await source.DeActivateProviderAsync()).IsError); }
    }
    private static async Task RunChild(string root, string mode, bool success)
    {
        var work = Path.Combine(root, "TestResults/Neo4jCurrent");
        var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet/dotnet.exe"))
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("vstest"); start.ArgumentList.Add(typeof(NativeMigrationBarrierProcessTests).Assembly.Location);
        start.ArgumentList.Add("/ResultsDirectory:" + work);
        var report = "barrier-" + mode + "-child.trx";
        start.ArgumentList.Add("/Logger:trx;LogFileName=" + report);
        start.ArgumentList.Add("/TestCaseFilter:FullyQualifiedName=" + typeof(NativeMigrationBarrierProcessTests).FullName + ".OwnedBarrierRecoveryChild");
        start.Environment.Clear();
        foreach (var name in new[] { "SystemRoot", "WINDIR", "PATH", "USERPROFILE", "APPDATA", "LOCALAPPDATA", "ProgramFiles", "ProgramFiles(x86)",
            "ProgramData", "COMSPEC", "SYSTEMDRIVE", "HOMEDRIVE", "HOMEPATH", "DOTNET_ROOT", "DOTNET_CLI_HOME", "OASIS_OWNED_NEO4J_URI" })
            if (Environment.GetEnvironmentVariable(name) is { } value) start.Environment[name] = value;
        var temp = Path.Combine(work, "barrier-child-tmp"); Directory.CreateDirectory(temp);
        start.Environment["TEMP"] = temp; start.Environment["TMP"] = temp; start.Environment["OASIS_BARRIER_PROCESS_MODE"] = mode;
        var reportPath = Path.Combine(work, report); if (File.Exists(reportPath)) File.Delete(reportPath); // Exact regenerable prior child report.
        using var child = Process.Start(start)!;
        var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try { await child.WaitForExitAsync(timeout.Token); }
        finally
        {
            if (!child.HasExited) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(); }
            await File.WriteAllTextAsync(Path.Combine(work, "barrier-" + mode + "-child.log"), await stdout + await stderr);
        }
        if (!success) { Assert.AreNotEqual(0, child.ExitCode); return; }
        Assert.AreEqual(0, child.ExitCode, "New recovery process must complete its exact selected case.");
        var document = new XmlDocument(); document.Load(reportPath);
        var counters = document.SelectSingleNode("//*[local-name()='Counters']")!;
        Assert.AreEqual("1", counters.Attributes!["total"]!.Value); Assert.AreEqual("1", counters.Attributes["passed"]!.Value);
    }
}
