using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface ILockWeb3NFTRequest
    {
        Guid LockedByAvatarId { get; set; }
        public Guid Web3NFTId { get; set; }
        //public string MintWalletAddress { get; set; }
        public string NFTTokenAddress { get; set; }
    }
}