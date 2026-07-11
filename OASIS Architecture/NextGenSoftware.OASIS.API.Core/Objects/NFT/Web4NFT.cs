using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public class Web4NFT : NFTBase, IWeb4NFT
    {
        public IList<Guid> ParentWeb5NFTIds { get; set; } = new List<Guid>();
        public IList<Web3NFT> Web3NFTs { get; set; } = new List<Web3NFT>();

        /// <summary>
        /// Contains a list of newly minted WEB3 NFTs when a WEB4 NFT is minted or reminted (this will only be populated during the minting/reminting process and is not persited).
        /// </summary>
        [JsonIgnore]
        public IList<IWeb3NFT> NewlyMintedWeb3NFTs { get; set; }

        [JsonIgnore]
        public IList<string> Web3NFTIds { get; set; } = new List<string>();
    }
}