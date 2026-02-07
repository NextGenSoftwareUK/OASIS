using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT
{
    //WEB4 OASIS NFT that can contain multiple WEB3 NFTs within it (one for each chain etc).
    public interface IWeb4NFT : INFTBase
    {
        public IList<Guid> ParentWeb5NFTIds { get; set; }
        //public IList<IWeb3NFT> Web3NFTs { get; set; }
        public IList<Web3NFT> Web3NFTs { get; set; }

        /// <summary>
        /// Contains a list of newly minted WEB3 NFTs when a WEB4 NFT is minted or reminted (this will only be populated during the minting/reminting process and is not persited).
        /// </summary>

        [JsonIgnore]
        public IList<IWeb3NFT> NewlyMintedWeb3NFTs { get; set; }

        [JsonIgnore]
        public IList<string> Web3NFTIds { get; set; }
    }
}