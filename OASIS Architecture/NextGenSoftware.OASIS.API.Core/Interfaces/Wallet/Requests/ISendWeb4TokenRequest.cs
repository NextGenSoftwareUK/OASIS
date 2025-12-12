using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests
{
    public interface ISendWeb4TokenRequest : ISendWeb3TokenRequest
    {
        //Optional (if FromWalletAddress is not provided then it can be retreived from the logged in avatar using the default wallet for the specified provider type).
        public EnumValue<ProviderType> FromProvider { get; set; }

        //Optional (if ToWalletAddress is not provided then it can be retreived from either ToAvatarId,ToAvatarUsername or ToAvatarEmail using the default wallet for the specified provider type).
        public EnumValue<ProviderType> ToProvider { get; set; }
        //Need at least one of these to identify the sender.
        
        //Implicit from loggin in avtatar.
        //public Guid FromAvatarId { get; set; }
        //public string FromAvatarUsername { get; set; }
        //public string FromAvatarEmail { get; set; }

        //Need at least one of these to identify the receiver.
        public Guid ToAvatarId { get; set; }
        public string ToAvatarUsername { get; set; }
        public string ToAvatarEmail { get; set; }
        public bool WaitTillTokenSent { get; set; }
        public int WaitForTokenToSendInSeconds { get; set; }
        public int AttemptToSendTokenEveryXSeconds { get; set; }
        public bool WaitTillTokenLocked { get; set; }
        public int WaitForTokenToLockInSeconds { get; set; }
        public int AttemptToLockEveryXSeconds { get; set; }
        public bool WaitTillTokenBurnt { get; set; }
        public int WaitForTokenToBurnInSeconds { get; set; }
        public int AttemptToBurnEveryXSeconds { get; set; }
        public bool WaitTillTokenUnlocked { get; set; }
        public int WaitForTokenToUnlockInSeconds { get; set; }
        public int AttemptToUnlockEveryXSeconds { get; set; }
    }
}