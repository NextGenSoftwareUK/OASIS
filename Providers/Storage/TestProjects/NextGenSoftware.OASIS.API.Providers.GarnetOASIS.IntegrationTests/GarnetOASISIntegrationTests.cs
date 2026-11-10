using NextGenSoftware.OASIS.API.Core.Holons;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;
using StackExchange.Redis;

namespace NextGenSoftware.OASIS.API.Providers.GarnetOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class GarnetOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SharedStorageLifecycle(bool synchronous)
    {
        var root = Environment.GetEnvironmentVariable("OASIS_GARNET_LIFECYCLE_REPORT_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Explicitly configure a dedicated Garnet instance and report directory.");

        var connection = Environment.GetEnvironmentVariable("GARNET_CONN");
        Assert.IsFalse(string.IsNullOrWhiteSpace(connection), "Configure the dedicated Garnet endpoint.");
        var options = ConfigurationOptions.Parse(connection!);
        Assert.AreEqual(1, options.EndPoints.Count, "Use one dedicated Garnet endpoint.");
        options.AllowAdmin = true;
        var directory = Path.Combine(root!, synchronous ? "sync" : "async");
        Assert.IsFalse(Directory.Exists(directory), "Preserve prior fixture evidence.");

        using var native = await ConnectionMultiplexer.ConnectAsync(options);
        var info = await native.GetServer(options.EndPoints[0]).InfoAsync("server");
        Assert.IsTrue(info.SelectMany(group => group).Any(pair => pair.Key == "garnet_version"),
            "The endpoint must identify itself as Garnet.");

        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "report.json");
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            var pending = path + ".pending";
            await using (var stream = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, report);
                stream.Flush(flushToDisk: true);
            }
            File.Move(pending, path, overwrite: true);
        }

        var provider = new GarnetOASIS(connection!);
        var result = await CoreStorageConformance.RunAsync(provider, nameof(GarnetOASIS), "native", synchronous, Checkpoint,
            (id, sync) => sync ? Task.FromResult(provider.DeleteHolonSoft(id, false)) : provider.DeleteHolonSoftAsync(id, false),
            expectedDefaultDeletion: "soft");
        Assert.IsTrue(result.Succeeded, $"{result.CurrentCase}: {result.FailureType} {result.FailureContract}");
        Assert.IsTrue(result.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RemoveOwnedTombstoneAndAvatarDetail(bool synchronous)
    {
        var root = Environment.GetEnvironmentVariable("OASIS_GARNET_LIFECYCLE_REPORT_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Configure a dedicated Garnet fixture and report directory.");

        var connection = Environment.GetEnvironmentVariable("GARNET_CONN");
        Assert.IsFalse(string.IsNullOrWhiteSpace(connection));
        var options = ConfigurationOptions.Parse(connection!);
        Assert.AreEqual(1, options.EndPoints.Count);
        options.AllowAdmin = true;
        using var native = await ConnectionMultiplexer.ConnectAsync(options);
        var server = native.GetServer(options.EndPoints[0]);
        var info = await server.InfoAsync("server");
        Assert.IsTrue(info.SelectMany(group => group).Any(pair => pair.Key == "garnet_version"));
        var database = native.GetDatabase(options.DefaultDatabase ?? 0);
        Assert.AreEqual(0L, await server.DatabaseSizeAsync(database.Database));

        var id = Guid.NewGuid();
        var holonId = Guid.NewGuid();
        var marker = "garnet-cleanup-" + id.ToString("N");
        var path = Path.Combine(root!, synchronous ? "cleanup-sync.json" : "cleanup-async.json");
        await using var journal = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(journal, new { id, holonId, synchronous });
        journal.Flush(flushToDisk: true);

        var provider = new GarnetOASIS(connection!);
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);
        Assert.IsTrue((await provider.SaveAvatarAsync(new Avatar { Id = id, Username = marker, Email = marker + "@example.invalid" })).IsSaved);
        Assert.IsTrue((await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = id, Username = marker, Email = marker + "@example.invalid" })).IsSaved);
        Assert.IsTrue((await provider.SaveHolonAsync(new Holon { Id = holonId, Name = marker, CreatedByAvatarId = id }, saveChildren: false)).IsSaved);

        var soft = synchronous ? provider.DeleteHolon(holonId) : await provider.DeleteHolonAsync(holonId);
        Assert.IsTrue(soft.IsDeleted);
        var hard = synchronous ? provider.DeleteHolonSoft(holonId, false) : await provider.DeleteHolonSoftAsync(holonId, false);
        Assert.IsTrue(!hard.IsError && hard.Result && hard.IsDeleted);
        var missing = synchronous ? provider.LoadHolon(holonId, loadChildren: false) : await provider.LoadHolonAsync(holonId, loadChildren: false);
        Assert.IsNull(missing.Result);
        Assert.IsFalse(missing.IsLoaded);
        var rows = synchronous ? provider.LoadAllHolons(loadChildren: false) : await provider.LoadAllHolonsAsync(loadChildren: false);
        Assert.IsFalse(rows.IsError, rows.Message);
        Assert.IsFalse(rows.Result.Any(row => row.Id == holonId));

        var removed = synchronous ? provider.DeleteAvatar(id, false) : await provider.DeleteAvatarAsync(id, false);
        Assert.IsTrue(!removed.IsError && removed.Result && removed.IsDeleted);
        var avatar = synchronous ? provider.LoadAvatar(id) : await provider.LoadAvatarAsync(id);
        var detail = synchronous ? provider.LoadAvatarDetail(id) : await provider.LoadAvatarDetailAsync(id);
        Assert.IsNull(avatar.Result);
        Assert.IsNull(detail.Result);
        Assert.IsFalse(avatar.IsLoaded || detail.IsLoaded);
        var inactive = synchronous ? provider.DeActivateProvider() : await provider.DeActivateProviderAsync();
        Assert.IsTrue(!inactive.IsError && inactive.Result);
        Assert.AreEqual(0L, await server.DatabaseSizeAsync(database.Database));
    }
}
