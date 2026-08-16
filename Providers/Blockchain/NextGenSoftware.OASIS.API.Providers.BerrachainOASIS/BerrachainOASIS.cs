using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BerrachainOASIS
{
    /// <summary>
    /// OASIS provider for the Berachain EVM L1 blockchain (chain ID 80094 mainnet, 80084 testnet Artio).
    /// Mainnet RPC: https://rpc.berachain.com
    /// Testnet RPC: https://artio.rpc.berachain.com
    /// Native token: BERA
    ///
    /// All storage, NFT, and network logic is delegated to Web3CoreOASISBaseProvider.
    /// ProviderType.BerrachainOASIS is defined in NextGenSoftware.OASIS.API.Core.Enums.ProviderType.
    /// </summary>
    public sealed class BerrachainOASIS : Web3CoreOASISBaseProvider,
        IOASISDBStorageProvider,
        IOASISNETProvider,
        IOASISSuperStar,
        IOASISBlockchainStorageProvider,
        IOASISNFTProvider
    {
        /// <summary>
        /// Initializes a new instance of the BerrachainOASIS provider.
        /// </summary>
        /// <param name="hostUri">RPC endpoint. Defaults to Berachain mainnet.</param>
        /// <param name="chainPrivateKey">Private key for signing transactions.</param>
        /// <param name="contractAddress">Deployed OASIS smart contract address.</param>
        public BerrachainOASIS(
            string hostUri = "https://rpc.berachain.com",
            string chainPrivateKey = "",
            string contractAddress = "")
            : base(hostUri, chainPrivateKey, contractAddress)
        {
            ProviderName = "BerrachainOASIS";
            ProviderDescription = "Berachain Proof-of-Liquidity EVM L1 Provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.BerrachainOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }
    }
}
