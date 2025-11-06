using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface IMintWeb4NFTRequest : IMintNFTRequestBase
    {
        IList<IMintWeb3NFTRequest> Web3NFTs { get; set; }
        public int NumberToMint { get; set; }
        public bool StoreNFTMetaDataOnChain { get; set; }
        public EnumValue<ProviderType> OffChainProvider { get; set; }
        public EnumValue<ProviderType> OnChainProvider { get; set; }
        public EnumValue<NFTStandardType> NFTStandardType { get; set; }
        public EnumValue<NFTOffChainMetaType> NFTOffChainMetaType { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public int? RoyaltyPercentage { get; set; }
        public bool WaitTillNFTMinted { get; set; }
        public int WaitForNFTToMintInSeconds { get; set; }
        public int AttemptToMintEveryXSeconds { get; set; }
        public bool WaitTillNFTSent { get; set; }
        public int WaitForNFTToSendInSeconds { get; set; }
        public int AttemptToSendEveryXSeconds { get; set; }
    }
}