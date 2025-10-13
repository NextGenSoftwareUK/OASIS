using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT
{
    public interface IOASISNFTCollection
    {
        Guid CreatedBy { get; set; }
        DateTime CreatedOn { get; set; }
        string Description { get; set; }
        Guid Id { get; set; }
        byte[] Image { get; set; }
        string ImageUrl { get; set; }
        Dictionary<string, object> MetaData { get; set; }
        byte[] Thumbnail { get; set; }
        string ThumbnailUrl { get; set; }
        string Title { get; set; }
        List<IOASISNFT> OASISNFTs { get; set; }

        //TODO: Is it better to have seperate collections for NFTs and GeoNFTs?
        //public List<IOASISGeoNFTCollection> OASIGeoSNFTs { get; set; }
    }
}