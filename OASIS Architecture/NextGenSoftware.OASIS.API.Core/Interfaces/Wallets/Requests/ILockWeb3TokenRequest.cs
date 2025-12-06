using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface ILockWeb3TokenRequest
    {
        Guid LockedByAvatarId { get; set; }
        public Guid Web3TokenId { get; set; }
        //public string MintWalletAddress { get; set; }
        public string TokenAddress { get; set; }
    }
}