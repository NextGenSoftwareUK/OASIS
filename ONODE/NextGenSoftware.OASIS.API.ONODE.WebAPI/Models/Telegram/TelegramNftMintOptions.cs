namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Telegram
{
    /// <summary>
    /// Configuration for Telegram NFT mint flow (bot token, bot avatar id, Pinata).
    /// </summary>
    public class TelegramNftMintOptions
    {
        public const string SectionName = "TelegramNftMint";

        /// <summary>Telegram bot token (from BotFather).</summary>
        public string BotToken { get; set; }

        /// <summary>OASIS avatar ID used to mint (bot avatar). Must have Solana wallet and be able to mint.</summary>
        public string BotAvatarId { get; set; }

        /// <summary>Pinata JWT for uploading images to IPFS (Bearer auth). If empty, PinataApiKey + PinataSecretKey are used.</summary>
        public string PinataJwt { get; set; }

        /// <summary>Pinata API key (when using key+secret instead of JWT).</summary>
        public string PinataApiKey { get; set; }

        /// <summary>Pinata secret API key (when using key+secret instead of JWT).</summary>
        public string PinataSecretKey { get; set; }

        /// <summary>Optional webhook secret to validate incoming updates.</summary>
        public string WebhookSecret { get; set; }

        /// <summary>Solana cluster for Solscan links: "devnet" or "mainnet-beta". Default devnet.</summary>
        public string SolanaCluster { get; set; } = "devnet";

        /// <summary>Optional GIF URL shown while minting (e.g. Pink Panther). If empty, default is used.</summary>
        public string MintingGifUrl { get; set; }

        /// <summary>Base URL of this API (e.g. https://your-domain.com) for calling metadata-by-mint when user pastes a Solscan token link. If empty, memecoin import uses request host.</summary>
        public string OasisApiBaseUrl { get; set; }
    }
}
