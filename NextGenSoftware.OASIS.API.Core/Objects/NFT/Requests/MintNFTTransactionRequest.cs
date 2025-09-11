using System;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Request
{
    public class MintNFTTransactionRequest : IMintNFTTransactionRequest
    {
        //public string MintWalletAddress { get; set; } The address that will actually mint the NFT (i.e. pay the gas fees etc). This will use the built-in OASIS accounts defined in the Smart Contracts.
        public string SendToAddressAfterMinting { get; set; }
        public Guid SendToAvatarAfterMintingId { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingUsername { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingEmail { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public Guid MintedByAvatarId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public byte[] Image { get; set; }
        public string ImageUrl { get; set; }
        public byte[] Thumbnail { get; set; }
        public string ThumbnailUrl { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public string MemoText { get; set; }
        // public string Token { get; set; } //TODO: Should be dervied from the OnChainProvider so may not need this?
        public int NumberToMint { get; set; }
        public bool StoreNFTMetaDataOnChain { get; set; }
        public Dictionary<string, object> MetaData { get; set; }
        public EnumValue<ProviderType> OffChainProvider { get; set; }
        public EnumValue<ProviderType> OnChainProvider { get; set; }
        public NFTStandardType NFTStandardType { get; set; }
        public NFTOffChainMetaType NFTOffChainMetaType { get; set; }
        public string Symbol { get; set; }
        public string JSONMetaDataURL { get; set; }
    }
}