using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public class Web4NFTCollection : Web4NFTCollectionBase, IWeb4NFTCollection
    {
        public Web4NFTCollection() : base(Enums.HolonType.Web4NFTCollection) { }

        [CustomOASISProperty]
        public IList<Guid> ParentWeb5NFTCollectionIds { get; set; }

        [CustomOASISProperty]
        public List<IWeb4NFT> Web4NFTs { get; set; } = new List<IWeb4NFT>();

        [CustomOASISProperty]
        public List<string> Web4NFTIds { get; set; } = new List<string>();
    }
}