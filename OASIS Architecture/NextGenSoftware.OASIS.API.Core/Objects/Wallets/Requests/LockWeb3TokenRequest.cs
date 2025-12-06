using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public class LockWeb3TokenRequest : ILockWeb3TokenRequest
    {
        public Guid LockedByAvatarId { get; set; }
        public Guid Web3TokenId { get; set; }
        //public string MintWalletAddress { get; set; }
        public string TokenAddress { get; set; }
    }
}