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
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var work = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent", "CosmosBlockChainOASIS", sync ? "sync" : "async");
        Directory.CreateDirectory(work);
        var reportPath = Path.Combine(work, "report.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"), "Preserve prior fixture/report before any new submission.");
        using var lease = new FileStream(Path.Combine(work, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
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
        catch (Exception failure)
        {
            await Checkpoint(new CoreStorageConformance.Report { Provider = "CosmosBlockChainOASIS", Mode = "native", Synchronous = sync,
                CurrentCase = "configuration", FailureType = failure.GetType().FullName! });
            Assert.Fail("Cosmos configuration blocked; no native storage operation dispatched.");
            return;
        }
        using (provider)
        {
            var result = await CoreStorageConformance.RunAsync(provider, "CosmosBlockChainOASIS", "native", sync, Checkpoint);
            Assert.IsTrue(result.Succeeded, result.CurrentCase + ": " + result.FailureType);
        }
    }
}
