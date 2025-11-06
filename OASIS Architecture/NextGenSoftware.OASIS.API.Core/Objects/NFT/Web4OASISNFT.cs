using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public class Web4OASISNFT : NFTBase, IWeb4OASISNFT
    {
        public IList<IWeb3NFT> Web3NFTs { get; set; } = new List<IWeb3NFT>();
    }
}