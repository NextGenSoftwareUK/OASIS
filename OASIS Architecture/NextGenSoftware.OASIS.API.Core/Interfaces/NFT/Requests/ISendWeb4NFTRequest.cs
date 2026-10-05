using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface ISendWeb4NFTRequest : ISendWeb3NFTRequest
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
        public bool WaitTillNFTSent { get; set; }
        public int WaitForNFTToSendInSeconds { get; set; }
        public int AttemptToSendNFTEveryXSeconds { get; set; }
        public bool WaitTillNFTLocked { get; set; }
        public int WaitForNFTToLockInSeconds { get; set; }
        public int AttemptToLockEveryXSeconds { get; set; }
        public bool WaitTillNFTBurnt { get; set; }
        public int WaitForNFTToBurnInSeconds { get; set; }
        public int AttemptToBurnEveryXSeconds { get; set; }
        public bool WaitTillNFTUnlocked { get; set; }
        public int WaitForNFTToUnlockInSeconds { get; set; }
        public int AttemptToUnlockEveryXSeconds { get; set; }
    }
}