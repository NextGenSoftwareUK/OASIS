
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface IUnlockWeb4NFTRequest : IUnlockWeb3NFTRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public bool WaitTillNFTUnlocked { get; set; }
        public int WaitForNFTToUnlockInSeconds { get; set; }
        public int AttemptToUnlockEveryXSeconds { get; set; }
    }
}