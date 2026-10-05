using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface ICreateWeb4GeoNFTCollectionRequest
    {
        Guid CreatedBy { get; set; }
        string Description { get; set; }
        byte[] Image { get; set; }
        string ImageUrl { get; set; }
        Dictionary<string, object> MetaData { get; set; }
        byte[] Thumbnail { get; set; }
        string ThumbnailUrl { get; set; }
        string Title { get; set; }
        List<string> Web4GeoNFTIds { get; set; }
        List<IWeb4GeoSpatialNFT> Web4GeoNFTs { get; set; }
        List<string> Tags { get; set; }
    }
}