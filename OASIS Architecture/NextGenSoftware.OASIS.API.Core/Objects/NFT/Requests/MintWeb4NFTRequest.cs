using System;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Request
{
    public class MintWeb4NFTRequest : IMintWeb4NFTTRequest
    {
        //public string MintWalletAddress { get; set; } The address that will actually mint the NFT (i.e. pay the gas fees etc). This will use the built-in OASIS accounts defined in the Smart Contracts.

        public IList<IMintWeb3NFTRequest> Web3NFTs { get; set; } = new List<IMintWeb3NFTRequest>();

        //Default Global NFT Properties (these will be applied to all Web3 NFTs being minted unless overridden in the individual Web3NFTs):
        public EnumValue<ProviderType> OffChainProvider { get; set; }
        public EnumValue<ProviderType> OnChainProvider { get; set; }
        public EnumValue<NFTStandardType> NFTStandardType { get; set; }
        public EnumValue<NFTOffChainMetaType> NFTOffChainMetaType { get; set; }
        public int NumberToMint { get; set; } //If the NumberToMint is not set in the Web3NFTs then it will mint this number for each Web3NFT.
        public bool StoreNFTMetaDataOnChain { get; set; }
        public string Symbol { get; set; }
        public string JSONMetaDataURL { get; set; }
        public string JSONMetaData { get; set; }


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


        public int? RoyaltyPercentage { get; set; }
        public bool? IsForSale { get; set; }
        public DateTime? SaleStartDate { get; set; }
        public DateTime? SaleEndDate { get; set; }

        public Dictionary<string, object> MetaData { get; set; }
        public List<string> Tags { get; set; }
        
        public bool WaitTillNFTMinted { get; set; } = true;
        public int WaitForNFTToMintInSeconds { get; set; } = 60;
        public int AttemptToMintEveryXSeconds { get; set; } = 1;
        public string SendToAddressAfterMinting { get; set; } //optionally send to this wallet after it has been minted.
        public Guid SendToAvatarAfterMintingId { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingUsername { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingEmail { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public bool WaitTillNFTSent { get; set; } = true;
        public int WaitForNFTToSendInSeconds { get; set; } = 60;
        public int AttemptToSendEveryXSeconds { get; set; } = 1;
    }
}