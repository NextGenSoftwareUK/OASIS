using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests
{
    public class LockWeb4NFTRequest : LockWeb3NFTRequest, ILockWeb4NFTRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public bool WaitTillNFTLocked { get; set; } = true;
        public int WaitForNFTToLockInSeconds { get; set; } = 60;
        public int AttemptToLockEveryXSeconds { get; set; } = 5;
    }
}