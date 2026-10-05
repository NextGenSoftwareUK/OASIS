using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests
{
    public class UpdateWeb4NFTRequest : IUpdateWeb4NFTRequest
    {
        public Guid Id { get; set; }   
        public Guid ModifiedByAvatarId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public byte[] Image { get; set; }
        public string ImageUrl { get; set; }
        public byte[] Thumbnail { get; set; }
        public string ThumbnailUrl { get; set; }
        public decimal? Price { get; set; }
        public decimal? Discount { get; set; }
        public Dictionary<string, object> MetaData { get; set; }
        public List<string> Tags { get; set; }


        public int? RoyaltyPercentage { get; set; }
        public Guid PreviousOwnerAvatarId { get; set; }
        public Guid CurrentOwnerAvatarId { get; set; }
        public bool? IsForSale { get; set; }
        public DateTime? SaleStartDate { get; set; }
        public DateTime? SaleEndDate { get; set; }
        public int? TotalNumberOfSales { get; set; }
        public string LastSaleTransactionHash { get; set; }
        public Guid LastSoldByAvatarId { get; set; }
        public Guid LastPurchasedByAvatarId { get; set; }
        public int? LastSaleQuantity { get; set; }
        public decimal? LastSaleDiscount { get; set; }
        public decimal? LastSaleTax { get; set; }
        public string SalesHistory { get; set; } //TODO: Create a collection of INFTSale objects instead.
        public decimal? LastSalePrice { get; set; }
        public decimal? LastSaleAmount { get; set; }
        public DateTime LastSaleDate { get; set; }
        public bool UpdateAllChildWeb3NFTs { get; set; }
        public IList<string> UpdateChildWebNFTIds { get; set; } //Only update the web nfts with these ids.

        public EnumValue<ProviderType> ProviderType { get; set; }


        //public EnumValue<ProviderType> OffChainProvider { get; set; }
        //public EnumValue<ProviderType> OnChainProvider { get; set; }
        //public EnumValue<NFTStandardType> NFTStandardType { get; set; }
        //public EnumValue<NFTOffChainMetaType> NFTOffChainMetaType { get; set; }
        //public string Symbol { get; set; }
        //public string JSONMetaDataURL { get; set; }
        //public string JSONMetaData { get; set; }
        //public bool WaitTillNFTMinted { get; set; } = true;
        //public int WaitForNFTToMintInSeconds { get; set; } = 60;
        //public int AttemptToMintEveryXSeconds { get; set; } = 1;
        //public string SendToAddressAfterMinting { get; set; } //optionally send to this wallet after it has been minted.
        //public Guid SendToAvatarAfterMintingId { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        //public string SendToAvatarAfterMintingUsername { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        //public string SendToAvatarAfterMintingEmail { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        //public bool WaitTillNFTSent { get; set; } = true;
        //public int WaitForNFTToSendInSeconds { get; set; } = 60;
        //public int AttemptToSendEveryXSeconds { get; set; } = 1;
    }
}