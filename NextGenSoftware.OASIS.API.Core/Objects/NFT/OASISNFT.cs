using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public class OASISNFT : IOASISNFT
    {
        public string UpdateAuthority { get; set; }
        public string MintAddress { get; set; }
        public string Symbol { get; set; }
        public uint SellerFeeBasisPoints { get; set; }
        public Guid Id { get; set; }
        public Guid MintedByAvatarId { get; set; }
        public DateTime MintedOn { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// The wallet address
        /// </summary>
        public string MintedByAddress { get; set; }
        public string Hash { get; set; }
        public string URL { get; set; }
        //public Guid OffChainProviderHolonId { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public byte[] Image { get; set; }
        public string ImageUrl { get; set; }
        public byte[] Thumbnail { get; set; }
        public string ThumbnailUrl { get; set; }
        //public string Token { get; set; } //TODO: Should be dervied from the OnChainProvider so may not need this?
        public string MemoText { get; set; }
        public Dictionary<string, object> MetaData { get; set; } = new Dictionary<string, object>();


        /// <summary>
        /// The Blockchain to store the token on.
        /// </summary>
        public EnumValue<ProviderType> OnChainProvider { get; set; }

        /// <summary>
        /// Where the meta data is stored for the NFT (JSON Meta file and associated media etc) - For example HoloOASIS or IPFSOASIS etc.
        /// </summary>
        public EnumValue<ProviderType> OffChainProvider { get; set; }
    }
}