using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests
{
    public class LockWeb4TokenRequest : LockWeb3TokenRequest, ILockWeb4TokenRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public bool WaitTillTokenLocked { get; set; } = true;
        public int WaitForTokenToLockInSeconds { get; set; } = 60;
        public int AttemptToLockEveryXSeconds { get; set; } = 5;
    }
}