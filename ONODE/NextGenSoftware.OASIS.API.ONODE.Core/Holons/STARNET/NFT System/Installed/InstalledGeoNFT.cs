using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledGeoNFT : InstalledSTARNETHolon, IInstalledGeoNFT
    {
        public InstalledGeoNFT() : base("STARGeoNFTDNAJSON")
        {
            this.HolonType = HolonType.InstalledGeoNFT;
        }

        [CustomOASISProperty]
        public NFTType NFTType { get; set; }
        //public IOASISGeoSpatialNFT GeoNFT { get; set; } //TODO: Not sure if we need this?

        [CustomOASISProperty]
        public Guid GeoNFTId { get; set; }
    }
}
