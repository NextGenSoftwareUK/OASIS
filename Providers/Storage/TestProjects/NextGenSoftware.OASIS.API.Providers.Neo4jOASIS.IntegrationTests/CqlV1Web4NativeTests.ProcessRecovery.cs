using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    private sealed record RestartIntent(ProviderType Authority, Guid Actor, Guid Operation,
        Guid Record, Guid? Revision, string Snapshot, string Sqlite);

    // The same frozen assembly serves both phases. Neither phase restores,
    // builds, seeds data, changes operation identity or cleans retained evidence.
    private static async Task RunRecoveryProcessPhase(string phase)
    {
        Assert.IsTrue(phase is "writer" or "reader");
        var work = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        var intentPath = Path.Combine(work, "cosmic-restart-intent.json");
        var intent = JsonSerializer.Deserialize<RestartIntent>(await File.ReadAllTextAsync(intentPath))!;
        const string mongo = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(mongo, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        Assert.AreEqual("bolt://127.0.0.1:17687", Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        Assert.AreEqual(Path.Combine(work, "cql-v1-web4.sqlite"), intent.Sqlite);

        IOASISStorageProvider provider = intent.Authority switch
        {
            ProviderType.MongoDBOASIS => new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(mongo, "cosmic_cql_v1_web4_storage"),
            ProviderType.SQLLiteDBOASIS => new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS("Data Source=" + intent.Sqlite),
            ProviderType.Neo4jOASIS => new Neo4jOASIS("bolt://127.0.0.1:17687", "neo4j", "oasis-local-test-only"),
            _ => throw new InvalidOperationException("Only the owned three-provider authority is admitted.")
        };
        var activated = await provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);
        var sources = new Sources(new() { [intent.Authority] = provider });
        var protection = new HolonMetadataEncryptionPolicy("owned-recovery-fixture", "fixture-key-v1", new RecoveryPreparationKeys());
        if (phase == "writer")
        {
            var boundary = DispatchProxyBoundary(provider, intent, work);
            var manager = sources.CreateManager(boundary);
            manager.PreparationEncryptionPolicy = protection;
            var root = StoredRecordJson.Deserialize<IHolon>(intent.Snapshot);
            Assert.AreEqual(intent.Record, root.Id);
            var result = await manager.DeleteHolonAsync(root, intent.Actor,
                new CosmicWriteOptions(intent.Operation, new Dictionary<Guid, Guid?> { [intent.Record] = intent.Revision }),
                false, intent.Authority);
            Assert.Fail("Writer must terminate at the native commit boundary: " + result.ErrorCode + " " + result.Message);
        }

        var recovered = await sources.CreateManager(provider).RecoverConditionalHolonWriteOutcomeAsync(
            intent.Operation, intent.Actor, intent.Authority, _ => protection,
            (name, assembly) => name == typeof(Holon).FullName && assembly == typeof(Holon).Assembly.GetName().Name
                ? typeof(Holon) : null!);
        Assert.IsFalse(recovered.IsError, recovered.Message + recovered.Exception);
        Assert.AreEqual(ConditionalHolonReceiptState.Committed, recovered.Result.ReceiptState);
        Assert.AreEqual(ConditionalHolonOperation.HardDelete, recovered.Result.Operation);
        Assert.AreEqual(intent.Operation, recovered.Result.OperationId);
        var absent = await provider.LoadHolonAsync(intent.Record, false, false, 0, false, false, 0);
        Assert.IsNull(absent.Result);
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
        await File.WriteAllTextAsync(Path.Combine(work, "cosmic-restart-reader.json"),
            JsonSerializer.Serialize(new { operation = intent.Operation, pid = Environment.ProcessId, recovered = true }));
    }

    private static IRecoveryMutationBoundary DispatchProxyBoundary(IOASISStorageProvider provider, RestartIntent intent, string work)
    {
        var boundary = System.Reflection.DispatchProxy.Create<IRecoveryMutationBoundary, RecoveryMutationBoundary>();
        var probe = (RecoveryMutationBoundary)(object)boundary;
        probe.Target = provider;
        probe.Authority = intent.Authority;
        probe.SqlitePath = intent.Sqlite;
        probe.AfterNativeCommit = () =>
        {
            // Flush the exact original identity before abrupt termination. Kill
            // avoids crash dumps and runs no managed cleanup/receipt replay.
            using (var file = new FileStream(Path.Combine(work, "cosmic-restart-committed.json"),
                FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            {
                JsonSerializer.Serialize(file, new { operation = intent.Operation, pid = Environment.ProcessId, commits = 1 });
                file.Flush(true);
            }
            Process.GetCurrentProcess().Kill();
            throw new InvalidOperationException("Unreachable after process termination.");
        };
        return boundary;
    }

    private static async Task<(int WriterPid, int ReaderPid)> VerifyProcessRestart(ConditionalHolonWriteRequest request,
        string sqlite, Func<string, object?, Task> journal)
    {
        var work = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        var paths = new[] { "cosmic-restart-intent.json", "cosmic-restart-committed.json", "cosmic-restart-reader.json" }
            .Select(name => Path.Combine(work, name)).ToArray();
        Assert.IsTrue(paths.All(path => !File.Exists(path)), "Preserve any unfinished process-restart evidence.");
        var root = request.Roots.Single();
        var intent = new RestartIntent(request.Write.Authority, request.Write.ActorId, request.Write.OperationId,
            root.Id, request.Write.Conditions.Single().ExpectedRevisionId, StoredRecordJson.Serialize(root), sqlite);
        await journal("before-process-interruption", intent);
        await File.WriteAllTextAsync(paths[0], JsonSerializer.Serialize(intent));

        async Task<int> Run(string phase)
        {
            var start = new ProcessStartInfo("dotnet") { UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            start.ArgumentList.Add("vstest");
            start.ArgumentList.Add(typeof(CqlV1Web4NativeTests).Assembly.Location);
            start.ArgumentList.Add("/ResultsDirectory:" + work);
            start.ArgumentList.Add("/Logger:trx;LogFileName=cosmic-restart-" + phase + ".trx");
            start.ArgumentList.Add("/TestCaseFilter:FullyQualifiedName=" + typeof(CqlV1Web4NativeTests).FullName
                + ".SignedHttpQueriesUseNativeVisibilityPagingAndDurableReceiptsOnAllThreeProviders");
            start.Environment["OASIS_COSMIC_RESTART_PHASE"] = phase;
            using var child = Process.Start(start)!;
            var stdout = child.StandardOutput.ReadToEndAsync();
            var stderr = child.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            try { await child.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                child.Kill(entireProcessTree: true);
                await child.WaitForExitAsync();
                throw new TimeoutException("Owned restart phase timed out; preserve its original intent and evidence.");
            }
            await File.WriteAllTextAsync(Path.Combine(work, "cosmic-restart-" + phase + ".log"),
                await stdout + await stderr);
            return child.ExitCode;
        }

        Assert.AreNotEqual(0, await Run("writer"), "The writer test host must terminate without an acknowledgement.");
        using var committed = JsonDocument.Parse(await File.ReadAllTextAsync(paths[1]));
        Assert.AreEqual(intent.Operation, committed.RootElement.GetProperty("operation").GetGuid());
        Assert.AreEqual(1, committed.RootElement.GetProperty("commits").GetInt32());
        Assert.AreEqual(0, await Run("reader"), "Fresh-process stored-only recovery must pass.");
        using var reader = JsonDocument.Parse(await File.ReadAllTextAsync(paths[2]));
        Assert.AreEqual(intent.Operation, reader.RootElement.GetProperty("operation").GetGuid());
        Assert.AreNotEqual(committed.RootElement.GetProperty("pid").GetInt32(), reader.RootElement.GetProperty("pid").GetInt32());
        await journal("process-restart-recovered", new { intent.Operation, intent.Authority,
            writerPid = committed.RootElement.GetProperty("pid").GetInt32(), readerPid = reader.RootElement.GetProperty("pid").GetInt32() });
        foreach (var path in paths) File.Delete(path);
        return (committed.RootElement.GetProperty("pid").GetInt32(), reader.RootElement.GetProperty("pid").GetInt32());
    }
}
