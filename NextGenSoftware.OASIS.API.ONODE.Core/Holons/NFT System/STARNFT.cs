using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class STARNFT : STARHolon, ISTARNFT
    {
        public STARNFT()
        {
            this.HolonType = HolonType.STARNFT;
        }

        public NFTType NFTType { get; set; }
        public IOASISNFT OASISNFT { get; set; } //TODO: Not sure if we need this?
        public Guid OASISNFTId { get; set; }
    }
}