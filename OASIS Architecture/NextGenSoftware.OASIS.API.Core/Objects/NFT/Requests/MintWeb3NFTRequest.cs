using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests
{
    //All properties are optional, if not provided the values defined in the parent WEB4 OASIS NFT will be used.
    public class MintWeb3NFTRequest : MintNFTRequestBase, IMintWeb3NFTRequest
    {
        public string? CollectionPublicKey { get; set; }
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
        public bool? WaitTillNFTMinted { get; set; } = true;
        public int? WaitForNFTToMintInSeconds { get; set; } = 180;
        public int? AttemptToMintEveryXSeconds { get; set; } = 1;
        public bool? WaitTillNFTVerified { get; set; } = true;
        public int? WaitForNFTToVerifyInSeconds { get; set; } = 180;
        public int? AttemptToVerifyEveryXSeconds { get; set; } = 1;
        public bool? WaitTillNFTSent { get; set; } = true;
        public int? WaitForNFTToSendInSeconds { get; set; } = 180;
        public int? AttemptToSendEveryXSeconds { get; set; } = 1;

        // DISABLED: Metaplex transfers Mint/Freeze Authority to the Master Edition PDA during CreateNFT,
        // before our code runs. The PDA has no private key, so SPL Token SetAuthority (opcode 6) always
        // fails with 0x4 OwnerMismatch when we try to sign with our wallet. RugCheck flags this as DANGER
        // but it is a false positive on every standard Metaplex NFT (DeGods, Mad Lads, etc.) — the Master
        // Edition enforces supply=1 permanently and nobody can mint more. Nothing we can do about this score.
        // Re-enable if Metaplex ever provides an official mechanism to clear these authorities.
        // public bool? RevokeTokenAuthorities { get; set; }

        // When true, sets isMutable=false on the Token Metadata account after minting,
        // making the NFT metadata permanently immutable — non-fatal if it fails.
        public bool? FreezeMetadata { get; set; }
    }
}