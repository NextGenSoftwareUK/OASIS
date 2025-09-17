using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledGeoNFT : InstalledSTARNETHolon, IInstalledGeoNFT
    {
        public InstalledGeoNFT() : base("GeoNFTDNAJSON")
        {
            this.HolonType = HolonType.InstalledGeoNFT;
        }

        public NFTType NFTType { get; set; }
        public IOASISGeoSpatialNFT GeoNFT { get; set; } //TODO: Not sure if we need this?
        public Guid GeoNFTId { get; set; }
    }
}
