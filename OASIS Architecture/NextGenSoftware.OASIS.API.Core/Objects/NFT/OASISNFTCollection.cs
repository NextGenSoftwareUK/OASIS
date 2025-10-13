using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public class OASISNFTCollection : IOASISNFTCollection
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
        public List<IOASISNFT> OASISNFTs { get; set; } = new List<IOASISNFT>();

        //TODO: Is it better to have seperate collections for NFTs and GeoNFTs?
        //public List<IOASISGeoNFTCollection> OASIGeoSNFTs { get; set; } = new List<IOASISGeoNFTCollection>();
    }
}