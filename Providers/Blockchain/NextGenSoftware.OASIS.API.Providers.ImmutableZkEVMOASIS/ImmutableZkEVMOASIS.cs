using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ImmutableZkEVMOASIS
{
    /// <summary>
    /// OASIS provider for Immutable zkEVM, the EVM-compatible gaming zk-rollup.
    /// Default network: Immutable zkEVM mainnet, chain id 13371, RPC https://rpc.immutable.com.
    /// Storage, NFT and network logic is the shared <see cref="Web3CoreOASISBaseProvider"/> (signed transactions to the
    /// OASIS storage contract). The previous implementation echoed saved objects back and returned blank avatars/holons
    /// for any id without touching the chain.
    /// </summary>
    public sealed class ImmutableZkEVMOASIS : Web3CoreOASISBaseProvider,
        IOASISDBStorageProvider,
        IOASISNETProvider,
        IOASISSuperStar,
        IOASISBlockchainStorageProvider,
        IOASISNFTProvider
    {
        public const long MainnetChainId = 13371;

        public ImmutableZkEVMOASIS(string rpcUrl = "https://rpc.immutable.com", string chainPrivateKey = "", string contractAddress = "")
            : this(rpcUrl, chainPrivateKey, contractAddress, new System.Numerics.BigInteger(MainnetChainId)) { }

        /// <summary>Signing, storage preparation and token flows require an explicit EVM chain identity.</summary>
        public ImmutableZkEVMOASIS(string hostUri, string chainPrivateKey, string contractAddress, System.Numerics.BigInteger chainId)
            : base(hostUri, chainPrivateKey, contractAddress, chainId)
        {
            if (chainId <= 0) throw new System.ArgumentOutOfRangeException(nameof(chainId));
            ProviderName = "ImmutableZkEVMOASIS";
            ProviderDescription = "Immutable zkEVM Provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ImmutableZkEVMOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }
    }
}
