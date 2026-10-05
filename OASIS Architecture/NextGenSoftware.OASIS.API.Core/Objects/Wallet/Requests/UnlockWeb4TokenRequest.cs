
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests
{
    public class UnlockWeb4TokenRequest : UnlockWeb3TokenRequest, IUnlockWeb4TokenRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public bool WaitTillTokenUnlocked { get; set; } = true;
        public int WaitForTokenToUnlockInSeconds { get; set; } = 60;
        public int AttemptToUnlockEveryXSeconds { get; set; } = 5;
    }
}