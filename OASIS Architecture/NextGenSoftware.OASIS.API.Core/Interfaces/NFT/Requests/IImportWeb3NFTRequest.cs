using System;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface IImportWeb3NFTRequest
    {
        public Guid ImportedByByAvatarId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string NFTMintedUsingWalletAddress { get; set; }  //optional.
        public string CurrentOwnerWalletAddress { get; set; }
        public string NFTTokenAddress { get; set; }
        public EnumValue<ProviderType> OnChainProvider { get; set; } //Set to the eqvialent OnChain provider that the NFT was minted with (e.g. Ethereum, Solana etc).
        public EnumValue<ProviderType> OffChainMetaType { get; set; } //Set to the provider they want to use to store the off-chain meta data (e.g. IPFS, OASIS etc).
        public bool StoreNFTMetaDataOnChain { get; set; }
        public EnumValue<NFTOffChainMetaType> NFTOffChainMetaType { get; set; }
        public EnumValue<NFTStandardType> NFTStandardType { get; set; } 
        public string MintTransactionHash { get; set; } //optional.
        public Dictionary<string, object> MetaData { get; set; }  //optional.
        public string JSONMetaDataURL { get; set; }  //optional.
        //public string JSONMetaData { get; set; }
        public string UpdateAuthority { get; set; }
        public string Symbol { get; set; } //optional.
        public uint SellerFeeBasisPoints { get; set; }  //     Seller cut //optional.
        public decimal Price { get; set; } //optional.
        public decimal Discount { get; set; } //optional.
        public byte[] Image { get; set; } //optional.
        public string ImageUrl { get; set; } //optional.
        public byte[] Thumbnail { get; set; } //optional.
        public string ThumbnailUrl { get; set; } //optional.
        public string MemoText { get; set; } //optional.
    }
}