using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

public partial class CosmosNativeStorageTests
{
    [DataTestMethod, DataRow(false), DataRow(true), TestCategory("CosmosCoreStorageConformance")]
    public async Task SharedStorageInterfaceAgainstRetainedCosmosEmulator(bool sync)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var work = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent", "AzureCosmosDBOASIS", sync ? "sync" : "async");
        Directory.CreateDirectory(work);
        var reportPath = Path.Combine(work, "report.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"), "Preserve original Cosmos fixture/report before another run.");
        using var lease = new FileStream(Path.Combine(work, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using (var file = new FileStream(reportPath + ".next", FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { await JsonSerializer.SerializeAsync(file, report); file.Flush(true); }
            File.Move(reportPath + ".next", reportPath, overwrite: true);
        }
        AzureCosmosDBOASIS provider;
        try
        {
            if (Environment.GetEnvironmentVariable("OASIS_COSMOSDB_NATIVE") != "1" ||
                Environment.GetEnvironmentVariable("OASIS_COSMOSDB_WORK") != Path.Combine(root.FullName, "TestResults", "CosmosDBCurrent"))
                throw new InvalidOperationException("Retained local emulator runner required; no hosted-account fallback.");
            const string endpoint = "https://127.0.0.1:8081";
            var databaseName = "oasis-provider-conformance-" + (sync ? "sync" : "async");
            using var client = new CosmosClient(endpoint, EmulatorKey, new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway,
                RequestTimeout = TimeSpan.FromSeconds(10), MaxRetryAttemptsOnRateLimitedRequests = 0,
                HttpClientFactory = () => new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator }) });
            var database = client.GetDatabase(databaseName);
            var exists = true;
            try
            {
                await database.ReadAsync();
            }
            catch (CosmosException missing) when (missing.StatusCode == HttpStatusCode.NotFound) { exists = false; }
            if (exists)
            {
                using var containers = database.GetContainerQueryIterator<ContainerProperties>();
                while (containers.HasMoreResults)
                    foreach (var container in await containers.ReadNextAsync())
                    {
                        using var rows = database.GetContainer(container.Id).GetItemQueryIterator<int>("SELECT VALUE COUNT(1) FROM c");
                        while (rows.HasMoreResults)
                            if ((await rows.ReadNextAsync()).Any(count => count != 0))
                                throw new InvalidOperationException("Existing conformance records require review; no data reset or drop.");
                    }
            }
            provider = new AzureCosmosDBOASIS(new Uri(endpoint), EmulatorKey, databaseName, new List<string> { "avatarItems", "avatarDetailItems", "holonItems" });
        }
        catch (Exception failure)
        {
            await Checkpoint(new CoreStorageConformance.Report { Provider = "AzureCosmosDBOASIS", Mode = "native", Synchronous = sync,
                CurrentCase = "configuration", FailureType = failure.GetType().FullName! });
            Assert.Fail("Cosmos emulator configuration blocked; no storage mutation dispatched.");
            return;
        }
        var result = await CoreStorageConformance.RunAsync(provider, "AzureCosmosDBOASIS", "native", sync, Checkpoint);
        Assert.IsTrue(result.Succeeded, result.CurrentCase + ": " + result.FailureType);
    }
}
