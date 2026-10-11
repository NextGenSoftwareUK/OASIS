using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaSdkKeyTests
{
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
