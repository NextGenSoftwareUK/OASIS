using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class STARNFT : STARNETHolon, ISTARNFT
    {
        public STARNFT() : base("STARNFTDNAJSON")
        {
            this.HolonType = HolonType.STARNFT;
        }

        public NFTType NFTType { get; set; }
        public IOASISNFT OASISNFT { get; set; } //TODO: Not sure if we need this?
        public Guid OASISNFTId { get; set; }
    }
}