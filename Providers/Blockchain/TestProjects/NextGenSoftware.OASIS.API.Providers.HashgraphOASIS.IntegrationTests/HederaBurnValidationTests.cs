using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaBurnValidationTests
{
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public async Task MissingOrNegativeAmountCannotBecomeDefaultBurn(int amount)
    {
        var result = await new HashgraphOASIS().BurnTokenAsync(new BurnWeb3TokenRequest
        {
            Amount = amount, TokenAddress = "0.0.1001", BurntByAvatarId = Guid.NewGuid(),
            OwnerPrivateKey = "", OwnerPublicKey = "", OwnerSeedPhrase = ""
        });
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message, "positive burn amount");
    }
}
