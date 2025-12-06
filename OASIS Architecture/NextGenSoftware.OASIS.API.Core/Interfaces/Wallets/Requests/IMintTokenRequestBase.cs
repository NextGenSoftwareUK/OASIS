using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface IMintTokenRequestBase
    {
        public Dictionary<string, object> MetaData { get; set; }
        public List<string> Tags { get; set; }
        public string Symbol { get; set; }
        public Guid MintedByAvatarId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string MemoText { get; set; }
        //public string SendToAddressAfterMinting { get; set; } //optionally send to this wallet after it has been minted.
        //public Guid SendToAvatarAfterMintingId { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        //public string SendToAvatarAfterMintingUsername { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        //public string SendToAvatarAfterMintingEmail { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
    }
}