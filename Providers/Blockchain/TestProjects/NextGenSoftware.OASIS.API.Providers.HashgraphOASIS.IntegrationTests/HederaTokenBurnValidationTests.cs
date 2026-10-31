using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaTokenBurnValidationTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SeedPhraseCannotBeIgnoredBeforeWalletLookup(bool synchronous)
    {
        var request = new BurnWeb3TokenRequest { TokenAddress = "0.0.1234", Amount = 1m, BurntByAvatarId = Guid.NewGuid(),
            OwnerPrivateKey = "", OwnerPublicKey = "", OwnerSeedPhrase = "must not be ignored" };
        var provider = new HashgraphOASIS();
        var result = synchronous ? provider.BurnToken(request) : await provider.BurnTokenAsync(request);
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message, "Recover an explicit Hedera signing key");
    }
}
