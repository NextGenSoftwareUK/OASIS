
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests
{
    public interface IUnlockWeb4TokenRequest : IUnlockWeb3TokenRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public bool WaitTillTokenUnlocked { get; set; }
        public int WaitForTokenToUnlockInSeconds { get; set; }
        public int AttemptToUnlockEveryXSeconds { get; set; }
    }
}