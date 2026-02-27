namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT
{
    /// <summary>
    /// Request body for minting fungible SPL tokens to a wallet.
    /// Used by Pangea/Launchboard for share issuance and vesting cron jobs.
    /// </summary>
    public class MintSplTokenRequest
    {
        /// <summary>The mint address of the SPL token to mint (e.g. the Common Stock mint address).</summary>
        public string TokenMintAddress { get; set; }

        /// <summary>The destination wallet address that will receive the minted tokens.</summary>
        public string ToWalletAddress { get; set; }

        /// <summary>The number of tokens to mint (in base units; for 0-decimal tokens this equals whole tokens).</summary>
        public ulong Amount { get; set; }

        /// <summary>The OASIS on-chain provider to use. Defaults to SolanaOASIS.</summary>
        public string OnChainProvider { get; set; } = "SolanaOASIS";

        /// <summary>Solana cluster: "devnet" (default) or "mainnet-beta".</summary>
        public string Cluster { get; set; } = "devnet";
    }
}
