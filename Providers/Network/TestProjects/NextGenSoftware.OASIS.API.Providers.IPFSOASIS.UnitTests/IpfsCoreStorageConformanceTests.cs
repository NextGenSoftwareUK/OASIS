using System.Text.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.IPFSOASIS.IPFSOASIS;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

// Uses the retained offline Kubo environment, not a mock transport or replacement repository.
[Collection("Owned IPFS")]
public class IpfsCoreStorageConformanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "IPFSCoreStorageConformance")]
    public async Task SharedStorageInterfaceAgainstRetainedIpfs(bool sync)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.NotNull(root);
        var work = Path.Combine(root!.FullName, "TestResults", "ProviderConformanceCurrent", "IPFSOASIS", sync ? "sync" : "async");
        Directory.CreateDirectory(work);
        var reportPath = Path.Combine(work, "report.json");
        Assert.False(File.Exists(reportPath) || File.Exists(reportPath + ".next"), "Preserve prior fixture/report before rerunning.");
        using var lease = new FileStream(Path.Combine(work, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using (var file = new FileStream(reportPath + ".next", FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { await JsonSerializer.SerializeAsync(file, report); file.Flush(true); }
            File.Move(reportPath + ".next", reportPath, overwrite: true);
        }
        Provider provider;
        try
        {
            const string endpoint = "http://127.0.0.1:18547";
            if (Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_RPC") != endpoint)
                throw new InvalidOperationException("Retained offline loopback Kubo runner required.");
            var repo = Environment.GetEnvironmentVariable("IPFS_PATH");
            var dnaPath = Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_DNA_PATH");
            if (string.IsNullOrWhiteSpace(repo) || !File.Exists(Path.Combine(repo, "config")) ||
                string.IsNullOrWhiteSpace(dnaPath) || Path.GetFileName(dnaPath) != "owned-dna.json" ||
                !string.Equals(Path.GetDirectoryName(Path.GetFullPath(dnaPath)), Path.GetDirectoryName(Path.GetFullPath(repo)), StringComparison.OrdinalIgnoreCase) ||
                File.Exists(dnaPath))
                throw new InvalidOperationException("Existing retained repository and unused owned fixture path required; no initialization or reset.");
            dnaPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(dnaPath))!, "conformance-" + (sync ? "sync" : "async") + "-dna.json");
            if (File.Exists(dnaPath)) throw new InvalidOperationException("Preserve the prior mode-specific IPFS fixture before rerunning.");
            var key = Convert.FromBase64String(Environment.GetEnvironmentVariable("OASIS_IPFS_STORAGE_ENCRYPTION_KEY") ?? "");
            try { if (key.Length != 32) throw new InvalidOperationException("Original persisted storage key required."); }
            finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(key); }
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { StorageProviders = new StorageProviderSettings {
                IPFSOASIS = new IPFSOASISSettings { ConnectionString = endpoint }
            } } };
            provider = new Provider(dna, dnaPath);
        }
        catch (Exception failure)
        {
            await Checkpoint(new CoreStorageConformance.Report { Provider = "IPFSOASIS", Mode = "native", Synchronous = sync,
                CurrentCase = "configuration", FailureType = failure.GetType().FullName! });
            Assert.True(false, "IPFS configuration blocked; no native storage operation dispatched. Restore the original retained repository/key.");
            return;
        }
        var priorDna = OASISDNAManager.OASISDNA;
        var priorPath = OASISDNAManager.OASISDNAPath;
        try
        {
            var result = await CoreStorageConformance.RunAsync(provider, "IPFSOASIS", "native", sync, Checkpoint);
            Assert.True(result.Succeeded, result.CurrentCase + ": " + result.FailureType);
        }
        finally { OASISDNAManager.OASISDNA = priorDna; OASISDNAManager.OASISDNAPath = priorPath; }
    }
}
