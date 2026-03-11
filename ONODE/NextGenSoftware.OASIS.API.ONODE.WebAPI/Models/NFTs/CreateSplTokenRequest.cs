namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT
{
    /// <summary>
    /// Request body for creating a plain fungible SPL token mint.
    /// The OASIS server wallet becomes the mint authority, so subsequent
    /// calls to /api/nft/mint-tokens will work without any authority transfer.
    ///
    /// Optional Name/Symbol/MetadataUri: when provided, OASIS will attach Metaplex Token Metadata
    /// after creating the mint so wallets (e.g. Phantom) show a label instead of "Unknown Token".
    /// Metaplex limits: name max 32 chars, symbol max 10 chars.
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

        /// <summary>
        /// Optional. Token display name for wallet UIs (Metaplex metadata). Max 32 characters; longer values are truncated.
        /// Example: "Series Seed Preferred Stock"
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Optional. Short symbol (Metaplex metadata). Max 10 characters; longer values are truncated.
        /// Example: "NOVA-SEED"
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Optional. Metadata JSON URI (HTTPS or Arweave). If empty but Name/Symbol are set, a minimal data URI or placeholder may be used.
        /// </summary>
        public string MetadataUri { get; set; }
    }
}
