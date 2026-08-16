using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AbstractOASIS
{
    /// <summary>
    /// OASIS provider for the Abstract EVM L2 blockchain (chain ID 2741 mainnet, 11124 testnet).
    /// Mainnet RPC: https://api.mainnet.abs.xyz
    /// Testnet RPC: https://api.testnet.abs.xyz
    /// Explorer:    https://abscan.org
    /// Native token: ETH
    ///
    /// All storage, NFT, and network logic is delegated to Web3CoreOASISBaseProvider.
    /// ProviderType.AbstractOASIS is defined in NextGenSoftware.OASIS.API.Core.Enums.ProviderType.
    /// </summary>
    public sealed class AbstractOASIS : Web3CoreOASISBaseProvider,
        IOASISDBStorageProvider,
        IOASISNETProvider,
        IOASISSuperStar,
        IOASISBlockchainStorageProvider,
        IOASISNFTProvider
    {
        /// <summary>
        /// Initializes a new instance of the AbstractOASIS provider.
        /// </summary>
        /// <param name="hostUri">RPC endpoint. Defaults to Abstract mainnet.</param>
        /// <param name="chainPrivateKey">Private key for signing transactions.</param>
        /// <param name="contractAddress">Deployed OASIS smart contract address.</param>
        public AbstractOASIS(
            string hostUri = "https://api.mainnet.abs.xyz",
            string chainPrivateKey = "",
            string contractAddress = "")
            : base(hostUri, chainPrivateKey, contractAddress)
        {
            ProviderName = "AbstractOASIS";
            ProviderDescription = "Abstract EVM L2 Provider — Consumer & Gaming Blockchain";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AbstractOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }
    }
}
