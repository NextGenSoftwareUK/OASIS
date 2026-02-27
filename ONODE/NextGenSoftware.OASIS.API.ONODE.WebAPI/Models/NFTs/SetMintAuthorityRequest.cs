namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT
{
    /// <summary>
    /// Request body for transferring mint authority of an SPL token to the OASIS server wallet.
    /// Call this once per token mint before using /api/nft/mint-tokens.
    /// The CurrentAuthorityPrivateKey must be the private key of whoever currently holds mint authority.
    /// After this call, the OASIS server wallet becomes the mint authority and all subsequent
    /// mint-tokens calls will succeed without needing a private key.
    /// </summary>
    public class SetMintAuthorityRequest
    {
        /// <summary>The mint address of the SPL token whose authority will be transferred.</summary>
        public string TokenMintAddress { get; set; }

        /// <summary>
        /// Base58-encoded private key of the current mint authority.
        /// This is used once to sign the SetAuthority transaction and is never stored.
        /// </summary>
        public string CurrentAuthorityPrivateKey { get; set; }

        /// <summary>Solana cluster: "devnet" (default) or "mainnet-beta".</summary>
        public string Cluster { get; set; } = "devnet";
    }
}
