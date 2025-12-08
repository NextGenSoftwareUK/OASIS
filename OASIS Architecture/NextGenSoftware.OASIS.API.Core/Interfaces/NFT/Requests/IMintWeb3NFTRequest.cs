using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    //All properties are optional, if not provided the values defined in the parent WEB4 OASIS NFT will be used.
    public interface IMintWeb3NFTRequest : IMintNFTRequestBase
    {
        NFTMetaDataMergeStrategy NFTMetaDataMergeStrategy { get; set; } //Defines how the Web3NFT meta data will be merged with the parent WEB4 OASIS NFT meta data. 
        NFTTagsMergeStrategy NFTTagsMergeStrategy { get; set; } //Defines how the Web3NFT tags will be merged with the parent WEB4 OASIS NFT tags. 
        public int? NumberToMint { get; set; }
        public bool? StoreNFTMetaDataOnChain { get; set; }
        public ProviderType? OffChainProvider { get; set; }
        public ProviderType? OnChainProvider { get; set; }
        public NFTStandardType? NFTStandardType { get; set; }
        public NFTOffChainMetaType? NFTOffChainMetaType { get; set; }
        public decimal? Price { get; set; }
        public decimal? Discount { get; set; }
        public int? RoyaltyPercentage { get; set; }
        public bool? WaitTillNFTMinted { get; set; }
        public int? WaitForNFTToMintInSeconds { get; set; }
        public int ?AttemptToMintEveryXSeconds { get; set; }
        public bool? WaitTillNFTSent { get; set; }
        public int? WaitForNFTToSendInSeconds { get; set; }
        public int? AttemptToSendEveryXSeconds { get; set; }
    }
}