using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS.UnitTests;

[TestClass]
public class AptosBurnAmountTests
{
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public async Task InvalidAmountIsRejectedBeforeAccountOrNetwork(int amount)
    {
        var result = await new AptosOASIS().BurnTokenAsync(new BurnWeb3TokenRequest
        {
            Amount = amount, TokenAddress = "OASIS", OwnerPublicKey = "",
            OwnerPrivateKey = "", OwnerSeedPhrase = ""
        });
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
        Assert.AreEqual("Amount", ((ArgumentOutOfRangeException)result.Exception).ParamName);
    }

    [TestMethod]
    public async Task SeedIsNotSilentlyIgnoredForConfiguredAccount()
    {
        var result = await new AptosOASIS().BurnTokenAsync(new BurnWeb3TokenRequest
        {
            Amount = 1, TokenAddress = "OASIS", OwnerPublicKey = "",
            OwnerPrivateKey = "", OwnerSeedPhrase = "not-the-configured-account"
        });
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message, "signing key explicitly");
    }
}
