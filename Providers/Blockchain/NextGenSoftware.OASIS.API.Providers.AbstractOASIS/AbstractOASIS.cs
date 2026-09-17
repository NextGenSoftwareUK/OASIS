using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AbstractOASIS
{
    /// <summary>
    /// OASIS provider for Abstract — a consumer-focused EVM L2 purpose-built for gaming and NFTs.
    /// Chain ID:   2741 (mainnet)
    /// RPC:        https://api.mainnet.abs.xyz
    /// Explorer:   https://abscan.org
    /// Native token: ETH
    ///
    /// Abstract is Ethereum-settled and ZK-proven (built on ZKsync's ZK stack).
    /// All storage, NFT, and network logic is delegated to Web3CoreOASISBaseProvider.
    /// </summary>
    public sealed class AbstractOASIS : Web3CoreOASISBaseProvider,
        IOASISDBStorageProvider,
        IOASISNETProvider,
        IOASISSuperStar,
        IOASISBlockchainStorageProvider,
        IOASISNFTProvider
    {
        public AbstractOASIS(
            string hostUri = "https://api.mainnet.abs.xyz",
            string chainPrivateKey = "",
            string contractAddress = "")
            : base(hostUri, chainPrivateKey, contractAddress)
        {
            ProviderName = "AbstractOASIS";
            ProviderDescription = "Abstract EVM L2 Provider — Consumer Gaming & NFT Chain (Ethereum-settled, ZK-proven)";
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
