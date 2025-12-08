using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests
{
    public interface ILockWeb4TokenRequest : ILockWeb3TokenRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public bool WaitTillTokenLocked { get; set; }
        public int WaitForTokenToLockInSeconds { get; set; }
        public int AttemptToLockEveryXSeconds { get; set; }
    }
}