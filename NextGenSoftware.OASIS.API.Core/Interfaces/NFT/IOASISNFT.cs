using System;
using NextGenSoftware.Utilities;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT
{
    public interface IOASISNFT
    {
        string UpdateAuthority { get; set; }
        string MintAddress { get; set; }
        string Symbol { get; set; }
        uint SellerFeeBasisPoints { get; set; }
        Guid Id { get; set; }
        Guid MintedByAvatarId { get; set; }
        DateTime MintedOn { get; set; }
        string MintedByAddress { get; set; }
        string Hash { get; set; }
        string URL { get; set; }
        string Title { get; set; }

        string Description { get; set; }

        // Guid OffChainProviderHolonId { get; set; }
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
    }
}