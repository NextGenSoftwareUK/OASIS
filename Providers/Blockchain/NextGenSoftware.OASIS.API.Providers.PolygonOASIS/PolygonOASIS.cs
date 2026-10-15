using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.PolygonOASIS;

public sealed class PolygonOASIS : Web3CoreOASISBaseProvider, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar, IOASISBlockchainStorageProvider, IOASISNFTProvider
{
    public PolygonOASIS(string hostUri, string chainPrivateKey, string contractAddress)
        : this(hostUri,chainPrivateKey,contractAddress,137) {}

    public PolygonOASIS(string hostUri,string chainPrivateKey,string contractAddress,System.Numerics.BigInteger chainId)
        : base(hostUri,chainPrivateKey,contractAddress,chainId)
    {
        if(chainId<=0) throw new System.ArgumentOutOfRangeException(nameof(chainId));
        this.ProviderName = "PolygonOASIS";
        this.ProviderDescription = "Polygon Provider";
        this.ProviderType = new(Core.Enums.ProviderType.PolygonOASIS);
        this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
    }
}
