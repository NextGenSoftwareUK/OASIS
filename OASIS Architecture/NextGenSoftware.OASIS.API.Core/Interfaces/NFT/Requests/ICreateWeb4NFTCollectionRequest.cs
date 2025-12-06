using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface ICreateWeb4NFTCollectionRequest
    {
        Guid CreatedBy { get; set; }
        string Description { get; set; }
        byte[] Image { get; set; }
        string ImageUrl { get; set; }
        Dictionary<string, object> MetaData { get; set; }
        List<string> Web4NFTIds { get; set; }
        List<IWeb4NFT> Web4NFTs { get; set; }
        byte[] Thumbnail { get; set; }
        string ThumbnailUrl { get; set; }
        string Title { get; set; }
        List<string> Tags { get; set; }
    }
}