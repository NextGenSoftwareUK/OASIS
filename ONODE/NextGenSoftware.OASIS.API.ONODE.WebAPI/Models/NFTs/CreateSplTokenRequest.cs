namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT
{
    /// <summary>
    /// Request body for creating a plain fungible SPL token mint.
    /// The OASIS server wallet becomes the mint authority, so subsequent
    /// calls to /api/nft/mint-tokens will work without any authority transfer.
    /// </summary>
    public class CreateSplTokenRequest
    {
        /// <summary>
        /// Number of decimal places for the token.
        /// Use 0 for whole-unit share tokens (e.g. 1 share = 1 token).
        /// Use 6 for USDC-like tokens with fractional units.
        /// Defaults to 0 if not specified.
        /// </summary>
        public byte Decimals { get; set; } = 0;

        /// <summary>Solana cluster: "devnet" (default for testing) or "mainnet-beta".</summary>
        public string Cluster { get; set; } = "devnet";
    }
}
