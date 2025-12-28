using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests
{
    public class RemintWeb4NFTRequest : NFTOptions, IRemintWeb4NFTRequest
    {
        public IWeb4NFT Web4NFT { get; set; }
        public IList<IMintWeb3NFTRequest> Web3NFTs { get; set; }
    }
}