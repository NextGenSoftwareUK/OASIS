using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.OASIS.API.Providers.PolygonOASIS.Infrastructure.Services.Polygon;

namespace NextGenSoftware.OASIS.API.Providers.PolygonOASIS;

public sealed class PolygonOASIS : Web3CoreOASISBaseProvider, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar, IOASISBlockchainStorageProvider, IOASISNFTProvider
{
    private PolygonBridgeService _bridgeService;

    public IPolygonBridgeService BridgeService 
    { 
        get 
        { 
            if (_bridgeService == null && Web3Client != null && TechnicalAccount != null)
                _bridgeService = new PolygonBridgeService(Web3Client, TechnicalAccount, useTestnet: true); // Safe default: Mumbai testnet
            return _bridgeService;
        }
    }

    public PolygonOASIS(string hostUri, string chainPrivateKey, string contractAddress)
        : base(hostUri, chainPrivateKey, contractAddress)
    {
        this.ProviderName = "PolygonOASIS";
        this.ProviderDescription = "Polygon Provider";
        this.ProviderType = new(Core.Enums.ProviderType.PolygonOASIS);
        this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
    }
}