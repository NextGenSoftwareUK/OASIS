using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BasechainOASIS
{
    /// <summary>
    /// OASIS provider for Loom Basechain, an EVM-compatible chain (https://loomx.io).
    /// Storage, NFT and network logic is the shared <see cref="Web3CoreOASISBaseProvider"/>: signed transactions to the
    /// OASIS storage contract. The previous implementation had no contract; holon saves only rebroadcast a raw
    /// transaction the caller had pre-signed into metadata, and avatar save/load/delete always failed.
    /// The RPC defaults to BASECHAIN_RPC_URL, then the Basechain mainnet endpoint.
    /// </summary>
    public sealed class BasechainOASIS : Web3CoreOASISBaseProvider,
        IOASISDBStorageProvider,
        IOASISNETProvider,
        IOASISSuperStar,
        IOASISBlockchainStorageProvider,
        IOASISNFTProvider
    {
        public BasechainOASIS(string rpcUrl = null, string chainPrivateKey = "", string contractAddress = "")
            : base(rpcUrl ?? Environment.GetEnvironmentVariable("BASECHAIN_RPC_URL") ?? "https://basechain-mainnet.dappchains.com",
                chainPrivateKey, contractAddress)
        {
            Describe();
        }

        /// <summary>Signing, storage preparation and token flows against an explicitly pinned EVM chain identity.</summary>
        public BasechainOASIS(string hostUri, string chainPrivateKey, string contractAddress, System.Numerics.BigInteger chainId)
            : base(hostUri, chainPrivateKey, contractAddress, chainId)
        {
            if (chainId <= 0) throw new ArgumentOutOfRangeException(nameof(chainId));
            Describe();
        }

        private void Describe()
        {
            ProviderName = "BasechainOASIS";
            ProviderDescription = "Loom Basechain EVM Provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.BasechainOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }
    }
}
