using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public class Web3NFT : NFTBase, IWeb3NFT
    {
        public Guid ParentWeb4NFTId { get; set; }
        public string OASISMintWalletAddress { get; set; } //The OASIS System account that minted the NFT.
        public string NFTMintedUsingWalletAddress { get; set; } //This may be different to OASISMintWalletAddress if it was imported.
        public string NFTTokenAddress { get; set; } //The address of the actual minted NFT on the chain.
        public string MintTransactionHash { get; set; }
        public string SendNFTTransactionHash { get; set; }
        public string UpdateAuthority { get; set; }
    }
}