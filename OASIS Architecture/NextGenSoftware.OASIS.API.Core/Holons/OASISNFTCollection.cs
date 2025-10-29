using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    //public class OASISNFTCollection : IOASISNFTCollection
    //public class OASISNFTCollection : HolonBase, IOASISNFTCollection
    public class OASISNFTCollection : Holon, IOASISNFTCollection
    {
        //public Guid Id { get; set; }
        //public string Title { get; set; }
        //public string Description { get; set; }
        //public DateTime CreatedOn { get; set; }
        //public Guid CreatedBy { get; set; }

        public OASISNFTCollection() : base(Enums.HolonType.NFTCollection) { }

        [CustomOASISProperty]
        public byte[] Image { get; set; }

        [CustomOASISProperty]
        public string ImageUrl { get; set; }

        [CustomOASISProperty]
        public byte[] Thumbnail { get; set; }

        [CustomOASISProperty]
        public string ThumbnailUrl { get; set; }
        //public Dictionary<string, object> MetaData { get; set; } = new Dictionary<string, object>();

        [CustomOASISProperty]
        public List<IOASISNFT> OASISNFTs { get; set; } = new List<IOASISNFT>();

        [CustomOASISProperty]
        public List<string> OASISNFTIds { get; set; } = new List<string>();

        [CustomOASISProperty]
        public List<string> Tags { get; set; }
    }
}