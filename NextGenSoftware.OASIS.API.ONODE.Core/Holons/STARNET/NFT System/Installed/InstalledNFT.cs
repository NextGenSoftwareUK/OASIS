using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledNFT : InstalledSTARNETHolon, IInstalledNFT
    {
        public InstalledNFT() : base("NFTDNAJSON")
        {
            this.HolonType = HolonType.InstalledNFT;
        }

        public NFTType NFTType { get; set; }
        public IOASISNFT OASISNFT { get; set; } //TODO: Not sure if we need this?
        public Guid OASISNFTId { get; set; }
    }
}
