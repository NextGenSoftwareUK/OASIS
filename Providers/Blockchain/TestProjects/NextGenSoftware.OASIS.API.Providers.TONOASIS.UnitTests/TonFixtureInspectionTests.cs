using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS.UnitTests;

/// <summary>Explicit operator-selected teardown of this test's public synthetic
/// fixture. Never used for a demo source, pending operation or runtime retry.</summary>
[TestClass]
[DoNotParallelize]
public class TonFixtureInspectionTests
{
    [TestMethod]
    [TestCategory("ManualFixtureMaintenance")]
    public async Task VerifyCompleteSyntheticFixtureBeforeOwnedTeardown()
    {
        if (Environment.GetEnvironmentVariable("OASIS_TON_FIXTURE_INSPECTION") != "verify-complete-synthetic-fixture")
            Assert.Inconclusive("Fixture maintenance requires explicit operator selection; never run it as routine unit cleanup.");
        var root = Environment.GetEnvironmentVariable("OASIS_TON_NATIVE_ROOT")!;
        var directory = Path.Combine(root, "TestResults/TonCqlCurrent/csharp-store-tests");
        CollectionAssert.AreEquivalent(new[] { "operations", "source.json", "source.lock" },
            Directory.GetFileSystemEntries(directory).Select(Path.GetFileName).ToArray());
        Assert.IsFalse(File.Exists(Path.Combine(directory, "pending.json")));
        var bytes = await File.ReadAllBytesAsync(Path.Combine(directory, "source.json"));
        using var state = JsonDocument.Parse(bytes);
        var sourceId = state.RootElement.GetProperty("sourceId").GetGuid();
        var options = new TonSandboxStorageOptions(Environment.GetEnvironmentVariable("OASIS_TON_NODE")!,
            Path.Combine(root, "Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TONOASIS/SdkBridge/native-worker.cjs"),
            Path.Combine(root, "TestResults/TonCqlCurrent/compiled/TonHolonStore_TonHolonStore.cjs"), directory, sourceId,
            Environment.GetEnvironmentVariable("OASIS_TON_CODE_HASH")!);
        var native = new TonSandboxStorageBackend(options);
        await native.VerifyAsync();
        var key = SHA256.HashData(Encoding.UTF8.GetBytes("COSMIC native TON synthetic storage test key"));
        try
        {
            using var encrypted = PersistedPrivateKeyValueBackend.Create(native, () => (byte[])key.Clone());
            await encrypted.VerifyAsync();
            var keys = await encrypted.ListKeysAsync("oasis/holon/");
            Assert.AreEqual(3, keys.Count);
            var observed = new List<Holon>();
            foreach (var logical in keys)
            {
                var row = OasisJson.DeserializeStored<Holon>(await encrypted.GetAsync(logical));
                Assert.AreEqual("oasis/holon/" + row.Id.ToString("N"), logical);
                Assert.AreEqual("Native TVM canonical storage fixture", row.Description);
                Assert.AreEqual(1, row.Version);
                observed.Add(row);
            }
            CollectionAssert.AreEquivalent(new[] { "Espresso ☕", "Latte", "Tea" }, observed.Select(row => row.Name).ToArray());
            Assert.AreEqual(1, observed.Select(row => row.CreatedByAvatarId).Distinct().Count());
            Assert.AreNotEqual(Guid.Empty, observed[0].CreatedByAvatarId);
            var journals = Directory.GetFiles(Path.Combine(directory, "operations"));
            Assert.AreEqual(4, journals.Length);
            foreach (var file in journals)
            {
                using var ack = JsonDocument.Parse(await File.ReadAllBytesAsync(file));
                var operationId = ack.RootElement.GetProperty("operationId").GetGuid();
                Assert.AreEqual(operationId.ToString("D") + ".json", Path.GetFileName(file));
                Assert.AreEqual(sourceId, ack.RootElement.GetProperty("sourceId").GetGuid());
                Assert.AreEqual(options.ExpectedCodeHash, ack.RootElement.GetProperty("codeHash").GetString());
                Assert.AreEqual("Acknowledged", (await native.InspectOperationAsync(operationId)).GetProperty("status").GetString());
            }
            CollectionAssert.AreEqual(bytes, await File.ReadAllBytesAsync(Path.Combine(directory, "source.json")));
            foreach (var file in journals) File.Delete(file);
            Directory.Delete(Path.Combine(directory, "operations"));
            File.Delete(Path.Combine(directory, "source.json"));
            File.Delete(Path.Combine(directory, "source.lock"));
            Directory.Delete(directory);
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
