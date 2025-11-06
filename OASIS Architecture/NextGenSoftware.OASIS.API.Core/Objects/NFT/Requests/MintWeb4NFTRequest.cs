using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Request
{
    public class MintWeb4NFTRequest : MintNFTRequestBase, IMintWeb4NFTRequest
    {
        public IList<IMintWeb3NFTRequest> Web3NFTs { get; set; } = new List<IMintWeb3NFTRequest>();
        //Default Global NFT Properties (these will be applied to all Web3 NFTs being minted unless overridden in the individual Web3NFTs):
        public EnumValue<ProviderType> OffChainProvider { get; set; }
        public EnumValue<ProviderType> OnChainProvider { get; set; }
        public EnumValue<NFTStandardType> NFTStandardType { get; set; }
        public EnumValue<NFTOffChainMetaType> NFTOffChainMetaType { get; set; }
        public int NumberToMint { get; set; } //If the NumberToMint is not set in the Web3NFTs then it will mint this number for each Web3NFT.
        public bool StoreNFTMetaDataOnChain { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public int? RoyaltyPercentage { get; set; }
        public bool WaitTillNFTMinted { get; set; } = true;
        public int WaitForNFTToMintInSeconds { get; set; } = 60;
        public int AttemptToMintEveryXSeconds { get; set; } = 1;
        public bool WaitTillNFTSent { get; set; } = true;
        public int WaitForNFTToSendInSeconds { get; set; } = 60;
        public int AttemptToSendEveryXSeconds { get; set; } = 1;
    }
}