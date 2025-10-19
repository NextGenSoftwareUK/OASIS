using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public class OASISGeoNFTCollection : IOASISGeoNFTCollection
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedOn { get; set; }
        public Guid CreatedBy { get; set; }
        public byte[] Image { get; set; }
        public string ImageUrl { get; set; }
        public byte[] Thumbnail { get; set; }
        public string ThumbnailUrl { get; set; }
        public Dictionary<string, object> MetaData { get; set; } = new Dictionary<string, object>();
        public List<IOASISGeoSpatialNFT> OASISGeoNFTs { get; set; } = new List<IOASISGeoSpatialNFT>();
        public List<string> OASISGeoNFTIds { get; set; } = new List<string>();
        public List<string> Tags { get; set; }
    }
}