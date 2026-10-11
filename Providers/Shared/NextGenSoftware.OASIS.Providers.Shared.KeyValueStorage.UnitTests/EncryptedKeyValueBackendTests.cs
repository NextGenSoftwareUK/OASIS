using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class EncryptedKeyValueBackendTests
{
    [TestMethod]
    public async Task AuthenticatedCompressedRecordsRejectExpansionBombAndInvalidGzip()
    {
        var store = new InMemoryBackend();
        var key = RandomNumberGenerator.GetBytes(32);
        using var backend = new EncryptedKeyValueBackend(store, key, compress: true);
        byte[] compressed;
        using (var stream = new System.IO.MemoryStream())
        {
            using (var gzip = new System.IO.Compression.GZipStream(stream,
                System.IO.Compression.CompressionLevel.SmallestSize, leaveOpen: true))
                gzip.Write(new byte[4 * 1024 * 1024 + 1]);
            compressed = stream.ToArray();
        }
        foreach (var invalid in new[] { compressed, new byte[] { 1, 2, 3, 4, 5 } })
        {
            const string format = "oasis-aes256gcm-gzip-v2:";
            var payload = new byte[28 + invalid.Length];
            RandomNumberGenerator.Fill(payload.AsSpan(0, 12));
            using (var aes = new AesGcm(key, 16))
                aes.Encrypt(payload.AsSpan(0, 12), invalid, payload.AsSpan(28), payload.AsSpan(12, 16),
                    System.Text.Encoding.UTF8.GetBytes(format + "invalid"));
            store.Items["invalid"] = format + Convert.ToBase64String(payload);
            var original = store.Items["invalid"];
            await Assert.ThrowsExceptionAsync<System.IO.InvalidDataException>(() => backend.GetAsync("invalid"));
            Assert.AreEqual(original, store.Items["invalid"]);
        }
    }

    [TestMethod]
    public async Task CompressedRecordsPreserveFullUnicodeAndReadExistingVersionOne()
    {
        var store = new InMemoryBackend();
        var key = RandomNumberGenerator.GetBytes(32);
        using var original = new EncryptedKeyValueBackend(store, key);
        using var compressed = new EncryptedKeyValueBackend(store, key, compress: true);
        await original.PutAsync("old", "existing record 🌳");
        Assert.AreEqual("existing record 🌳", await compressed.GetAsync("old"));
        var full = string.Concat(Enumerable.Repeat("full avatar detail 🌳🌱", 10000));
        await compressed.PutAsync("full", full);
        Assert.IsTrue(store.Items["full"].StartsWith("oasis-aes256gcm-gzip-v2:"));
        Assert.IsTrue(store.Items["full"].Length < 10000);
        Assert.AreEqual(full, await original.GetAsync("full"));
        store.Items["moved"] = store.Items["full"];
        await Assert.ThrowsExceptionAsync<AuthenticationTagMismatchException>(() => compressed.GetAsync("moved"));
        store.Items["full"] = store.Items["full"].Replace("oasis-aes256gcm-gzip-v2:", "oasis-aes256gcm-v1:");
        await Assert.ThrowsExceptionAsync<AuthenticationTagMismatchException>(() => compressed.GetAsync("full"));
    }

    [TestMethod]
    public async Task OversizedCompressionInputCannotOverwriteExistingRecord()
    {
        var store = new InMemoryBackend();
        using var backend = new EncryptedKeyValueBackend(store, RandomNumberGenerator.GetBytes(32), compress: true);
        await backend.PutAsync("key", "preserve");
        var original = store.Items["key"];
        await Assert.ThrowsExceptionAsync<System.IO.InvalidDataException>(() => backend.PutAsync("key", new string('x', 4 * 1024 * 1024 + 1)));
        Assert.AreEqual(original, store.Items["key"]);
        Assert.AreEqual("preserve", await backend.GetAsync("key"));
    }

    [TestMethod]
    public async Task MalformedUnicodeIsRejectedWithoutReplacingOrOverwritingRecords()
    {
        foreach (var usePrivateIndexes in new[] { false, true })
        {
            var store = new InMemoryBackend();
            using var encrypted = new EncryptedKeyValueBackend(store, RandomNumberGenerator.GetBytes(32));
            using var privateBackend = new PrivateKeyValueBackend(store, RandomNumberGenerator.GetBytes(32));
            IKeyValueBackend backend = usePrivateIndexes ? privateBackend : encrypted;
            await backend.PutAsync("key/\uFFFD", "preserve");
            var original = store.Items.Single();
            foreach (var malformed in new[] { "key/\uD800", "key/\uDC00" })
            {
                await Assert.ThrowsExceptionAsync<System.Text.EncoderFallbackException>(() => backend.PutAsync(malformed, "bad"));
                await Assert.ThrowsExceptionAsync<System.Text.EncoderFallbackException>(() => backend.GetAsync(malformed));
                await Assert.ThrowsExceptionAsync<System.Text.EncoderFallbackException>(() => backend.DeleteAsync(malformed));
                await Assert.ThrowsExceptionAsync<System.Text.EncoderFallbackException>(() => backend.ListKeysAsync(malformed));
                await Assert.ThrowsExceptionAsync<System.Text.EncoderFallbackException>(() => backend.PutAsync("key/\uFFFD", malformed));
            }
            Assert.AreEqual(1, store.Items.Count);
            Assert.AreEqual(original.Value, store.Items[original.Key]);
            Assert.AreEqual("preserve", await backend.GetAsync("key/\uFFFD"));
            await backend.PutAsync("key/🌳", "valid supplementary Unicode 🌱");
            Assert.AreEqual("valid supplementary Unicode 🌱", await backend.GetAsync("key/🌳"));
        }
    }

    [TestMethod]
    public async Task ActivationWithWrongPersistentKeyFailsBeforeWritingNewRecords()
    {
        var store = new InMemoryBackend();
        using var backend = new PrivateKeyValueBackend(store, RandomNumberGenerator.GetBytes(32));
        await backend.PutAsync("existing", "preserve me");
        var physicalKey = store.Items.Keys.Single();
        var ciphertext = store.Items[physicalKey];
        using var wrongKey = new PrivateKeyValueBackend(store, RandomNumberGenerator.GetBytes(32));
        await Assert.ThrowsExceptionAsync<AuthenticationTagMismatchException>(() => wrongKey.VerifyAsync());
        Assert.AreEqual(1, store.Items.Count);
        Assert.AreEqual(ciphertext, store.Items[physicalKey]);
        Assert.AreEqual("preserve me", await backend.GetAsync("existing"));
    }

    [TestMethod]
    public async Task PrivateActivationProbeUsesEncryptedCrudAndRemovesItsOwnedRecord()
    {
        var store = new InMemoryBackend();
        using var backend = new PrivateKeyValueBackend(store, RandomNumberGenerator.GetBytes(32));
        await backend.PutAsync("existing", "preserve me");
        var physicalKey = store.Items.Keys.Single();
        var ciphertext = store.Items[physicalKey];
        await backend.VerifyAsync();
        Assert.AreEqual(1, store.Items.Count);
        Assert.AreEqual(ciphertext, store.Items[physicalKey]);
        Assert.AreEqual("preserve me", await backend.GetAsync("existing"));
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => backend.VerifyAsync(new System.Threading.CancellationToken(true)));
        Assert.AreEqual(1, store.Items.Count);
    }

    [TestMethod]
    public async Task ProviderCrudPreservesAvatarSecretsDetailsAndHolonVersionsThroughEncryption()
    {
        var store = new InMemoryBackend();
        using var encrypted = new PrivateKeyValueBackend(store, RandomNumberGenerator.GetBytes(32));
        var provider = new TestProvider(encrypted);
        provider.IsVersionControlEnabled = true;
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var avatar = new Avatar { Username = "encrypted-user", Email = "encrypted@example.test", Password = "sensitive-test-password" };
        var saved = await provider.SaveAvatarAsync(avatar);
        Assert.IsFalse(saved.IsError, saved.Message);
        var loaded = await provider.LoadAvatarByEmailAsync(avatar.Email);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.AreEqual(avatar.Password, loaded.Result.Password);
        Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = saved.Result.Id, Username = avatar.Username, Karma = 42 })).IsError);
        Assert.AreEqual(42L, Convert.ToInt64((await provider.LoadAvatarDetailByEmailAsync(avatar.Email)).Result.Karma));
        var holon = new Holon { Name = "private holon" };
        Assert.IsFalse((await provider.SaveHolonAsync(holon)).IsError);
        holon.Name = "updated private holon";
        Assert.IsFalse((await provider.SaveHolonAsync(holon)).IsError);
        var firstVersion = await provider.LoadHolonAsync(holon.Id, loadChildren: false, version: 1);
        Assert.IsFalse(firstVersion.IsError, firstVersion.Message);
        Assert.AreEqual("private holon", firstVersion.Result.Name);
        Assert.AreEqual("updated private holon", (await provider.LoadHolonAsync(holon.Id, loadChildren: false)).Result.Name);
        Assert.IsTrue(store.Items.Values.All(v => v.StartsWith("oasis-aes256gcm-v1:", StringComparison.Ordinal)));
        Assert.IsTrue(store.Items.Values.All(v => !v.Contains(avatar.Password)));
        Assert.IsTrue(store.Items.Keys.All(k => k.StartsWith("oasis/private/v1/", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task PrivateIndexesHideLogicalKeysAndRecoverThemForOrderedPrefixEnumeration()
    {
        var store = new InMemoryBackend();
        var key = RandomNumberGenerator.GetBytes(32);
        using (var backend = new PrivateKeyValueBackend(store, key))
        {
            await backend.PutAsync("email/private@example.test", "secret");
            await backend.PutAsync("email/é", "");
            await backend.PutAsync("other/key", "other");
            Assert.IsTrue(store.Items.Keys.All(k => k.Length == 81 && !k.Contains("email")));
            CollectionAssert.AreEqual(new[] { "email/private@example.test", "email/é" },
                (await backend.ListKeysAsync("email/")).ToArray());
            Assert.AreEqual("", await backend.GetAsync("email/é"));
            await backend.DeleteAsync("other/key");
            Assert.IsNull(await backend.GetAsync("other/key"));
        }
        using var restarted = new PrivateKeyValueBackend(store, key);
        Assert.AreEqual("secret", await restarted.GetAsync("email/private@example.test"));
        CollectionAssert.AreEqual(new[] { "email/private@example.test", "email/é" },
            (await restarted.ListKeysAsync("email/")).ToArray());
    }

    [TestMethod]
    public async Task PrivateIndexEnumerationRejectsCiphertextMovedToAnotherPhysicalKey()
    {
        var store = new InMemoryBackend();
        using var backend = new PrivateKeyValueBackend(store, RandomNumberGenerator.GetBytes(32));
        await backend.PutAsync("logical", "secret");
        store.Items["oasis/private/v1/" + new string('0', 64)] = store.Items.Values.Single();
        await Assert.ThrowsExceptionAsync<AuthenticationTagMismatchException>(() => backend.ListKeysAsync(""));
    }

    [TestMethod]
    public async Task RoundTripPreservesUnicodeEmptyAndMissingWithoutPlaintextStorage()
    {
        var store = new InMemoryBackend();
        using var encrypted = new EncryptedKeyValueBackend(store, RandomNumberGenerator.GetBytes(32));
        Assert.IsNull(await encrypted.GetAsync("missing"));
        await encrypted.PutAsync("record", "private é 🔐 record");
        Assert.AreEqual("private é 🔐 record", await encrypted.GetAsync("record"));
        Assert.IsFalse(store.Items["record"].Contains("private"));
        await encrypted.PutAsync("empty", "");
        Assert.AreEqual("", await encrypted.GetAsync("empty"));
        var prior = store.Items["record"];
        await encrypted.PutAsync("record", "private é 🔐 record");
        Assert.AreNotEqual(prior, store.Items["record"]);
    }

    [TestMethod]
    public async Task TamperingWrongKeyAndMovingCiphertextFailAuthentication()
    {
        var store = new InMemoryBackend();
        using var encrypted = new EncryptedKeyValueBackend(store, RandomNumberGenerator.GetBytes(32));
        await encrypted.PutAsync("record", "secret");
        store.Items["other"] = store.Items["record"];
        await Assert.ThrowsExceptionAsync<AuthenticationTagMismatchException>(() => encrypted.GetAsync("other"));
        using var wrongKey = new EncryptedKeyValueBackend(store, RandomNumberGenerator.GetBytes(32));
        await Assert.ThrowsExceptionAsync<AuthenticationTagMismatchException>(() => wrongKey.GetAsync("record"));
        var separator = store.Items["record"].IndexOf(':');
        var bytes = Convert.FromBase64String(store.Items["record"].Substring(separator + 1));
        bytes[28] ^= 1;
        store.Items["record"] = store.Items["record"].Substring(0, separator + 1) + Convert.ToBase64String(bytes);
        await Assert.ThrowsExceptionAsync<AuthenticationTagMismatchException>(() => encrypted.GetAsync("record"));
    }

    [TestMethod]
    public async Task PlaintextAndTruncatedPayloadsAreNotAcceptedAsLegacyFallbacks()
    {
        var store = new InMemoryBackend();
        using var encrypted = new EncryptedKeyValueBackend(store, RandomNumberGenerator.GetBytes(32));
        store.Items["plain"] = "secret";
        store.Items["short"] = "oasis-aes256gcm-v1:AA==";
        await Assert.ThrowsExceptionAsync<CryptographicException>(() => encrypted.GetAsync("plain"));
        await Assert.ThrowsExceptionAsync<CryptographicException>(() => encrypted.GetAsync("short"));
    }

    [TestMethod]
    public async Task KeyIsCopiedAndAllOperationsRejectDisposal()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var store = new InMemoryBackend();
        var encrypted = new EncryptedKeyValueBackend(store, key);
        await encrypted.PutAsync("record", "secret");
        CryptographicOperations.ZeroMemory(key);
        Assert.AreEqual("secret", await encrypted.GetAsync("record"));
        encrypted.Dispose();
        encrypted.Dispose();
        await Assert.ThrowsExceptionAsync<ObjectDisposedException>(() => encrypted.GetAsync("record"));
        await Assert.ThrowsExceptionAsync<ObjectDisposedException>(() => encrypted.PutAsync("record", "x"));
        Assert.ThrowsException<ObjectDisposedException>(() => encrypted.DeleteAsync("record"));
        Assert.ThrowsException<ObjectDisposedException>(() => encrypted.ListKeysAsync(""));
        Assert.ThrowsException<ObjectDisposedException>(() => encrypted.VerifyAsync());
        Assert.ThrowsException<ArgumentException>(() => new EncryptedKeyValueBackend(store, new byte[31]));
    }

    [TestMethod]
    public async Task EnumerationDeletionAndTransportFailureRetainBackendSemantics()
    {
        var store = new InMemoryBackend();
        using var encrypted = new EncryptedKeyValueBackend(store, RandomNumberGenerator.GetBytes(32));
        await encrypted.PutAsync("prefix/a", "record");
        CollectionAssert.AreEqual(new[] { "prefix/a" }, (await encrypted.ListKeysAsync("prefix/")).ToArray());
        await encrypted.DeleteAsync("prefix/a");
        await encrypted.DeleteAsync("prefix/a");
        Assert.IsNull(await encrypted.GetAsync("prefix/a"));
        store.Reachable = false;
        Assert.ThrowsException<InvalidOperationException>(() => encrypted.VerifyAsync());
    }
}
