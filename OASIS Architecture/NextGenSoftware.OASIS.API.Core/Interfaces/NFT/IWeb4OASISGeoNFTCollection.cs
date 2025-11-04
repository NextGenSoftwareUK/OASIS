using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public interface IWeb4OASISGeoNFTCollection : IWeb4OASISNFTCollectionBase
    {
        List<IWeb4OASISGeoSpatialNFT> Web4OASISGeoNFTs { get; set; }
        List<string> Web4OASISGeoNFTIds { get; set; }
    }
}