using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ScrollOASIS
{
    /// <summary>
    /// OASIS provider for Scroll — a zkEVM L2 with bytecode-level EVM equivalence.
    /// Chain ID:   534352 (mainnet)
    /// RPC:        https://rpc.scroll.io
    /// Explorer:   https://scrollscan.com
    /// Native token: ETH
    ///
    /// Scroll prioritises bytecode-level EVM equivalence so existing Solidity contracts deploy unchanged.
    /// All storage, NFT, and network logic is delegated to Web3CoreOASISBaseProvider.
    /// </summary>
    public sealed class ScrollOASIS : Web3CoreOASISBaseProvider,
        IOASISDBStorageProvider,
        IOASISNETProvider,
        IOASISSuperStar,
        IOASISBlockchainStorageProvider,
        IOASISNFTProvider
    {
        public ScrollOASIS(
            string hostUri = "https://rpc.scroll.io",
            string chainPrivateKey = "",
            string contractAddress = "")
            : base(hostUri, chainPrivateKey, contractAddress)
        {
            ProviderName = "ScrollOASIS";
            ProviderDescription = "Scroll zkEVM L2 Provider — Bytecode-level EVM Equivalence";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ScrollOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }
    }
}
