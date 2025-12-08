using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests
{
    public class UnlockWeb3NFTRequest : IUnlockWeb3NFTRequest
    {
        public Guid UnlockedByAvatarId { get; set; }
        public Guid Web3NFTId { get; set; }
        //public string MintWalletAddress { get; set; }
        public string NFTTokenAddress { get; set; }
    }
}