using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.RoninOASIS
{
    /// <summary>
    /// OASIS provider for Ronin, the EVM-compatible gaming chain by Sky Mavis.
    /// Default network: Ronin mainnet, chain id 2020, RPC https://api.roninchain.com/rpc.
    /// Storage, NFT and network logic is the shared <see cref="Web3CoreOASISBaseProvider"/> (signed transactions to the
    /// OASIS storage contract). The previous implementation echoed saved objects back and returned blank avatars/holons
    /// for any id without touching the chain.
    /// </summary>
    public sealed class RoninOASIS : Web3CoreOASISBaseProvider,
        IOASISDBStorageProvider,
        IOASISNETProvider,
        IOASISSuperStar,
        IOASISBlockchainStorageProvider,
        IOASISNFTProvider
    {
        public const long MainnetChainId = 2020;

        public RoninOASIS(string rpcUrl = "https://api.roninchain.com/rpc", string chainPrivateKey = "", string contractAddress = "")
            : this(rpcUrl, chainPrivateKey, contractAddress, new System.Numerics.BigInteger(MainnetChainId)) { }

        /// <summary>Signing, storage preparation and token flows require an explicit EVM chain identity.</summary>
        public RoninOASIS(string hostUri, string chainPrivateKey, string contractAddress, System.Numerics.BigInteger chainId)
            : base(hostUri, chainPrivateKey, contractAddress, chainId)
        {
            if (chainId <= 0) throw new System.ArgumentOutOfRangeException(nameof(chainId));
            ProviderName = "RoninOASIS";
            ProviderDescription = "Ronin EVM Provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.RoninOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }
    }
}
