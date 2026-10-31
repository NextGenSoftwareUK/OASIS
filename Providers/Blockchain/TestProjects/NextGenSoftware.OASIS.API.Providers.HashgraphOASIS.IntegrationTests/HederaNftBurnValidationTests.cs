using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaNftBurnValidationTests
{
    [DataTestMethod]
    [DataRow("0.0.1234")]
    [DataRow("0.0.1234/0")]
    [DataRow("0.0.1234/-1")]
    [DataRow("0.0.1234/1.5")]
    [DataRow("0.0.1234/9223372036854775808")]
    [DataRow("0.0.1234/0001")]
    public async Task InvalidSerialNeverReachesWalletLookup(string address)
    {
        var result = await new HashgraphOASIS().BurnNFTAsync(Request(address));
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message, "token/serial");
    }

    [DataTestMethod]
    [DataRow("0.0.1234/1")]
    [DataRow("0.0.1234/9007199254740993")]
    [DataRow("0.0.1234/9223372036854775807")]
    public async Task ExactSerialRequiresRealAvatarWallet(string address)
    {
        var result = await new HashgraphOASIS().BurnNFTAsync(Request(address));
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message, "treasury wallet is required");
    }

    [TestMethod]
    public async Task SeedPhraseCannotBeSilentlyIgnored()
    {
        var result = await new HashgraphOASIS().BurnNFTAsync(new BurnWeb3NFTRequest
        {
            NFTTokenAddress = "0.0.1234/1", BurntByAvatarId = Guid.NewGuid(),
            OwnerPrivateKey = "", OwnerPublicKey = "", OwnerSeedPhrase = "must not be ignored"
        });
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message, "Recover an explicit Hedera signing key");
    }

    private static BurnWeb3NFTRequest Request(string address) => new()
    {
        NFTTokenAddress = address, OwnerPrivateKey = "", OwnerPublicKey = "", OwnerSeedPhrase = ""
    };
}
