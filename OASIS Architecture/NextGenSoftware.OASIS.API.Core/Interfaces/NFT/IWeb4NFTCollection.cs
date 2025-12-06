using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT
{
    public interface IWeb4NFTCollection : IWeb4NFTCollectionBase
    {
        List<IWeb4NFT> Web4NFTs { get; set; }
        List<string> Web4NFTIds { get; set; }
    }
}