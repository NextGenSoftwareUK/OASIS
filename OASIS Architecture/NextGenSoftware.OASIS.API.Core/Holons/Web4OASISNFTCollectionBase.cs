using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public class Web4OASISNFTCollectionBase : Holon, IWeb4OASISNFTCollectionBase
    {
        public Web4OASISNFTCollectionBase(HolonType holonType) : base(holonType) { }

        [CustomOASISProperty]
        public byte[] Image { get; set; }

        [CustomOASISProperty]
        public string ImageUrl { get; set; }

        [CustomOASISProperty]
        public byte[] Thumbnail { get; set; }

        [CustomOASISProperty]
        public string ThumbnailUrl { get; set; }

        [CustomOASISProperty]
        public List<string> Tags { get; set; }
    }
}