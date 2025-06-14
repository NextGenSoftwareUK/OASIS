using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public interface ISTARNFT
    {
        public NFTType NFTType { get; set; }
        IOASISNFT OASISNFT { get; set; }
        public Guid OASISNFTId { get; set; }
    }
}