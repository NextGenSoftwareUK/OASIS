using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaStorageValidationTests
{
    [TestMethod]
    public void ConfiguredAdapterCancellationDoesNotResolveEncryptionSecrets()
    {
        var reads = 0;
        using var backend = new ConfiguredHederaStorageBackend("testnet", null, new HederaStorageConfiguration {
            ContractId = "0.0.123", OperatorAccountId = "0.0.1001", SigningKeyType = "der",
            SigningKeyResolver = () => "", GasLimit = 100000, MaxFeeTinybars = 1,
            EncryptionKeyResolver = () => { reads++; return new byte[32]; }
        });
        Assert.ThrowsException<OperationCanceledException>(() => backend.GetAsync("key", new CancellationToken(true)));
        Assert.AreEqual(0, reads);
    }

    [TestMethod]
    public async Task ProviderWithoutStorageConfigurationDoesNotFabricateActivationOrCrudSuccess()
    {
        var provider = new HashgraphOASIS(network: "testnet");
        var activated = await provider.ActivateProviderAsync();
        Assert.IsTrue(activated.IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        var saved = await provider.SaveAvatarAsync(new NextGenSoftware.OASIS.API.Core.Holons.Avatar {
            Username = "unconfigured-test", Email = "unconfigured@example.test"
        });
        Assert.IsTrue(saved.IsError);
        Assert.IsFalse(saved.IsSaved);
    }

    private static HederaStorageBackend Create(Func<string> key)
        => new("testnet", "0.0.123", "0.0.1001", "der", key, 100000, 100000000);

    [TestMethod]
    public void ConfigurationRejectsUnspecifiedNetworkIdentityAlgorithmAndFee()
    {
        Assert.ThrowsException<ArgumentException>(() => new HederaStorageBackend("other", "0.0.123", "0.0.1001", "der", () => "", 100000, 1));
        Assert.ThrowsException<ArgumentException>(() => new HederaStorageBackend("testnet", "not-contract", "0.0.1001", "der", () => "", 100000, 1));
        Assert.ThrowsException<ArgumentException>(() => new HederaStorageBackend("testnet", "0.0.123", "0.0.1001", "guess", () => "", 100000, 1));
        Assert.ThrowsException<ArgumentException>(() => new HederaStorageBackend("testnet", "0.0.123", "0.0.1001", "der", () => "", 100000, 0));
        Assert.ThrowsException<ArgumentException>(() => new HederaStorageBackend("local", "0.0.123", "0.0.1001", "der", () => "", 100000, 1));
    }

    [TestMethod]
    public async Task CancellationStopsEveryOperationBeforeKeyResolutionOrProcessStart()
    {
        var keyReads = 0;
        var backend = Create(() => { keyReads++; throw new InvalidOperationException("Must not resolve a key."); });
        var token = new CancellationToken(true);
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => backend.GetAsync("key", token));
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => backend.PutAsync("key", "value", token));
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => backend.DeleteAsync("key", token));
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => backend.ListKeysAsync("", token));
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => backend.VerifyAsync(token));
        Assert.AreEqual(0, keyReads);
    }

    [TestMethod]
    public async Task AbsentSigningKeyFailsWithoutStartingAnSdkProcess()
    {
        var backend = Create(() => "");
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.GetAsync("key"));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.PutAsync("key", "value"));
    }
}
