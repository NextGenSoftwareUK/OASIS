using System.Net.Http.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class NearSharedLifecycleTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SharedStorageLifecycle(bool synchronous)
    {
        var root = Environment.GetEnvironmentVariable("OASIS_NEAR_LIFECYCLE_REPORT_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Explicitly admit a dedicated writable NEAR fixture and report directory.");

        var provider = new NEAROASIS(
            Required("NEAROASIS_RPCENDPOINT"), Required("NEAROASIS_NETWORKID"),
            Required("NEAROASIS_CHAINID"), Required("NEAROASIS_CONTRACTADDRESS"),
            Required("NEAROASIS_ACCOUNTID"), Required("NEAROASIS_PRIVATEKEY"));
        Directory.CreateDirectory(root!);
        using var lease = new FileStream(Path.Combine(root!, "environment.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var directory = Path.Combine(root!, synchronous ? "sync" : "async");
        Assert.IsFalse(Directory.Exists(directory), "Preserve the existing fixture evidence.");
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

        var result = await CoreStorageConformance.RunAsync(provider, nameof(NEAROASIS), "native", synchronous, Checkpoint, expectedDefaultDeletion: "absent");
        Assert.IsTrue(result.Succeeded, $"{result.CurrentCase}: {result.FailureType} {result.FailureContract}");
        Assert.IsTrue(result.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
    }

    [TestMethod]
    public async Task HardDeleteRemovesAvatarAndDetailHistory()
    {
        var root = Environment.GetEnvironmentVariable("OASIS_NEAR_LIFECYCLE_REPORT_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Explicitly admit a dedicated writable NEAR fixture and report directory.");

        Directory.CreateDirectory(root!);
        using var lease = new FileStream(Path.Combine(root!, "environment.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var id = Guid.NewGuid();
        var marker = "near-avatar-cleanup-" + id.ToString("N");
        using var journal = new FileStream(Path.Combine(root!, "avatar-history-cleanup.json"),
            FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(journal, new { id });
        journal.Flush(flushToDisk: true);

        var provider = new NEAROASIS(
            Required("NEAROASIS_RPCENDPOINT"), Required("NEAROASIS_NETWORKID"),
            Required("NEAROASIS_CHAINID"), Required("NEAROASIS_CONTRACTADDRESS"),
            Required("NEAROASIS_ACCOUNTID"), Required("NEAROASIS_PRIVATEKEY"));
        var activated = await provider.ActivateProviderAsync();
        Assert.IsTrue(!activated.IsError && activated.Result);
        try
        {
            var avatar = new Avatar { Id = id, Username = marker, Email = marker + "@example.invalid" };
            var detail = new AvatarDetail { Id = id, Username = marker, Email = avatar.Email, Karma = 1 };
            Assert.IsTrue((await provider.SaveAvatarAsync(avatar)).IsSaved);
            Assert.IsTrue((await provider.SaveAvatarDetailAsync(detail)).IsSaved);
            avatar.FirstName = marker;
            detail.Karma = 2;
            Assert.IsTrue((await provider.SaveAvatarAsync(avatar)).IsSaved);
            Assert.IsTrue((await provider.SaveAvatarDetailAsync(detail)).IsSaved);
            Assert.AreEqual(id, (await provider.LoadAvatarAsync(id, version: 1)).Result?.Id);
            Assert.AreEqual(1, (await provider.LoadAvatarDetailAsync(id, version: 1)).Result?.Karma);

            using var rpc = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            async Task<JsonValueKind> ReadNativeKey(string key)
            {
                using var response = await rpc.PostAsJsonAsync(Required("NEAROASIS_RPCENDPOINT"), new
                {
                    jsonrpc = "2.0", id = "avatar-cleanup", method = "query",
                    @params = new
                    {
                        request_type = "call_function", finality = "final",
                        account_id = Required("NEAROASIS_CONTRACTADDRESS"), method_name = "get",
                        args_base64 = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new { key }))
                    }
                });
                response.EnsureSuccessStatusCode();
                using var envelope = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var bytes = envelope.RootElement.GetProperty("result").GetProperty("result")
                    .EnumerateArray().Select(value => value.GetByte()).ToArray();
                using var record = JsonDocument.Parse(bytes);
                return record.RootElement.ValueKind;
            }

            var keys = new[] { $"avatar:{id:D}", $"avatar-detail:{id:D}",
                $"history:avatar:{id:D}:0000000001", $"history:avatar-detail:{id:D}:0000000001" };
            foreach (var key in keys)
                Assert.AreEqual(JsonValueKind.String, await ReadNativeKey(key), key);

            var deleted = await provider.DeleteAvatarAsync(id, softDelete: false);
            Assert.IsTrue(!deleted.IsError && deleted.Result && deleted.IsDeleted && deleted.DeletedCount == 1);
            foreach (var key in keys)
                Assert.AreEqual(JsonValueKind.Null, await ReadNativeKey(key), key);
            foreach (var version in new[] { 0, 1, 2 })
            {
                var missingAvatar = await provider.LoadAvatarAsync(id, version);
                var missingDetail = await provider.LoadAvatarDetailAsync(id, version);
                Assert.IsFalse(missingAvatar.IsError, missingAvatar.Message);
                Assert.IsFalse(missingDetail.IsError, missingDetail.Message);
                Assert.IsNull(missingAvatar.Result);
                Assert.IsNull(missingDetail.Result);
                Assert.IsFalse(missingAvatar.IsLoaded || missingDetail.IsLoaded);
            }
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }

    private static string Required(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException($"Missing {name}.");
    }
}
