using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public class LockWeb4NFTRequest : LockWeb3NFTRequest, ILockWeb4NFTRequest
    {
        public bool WaitTillNFTLocked { get; set; }
        public int WaitForNFTToLockInSeconds { get; set; }
        public int AttemptToLockEveryXSeconds { get; set; }
    }
}