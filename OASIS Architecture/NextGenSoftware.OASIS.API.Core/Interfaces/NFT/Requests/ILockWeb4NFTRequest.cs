using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface ILockWeb4NFTRequest : ILockWeb3NFTRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public bool WaitTillNFTLocked { get; set; }
        public int WaitForNFTToLockInSeconds { get; set; }
        public int AttemptToLockEveryXSeconds { get; set; }
    }
}