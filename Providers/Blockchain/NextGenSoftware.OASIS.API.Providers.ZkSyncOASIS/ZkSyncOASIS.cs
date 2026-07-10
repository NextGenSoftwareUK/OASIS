using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ZkSyncOASIS;

/// <summary>
/// zkSync Era provider - EVM-compatible L2 on Ethereum
/// </summary>
public sealed class ZkSyncOASIS : Web3CoreOASISBaseProvider,
    IOASISDBStorageProvider,
    IOASISNETProvider,
    IOASISSuperStar,
    IOASISBlockchainStorageProvider,
    IOASISNFTProvider
{
    public ZkSyncOASIS(
        string hostUri = "https://mainnet.era.zksync.io",
        string chainPrivateKey = "",
        string contractAddress = "")
        : base(hostUri, chainPrivateKey, contractAddress)
    {
        ProviderName = "ZkSyncOASIS";
        ProviderDescription = "zkSync Era EVM-compatible L2 provider for OASIS";
        ProviderType = new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.ZkSyncOASIS);
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        ProviderCategories.Add(new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        ProviderCategories.Add(new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
        ProviderCategories.Add(new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        ProviderCategories.Add(new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        ProviderCategories.Add(new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Storage));
    }
}

