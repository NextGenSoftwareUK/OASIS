using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Providers.TestSupport;
using System.Security.Cryptography;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class SolanaCoreStorageConformanceTests
{
    [TestMethod, TestCategory("SolanaCoreStorageInspect")]
    public async Task InspectOriginalIdFailuresWithoutMutation()
    {
        var nativeWork = Environment.GetEnvironmentVariable("OASIS_SOLANA_NATIVE_EVIDENCE")!;
        var root = Directory.GetParent(Directory.GetParent(nativeWork)!.FullName)!.FullName;
        var evidencePath = Path.Combine(root, "Docs", "WIP", "HyperDrive-Providers", "NativeEvidence", "conformance-solanaoasis-current.json");
        using var registry = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "Docs", "WIP", "HyperDrive-Providers", "CONFORMANCE_RESULTS.json")));
        var registered = registry.RootElement.GetProperty("results").EnumerateArray().Single(row => row.GetProperty("provider").GetString() == "SOLANAOASIS");
        Assert.AreEqual(registered.GetProperty("sha256").GetString()!.ToUpperInvariant(), Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(evidencePath))));
        using var evidence = JsonDocument.Parse(await File.ReadAllTextAsync(evidencePath));
        // Registered, hash-verified evidence survives cleanup; disposable raw reports do not.
        var originalRuns = evidence.RootElement.GetProperty("runs").EnumerateArray().ToArray();
        Assert.AreEqual(2, originalRuns.Length);
        Assert.AreEqual(2, originalRuns.Select(run => run.GetProperty("Synchronous").GetBoolean()).Distinct().Count());
        var output = Path.Combine(nativeWork, "solana-core-read-diagnostic.json");
        Assert.IsFalse(File.Exists(output), "Preserve the previous diagnostic.");
        Assert.AreEqual("http://127.0.0.1:18899", Environment.GetEnvironmentVariable("SOLANA_RPC_URL"));
        var provider = new SolanaOASIS("http://127.0.0.1:18899", Environment.GetEnvironmentVariable("SOLANA_PRIVATE_KEY")!, Environment.GetEnvironmentVariable("SOLANA_PUBLIC_KEY")!);
        var rows = new List<object>();
        try
        {
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError);
            Assert.IsTrue(activation.Result);
            foreach (var run in originalRuns)
            {
                Assert.AreEqual("avatar-id", run.GetProperty("CurrentCase").GetString());
                var id = run.GetProperty("OwnerId").GetGuid();
                var mode = run.GetProperty("Synchronous").GetBoolean() ? "sync" : "async";
                var result = await provider.LoadAvatarAsync(id);
                // Only known static contract messages are classified; never dump provider/RPC messages or payloads.
                var known = new[] { "Solana finalized history transaction is unavailable; no partial collection returned.",
                    "Solana finalized history query failed; no partial collection returned.",
                    "Solana storage history contains conflicting or non-monotonic versions.",
                    "Solana storage history contains an untyped or unknown record.",
                    "No stored Solana record matches the requested identity." };
                rows.Add(new { Id = id, Mode = mode, result.IsError, result.IsLoaded, result.LoadedCount,
                    ReturnedId = result.Result?.Id, result.ErrorCode, ExceptionType = result.Exception?.GetType().FullName,
                    MissingMemo = result.Exception is NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories.SolanaMemoChunkUnavailableException missing
                        ? new { missing.RootSignature, missing.MissingSignature, missing.ChunkIndex, missing.ChunkCount, missing.WriteId } : null,
                    StaticCategory = known.FirstOrDefault(value => result.Exception?.Message == value) ?? "unclassified",
                    FailureMethods = result.Exception == null ? Array.Empty<string>() : (new System.Diagnostics.StackTrace(result.Exception, true).GetFrames() ?? Array.Empty<System.Diagnostics.StackFrame>())
                        .Where(frame => frame.GetMethod()?.DeclaringType?.FullName?.StartsWith("NextGenSoftware.OASIS.API.Providers.SOLANAOASIS", StringComparison.Ordinal) == true)
                        .Take(6).Select(frame => frame.GetMethod()!.DeclaringType!.FullName + "." + frame.GetMethod()!.Name + ":" + frame.GetFileLineNumber()).ToArray() });
            }
            Assert.AreEqual(2, rows.Count);
            await using var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(file, rows); file.Flush(true);
        }
        finally { await provider.DeActivateProviderAsync(); }
    }

    [DataTestMethod, DataRow(false), DataRow(true), TestCategory("SolanaCoreStorageConformance")]
    public async Task SharedStorageInterfaceAgainstRetainedValidator(bool sync)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var work = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent", "SOLANAOASIS", sync ? "sync" : "async");
        Directory.CreateDirectory(work);
        var reportPath = Path.Combine(work, "report.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"), "Preserve prior fixture/report before another submission.");
        using var lease = new FileStream(Path.Combine(work, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using (var file = new FileStream(reportPath + ".next", FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { await JsonSerializer.SerializeAsync(file, report); file.Flush(true); }
            File.Move(reportPath + ".next", reportPath, overwrite: true);
        }
        SolanaOASIS provider;
        try
        {
            var nativeWork = Environment.GetEnvironmentVariable("OASIS_SOLANA_NATIVE_EVIDENCE");
            if (string.IsNullOrWhiteSpace(nativeWork) ||
                Path.GetFullPath(nativeWork) != Path.Combine(root.FullName, "TestResults", "SolanaCurrent") ||
                Directory.EnumerateFiles(nativeWork, "*.pending.json").Any())
                throw new InvalidOperationException("Retained runner with no unresolved intent required.");
            const string endpoint = "http://127.0.0.1:18899";
            if (Environment.GetEnvironmentVariable("SOLANA_RPC_URL") != endpoint)
                throw new InvalidOperationException("Retained local validator required; no public-chain fallback.");
            var key = PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_SOLANA_STORAGE_ENCRYPTION_KEY");
            CryptographicOperations.ZeroMemory(key);
            string Required(string variable) => Environment.GetEnvironmentVariable(variable) is string value && !string.IsNullOrWhiteSpace(value)
                ? value : throw new InvalidOperationException("Existing protected test wallet required.");
            provider = new SolanaOASIS(endpoint, Required("SOLANA_PRIVATE_KEY"), Required("SOLANA_PUBLIC_KEY"));
        }
        catch (Exception failure)
        {
            await Checkpoint(new CoreStorageConformance.Report { Provider = "SOLANAOASIS", Mode = "native", Synchronous = sync,
                CurrentCase = "configuration", FailureType = failure.GetType().FullName! });
            Assert.Fail("Retained Solana configuration blocked; no native operation dispatched.");
            return;
        }
        var result = await CoreStorageConformance.RunAsync(provider, "SOLANAOASIS", "native", sync, Checkpoint);
        Assert.IsTrue(result.Succeeded, result.CurrentCase + ": " + result.FailureType);
    }
}
