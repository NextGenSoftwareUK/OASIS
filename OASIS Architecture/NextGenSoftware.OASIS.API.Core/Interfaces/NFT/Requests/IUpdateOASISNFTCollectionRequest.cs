using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface IUpdateOASISNFTCollectionRequest
    {
        string Description { get; set; }
        Guid Id { get; set; }
        byte[] Image { get; set; }
        string ImageUrl { get; set; }
        Dictionary<string, object> MetaData { get; set; }
        Guid ModifiedBy { get; set; }
        List<string> OASISNFTIds { get; set; }
        List<IOASISNFT> OASISNFTs { get; set; }
        List<string> Tags { get; set; }
        byte[] Thumbnail { get; set; }
        string ThumbnailUrl { get; set; }
        string Title { get; set; }
    }
}