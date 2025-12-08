using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests
{
    public class CreateWeb4NFTCollectionRequest : ICreateWeb4NFTCollectionRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid CreatedBy { get; set; }
        public byte[] Image { get; set; }
        public string ImageUrl { get; set; }
        public byte[] Thumbnail { get; set; }
        public string ThumbnailUrl { get; set; }
        public Dictionary<string, object> MetaData { get; set; } = new Dictionary<string, object>();
        public List<IWeb4NFT> Web4NFTs { get; set; } = new List<IWeb4NFT>(); //Can pass in either full NFT objects or just their IDs in the OASISNFTIds property
        public List<string> Web4NFTIds { get; set; } = new List<string>();
        public List<string> Tags { get; set; }
    }
}