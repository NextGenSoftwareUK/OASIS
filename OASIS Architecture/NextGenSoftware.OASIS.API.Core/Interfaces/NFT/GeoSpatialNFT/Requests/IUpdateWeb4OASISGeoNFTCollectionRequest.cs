using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Requests
{
    public interface IUpdateWeb4OASISGeoNFTCollectionRequest
    {
        string Description { get; set; }
        Guid Id { get; set; }
        byte[] Image { get; set; }
        string ImageUrl { get; set; }
        Dictionary<string, object> MetaData { get; set; }
        Guid ModifiedBy { get; set; }
        //List<string> OASISGeoNFTIds { get; set; }
        //List<IOASISGeoSpatialNFT> OASISGeoNFTs { get; set; }
        List<string> Tags { get; set; }
        byte[] Thumbnail { get; set; }
        string ThumbnailUrl { get; set; }
        string Title { get; set; }
    }
}