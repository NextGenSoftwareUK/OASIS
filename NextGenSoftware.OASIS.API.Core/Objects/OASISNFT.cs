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
        public decimal Price { get; set; }
        public decimal Discount { get; set; }

        public byte[] Thumbnail { get; set; }

        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();

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