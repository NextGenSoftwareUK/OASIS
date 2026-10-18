using System.Numerics;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BaseOASIS;

/// <summary>
/// Base storage provider backed by the shared EVM Web3Core contract and Nethereum client.
/// </summary>
public sealed class BaseOASIS : Web3CoreOASISBaseProvider,
    IOASISDBStorageProvider,
    IOASISNETProvider,
    IOASISSuperStar,
    IOASISBlockchainStorageProvider,
    IOASISNFTProvider
{
    public BaseOASIS(
        string hostUri = "https://mainnet.base.org",
        string chainPrivateKey = "",
        BigInteger? chainId = null,
        string contractAddress = "")
        : base(hostUri, chainPrivateKey, contractAddress, chainId ?? new BigInteger(8453))
    {
        ProviderName = "BaseOASIS";
        ProviderDescription = "Base Provider - EVM-compatible using Web3Core";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.BaseOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
    }
}
