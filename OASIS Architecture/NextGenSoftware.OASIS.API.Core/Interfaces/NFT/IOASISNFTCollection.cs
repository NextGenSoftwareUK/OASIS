using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT
{
    public interface IOASISNFTCollection : IHolonBase
    {
        byte[] Image { get; set; }
        string ImageUrl { get; set; }
        byte[] Thumbnail { get; set; }
        string ThumbnailUrl { get; set; }
        List<IOASISNFT> OASISNFTs { get; set; }
        List<string> OASISNFTIds { get; set; }
        List<string> Tags { get; set; }
    }
}