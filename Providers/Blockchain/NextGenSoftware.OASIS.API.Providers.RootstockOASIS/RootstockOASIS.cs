using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.RootstockOASIS;

public sealed class RootstockOASIS : Web3CoreOASISBaseProvider, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar, IOASISBlockchainStorageProvider, IOASISNFTProvider
{
    public RootstockOASIS(string hostUri, string chainPrivateKey, string contractAddress)
        : base(hostUri, chainPrivateKey, contractAddress)
    {
        this.ProviderName = "RootstockOASIS";
        this.ProviderDescription = "Rootstock Provider";
        this.ProviderType = new(Core.Enums.ProviderType.RootstockOASIS);
        this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
    }
}
