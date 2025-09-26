using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.PolygonOASIS;

public sealed class PolygonOASIS : Web3CoreOASISBaseProvider, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar, IOASISBlockchainStorageProvider, IOASISNFTProvider
{
    public PolygonOASIS(string hostUri, string chainPrivateKey, string contractAddress)
        : base(hostUri, chainPrivateKey, contractAddress)
    {
        this.ProviderName = "PolygonOASIS";
        this.ProviderDescription = "Polygon Provider";
        this.ProviderType = new(Core.Enums.ProviderType.PolygonOASIS);
        this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
    }
}