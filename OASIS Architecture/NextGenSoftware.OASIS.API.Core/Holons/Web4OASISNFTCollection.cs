using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public class Web4OASISNFTCollection : Web4OASISNFTCollectionBase, IWeb4OASISNFTCollection
    {
        public Web4OASISNFTCollection() : base(Enums.HolonType.NFTCollection) { }

        [CustomOASISProperty]
        public List<IWeb4OASISNFT> Web4OASISNFTs { get; set; } = new List<IWeb4OASISNFT>();

        [CustomOASISProperty]
        public List<string> Web4OASISNFTIds { get; set; } = new List<string>();
    }
}