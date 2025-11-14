using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MonadOASIS;

/// <summary>
/// Lightweight wrapper around the shared Web3Core provider so Monad can plug straight into
/// the existing HyperDrive replication/failover flow. All storage, NFT, and wallet logic
/// is handled by <see cref="Web3CoreOASISBaseProvider"/>; this class only sets the metadata
/// and wires provider configuration from DNA.
/// </summary>
public sealed class MonadOASIS : Web3CoreOASISBaseProvider,
    IOASISDBStorageProvider,
    IOASISNETProvider,
    IOASISSuperStar,
    IOASISBlockchainStorageProvider,
    IOASISNFTProvider
{
    public MonadOASIS(string hostUri, string chainPrivateKey, string contractAddress)
        : base(hostUri, chainPrivateKey, contractAddress)
    {
        ProviderName = "MonadOASIS";
        ProviderDescription = "Monad high-throughput EVM provider";
        ProviderType = new(Core.Enums.ProviderType.MonadOASIS);
        ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
    }
}

