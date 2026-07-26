using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public interface IWeb4NFTCollectionBase : IHolonBase
    {
        new Dictionary<string, string> MetaData { get; set; }
        byte[] Image { get; set; }
        string ImageUrl { get; set; }
        byte[] Thumbnail { get; set; }
        string ThumbnailUrl { get; set; }
        List<string> Tags { get; set; }
    }
}