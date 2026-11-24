using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class InfluxDBNativeStorageConformanceTests
{
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstInfluxDB(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable native InfluxDB CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        var provider = new InfluxDBOASIS("http://127.0.0.1:18086", Env("OASIS_NATIVE_PASSWORD"), "oasis", synchronous ? "oasis_sync" : "oasis_async");
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
                http.DefaultRequestHeaders.Add("Authorization", "Token " + Env("OASIS_NATIVE_PASSWORD"));
                var bucket = synchronous ? "oasis_sync" : "oasis_async";
                var flux = $"from(bucket: \"{bucket}\") |> range(start: 0) |> filter(fn: (r) => r._measurement == \"oasis_holons\" and r.id == \"{id:D}\" and r._field == \"data_json\") |> last()";
                using var response = await http.PostAsync("http://127.0.0.1:18086/api/v2/query?org=oasis",
                    new StringContent(flux, System.Text.Encoding.UTF8, "application/vnd.flux"));
                response.EnsureSuccessStatusCode();
                var csv = await response.Content.ReadAsStringAsync();
                if (!csv.Contains(id.ToString("D"))) return false;
                StringAssert.Contains(csv, name);
                StringAssert.Contains(csv, owner.ToString("D"));
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
