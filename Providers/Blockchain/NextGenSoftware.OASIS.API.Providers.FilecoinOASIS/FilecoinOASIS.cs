using System;
using System.Numerics;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.FilecoinOASIS
{
    /// <summary>
    /// OASIS storage provider for the Filecoin EVM (FEVM, https://docs.filecoin.io/smart-contracts/fundamentals/the-fvm).
    /// Avatars, avatar details and holons are written by signed Ethereum-format transactions to the
    /// shared Web3CoreOASIS storage contract deployed on FEVM, exactly like the other Web3Core chains,
    /// and read back through the Lotus Ethereum JSON-RPC (<c>/rpc/v1</c>).
    /// Mainnet chain id is 314 (Glif public node by default); the Lotus local devnet uses 31415926.
    /// Read-only public account metadata is served separately by <see cref="FilecoinPublicAccountMetadataSource"/>.
    /// </summary>
    public sealed class FilecoinOASIS : Web3CoreOASISBaseProvider,
        IOASISDBStorageProvider,
        IOASISNETProvider,
        IOASISSuperStar,
        IOASISBlockchainStorageProvider,
        IOASISNFTProvider
    {
        public const string MainnetRpc = "https://api.node.glif.io/rpc/v1";
        public static readonly BigInteger MainnetChainId = new(314);

        // FEVM meters execution in Filecoin gas units (a storage upsert costs tens of millions),
        // so the fixed Ethereum-schedule limits would run every mutation out of gas.
        protected override bool EstimateContractGas => true;

        public FilecoinOASIS(
            string hostUri = MainnetRpc,
            string chainPrivateKey = "",
            string contractAddress = "")
            : this(hostUri, chainPrivateKey, contractAddress, MainnetChainId) { }

        public FilecoinOASIS(
            string hostUri,
            string chainPrivateKey,
            string contractAddress,
            BigInteger? chainId)
            : base(hostUri, chainPrivateKey, contractAddress, chainId ?? MainnetChainId)
        {
            if (chainId.HasValue && chainId.Value <= 0)
                throw new ArgumentOutOfRangeException(nameof(chainId), "An explicit chain identity must be positive.");
            ProviderName = "FilecoinOASIS";
            ProviderDescription = "Filecoin Provider - Filecoin EVM (FEVM) storage using Web3Core";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.FilecoinOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }
    }
}
