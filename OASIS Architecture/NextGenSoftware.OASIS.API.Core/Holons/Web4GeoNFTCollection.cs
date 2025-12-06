using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public class Web4GeoNFTCollection : Web4NFTCollectionBase, IWeb4GeoNFTCollection
    {
        public Web4GeoNFTCollection() : base(Enums.HolonType.Web4GeoNFTCollection) { }

        [CustomOASISProperty]
        public List<IWeb4GeoSpatialNFT> Web4OASISGeoNFTs { get; set; } = new List<IWeb4GeoSpatialNFT>();

        [CustomOASISProperty]
        public List<string> Web4OASISGeoNFTIds { get; set; } = new List<string>();
    }
}