using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT
{
    //WEB4 OASIS NFT that can contain multiple WEB3 NFTs within it (one for each chain etc).
    public interface IWeb4OASISNFT : INFTBase
    {
        //public IList<IWeb3NFT> Web3NFTs { get; set; }
        public IList<Web3NFT> Web3NFTs { get; set; }
        public IList<string> Web3NFTIds { get; set; }
    }
}