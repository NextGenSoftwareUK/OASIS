using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT
{
    public class CreateCollectionNFTRequest
    {
        // Collection-specific
        public ulong InitialSize { get; set; } = 0;
        public bool WaitTillCollectionSizeSet { get; set; } = true;
        public int WaitForCollectionSizeToBeSetInSeconds { get; set; } = 60;
        public int AttemptToSetCollectionSizeEveryXSeconds { get; set; } = 1;

        // Shared with MintNFTTransactionRequest
        public string CollectionPublicKey { get; set; }
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
        public int NumberToMint { get; set; }
        public bool StoreNFTMetaDataOnChain { get; set; }
        public Dictionary<string, object> MetaData { get; set; }
        public string OffChainProvider { get; set; }
        public string OnChainProvider { get; set; } = "SolanaOASIS";
        public string NFTOffChainMetaType { get; set; }
        public string JSONMetaDataURL { get; set; }
        public string NFTStandardType { get; set; }
        public string MintedByAvatarId { get; set; }
        public string SendToAddressAfterMinting { get; set; }
        public string SendToAvatarAfterMintingId { get; set; }
        public string SendToAvatarAfterMintingUsername { get; set; }
        public string SendToAvatarAfterMintingEmail { get; set; }
        public bool WaitTillNFTMinted { get; set; } = true;
        public int WaitForNFTToMintInSeconds { get; set; } = 180;
        public int AttemptToMintEveryXSeconds { get; set; } = 1;
        public bool WaitTillNFTVerified { get; set; } = true;
        public int WaitForNFTToVerifyInSeconds { get; set; } = 180;
        public int AttemptToVerifyEveryXSeconds { get; set; } = 1;
        public bool WaitTillNFTSent { get; set; } = true;
        public int WaitForNFTToSendInSeconds { get; set; } = 180;
        public int AttemptToSendEveryXSeconds { get; set; } = 1;
        public bool? FreezeMetadata { get; set; }
        public int? RoyaltyPercentage { get; set; }
        public bool? IsForSale { get; set; }
        public DateTime? SaleStartDate { get; set; }
        public DateTime? SaleEndDate { get; set; }
        public List<string> Tags { get; set; }
    }
}
