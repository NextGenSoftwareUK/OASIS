
namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT
{
    public interface IWeb3Token : ITokenBase
    {
        string MintTransactionHash { get; set; }
        string SendTokenTransactionHash { get; set; }
        string TokenMintedUsingWalletAddress { get; set; }
        string TokenAddress { get; set; }
        string OASISMintWalletAddress { get; set; }
        string UpdateAuthority { get; set; }
    }
}