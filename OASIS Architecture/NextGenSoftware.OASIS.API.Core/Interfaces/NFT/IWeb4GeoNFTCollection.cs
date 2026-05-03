using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public interface IWeb4GeoNFTCollection : IWeb4NFTCollectionBase
    {
        IList<Guid> ParentWeb5GeoNFTCollectionIds { get; set; }
        List<IWeb4GeoSpatialNFT> Web4GeoNFTs { get; set; }
        List<string> Web4GeoNFTIds { get; set; }
    }
}