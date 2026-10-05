using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public class Web4NFT : NFTBase, IWeb4NFT
    {
        //public IList<IWeb3NFT> Web3NFTs { get; set; } = new List<IWeb3NFT>();
        public IList<Web3NFT> Web3NFTs { get; set; } = new List<Web3NFT>();
        public IList<string> Web3NFTIds { get; set; } = new List<string>();
    }
}