using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface IUpdateWeb4NFTRequest
    {
        Guid Id { get; set; }
        string Description { get; set; }
        decimal? Discount { get; set; }
        byte[] Image { get; set; }
        string ImageUrl { get; set; }
        Dictionary<string, object> MetaData { get; set; }
        decimal? Price { get; set; }
        List<string> Tags { get; set; }
        byte[] Thumbnail { get; set; }
        string ThumbnailUrl { get; set; }
        string Title { get; set; }
        Guid ModifiedByAvatarId { get; set; }

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
    }
}