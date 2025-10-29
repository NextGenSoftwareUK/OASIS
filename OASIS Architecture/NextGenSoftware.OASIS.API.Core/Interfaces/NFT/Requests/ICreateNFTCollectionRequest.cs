using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface ICreateNFTCollectionRequest
    {
        Guid CreatedBy { get; set; }
        string Description { get; set; }
        byte[] Image { get; set; }
        string ImageUrl { get; set; }
        Dictionary<string, object> MetaData { get; set; }
        List<string> OASISNFTIds { get; set; }
        List<IOASISNFT> OASISNFTs { get; set; }
        byte[] Thumbnail { get; set; }
        string ThumbnailUrl { get; set; }
        string Title { get; set; }
        List<string> Tags { get; set; }
    }
}