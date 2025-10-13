//using System;
//using System.Collections.Generic;
//using NextGenSoftware.Utilities;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

//namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
//{
//    public class Web3NFT
//    {
//        //public Guid MintedByAvatarId { get; set; }
//        //public Guid ImportedByAvatarId { get; set; }
//        public string OASISMintWalletAddress { get; set; } //The OASIS System account that minted the NFT.
//        public string NFTMintedUsingWalletAddress { get; set; } //This may be different to OASISMintWalletAddress if it was imported.
//        public string NFTTokenAddress { get; set; } //The address of the actual minted NFT on the chain.
//        public string UpdateAuthority { get; set; }

//        public string Symbol { get; set; }
//        //public uint SellerFeeBasisPoints { get; set; }  //     Seller cut
//        //public Guid Id { get; set; }

//        public DateTime MintedOn { get; set; }
//        public DateTime ImportedOn { get; set; }
//        //public string Title { get; set; }
//        //public string Description { get; set; }
//        public string MintTransactionHash { get; set; }
//        public string JSONMetaData { get; set; }
//        public string JSONMetaDataURL { get; set; }
//        public Guid JSONMetaDataURLHolonId { get; set; }
//        //public decimal Price { get; set; }
//        //public decimal Discount { get; set; }
//        //public byte[] Image { get; set; }
//        //public string ImageUrl { get; set; }
//        //public byte[] Thumbnail { get; set; }
//        //public string ThumbnailUrl { get; set; }
//        //public string MemoText { get; set; }
//        //public Dictionary<string, object> MetaData { get; set; } = new Dictionary<string, object>();


//        ///// <summary>
//        ///// The Blockchain to store the token on.
//        ///// </summary>
//        public EnumValue<ProviderType> OnChainProvider { get; set; }

//        ///// <summary>
//        ///// Where the meta data is stored for the NFT (JSON Meta file and associated media etc) - For example HoloOASIS or IPFSOASIS etc.
//        ///// </summary>
//        //public EnumValue<ProviderType> OffChainProvider { get; set; }
//        public bool StoreNFTMetaDataOnChain { get; set; }
//        public EnumValue<NFTStandardType> NFTStandardType { get; set; }
//        public EnumValue<NFTOffChainMetaType> NFTOffChainMetaType { get; set; }
//    }
//}