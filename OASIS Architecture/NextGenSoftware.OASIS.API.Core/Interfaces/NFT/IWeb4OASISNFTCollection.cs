using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT
{
    public interface IWeb4OASISNFTCollection : IWeb4OASISNFTCollectionBase
    {
        List<IWeb4OASISNFT> Web4OASISNFTs { get; set; }
        List<string> Web4OASISNFTIds { get; set; }
    }
}