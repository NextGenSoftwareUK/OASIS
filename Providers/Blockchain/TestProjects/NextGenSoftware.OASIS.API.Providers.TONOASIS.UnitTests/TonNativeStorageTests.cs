using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS.UnitTests;

[TestClass]
[DoNotParallelize]
public class TonNativeStorageTests
{
    // This probe exercises the shared canonical save/load and encryption owner.
    // It is not a second shipping TON provider or a claim of CQL completion.
    private sealed class Probe(PersistedPrivateKeyValueBackend backend) : KeyValueStorageProviderBase(backend)
    {
        public void Identify()
        {
            ProviderName = "Native TON storage probe";
            ProviderType = new(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TONOASIS);
        }
    }

    [TestMethod]
    public async Task CanonicalEncryptedHolonsSurviveIndependentNativeWorkersAndReadOnlyReopen()
    {
        var root = Environment.GetEnvironmentVariable("OASIS_TON_NATIVE_ROOT");
        Assert.IsFalse(string.IsNullOrWhiteSpace(root), "Use the explicit native runner.");
        var directory = Path.Combine(root!, "TestResults/TonCqlCurrent/csharp-store-tests");
        Assert.IsFalse(Directory.Exists(directory), "Preserve an unfinished fixture; never reset it.");
        Directory.CreateDirectory(directory);
        var options = new TonSandboxStorageOptions(
            Environment.GetEnvironmentVariable("OASIS_TON_NODE")!,
            Path.Combine(root!, "Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TONOASIS/SdkBridge/native-worker.cjs"),
            Path.Combine(root!, "TestResults/TonCqlCurrent/compiled/TonHolonStore_TonHolonStore.cjs"),
            directory, Guid.NewGuid(), Environment.GetEnvironmentVariable("OASIS_TON_CODE_HASH")!, ReadOnly: false);
        var native = new TonSandboxStorageBackend(options);
        var created = await native.CreateSourceAsync();
        var creationId = created.GetProperty("operationId").GetGuid();
        // Public deterministic test material; no real credential is created.
        var key = SHA256.HashData(Encoding.UTF8.GetBytes("COSMIC native TON synthetic storage test key"));
        var actor = Guid.NewGuid();
        var rows = new[] { "Espresso ☕", "Latte", "Tea" }.Select(name => new Holon {
            Id = Guid.NewGuid(), Name = name, Description = "Native TVM canonical storage fixture", CreatedByAvatarId = actor
        }).ToArray();
        using var encrypted = PersistedPrivateKeyValueBackend.Create(native, () => (byte[])key.Clone());
        var provider = new Probe(encrypted) { IsVersionControlEnabled = false };
        provider.Identify();
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        try
        {
            foreach (var row in rows)
            {
                var saved = await provider.SaveHolonAsync(row, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
            }
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }

        var physicalKeys = await native.ListKeysAsync(string.Empty);
        Assert.AreEqual(3, physicalKeys.Count);
        foreach (var physical in physicalKeys)
        {
            StringAssert.StartsWith(physical, "oasis/private/v1/");
            var ciphertext = await native.GetAsync(physical);
            Assert.IsFalse(ciphertext.Contains("Espresso") || ciphertext.Contains("Native TVM canonical"));
        }
        var source = Path.Combine(directory, "source.json");
        var before = SHA256.HashData(await File.ReadAllBytesAsync(source));
        var acknowledgements = Directory.GetFiles(Path.Combine(directory, "operations"));
        Assert.AreEqual(4, acknowledgements.Length);
        var reader = new TonSandboxStorageBackend(options with { ReadOnly = true });
        using var protectedReader = PersistedPrivateKeyValueBackend.Create(reader, () => (byte[])key.Clone());
        var reopened = new Probe(protectedReader);
        reopened.Identify();
        Assert.IsFalse((await reopened.ActivateProviderAsync()).IsError);
        try
        {
            foreach (var row in rows)
            {
                var loaded = await reopened.LoadHolonAsync(row.Id, loadChildren: false);
                Assert.IsFalse(loaded.IsError, loaded.Message + loaded.Exception);
                Assert.AreEqual(row.Id, loaded.Result.Id);
                Assert.AreEqual(row.Name, loaded.Result.Name);
                Assert.AreEqual(actor, loaded.Result.CreatedByAvatarId);
            }
            var denied = await reopened.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Name = "must not write" }, saveChildren: false);
            Assert.IsTrue(denied.IsError);
            Assert.IsFalse(denied.IsSaved);
        }
        finally { Assert.IsFalse((await reopened.DeActivateProviderAsync()).IsError); }
        CollectionAssert.AreEqual(before, SHA256.HashData(await File.ReadAllBytesAsync(source)));
        Assert.AreEqual(4, Directory.GetFiles(Path.Combine(directory, "operations")).Length);
        Assert.AreEqual("Acknowledged", (await reader.InspectOperationAsync(creationId)).GetProperty("status").GetString());
        using (var cancelled = new CancellationTokenSource())
        {
            cancelled.Cancel();
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => reader.GetAsync(physicalKeys[0], cancelled.Token));
        }
        using (var otherOwner = new FileStream(Path.Combine(directory, "source.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            await Assert.ThrowsExceptionAsync<IOException>(() => reader.GetAsync(physicalKeys[0]));
        using (var wrongKey = PersistedPrivateKeyValueBackend.Create(reader, () => new byte[32]))
            await Assert.ThrowsExceptionAsync<InvalidDataException>(() => wrongKey.VerifyAsync());
        CollectionAssert.AreEqual(before, SHA256.HashData(await File.ReadAllBytesAsync(source)));

        // Only this newly created synthetic fixture is disposable. Validate its
        // exact source/operation ownership before deleting named files.
        Assert.IsFalse(File.Exists(Path.Combine(directory, "pending.json")));
        using var state = JsonDocument.Parse(await File.ReadAllTextAsync(source));
        Assert.AreEqual(options.SourceId, state.RootElement.GetProperty("sourceId").GetGuid());
        foreach (var file in acknowledgements)
        {
            using var ack = JsonDocument.Parse(await File.ReadAllTextAsync(file));
            Assert.AreEqual(options.SourceId, ack.RootElement.GetProperty("sourceId").GetGuid());
            File.Delete(file);
        }
        Directory.Delete(Path.Combine(directory, "operations"));
        File.Delete(source);
        File.Delete(Path.Combine(directory, "source.lock"));
        Directory.Delete(directory);
        CryptographicOperations.ZeroMemory(key);
    }
}
