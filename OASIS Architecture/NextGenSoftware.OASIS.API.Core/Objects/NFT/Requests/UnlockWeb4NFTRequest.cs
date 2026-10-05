
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests
{
    public class UnlockWeb4NFTRequest : UnlockWeb3NFTRequest, IUnlockWeb4NFTRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public bool WaitTillNFTUnlocked { get; set; } = true;
        public int WaitForNFTToUnlockInSeconds { get; set; } = 60;
        public int AttemptToUnlockEveryXSeconds { get; set; } = 5;
    }
}