using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT
{
    public interface INFTBase
    {
        Guid Id { get; set; }
        public string SendToAddressAfterMinting { get; set; }
        public Guid SendToAvatarAfterMintingId { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingUsername { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        //public string SendToAvatarAfterMintingEmail { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        //public string SendNFTTransactionHash { get; set; }
        public Guid MintedByAvatarId { get; set; }
        public Guid ImportedByAvatarId { get; set; }
        public Guid ModifiedByAvatarId { get; set; }
        //public string OASISMintWalletAddress { get; set; } //The OASIS System account that minted the NFT.
        //public string NFTMintedUsingWalletAddress { get; set; } //This may be different to OASISMintWalletAddress if it was imported.
        //public string NFTTokenAddress { get; set; } //The address of the actual minted NFT on the chain.
        //public string UpdateAuthority { get; set; }
        string Symbol { get; set; }
        uint SellerFeeBasisPoints { get; set; } //     Seller cut
        DateTime MintedOn { get; set; }
        DateTime ImportedOn { get; set; }
        DateTime ModifiedOn { get; set; }
        string JSONMetaData { get; set; }
        string JSONMetaDataURL { get; set; }
        Guid JSONMetaDataURLHolonId { get; set; }
        string Title { get; set; }
        string Description { get; set; }
        decimal Price { get; set; }
        decimal Discount { get; set; }
        public byte[] Image { get; set; }
        public string ImageUrl { get; set; }
        byte[] Thumbnail { get; set; }
        string ThumbnailUrl { get; set; }
        //public string Token { get; set; } //TODO: Should be dervied from the OnChainProvider so may not need this?
        public string MemoText { get; set; }
        //Dictionary<string, object> MetaData { get; set; } //TODO: Possibly change to string for values... but then how do we store binaries? Could serilaize?
        Dictionary<string, string> MetaData { get; set; }
        public List<string> Tags { get; set; }
        EnumValue<ProviderType> OffChainProvider { get; set; }
        EnumValue<ProviderType> OnChainProvider { get; set; }
        public bool StoreNFTMetaDataOnChain { get; set; }
        public EnumValue<NFTStandardType> NFTStandardType { get; set; }
        public EnumValue<NFTOffChainMetaType> NFTOffChainMetaType { get; set; }

        //TODO: Integrate these properties properly later...
        public int RoyaltyPercentage { get; set; }
        public Guid PreviousOwnerAvatarId { get; set; }
        public Guid CurrentOwnerAvatarId { get; set; }
        public bool IsForSale { get; set; }
        public DateTime? SaleStartDate { get; set; }
        public DateTime? SaleEndDate { get; set; }
        public int TotalNumberOfSales { get; set; }
        public string LastSaleTransactionHash { get; set; }
        public Guid LastSoldByAvatarId { get; set; }
        public Guid LastPurchasedByAvatarId { get; set; }
        public int LastSaleQuantity { get; set; }
        public decimal LastSaleDiscount { get; set; }
        public decimal LastSaleTax { get; set; }
        public string SalesHistory { get; set; } //TODO: Create a collection of INFTSale objects instead.
        public decimal LastSalePrice { get; set; }
        public decimal LastSaleAmount { get; set; }
        public DateTime LastSaleDate { get; set; }
    }
}