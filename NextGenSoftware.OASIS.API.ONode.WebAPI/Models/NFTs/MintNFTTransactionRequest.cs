using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT
{
    public class MintNFTTransactionRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public byte[] Image { get; set; }
        public string ImageUrl { get; set; }
        public byte[] Thumbnail { get; set; }
        public string ThumbnailUrl { get; set; }
        public decimal Price { get; set; }
        public string Symbol { get; set; }
        public decimal Discount { get; set; }
        public string MemoText { get; set; }
        // public string Token { get; set; } //TODO: Should be dervied from the OnChainProvider so may not need this?
        public int NumberToMint { get; set; }
        public bool StoreNFTMetaDataOnChain { get; set; }
        public Dictionary<string, object> MetaData { get; set; }
        public string OffChainProvider { get; set; }
        public string OnChainProvider { get; set; }
        public string NFTOffChainMetaType { get; set; }
        public string JSONMetaDataURL { get; set; }
        public string NFTStandardType { get; set; }
        public bool WaitTillNFTMinted { get; set; } = true;
        public int WaitForNFTToMintInSeconds { get; set; } = 60;
        public int AttemptToMintEveryXSeconds { get; set; } = 5;
        public string SendToAddressAfterMinting { get; set; } //optionally send to this wallet after it has been minted.
        public string SendToAvatarAfterMintingId { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingUsername { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingEmail { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public bool WaitTillNFTSent { get; set; } = true;
        public int WaitForNFTToSendInSeconds { get; set; } = 60;
        public int AttemptToSendEveryXSeconds { get; set; } = 5;
    }
}