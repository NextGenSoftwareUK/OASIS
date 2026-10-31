using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaSdkKeyTests
{
    [TestMethod]
    public async Task SdkMnemonicIdentityRoundTripsWithoutActivationOrLedgerClaim()
    {
        var provider = new HashgraphOASIS();
        var generated = await provider.CreateAccountAsync();
        Assert.IsFalse(generated.IsError, generated.Message);
        Assert.AreEqual(12, generated.Result.SeedPhrase.Split(' ').Length);
        StringAssert.Contains(generated.Message, "No ledger account was created");
        var restored = await provider.RestoreAccountAsync(generated.Result.SeedPhrase);
        Assert.IsFalse(restored.IsError, restored.Message);
        Assert.AreEqual(generated.Result.PublicKey, restored.Result.PublicKey);
        Assert.AreEqual(generated.Result.PrivateKey, restored.Result.PrivateKey);
        var sdkKey = await provider.RestoreKeyPairAsync(restored.Result.PrivateKey);
        Assert.IsFalse(sdkKey.IsError, sdkKey.Message);
        Assert.AreEqual(restored.Result.PublicKey, sdkKey.Result.PublicKey);
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task InvalidOrCancelledMnemonicOperationsReturnNoCredentials()
    {
        var provider = new HashgraphOASIS();
        foreach (var phrase in new[] { "", "not a valid mnemonic", string.Join(" ", Enumerable.Repeat("abandon", 12)) })
        {
            var invalid = await provider.RestoreAccountAsync(phrase);
            Assert.IsTrue(invalid.IsError);
            Assert.IsNull(invalid.Result.PrivateKey);
        }
        var cancelled = await provider.CreateAccountAsync(new CancellationToken(true));
        Assert.IsTrue(cancelled.IsError); Assert.IsNull(cancelled.Result.PrivateKey);
        var cancelledRestore = await provider.RestoreAccountAsync("unused", new CancellationToken(true));
        Assert.IsTrue(cancelledRestore.IsError); Assert.IsNull(cancelledRestore.Result.PrivateKey);
    }

    [TestMethod]
    public async Task ProviderGeneratesAndRestoresRealSdkKeysWithoutInventingAccount()
    {
        var provider = new HashgraphOASIS();
        var generated = await provider.GenerateKeyPairAsync();
        Assert.IsFalse(generated.IsError, generated.Message);
        Assert.IsNotNull(generated.Result);
        Assert.AreNotEqual(generated.Result.PrivateKey, generated.Result.PublicKey);
        Assert.IsTrue(string.IsNullOrEmpty(generated.Result.WalletAddressLegacy));
        var recovered = await provider.RestoreKeyPairAsync(generated.Result.PrivateKey);
        Assert.IsFalse(recovered.IsError, recovered.Message);
        Assert.AreEqual(generated.Result.PublicKey, recovered.Result.PublicKey);
        Assert.AreEqual(generated.Result.PrivateKey, recovered.Result.PrivateKey);
        Assert.IsTrue(string.IsNullOrEmpty(recovered.Result.WalletAddressLegacy));
    }
}
