using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.RootstockOASIS;

public sealed class RootstockOASIS : Web3CoreOASISBaseProvider, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar, IOASISBlockchainStorageProvider, IOASISNFTProvider
{
    public RootstockOASIS(string hostUri, string chainPrivateKey, string contractAddress)
        : this(hostUri, chainPrivateKey, contractAddress, new System.Numerics.BigInteger(30)) { }

    public RootstockOASIS(string hostUri, string chainPrivateKey, string contractAddress, System.Numerics.BigInteger chainId)
        : base(hostUri, chainPrivateKey, contractAddress, chainId)
    {
        if (chainId <= 0) throw new System.ArgumentOutOfRangeException(nameof(chainId));
        this.ProviderName = "RootstockOASIS";
        this.ProviderDescription = "Rootstock Provider";
        this.ProviderType = new(Core.Enums.ProviderType.RootstockOASIS);

        this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
    }
}
