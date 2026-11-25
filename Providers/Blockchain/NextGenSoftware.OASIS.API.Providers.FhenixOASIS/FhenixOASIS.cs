using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.FhenixOASIS
{
    /// <summary>
    /// OASIS provider for Fhenix, an EVM-compatible FHE (fully homomorphic encryption) chain.
    /// Default network: Helium testnet, chain id 8008135, RPC https://api.helium.fhenix.zone.
    /// Storage, NFT and network logic is the shared <see cref="Web3CoreOASISBaseProvider"/> (signed transactions to the
    /// OASIS storage contract). The previous implementation echoed saved objects back and returned blank avatars/holons
    /// for any id without touching the chain.
    /// </summary>
    public sealed class FhenixOASIS : Web3CoreOASISBaseProvider,
        IOASISDBStorageProvider,
        IOASISNETProvider,
        IOASISSuperStar,
        IOASISBlockchainStorageProvider,
        IOASISNFTProvider
    {
        public const long HeliumChainId = 8008135;

        public FhenixOASIS(string rpcUrl = "https://api.helium.fhenix.zone", string chainPrivateKey = "", string contractAddress = "")
            : this(rpcUrl, chainPrivateKey, contractAddress, new System.Numerics.BigInteger(HeliumChainId)) { }

        /// <summary>Signing, storage preparation and token flows require an explicit EVM chain identity.</summary>
        public FhenixOASIS(string hostUri, string chainPrivateKey, string contractAddress, System.Numerics.BigInteger chainId)
            : base(hostUri, chainPrivateKey, contractAddress, chainId)
        {
            if (chainId <= 0) throw new System.ArgumentOutOfRangeException(nameof(chainId));
            ProviderName = "FhenixOASIS";
            ProviderDescription = "Fhenix FHE EVM Provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.FhenixOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }
    }
}
