
using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public class MintWeb4TokenRequest : MintTokenRequestBase, IMintWeb4TokenRequest
    {
        public string SendToAddressAfterMinting { get; set; } //optionally send to this wallet after it has been minted.
        public Guid SendToAvatarAfterMintingId { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingUsername { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingEmail { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
    }
}