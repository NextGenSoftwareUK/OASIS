using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MonadOASIS
{
    /// <summary>
    /// OASIS provider for Monad — a high-performance EVM-compatible L1.
    /// Chain ID:   41454 (mainnet)
    /// RPC:        https://rpc.monad.xyz
    /// Explorer:   https://monadexplorer.com
    /// Native token: MON
    ///
    /// Monad achieves 10,000+ TPS through parallel EVM execution (pipelined execution + MonadBFT consensus),
    /// while remaining fully EVM-compatible at the bytecode level.
    /// All storage, NFT, and network logic is delegated to Web3CoreOASISBaseProvider.
    /// </summary>
    public sealed class MonadOASIS : Web3CoreOASISBaseProvider,
        IOASISDBStorageProvider,
        IOASISNETProvider,
        IOASISSuperStar,
        IOASISBlockchainStorageProvider,
        IOASISNFTProvider
    {
        public MonadOASIS(
            string hostUri = "https://rpc.monad.xyz",
            string chainPrivateKey = "",
            string contractAddress = "")
            : base(hostUri, chainPrivateKey, contractAddress)
        {
            ProviderName = "MonadOASIS";
            ProviderDescription = "Monad EVM L1 Provider — 10,000+ TPS Parallel EVM Execution";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MonadOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }
    }
}
