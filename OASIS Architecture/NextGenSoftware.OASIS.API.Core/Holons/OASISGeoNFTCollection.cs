using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    //public class OASISGeoNFTCollection : HolonBase, IOASISGeoNFTCollection
    public class OASISGeoNFTCollection : Holon, IOASISGeoNFTCollection
    {
        public OASISGeoNFTCollection() : base(Enums.HolonType.GeoNFTCollection) { }

        [CustomOASISProperty]
        public byte[] Image { get; set; }

        [CustomOASISProperty]
        public string ImageUrl { get; set; }

        [CustomOASISProperty]
        public byte[] Thumbnail { get; set; }

        [CustomOASISProperty]
        public string ThumbnailUrl { get; set; }

        [CustomOASISProperty]
        public List<IOASISGeoSpatialNFT> OASISGeoNFTs { get; set; } = new List<IOASISGeoSpatialNFT>();

        [CustomOASISProperty]
        public List<string> OASISGeoNFTIds { get; set; } = new List<string>();

        [CustomOASISProperty]
        public List<string> Tags { get; set; }
    }
}