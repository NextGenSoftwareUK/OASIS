using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;

namespace NextGenSoftware.OASIS.API.Providers.GarnetOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class GarnetEmptyCheckpointTests
{
    [TestMethod]
    public async Task EmptyNativeCheckpointAcknowledgesActualStorageIoWithoutHolonWrites()
    {
        Assert.IsTrue(OperatingSystem.IsLinux());
        var state = Environment.GetEnvironmentVariable("OASIS_GARNET_STORAGE_PROBE_STATE")!;
        Assert.AreEqual("/var/tmp/oasis-garnet-storage-probe", state);
        Assert.IsTrue(File.Exists(Path.Combine(state, "active-process.json")));
        var options = new ConfigurationOptions
        {
            EndPoints = { "127.0.0.1:16492" },
            Password = Environment.GetEnvironmentVariable("OASIS_GARNET_NATIVE_PASSWORD")
                ?? throw new InvalidOperationException("Protected native credential required."),
            DefaultDatabase = 0, AllowAdmin = true, AbortOnConnectFail = true,
            ConnectRetry = 0, BacklogPolicy = BacklogPolicy.FailFast,
            ConnectTimeout = 3000, SyncTimeout = 5000, AsyncTimeout = 5000
        };
        using var client = await ConnectionMultiplexer.ConnectAsync(options);
        var server = client.GetServer("127.0.0.1", 16492);
        var info = (await server.InfoAsync("server")).SelectMany(group => group)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        StringAssert.Contains(info["garnet_version"], "2.2.5");
        Assert.AreEqual(0L, await server.DatabaseSizeAsync(0), "Probe must remain empty; never adopt a data source.");

        // SAVE is the only state-changing command here. No SET, holon save,
        // restoration, seed or replay is performed against either environment.
        var acknowledged = await server.ExecuteAsync("SAVE");
        Assert.AreEqual("OK", acknowledged.ToString());
        Assert.AreEqual(0L, await server.DatabaseSizeAsync(0));
        var directory = Path.Combine(state, "data", "checkpoints");
        var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
        Assert.IsTrue(files.Length is > 0 and <= 100);
        var total = files.Sum(file => new FileInfo(file).Length);
        Assert.IsTrue(total > 0 && total < 64L * 1024 * 1024, "Empty checkpoint must be real and bounded.");
        using var receipt = new FileStream(Path.Combine(state, "empty-checkpoint.json"),
            FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        JsonSerializer.Serialize(receipt, new
        {
            emptyBeforeAndAfter = true, acknowledged = true, nativeVersion = info["garnet_version"],
            totalBytes = total,
            files = files.ToDictionary(file => Path.GetRelativePath(directory, file),
                file => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))))
        });
        receipt.Flush(true);
    }
}
