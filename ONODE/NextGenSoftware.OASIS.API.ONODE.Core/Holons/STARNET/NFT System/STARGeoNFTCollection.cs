using System;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class STARGeoNFTCollection : STARNETHolon, ISTARGeoNFTCollection
    {
        public STARGeoNFTCollection() : base("STARGeoNFTCollectionDNAJSON")
        {
            this.HolonType = HolonType.Web5GeoNFTCollection;
        }

        //We need to store this here as well as in the STARNETDNA so the OASIS SearchManager can search by it as well as HolonManager LoadByMetaData etc... later we will also be able to search the DNA (this may not be needed then depending on performance because it means searching the JSON in the DNA)...
        [CustomOASISProperty]
        public NFTCollectionType GeoNFTCollectionType { get; set; }

        //We need to store this here as well as in the STARNETDNA so the OASIS SearchManager can search by it as well as HolonManager LoadByMetaData etc... later we will also be able to search the DNA (this may not be needed then depending on performance because it means searching the JSON in the DNA)...
        [CustomOASISProperty]
        public Guid GeoNFTCollectionId { get; set; }
    }
}