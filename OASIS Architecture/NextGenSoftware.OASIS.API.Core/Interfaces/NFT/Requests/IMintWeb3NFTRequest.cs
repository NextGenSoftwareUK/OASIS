using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    //All properties are optional, if not provided the values defined in the parent WEB4 OASIS NFT will be used.
    public interface IMintWeb3NFTRequest
    {
        //Guid RequestId { get; set; } //Optional (auto-generated if not provided).
        NFTOffChainMetaType? NFTOffChainMetaType { get; set; }
        NFTStandardType? NFTStandardType { get; set; }
        int? NumberToMint { get; set; } //If this is not set then it will mint the NumberToMint defined in the parent WEB4 OASIS NFT for each Web3NFT.
        ProviderType? OffChainProvider { get; set; }
        ProviderType? OnChainProvider { get; set; }
        bool? StoreNFTMetaDataOnChain { get; set; }
        string Symbol { get; set; }

        NFTMetaDataMergeStrategy NFTMetaDataMergeStrategy { get; set; } //Defines how the Web3NFT meta data will be merged with the parent WEB4 OASIS NFT meta data.
        Dictionary<string, object> MetaData { get; set; } // The Web3NFT specific meta data (will be merged with the parent WEB4 OASIS NFT meta data or replaced depending on NFTMetaDataMergeStrategy).
        public string JSONMetaDataURL { get; set; } //TODO: Not sure if these should only be in the MintNFTTransactionRequest (WEB4 OASIS NFT Only).
        public string JSONMetaData { get; set; } //TODO: Not sure if these should only be in the MintNFTTransactionRequest (WEB4 OASIS NFT Only).

        
        //If these are not set it will use the values defined in the parent WEB4 OASIS NFT for each Web3NFT.
        public bool? WaitTillNFTMinted { get; set; }
        public int? WaitForNFTToMintInSeconds { get; set; }
        public int? AttemptToMintEveryXSeconds { get; set; }
        public string SendToAddressAfterMinting { get; set; } //optionally send to this wallet after it has been minted.
        public Guid SendToAvatarAfterMintingId { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingUsername { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public string SendToAvatarAfterMintingEmail { get; set; } //If you want to send to an avatar at least one of these 3 fields needs to be specefied.
        public bool? WaitTillNFTSent { get; set; }
        public int? WaitForNFTToSendInSeconds { get; set; }
        public int? AttemptToSendEveryXSeconds { get; set; }
    }
}