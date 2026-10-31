using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS;

/// <summary>
/// Existing EVM adapter labelled TON. This does not implement native TON TVM
/// storage and must not be advertised as native TON or enrolled in its CQL demo.
/// Native contract admission is tracked in Docs/WIP/COSMIC ORM/TON_CQL_MVP.md.
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
        ProviderDescription = "Unverified EVM adapter; native TON TVM provider integration pending";
        ProviderType = new(Core.Enums.ProviderType.TONOASIS);
        ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
    }
}


