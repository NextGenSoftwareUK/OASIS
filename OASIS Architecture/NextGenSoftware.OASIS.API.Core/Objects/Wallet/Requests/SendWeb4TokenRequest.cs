using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests
{
    public class SendWeb4TokenRequest : SendWeb3TokenRequest, ISendWeb4TokenRequest
    {
        public EnumValue<ProviderType> FromProvider { get; set; }
        public EnumValue<ProviderType> ToProvider { get; set; }
        //Need at least one of these to identify the sender.
        public Guid FromAvatarId { get; set; }
        public string FromAvatarUsername { get; set; }
        public string FromAvatarEmail { get; set; }

        //Need at least one of these to identify the receiver.
        public Guid ToAvatarId { get; set; }
        public string ToAvatarUsername { get; set; }
        public string ToAvatarEmail { get; set; }
        public bool WaitTillTokenSent { get; set; } = true;
        public int WaitForTokenToSendInSeconds { get; set; } = 60;
        public int AttemptToSendTokenEveryXSeconds { get; set; } = 5;
        public bool WaitTillTokenLocked { get; set; } = true;
        public int WaitForTokenToLockInSeconds { get; set; } = 60;
        public int AttemptToLockEveryXSeconds { get; set; } = 5;
        public bool WaitTillTokenBurnt { get; set; } = true;
        public int WaitForTokenToBurnInSeconds { get; set; } = 60;
        public int AttemptToBurnEveryXSeconds { get; set; } = 5;
        public bool WaitTillTokenUnlocked { get; set; } = true;
        public int WaitForTokenToUnlockInSeconds { get; set; } = 60;
        public int AttemptToUnlockEveryXSeconds { get; set; } = 5;
    }
}