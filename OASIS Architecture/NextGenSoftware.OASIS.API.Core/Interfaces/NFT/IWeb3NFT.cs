using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT
{
    public interface IWeb3NFT
    {
        DateTime ImportedOn { get; set; }
        DateTime MintedOn { get; set; }
        string MintTransactionHash { get; set; }
        string NFTMintedUsingWalletAddress { get; set; }
        EnumValue<NFTOffChainMetaType> NFTOffChainMetaType { get; set; }
        EnumValue<NFTStandardType> NFTStandardType { get; set; }
        string NFTTokenAddress { get; set; }
        string OASISMintWalletAddress { get; set; }
        EnumValue<ProviderType> OffChainProvider { get; set; }
        EnumValue<ProviderType> OnChainProvider { get; set; }
        bool StoreNFTMetaDataOnChain { get; set; }
        string Symbol { get; set; }
        string UpdateAuthority { get; set; }
        Dictionary<string, object> MetaData { get; set; }
        string JSONMetaData { get; set; } //Will be the same as the parent WEB4 OASIS NFT (may not be needed here). Unless the Web3Request chooses to override it...
        string JSONMetaDataURL { get; set; } //Will be the same as the parent WEB4 OASIS NFT. (may not be needed here). Unless the Web3Request chooses to override it...
        Guid JSONMetaDataURLHolonId { get; set; } //Will be the same as the parent WEB4 OASIS NFT. (may not be needed here). Unless the Web3Request chooses to override it...
    }
}