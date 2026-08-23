using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BerachainOASIS
{
    /// <summary>
    /// OASIS provider for Berachain — an EVM-compatible L1 with Proof-of-Liquidity (PoL) consensus.
    /// Chain ID:   80094 (mainnet)
    /// RPC:        https://rpc.berachain.com
    /// Explorer:   https://berascan.com
    /// Native token: BERA
    ///
    /// PoL ties validator rewards to liquidity provision, making DeFi-native to the consensus layer.
    /// All storage, NFT, and network logic is delegated to Web3CoreOASISBaseProvider.
    /// </summary>
    public sealed class BerachainOASIS : Web3CoreOASISBaseProvider,
        IOASISDBStorageProvider,
        IOASISNETProvider,
        IOASISSuperStar,
        IOASISBlockchainStorageProvider,
        IOASISNFTProvider
    {
        public BerachainOASIS(
            string hostUri = "https://rpc.berachain.com",
            string chainPrivateKey = "",
            string contractAddress = "")
            : base(hostUri, chainPrivateKey, contractAddress)
        {
            ProviderName = "BerachainOASIS";
            ProviderDescription = "Berachain EVM L1 Provider — Proof-of-Liquidity Blockchain";
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
