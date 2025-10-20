using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public interface IOASISGeoNFTCollection : IHolonBase
    {
        //TODO: Cant decide whether to just extend IHolon here or just have the properties directly in this interface? Because need to import/export collections like we can NFTs etc and we dont want to export the full holon object with lots of props that are not relevant.
        //Guid CreatedBy { get; set; }
        //DateTime CreatedOn { get; set; }
        //Guid ModifiedBy { get; set; }
        //DateTime ModifiedOn { get; set; }
        //Guid DeletedBy { get; set; }
        //DateTime DeletedOn { get; set; }
        //public bool IsActive { get; set; }
        //string Title { get; set; }
        //string Description { get; set; }
        //Guid Id { get; set; }
        //Dictionary<string, object> MetaData { get; set; }
        byte[] Image { get; set; }
        string ImageUrl { get; set; }
        byte[] Thumbnail { get; set; }
        string ThumbnailUrl { get; set; }
        List<IOASISGeoSpatialNFT> OASISGeoNFTs { get; set; }
        List<string> OASISGeoNFTIds { get; set; }
        List<string> Tags { get; set; }
    }
}