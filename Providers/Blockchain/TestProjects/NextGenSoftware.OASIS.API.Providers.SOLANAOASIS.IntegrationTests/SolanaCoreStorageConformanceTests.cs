using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Providers.TestSupport;
using System.Security.Cryptography;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class SolanaCoreStorageConformanceTests
{
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
