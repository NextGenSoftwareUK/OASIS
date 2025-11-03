using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Request
{
    //All properties are optional, if not provided the values defined in the parent WEB4 OASIS NFT will be used.
    public class MintWeb3NFTRequest : IMintWeb3NFTRequest
    {
        public int? NumberToMint { get; set; }
        public bool? StoreNFTMetaDataOnChain { get; set; }
        public ProviderType? OffChainProvider { get; set; }
        public ProviderType? OnChainProvider { get; set; }
        public NFTStandardType? NFTStandardType { get; set; }
        public NFTOffChainMetaType? NFTOffChainMetaType { get; set; }
        public string Symbol { get; set; }


        public NFTMetaDataMergeStrategy NFTMetaDataMergeStrategy { get; set; } = NFTMetaDataMergeStrategy.Merge; //Defines how the Web3NFT meta data will be merged with the parent WEB4 OASIS NFT meta data.
        public Dictionary<string, object> MetaData { get; set; } // The Web3NFT specific meta data (will be merged with the parent WEB4 OASIS NFT meta data).
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