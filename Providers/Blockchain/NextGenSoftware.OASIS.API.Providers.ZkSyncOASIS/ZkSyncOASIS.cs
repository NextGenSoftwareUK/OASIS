using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.zkSyncOASIS
{
    /// <summary>
    /// OASIS provider for zkSync Era — a ZK rollup EVM L2 with native account abstraction.
    /// Chain ID:   324 (mainnet)
    /// RPC:        https://mainnet.era.zksync.io
    /// Explorer:   https://explorer.zksync.io
    /// Native token: ETH
    ///
    /// zkSync Era is EVM-compatible and supports native account abstraction (EIP-4337 superset).
    /// All storage, NFT, and network logic is delegated to Web3CoreOASISBaseProvider.
    /// </summary>
    public sealed class zkSyncOASIS : Web3CoreOASISBaseProvider,
        IOASISDBStorageProvider,
        IOASISNETProvider,
        IOASISSuperStar,
        IOASISBlockchainStorageProvider,
        IOASISNFTProvider
    {
        public zkSyncOASIS(
            string hostUri = "https://mainnet.era.zksync.io",
            string chainPrivateKey = "",
            string contractAddress = "")
            : base(hostUri, chainPrivateKey, contractAddress)
        {
            ProviderName = "zkSyncOASIS";
            ProviderDescription = "zkSync Era EVM L2 Provider — ZK Rollup with Native Account Abstraction";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ZkSyncOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }
    }
}
