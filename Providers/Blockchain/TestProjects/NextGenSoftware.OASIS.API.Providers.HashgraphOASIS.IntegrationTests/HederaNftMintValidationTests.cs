using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaNftMintValidationTests
{
    [DataTestMethod]
    [DataRow("zeroCollection","CollectionPublicKey")]
    [DataRow("negativePrice","nonnegative")]
    [DataRow("negativeDiscount","nonnegative")]
    [DataRow("reversedSale","sale start")]
    public async Task InvalidMintStateFailsBeforeWalletLookup(string failure,string message)
    {
        var request=Valid();
        switch(failure){
            case "zeroCollection":request.CollectionPublicKey="0.0.0";break;
            case "negativePrice":request.Price=-1;break;
            case "negativeDiscount":request.Discount=-1;break;
            case "reversedSale":request.SaleStartDate=DateTime.UnixEpoch.AddDays(1);request.SaleEndDate=DateTime.UnixEpoch;break;
        }
        var result=await new HashgraphOASIS().MintNFTAsync(request);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);StringAssert.Contains(result.Message,message);
    }

    [TestMethod]
    public async Task MalformedMintMetadataRejectsBeforeAvatarLookup()
    {
        var request=Valid();request.JSONMetaDataURL=new string((char)0xd800,1);
        var result=await new HashgraphOASIS().MintNFTAsync(request);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        Assert.IsInstanceOfType<System.Text.EncoderFallbackException>(result.Exception);
    }

    [TestMethod]
    public async Task CollectionRequiredForBothEntryPoints()
    {
        var provider = new HashgraphOASIS();
        var request = new MintWeb3NFTRequest();
        foreach (var result in new[] { provider.MintNFT(request), await provider.MintNFTAsync(request) })
        {
            Assert.IsTrue(result.IsError);
            Assert.IsNull(result.Result);
            StringAssert.Contains(result.Message, "CollectionPublicKey");
        }
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(11)]
    public async Task UnsupportedBatchCannotReachWalletLookup(int count)
    {
        var request = Valid(); request.NumberToMint = count;
        var result = await new HashgraphOASIS().MintNFTAsync(request);
        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "1 to 10");
    }

    [TestMethod]
    public async Task ConflictingAmountIsRejected()
    {
        var request = Valid(); request.Amount = 2;
        var result = await new HashgraphOASIS().MintNFTAsync(request);
        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "Amount must agree");
    }

    [TestMethod]
    public async Task MetadataIsNotTruncatedToChainLimit()
    {
        var request = Valid(); request.JSONMetaDataURL = new string('é', 51);
        var result = await new HashgraphOASIS().MintNFTAsync(request);
        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "100 UTF-8 bytes");
    }

    [TestMethod]
    public async Task MintRequiresRealAvatar()
    {
        var result = await new HashgraphOASIS().MintNFTAsync(Valid());
        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "real Hedera wallet");
    }

    [TestMethod]
    public async Task CollectionAuthorityChangeCannotBeIgnored()
    {
        var request = Valid(); request.FreezeMetadata = true;
        var result = await new HashgraphOASIS().MintNFTAsync(request);
        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "collection operation");
    }

    [TestMethod]
    public async Task AnotherChainsStandardCannotBeReportedAsHts()
    {
        var request = Valid(); request.NFTStandardType = NextGenSoftware.OASIS.API.Core.Enums.NFTStandardType.ERC721;
        var result = await new HashgraphOASIS().MintNFTAsync(request);
        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message, "HederaHTS");
    }

    private static MintWeb3NFTRequest Valid() => new() { CollectionPublicKey = "0.0.1234", JSONMetaDataURL = "ipfs://metadata" };
}
