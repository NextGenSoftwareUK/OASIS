using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public class MintNFTTransaction : IMintNFTTransaction
    {
        public string MintWalletAddress { get; set; }
        public Guid MintedByAvatarId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public byte[] Thumbnail { get; set; }
        public string ThumbnailUrl { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public string MemoText { get; set; }
        // public string Token { get; set; } //TODO: Should be dervied from the OnChainProvider so may not need this?
        public int NumberToMint { get; set; }
        public Dictionary<string, object> MetaData { get; set; }
        public ProviderType OffChainProvider { get; set; }
        public ProviderType OnChainProvider { get; set; }
    }
}