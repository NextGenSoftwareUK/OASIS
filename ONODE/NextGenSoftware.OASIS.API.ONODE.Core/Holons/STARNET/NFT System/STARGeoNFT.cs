using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class STARGeoNFT : STARNETHolon, ISTARGeoNFT
    {
        public STARGeoNFT() : base("STARGeoNFTDNAJSON")
        {
            this.HolonType = HolonType.STARGeoNFT;
        }

        //We need to store this here as well as in the STARNETDNA so the OASIS SearchManager can search by it as well as HolonManager LoadByMetaData etc... later we will also be able to search the DNA (this may not be needed then depending on performance because it means searching the JSON in the DNA)...
        [CustomOASISProperty]
        public NFTType NFTType { get; set; }

        //We need to store this here as well as in the STARNETDNA so the OASIS SearchManager can search by it as well as HolonManager LoadByMetaData etc... later we will also be able to search the DNA (this may not be needed then depending on performance because it means searching the JSON in the DNA)...//We need to store this here as well as in the STARNETDNA so the OASIS SearchManager can search by it as well as HolonManager LoadByMetaData etc... later we will also be able to search the DNA (this may not be needed then depending on performance because it means searching the JSON in the DNA)...
        [CustomOASISProperty]
        public Guid GeoNFTId { get; set; }
    }
}