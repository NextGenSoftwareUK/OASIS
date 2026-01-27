using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests
{
    public class UpdateWeb4NFTCollectionRequest : IUpdateWeb4NFTCollectionRequest
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid ModifiedBy { get; set; }
        public byte[] Image { get; set; }
        public string ImageUrl { get; set; }
        public byte[] Thumbnail { get; set; }
        public string ThumbnailUrl { get; set; }
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public List<IWeb4NFT> Web4OASISNFTs { get; set; } = new List<IWeb4NFT>(); //Can pass in either full NFT objects or just their IDs in the OASISNFTIds property
        public List<string> Web4OASISNFTIds { get; set; } = new List<string>();
        public List<string> Tags { get; set; }
        public EnumValue<ProviderType> ProviderType { get; set; }
    }
}