using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    //public class OASISNFTCollection : IOASISNFTCollection
    //public class OASISNFTCollection : HolonBase, IOASISNFTCollection
    public class Web4OASISNFTCollection : Holon, IWeb4OASISNFTCollection
    {
        //public Guid Id { get; set; }
        //public string Title { get; set; }
        //public string Description { get; set; }
        //public DateTime CreatedOn { get; set; }
        //public Guid CreatedBy { get; set; }

        public Web4OASISNFTCollection() : base(Enums.HolonType.NFTCollection) { }

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
        public List<IWeb4OASISNFT> Web4OASISNFTs { get; set; } = new List<IWeb4OASISNFT>();

        [CustomOASISProperty]
        public List<string> Web4OASISNFTIds { get; set; } = new List<string>();

        [CustomOASISProperty]
        public List<string> Tags { get; set; }
    }
}