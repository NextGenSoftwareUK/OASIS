using System.Globalization;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS.IntegrationTests;

/// <summary>
/// Runs the shared core storage suite against the enrolled record-box application on a disposable,
/// authenticated Algorand LocalNet (prepared by Scripts/TestHosts/Test-AlgorandRecordBoxes.py).
/// </summary>
[TestClass, DoNotParallelize]
public sealed class AlgorandNativeStorageConformanceTests
{
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");
    private static string Record(string name) => Env("OASIS_ALGORAND_RECORD_" + name);

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstAlgorand(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable native Algorand LocalNet CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        var provider = new AlgorandOASIS(
            "http://127.0.0.1:4001", "http://127.0.0.1:8980", new string('a', 64), null,
            new AlgorandRecordBoxSettings(
                ulong.Parse(Record("APP"), CultureInfo.InvariantCulture),
                Record("GENESIS"), Record("WRITER"), Record("PROGRAM"),
                Convert.FromBase64String(Record("KEY")),
                Convert.FromBase64String(Record("WRITER_SEED"))));

        var report = await CoreStorageConformance.RunAsync(provider, provider.ProviderName, "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            });

        if (!report.Succeeded)
        {
            var probes = new (string, Func<Task<string>>)[]
            {
                ("avatar", async () => (await provider.LoadAvatarAsync(report.OwnerId)).Message),
                ("detail", async () => (await provider.LoadAvatarDetailAsync(report.OwnerId)).Message),
                ("holon", async () => (await provider.LoadHolonAsync(report.RootId, loadChildren: false)).Message),
                ("all", async () => (await provider.LoadAllHolonsAsync(loadChildren: false)).Message),
                ("graph", async () =>
                {
                    var parent = new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = Guid.NewGuid(), Name = "probe", CreatedByAvatarId = report.OwnerId };
                    parent.Children.Add(new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = Guid.NewGuid(), Name = "probe-child", CreatedByAvatarId = report.OwnerId });
                    var saved = await provider.SaveHolonAsync(parent, continueOnError: false);
                    return $"error={saved.IsError} saved={saved.IsSaved} count={saved.SavedCount} {saved.Message} {saved.Exception}";
                }),
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
