using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaNftSendValidationTests
{
    [TestMethod]
    public async Task BothEntryPointsRequireSigner()
    {
        var provider = new HashgraphOASIS();
        foreach (var result in new[] { provider.SendNFT(Valid()), await provider.SendNFTAsync(Valid()) })
        {
            Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
            StringAssert.Contains(result.Message, "signing key is required");
        }
    }

    [TestMethod]
    public async Task ContradictoryIdentityCannotBeIgnored()
    {
        var request = Valid(); request.TokenId = "8";
        var result = await new HashgraphOASIS().SendNFTAsync(request);
        Assert.IsTrue(result.IsError); StringAssert.Contains(result.Message, "identifiers must agree");
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(2)]
    public async Task SerialQuantityIsExactlyOne(int amount)
    {
        var request = Valid(); request.Amount = amount;
        var result = await new HashgraphOASIS().SendNFTAsync(request);
        Assert.IsTrue(result.IsError); StringAssert.Contains(result.Message, "Amount = 1");
    }

    [TestMethod]
    public async Task SeedPhraseCannotBeIgnored()
    {
        var request = Valid(); request.OwnerSeedPhrase = "synthetic seed";
        var result = await new HashgraphOASIS().SendNFTAsync(request);
        Assert.IsTrue(result.IsError); StringAssert.Contains(result.Message, "Recover an explicit");
    }

    private static SendWeb3NFTRequest Valid() => new()
    { FromNFTTokenAddress = "0.0.1234/7", FromWalletAddress = "0.0.1001", ToWalletAddress = "0.0.1002", Amount = 1 };
}
