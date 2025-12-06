using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public class BurnWeb3NFTRequest : IBurnWeb3NFTRequest
    {
        public Guid BurntByAvatarId { get; set; }
        public Guid Web3NFTId { get; set; }
        //public string MintWalletAddress { get; set; }
        public string NFTTokenAddress { get; set; }
    }
}