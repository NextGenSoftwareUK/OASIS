
using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests
{
    public interface IMintWeb4TokenRequest : IMintTokenRequestBase
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public string SendToAddressAfterMinting { get; set; } //optionally send to this wallet after it has been minted.
        public Guid SendToAvatarAfterMintingId { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingUsername { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingEmail { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public bool WaitTillTokenMinted { get; set; }
        public int WaitForTokenToMintInSeconds { get; set; }
        public int AttemptToMintEveryXSeconds { get; set; }
        public bool WaitTillTokenSent { get; set; }
        public int WaitForTokenToSendInSeconds { get; set; }
        public int AttemptToSendEveryXSeconds { get; set; }
    }
}