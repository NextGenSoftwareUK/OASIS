using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class Web3NftMetadataTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public void ExistingStringAndJsonElementMetadataRetainNativeIdentity(bool jsonElement)
    {
        var original=new Web3NFT { Id=Guid.NewGuid(),TokenId="{\"U128\":\"18446744073709551618\"}",
            NFTTokenAddress="collection",CollectionPublicKey="collection",Title="Tree",
            OnChainProvider=new(ProviderType.PolkadotOASIS),NFTStandardType=new(NFTStandardType.PSP34) };
        var json=JsonSerializer.Serialize(original);
        var holon=new Holon(HolonType.Web3NFT) { Id=original.Id };
        holon.MetaData[Web3NftMetadata.MetadataKey]=jsonElement?JsonSerializer.Deserialize<JsonElement>(json):json;
        var decoded=Web3NftMetadata.Deserialize(holon);
        Assert.Equal(original.Id,decoded.Id);Assert.Equal(original.TokenId,decoded.TokenId);
        Assert.Equal(original.NFTTokenAddress,decoded.NFTTokenAddress);
        Assert.Equal(ProviderType.PolkadotOASIS,decoded.OnChainProvider.Value);
        Assert.Equal(NFTStandardType.PSP34,decoded.NFTStandardType.Value);
        Assert.NotSame(original,decoded);
    }

    [Fact]
    public void MissingNativeIdIsNeverReplacedByTheOasisGuid()
    {
        var holon=new Holon(HolonType.Web3NFT) { Id=Guid.NewGuid() };
        holon.MetaData[Web3NftMetadata.MetadataKey]=JsonSerializer.Serialize(new Web3NFT { Id=holon.Id });
        Assert.Null(Web3NftMetadata.Deserialize(holon).TokenId);
    }

    [Theory][InlineData(null)][InlineData("null")][InlineData("{")]
    public void MissingNullAndMalformedPayloadsFailExplicitly(string? payload)
    {
        var holon=new Holon(HolonType.Web3NFT);
        if(payload!=null)holon.MetaData[Web3NftMetadata.MetadataKey]=payload;
        Assert.ThrowsAny<Exception>(()=>Web3NftMetadata.Deserialize(holon));
    }
}
