
using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests
{
    public class MintWeb4TokenRequest : MintTokenRequestBase, IMintWeb4TokenRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public string SendToAddressAfterMinting { get; set; } //optionally send to this wallet after it has been minted.
        public Guid SendToAvatarAfterMintingId { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingUsername { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingEmail { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public bool WaitTillTokenMinted { get; set; } = true;
        public int WaitForTokenToMintInSeconds { get; set; } = 60;
        public int AttemptToMintEveryXSeconds { get; set; } = 5;
        public bool WaitTillTokenSent { get; set; } = true;
        public int WaitForTokenToSendInSeconds { get; set; } = 60;
        public int AttemptToSendEveryXSeconds { get; set; } = 1;
    }
}