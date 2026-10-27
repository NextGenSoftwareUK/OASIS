using System;
using System.Numerics;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

/// <summary>Ethereum V2 facade over the single Nethereum storage implementation.
/// Requires the compatible existing Web3Core contract; never retries a legacy ABI or deploys one.</summary>
public sealed class EthereumOASIS_Web3Core : Web3CoreOASISBaseProvider
{
    public EthereumOASIS_Web3Core(string hostUri,string chainPrivateKey,string contractAddress,BigInteger expectedChainId)
        : base(hostUri,chainPrivateKey,contractAddress,expectedChainId)
    {
        if(expectedChainId<=0)throw new ArgumentOutOfRangeException(nameof(expectedChainId));
        ProviderName="EthereumOASIS";ProviderDescription="Ethereum V2 using the shared Nethereum Web3Core provider";
        ProviderType=new EnumValue<ProviderType>(Core.Enums.ProviderType.EthereumOASIS);
        ProviderCategory=new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        foreach(var capability in new[]{Core.Enums.ProviderCategory.Blockchain,Core.Enums.ProviderCategory.EVMBlockchain,
            Core.Enums.ProviderCategory.Storage,Core.Enums.ProviderCategory.NFT,Core.Enums.ProviderCategory.SmartContract})
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(capability));
    }
}
