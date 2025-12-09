using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface IMintNFTRequestBase : IMintTokenRequestBase//: IMintWeb4TokenRequest
    {
        public string JSONMetaDataURL { get; set; }
        public string JSONMetaData { get; set; }
        public byte[] Image { get; set; }
        public string ImageUrl { get; set; }
        public byte[] Thumbnail { get; set; }
        public string ThumbnailUrl { get; set; }
        public bool? IsForSale { get; set; }
        public DateTime? SaleStartDate { get; set; }
        public DateTime? SaleEndDate { get; set; }
        public string SendToAddressAfterMinting { get; set; } //optionally send to this wallet after it has been minted.
        public Guid SendToAvatarAfterMintingId { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingUsername { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingEmail { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
    }
}