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
    public async Task ProviderCrudPreservesAvatarSecretsDetailsAndHolonVersionsThroughEncryption()
    {
        var store = new InMemoryBackend();
        using var encrypted = new EncryptedKeyValueBackend(store, RandomNumberGenerator.GetBytes(32));
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
