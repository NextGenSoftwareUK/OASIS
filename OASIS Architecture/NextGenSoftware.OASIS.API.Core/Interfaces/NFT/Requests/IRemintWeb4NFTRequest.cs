using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface IRemintWeb4NFTRequest : INFTOptions
    {
        IWeb4NFT Web4NFT { get; set; }
        IList<IMintWeb3NFTRequest> Web3NFTs { get; set; }
    }
}