using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface IUpdateWeb4NFTCollectionRequestBase
    {
        string Description { get; set; }
        Guid Id { get; set; }
        byte[] Image { get; set; }
        string ImageUrl { get; set; }
        Dictionary<string, object> MetaData { get; set; }
        Guid ModifiedBy { get; set; }
        List<string> Tags { get; set; }
        byte[] Thumbnail { get; set; }
        string ThumbnailUrl { get; set; }
        string Title { get; set; }
    }
}