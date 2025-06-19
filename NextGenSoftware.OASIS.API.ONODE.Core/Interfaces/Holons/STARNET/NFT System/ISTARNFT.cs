using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface ISTARNFT : ISTARNETHolon
    {
        public NFTType NFTType { get; set; }
        IOASISNFT OASISNFT { get; set; }
        public Guid OASISNFTId { get; set; }
    }
}