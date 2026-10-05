using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests
{
    public class SendWeb4NFTRequest : SendWeb3NFTRequest, ISendWeb4NFTRequest
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
        public bool WaitTillNFTSent { get; set; } = true;
        public int WaitForNFTToSendInSeconds { get; set; } = 60;
        public int AttemptToSendNFTEveryXSeconds { get; set; } = 5;
        public bool WaitTillNFTLocked { get; set; } = true;
        public int WaitForNFTToLockInSeconds { get; set; } = 60;
        public int AttemptToLockEveryXSeconds { get; set; } = 5;
        public bool WaitTillNFTBurnt { get; set; } = true;
        public int WaitForNFTToBurnInSeconds { get; set; } = 60;    
        public int AttemptToBurnEveryXSeconds { get; set; } = 5;
        public bool WaitTillNFTUnlocked { get; set; } = true;
        public int WaitForNFTToUnlockInSeconds { get; set; } = 60;
        public int AttemptToUnlockEveryXSeconds { get; set; } = 5;
    }
}