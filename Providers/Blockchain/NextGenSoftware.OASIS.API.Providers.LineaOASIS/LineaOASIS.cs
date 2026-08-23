using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.LineaOASIS
{
    /// <summary>
    /// OASIS provider for Linea — a ConsenSys zkEVM L2 with deep MetaMask integration.
    /// Chain ID:   59144 (mainnet)
    /// RPC:        https://rpc.linea.build
    /// Explorer:   https://lineascan.build
    /// Native token: ETH
    ///
    /// Linea is developed by ConsenSys and is natively integrated with MetaMask,
    /// giving it immediate access to MetaMask's 30M+ user base.
    /// All storage, NFT, and network logic is delegated to Web3CoreOASISBaseProvider.
    /// </summary>
    public sealed class LineaOASIS : Web3CoreOASISBaseProvider,
        IOASISDBStorageProvider,
        IOASISNETProvider,
        IOASISSuperStar,
        IOASISBlockchainStorageProvider,
        IOASISNFTProvider
    {
        public LineaOASIS(
            string hostUri = "https://rpc.linea.build",
            string chainPrivateKey = "",
            string contractAddress = "")
            : base(hostUri, chainPrivateKey, contractAddress)
        {
            ProviderName = "LineaOASIS";
            ProviderDescription = "Linea zkEVM L2 Provider — ConsenSys / MetaMask-Integrated ZK Rollup";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.LineaOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }
    }
}
