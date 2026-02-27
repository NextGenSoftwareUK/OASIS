namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT
{
    /// <summary>
    /// Request body for burning fungible SPL tokens from a wallet.
    /// Used by Pangea/Launchboard on SAFE conversion or security cancellation.
    /// </summary>
    public class BurnSplTokenRequest
    {
        /// <summary>The mint address of the SPL token to burn.</summary>
        public string TokenMintAddress { get; set; }

        /// <summary>The wallet address from which tokens will be burned.</summary>
        public string FromWalletAddress { get; set; }

        /// <summary>The number of tokens to burn (in base units).</summary>
        public ulong Amount { get; set; }

        /// <summary>The OASIS on-chain provider to use. Defaults to SolanaOASIS.</summary>
        public string OnChainProvider { get; set; } = "SolanaOASIS";

        /// <summary>Solana cluster: "devnet" (default) or "mainnet-beta".</summary>
        public string Cluster { get; set; } = "devnet";
    }
}
