using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.ChromaOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class ChromaNativeStorageConformanceTests
{
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstChroma(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable native Chroma CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        var provider = new ChromaOASIS("http://127.0.0.1:18001");
        var report = await CoreStorageConformance.RunAsync(provider, provider.ProviderName, "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            },
            (id, sync) => sync ? Task.FromResult(provider.DeleteHolonSoft(id, false)) : provider.DeleteHolonSoftAsync(id, false),
            expectedDefaultDeletion: "soft",
            observeNativeTombstone: async (id, owner, name) =>
            {
                using var http = new HttpClient();
                const string collections = "http://127.0.0.1:18001/api/v2/tenants/default_tenant/databases/default_database/collections";
                using var described = JsonDocument.Parse(await http.GetStringAsync(collections + "/oasis_holons"));
                var cid = described.RootElement.GetProperty("id").GetString();
                using var response = await http.PostAsync($"{collections}/{cid}/get",
                    new StringContent(JsonSerializer.Serialize(new { ids = new[] { id.ToString() }, include = new[] { "documents" } }), System.Text.Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var docs = doc.RootElement.GetProperty("documents");
                if (docs.GetArrayLength() == 0) return false;
                var raw = docs[0].GetString()!;
                StringAssert.Contains(raw, name);
                StringAssert.Contains(raw, owner.ToString("D"));
                StringAssert.Contains(raw, "DeletedDate");
                return true;
            });

        if (!report.Succeeded)
        {
            var probes = new (string, Func<Task<string>>)[]
            {
                ("avatar", async () => (await provider.LoadAvatarAsync(report.OwnerId)).Message),
                ("detail", async () => (await provider.LoadAvatarDetailAsync(report.OwnerId)).Message),
                ("holon", async () => (await provider.LoadHolonAsync(report.RootId, loadChildren: false)).Message),
                ("all", async () => (await provider.LoadAllHolonsAsync(loadChildren: false)).Message),
            };
            foreach (var (name, probe) in probes)
            {
                try { Console.WriteLine($"Diagnose({name}): {await probe()}"); }
                catch (Exception error) { Console.WriteLine($"Diagnose({name}): {error.GetType().Name} {error.Message}"); }
            }
        }
        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
    }
}
