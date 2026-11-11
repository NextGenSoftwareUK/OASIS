using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class CosmosCoreStorageConformanceTests
{
    [DataTestMethod, DataRow(false), DataRow(true), TestCategory("CosmosCoreStorageConformance")]
    public async Task SharedStorageInterfaceAgainstRetainedContract(bool sync)
    {
        if (Environment.GetEnvironmentVariable("OASIS_COSMOS_STORAGE_CONFORMANCE") != "1")
            Assert.Inconclusive("Enable only for the retained isolated Cosmos storage fixture.");

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var fresh = Environment.GetEnvironmentVariable("OASIS_COSMOS_FRESH_CONFORMANCE") == "1";
        var work = Path.Combine(root.FullName, "TestResults", fresh ? "CosmosFreshConformance" : "ProviderConformanceCurrent",
            "CosmosBlockChainOASIS", sync ? "sync" : "async");
        Directory.CreateDirectory(work);
        using var lease = new FileStream(Path.Combine(Path.GetDirectoryName(work)!, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var reportPath = Path.Combine(work, "report.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"), "Preserve prior fixture/report before any new submission.");
        var otherReportPath = Path.Combine(Path.GetDirectoryName(work)!, sync ? "async" : "sync", "report.json");
        Assert.IsFalse(File.Exists(otherReportPath + ".next"), "Reconcile the other mode's interrupted checkpoint.");
        if (File.Exists(otherReportPath))
        {
            await using var previous = File.OpenRead(otherReportPath);
            var prior = await JsonSerializer.DeserializeAsync<CoreStorageConformance.Report>(previous);
            Assert.IsTrue(prior?.FixtureCleanupComplete == true || (!fresh && prior?.CurrentCase == "configuration"),
                "Reconcile the other mode's fixture before any new submission.");
        }

        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using (var file = new FileStream(reportPath + ".next", FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { await JsonSerializer.SerializeAsync(file, report); file.Flush(true); }
            File.Move(reportPath + ".next", reportPath, overwrite: true);
        }
        CosmosBlockChainOASIS provider;
        try
        {
            if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "CosmosSdkBridge", "node_modules", "@cosmjs", "stargate", "package.json")))
                throw new InvalidOperationException("Retained pinned Cosmos SDK installation is missing; no automatic install or native dispatch.");
            string Required(string name) => Environment.GetEnvironmentVariable(name) is string value && !string.IsNullOrWhiteSpace(value)
                ? value : throw new InvalidOperationException("Explicit retained local Cosmos configuration required.");
            const string endpoint = "http://127.0.0.1:26657";
            const string chain = "oasis-cosmos-local";
            if (Required("OASIS_COSMOS_TEST_RPC") != endpoint || Required("OASIS_COSMOS_TEST_CHAIN") != chain)
                throw new InvalidOperationException("Retained local chain required; no public-chain fallback.");
            provider = new CosmosBlockChainOASIS(endpoint, chain, Required("OASIS_COSMOS_TEST_KEY"), Required("OASIS_COSMOS_TEST_CONTRACT"),
                "wasm", "0.025stake", "stake", 0, CosmosBlockChainOASISIntegrationTests.LocalStorageKey);
        }
        catch (Exception failure) when (!fresh)
        {
            Assert.Inconclusive($"Cosmos fixture configuration blocked ({failure.GetType().Name}); no native storage operation dispatched.");
            return;
        }
        using (provider)
        {
            var result = await CoreStorageConformance.RunAsync(provider, "CosmosBlockChainOASIS", "native", sync, Checkpoint);
            Assert.IsTrue(result.Succeeded, $"{result.CurrentCase}: {result.FailureType} ({result.FailureContract})");
            Assert.IsTrue(result.FixtureCleanupComplete);
            Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
        }
    }
}
