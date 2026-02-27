namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT
{
    /// <summary>
    /// Request body for transferring fungible SPL tokens between wallets.
    /// Used by Pangea/Launchboard for secondary share transfers on the cap table.
    /// </summary>
    public class SendSplTokenRequest
    {
        /// <summary>The mint address of the SPL token to transfer.</summary>
        public string TokenMintAddress { get; set; }

        /// <summary>The sender wallet address.</summary>
        public string FromWalletAddress { get; set; }

        /// <summary>The recipient wallet address.</summary>
        public string ToWalletAddress { get; set; }

        /// <summary>The number of tokens to transfer (in base units).</summary>
        public ulong Amount { get; set; }

        /// <summary>The OASIS on-chain provider to use. Defaults to SolanaOASIS.</summary>
        public string OnChainProvider { get; set; } = "SolanaOASIS";

        /// <summary>Solana cluster: "devnet" (default) or "mainnet-beta".</summary>
        public string Cluster { get; set; } = "devnet";
    }
}
