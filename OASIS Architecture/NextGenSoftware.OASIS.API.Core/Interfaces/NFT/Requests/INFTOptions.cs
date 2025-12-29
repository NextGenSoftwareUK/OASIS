
using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface INFTOptions
    {
        public bool WaitTillNFTMinted { get; set; }
        public int WaitForNFTToMintInSeconds { get; set; }
        public int AttemptToMintEveryXSeconds { get; set; }
        public bool WaitTillNFTSent { get; set; }
        public int WaitForNFTToSendInSeconds { get; set; }
        public int AttemptToSendEveryXSeconds { get; set; }
        public string SendToAddressAfterMinting { get; set; } //optionally send to this wallet after it has been minted.
        public Guid SendToAvatarAfterMintingId { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingUsername { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingEmail { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
    }
}