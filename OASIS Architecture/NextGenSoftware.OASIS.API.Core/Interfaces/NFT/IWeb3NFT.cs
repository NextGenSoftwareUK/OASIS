
using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT
{
    public interface IWeb3NFT : INFTBase
    {
        Guid ParentWeb4NFTId { get; set; }
        string MintTransactionHash { get; set; }
        string SendNFTTransactionHash { get; set; }
        string NFTMintedUsingWalletAddress { get; set; }
        string NFTTokenAddress { get; set; }
        string OASISMintWalletAddress { get; set; }
        string UpdateAuthority { get; set; }
    }
}