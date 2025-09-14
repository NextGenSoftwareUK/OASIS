using System;
using NextGenSoftware.Utilities;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT
{
    public interface IOASISNFT
    {
        public string SendToAddressAfterMinting { get; set; }
        public Guid SendToAvatarAfterMintingId { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingUsername { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        //public string SendToAvatarAfterMintingEmail { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendNFTTransactionHash { get; set; }
        public Guid MintedByAvatarId { get; set; }
        public string OASISMintWalletAddress { get; set; } //The OASIS System account that minted the NFT.
        public string NFTTokenAddress { get; set; } //The address of the actual minted NFT on the chain.
        public string UpdateAuthority { get; set; }
        string Symbol { get; set; }
        uint SellerFeeBasisPoints { get; set; } //     Seller cut
        Guid Id { get; set; }
        DateTime MintedOn { get; set; }
        string MintTransactionHash { get; set; }
        string JSONMetaDataURL { get; set; }
        string Title { get; set; }
        string Description { get; set; }
        Guid JSONMetaDataURIHolonId { get; set; }
        decimal Price { get; set; }
        decimal Discount { get; set; }
        public byte[] Image { get; set; }
        public string ImageUrl { get; set; }
        byte[] Thumbnail { get; set; }
        string ThumbnailUrl { get; set; }
        //public string Token { get; set; } //TODO: Should be dervied from the OnChainProvider so may not need this?
        public string MemoText { get; set; }
        Dictionary<string, object> MetaData { get; set; }
        EnumValue<ProviderType> OffChainProvider { get; set; }
        EnumValue<ProviderType> OnChainProvider { get; set; }
        public bool StoreNFTMetaDataOnChain { get; set; }
        public EnumValue<NFTStandardType> NFTStandardType { get; set; }
        public EnumValue<NFTOffChainMetaType> NFTOffChainMetaType { get; set; }

    }
}