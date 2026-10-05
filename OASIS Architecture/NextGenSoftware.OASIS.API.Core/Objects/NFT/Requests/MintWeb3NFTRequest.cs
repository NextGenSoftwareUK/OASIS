using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests
{
    //All properties are optional, if not provided the values defined in the parent WEB4 OASIS NFT will be used.
    public class MintWeb3NFTRequest : MintNFTRequestBase, IMintWeb3NFTRequest
    {
        public decimal? Price { get; set; }
        public decimal? Discount { get; set; }
        public int? RoyaltyPercentage { get; set; }
        public int? NumberToMint { get; set; }
        public bool? StoreNFTMetaDataOnChain { get; set; }
        public ProviderType? OffChainProvider { get; set; }
        public ProviderType? OnChainProvider { get; set; }
        public NFTStandardType? NFTStandardType { get; set; }
        public NFTOffChainMetaType? NFTOffChainMetaType { get; set; }

        public NFTTagsMergeStrategy NFTTagsMergeStrategy { get; set; } //Defines how the Web3NFT tags will be merged with the parent WEB4 OASIS NFT tags. 
        public NFTMetaDataMergeStrategy NFTMetaDataMergeStrategy { get; set; } = NFTMetaDataMergeStrategy.Merge; //Defines how the Web3NFT meta data will be merged with the parent WEB4 OASIS NFT meta data.
      
        //If these are not set it will use the values defined in the parent WEB4 OASIS NFT for each Web3NFT.
        public bool? WaitTillNFTMinted { get; set; }
        public int? WaitForNFTToMintInSeconds { get; set; }
        public int? AttemptToMintEveryXSeconds { get; set; }
        public bool? WaitTillNFTSent { get; set; }
        public int? WaitForNFTToSendInSeconds { get; set; }
        public int? AttemptToSendEveryXSeconds { get; set; }
    }
}