using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.LineaOASIS;

/// <summary>
/// Linea provider - EVM-compatible L2 on Ethereum
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
        ProviderDescription = "Linea EVM-compatible L2 provider for OASIS";
        ProviderType = new Core.Enums.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.LineaOASIS);
        ProviderCategory = new Core.Enums.EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        ProviderCategories.Add(new Core.Enums.EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        ProviderCategories.Add(new Core.Enums.EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
        ProviderCategories.Add(new Core.Enums.EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        ProviderCategories.Add(new Core.Enums.EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        ProviderCategories.Add(new Core.Enums.EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Storage));
    }
}

