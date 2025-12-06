using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public class LockWeb4TokenRequest : LockWeb3TokenRequest, ILockWeb4TokenRequest
    {
        public bool WaitTillNFTLocked { get; set; }
        public int WaitForNFTToLockInSeconds { get; set; }
        public int AttemptToLockEveryXSeconds { get; set; }
    }
}