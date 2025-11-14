using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS;

/// <summary>
/// TON integration built on top of TON EVM, allowing us to reuse the standard Web3Core
/// contract flow (avatars/holons/NFTs) without introducing a new runtime.
/// </summary>
public sealed class TONOASIS : Web3CoreOASISBaseProvider,
    IOASISDBStorageProvider,
    IOASISNETProvider,
    IOASISSuperStar,
    IOASISBlockchainStorageProvider,
    IOASISNFTProvider
{
    public TONOASIS(string hostUri, string chainPrivateKey, string contractAddress)
        : base(hostUri, chainPrivateKey, contractAddress)
    {
        ProviderName = "TONOASIS";
        ProviderDescription = "TON EVM provider";
        ProviderType = new(Core.Enums.ProviderType.TONOASIS);
        ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
    }
}

