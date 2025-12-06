using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface ILockWeb4TokenRequest : ILockWeb3TokenRequest
    {
        public bool WaitTillNFTLocked { get; set; }
        public int WaitForNFTToLockInSeconds { get; set; }
        public int AttemptToLockEveryXSeconds { get; set; }
    }
}