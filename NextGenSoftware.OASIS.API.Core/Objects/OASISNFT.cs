using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class OASISNFT : IOASISNFT
    {
        public Guid Id { get; set; }
        public Guid MintedByAvatarId { get; set; }

        /// <summary>
        /// The wallet address
        /// </summary>
        public string MintedByAddress { get; set; }
        public string Hash { get; set; }
        //public Guid OffChainProviderHolonId { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public byte[] Thumbnail { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Token { get; set; } //TODO: Should be dervied from the OnChainProvider so may not need this?
        public Dictionary<string, object> MetaData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// The Blockchain to store the token on.
        /// </summary>
        public ProviderType OnChainProvider { get; set; } = ProviderType.EthereumOASIS;

        /// <summary>
        /// Where the meta data is stored for the NFT (JSON Meta file and associated media etc) - For example HoloOASIS or IPFSOASIS etc.
        /// </summary>
        public ProviderType OffChainProvider { get; set; } = ProviderType.HoloOASIS;
    }
}