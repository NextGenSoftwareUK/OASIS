using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public class Web4OASISGeoNFTCollection : Web4OASISNFTCollectionBase, IWeb4OASISGeoNFTCollection
    {
        public Web4OASISGeoNFTCollection() : base(Enums.HolonType.GeoNFTCollection) { }

        [CustomOASISProperty]
        public List<IWeb4OASISGeoSpatialNFT> Web4OASISGeoNFTs { get; set; } = new List<IWeb4OASISGeoSpatialNFT>();

        [CustomOASISProperty]
        public List<string> Web4OASISGeoNFTIds { get; set; } = new List<string>();
    }
}