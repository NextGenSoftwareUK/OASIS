using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT
{
    public interface IWeb4OASISNFTCollection : IHolonBase
    {
        byte[] Image { get; set; }
        string ImageUrl { get; set; }
        byte[] Thumbnail { get; set; }
        string ThumbnailUrl { get; set; }
        List<IWeb4OASISNFT> Web4OASISNFTs { get; set; }
        List<string> Web4OASISNFTIds { get; set; }
        List<string> Tags { get; set; }
    }
}