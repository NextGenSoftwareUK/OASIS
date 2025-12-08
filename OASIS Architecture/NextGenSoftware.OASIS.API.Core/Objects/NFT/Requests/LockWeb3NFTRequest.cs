using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests
{
    public class LockWeb3NFTRequest : ILockWeb3NFTRequest
    {
        public Guid LockedByAvatarId { get; set; }
        public Guid Web3NFTId { get; set; }
        //public string MintWalletAddress { get; set; }
        public string NFTTokenAddress { get; set; }
    }
}