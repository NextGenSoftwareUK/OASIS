using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class NftNativeIdentityTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("115792089237316195423570985008687907853269984665640564039457584007913129639935")]
    public void NativeIdentityIsASeparateLosslessString(string tokenId)
    {
        var id=Guid.NewGuid();
        var nft=JsonConvert.DeserializeObject<Web3NFT>(JsonConvert.SerializeObject(new Web3NFT {Id=id,TokenId=tokenId}))!;
        Assert.Equal(id,nft.Id);Assert.Equal(tokenId,nft.TokenId);
        var request=JsonConvert.DeserializeObject<BurnWeb3NFTRequest>(JsonConvert.SerializeObject(new BurnWeb3NFTRequest {
            Web3NFTId=id,TokenId=tokenId,OwnerPrivateKey="",OwnerPublicKey="",OwnerSeedPhrase=""}))!;
        Assert.Equal(id,request.Web3NFTId);Assert.Equal(tokenId,request.TokenId);
        Assert.Null(JsonConvert.DeserializeObject<Web3NFT>("{}")!.TokenId);
    }
}
