using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public class Web4Token : TokenBase, IWeb4Token
    {
        //public IList<IWeb3NFT> Web3NFTs { get; set; } = new List<IWeb3NFT>();
        public IList<Web3Token> Web3Tokens { get; set; } = new List<Web3Token>();
        public IList<string> Web3TokenIds { get; set; } = new List<string>();
    }
}