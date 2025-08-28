using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT
{
    public class MintNFTTransactionRequest : OASISRequest
    {
        public string WalletAddress { get; set; }
        public string NFTMetadata { get; set; }
        public string Provider { get; set; }
        public Guid? AvatarId { get; set; }
        public string OnChainProvider { get; set; }
        public string OffChainProvider { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public byte[] Image { get; set; }
        public string ImageUrl { get; set; }
        public byte[] Thumbnail { get; set; }
        public string ThumbnailUrl { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public string MemoText { get; set; }
        public int NumberToMint { get; set; }
        public Dictionary<string, object> MetaData { get; set; }
    }
}
