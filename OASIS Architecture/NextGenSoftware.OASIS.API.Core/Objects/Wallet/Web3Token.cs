using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public class Web3Token : TokenBase, IWeb3Token
    {
        public string OASISMintWalletAddress { get; set; } //The OASIS System account that minted the token.
        public string TokenMintedUsingWalletAddress { get; set; } //This may be different to OASISMintWalletAddress if it was imported.
        public string TokenAddress { get; set; } //The address of the actual minted token on the chain.
        public string MintTransactionHash { get; set; }
        public string SendTokenTransactionHash { get; set; }
        public string UpdateAuthority { get; set; }
    }
}