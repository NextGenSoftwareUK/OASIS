using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SeiOASIS
{
    /// <summary>
    /// OASIS provider for the Sei EVM blockchain (chain ID 1329 mainnet, 713715 testnet).
    /// Mainnet RPC: https://evm-rpc.sei-apis.com
    /// Testnet RPC: https://evm-rpc-testnet.sei-apis.com
    /// Explorer:    https://seitrace.com
    /// Native token: SEI
    ///
    /// All storage, NFT, and network logic is delegated to Web3CoreOASISBaseProvider.
    /// </summary>
    public sealed class SeiOASIS : Web3CoreOASISBaseProvider,
        IOASISDBStorageProvider,
        IOASISNETProvider,
        IOASISSuperStar,
        IOASISBlockchainStorageProvider,
        IOASISNFTProvider
    {
        public SeiOASIS(
            string hostUri = "https://evm-rpc.sei-apis.com",
            string chainPrivateKey = "",
            string contractAddress = "")
            : base(hostUri, chainPrivateKey, contractAddress)
        {
            ProviderName = "SeiOASIS";
            ProviderDescription = "Sei EVM Provider — High-Performance L1 (400ms finality)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SeiOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }
    }
}
