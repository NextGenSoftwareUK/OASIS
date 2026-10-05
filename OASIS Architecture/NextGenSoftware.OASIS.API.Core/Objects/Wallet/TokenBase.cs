using System;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public abstract class TokenBase : ITokenBase
    {
        public Guid Id { get; set; }
        public string SendToAddressAfterMinting { get; set; }
        public Guid SendToAvatarAfterMintingId { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingUsername { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.

        public Guid MintedByAvatarId { get; set; }
        public Guid ImportedByAvatarId { get; set; }
        public Guid ModifiedByAvatarId { get; set; }
        
        public string Symbol { get; set; }
        public uint SellerFeeBasisPoints { get; set; }  //     Seller cut
       
        
        public DateTime MintedOn { get; set; }
        public DateTime ImportedOn { get; set; }
        public DateTime ModifiedOn { get; set; }    
        public string Title { get; set; }
        public string Description { get; set; }
        //public string JSONMetaData { get; set; }
        //public string JSONMetaDataURL { get; set; }
        //public Guid JSONMetaDataURLHolonId { get; set; }
        public decimal Price { get; set; }
        //public decimal Discount { get; set; }
        //public byte[] Image { get; set; }
        //public string ImageUrl { get; set; }
        //public byte[] Thumbnail { get; set; }
        //public string ThumbnailUrl { get; set; }
        //public string Token { get; set; } //TODO: Should be dervied from the OnChainProvider so may not need this?
        public string MemoText { get; set; }
        public Dictionary<string, object> MetaData { get; set; } = new Dictionary<string, object>();

        public List<string> Tags { get; set; }


        /// <summary>
        /// The Blockchain to store the token on.
        /// </summary>
        public EnumValue<ProviderType> OnChainProvider { get; set; }

        /// <summary>
        /// Where the meta data is stored for the NFT (JSON Meta file and associated media etc) - For example HoloOASIS or IPFSOASIS etc.
        /// </summary>
        //public EnumValue<ProviderType> OffChainProvider { get; set; }
        //public bool StoreNFTMetaDataOnChain { get; set; }
        //public EnumValue<NFTStandardType> NFTStandardType { get; set; }
        //public EnumValue<NFTOffChainMetaType> NFTOffChainMetaType { get; set; }



        //public int RoyaltyPercentage { get; set; }
        //public Guid PreviousOwnerAvatarId { get; set; }
        //public Guid CurrentOwnerAvatarId { get; set; }
        //public bool IsForSale { get; set; }
        //public DateTime? SaleStartDate { get; set; }
        //public DateTime? SaleEndDate { get; set; }
        //public int TotalNumberOfSales { get; set; }
        //public string LastSaleTransactionHash { get; set; }
        //public Guid LastSoldByAvatarId { get; set; }
        //public Guid LastPurchasedByAvatarId { get; set; }
        //public int LastSaleQuantity { get; set; }
        //public decimal LastSaleDiscount { get; set; }
        //public decimal LastSaleTax { get; set; }
        //public string SalesHistory { get; set; }
        //public decimal LastSalePrice { get; set; }
        //public decimal LastSaleAmount { get; set; }
        //public DateTime LastSaleDate { get; set; }
     }
}