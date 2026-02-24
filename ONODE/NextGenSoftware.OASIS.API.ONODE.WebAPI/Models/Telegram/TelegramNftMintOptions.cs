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

        /// <summary>Optional GIF URL shown while minting (e.g. SAINT branding). If empty, default is used.</summary>
        public string MintingGifUrl { get; set; }

        /// <summary>Optional list of GIF URLs; one is chosen at random each mint. Overrides MintingGifUrl when set.</summary>
        public string[] MintingGifUrls { get; set; }

        /// <summary>Base URL of this API (e.g. https://your-domain.com) for calling metadata-by-mint when user pastes a Solscan token link. If empty, memecoin import uses request host.</summary>
        public string OasisApiBaseUrl { get; set; }

        /// <summary>SPL token mint address for balance gating. If set, the wallet (receive address) must hold at least SaintTokenRequiredBalance to proceed to mint.</summary>
        public string SaintTokenMint { get; set; }

        /// <summary>SPL token mint used for x402/drip distribution: payments go to holders of this token (not the Saint NFT). If empty, SaintTokenMint is used. Set to the $SAINT token mint so blessings go to $SAINT holders.</summary>
        public string SaintDistributionTokenMint { get; set; }

        /// <summary>Minimum token balance required in the receive wallet when SaintTokenMint is set (e.g. 1000000 for 1M tokens).</summary>
        public decimal SaintTokenRequiredBalance { get; set; }

        /// <summary>Optional message when balance is below required (e.g. "Get tokens at ..."). If empty, a default message is used.</summary>
        public string SaintTokenInsufficientMessage { get; set; }

        /// <summary>Invite link for the SAINTS secret group. When set, users who have minted via the bot can use /join_saints to get this link.</summary>
        public string SaintsSecretGroupInviteLink { get; set; }

        /// <summary>Telegram chat ID of the SAINTS group (e.g. -1001234567890). When set, any message in this group triggers a check: if the sender is in top 20 $SAINT holders and we haven't sent them the Hall verification NFT yet, we DM them the NFT.</summary>
        public long? SaintsGroupChatId { get; set; }

        /// <summary>Video/GIF shown when user gets the secret group link (/join_saints success). If empty, default is {OasisApiBaseUrl}/saints/ascended-halls.mp4 when base is public.</summary>
        public string SaintsJoinSaintsGifUrl { get; set; }

        /// <summary>Optional caption for the join_saints success video (e.g. "Welcome to the Ascended Halls.").</summary>
        public string SaintsJoinSaintsLine { get; set; }

        // --- Ascension-themed mint flow (optional GIFs + copy) ---

        /// <summary>GIF shown when user sends /mint (welcome). If set, sent before the image prompt.</summary>
        public string SaintsMintWelcomeGifUrl { get; set; }

        /// <summary>First line when starting mint (e.g. "✨ Welcome to the SAINT mint. Channel your vision."). Shown with or without WelcomeGif.</summary>
        public string SaintsMintWelcomeLine { get; set; }

        /// <summary>GIF shown after image/skip is set, before "Title for your NFT?".</summary>
        public string SaintsMintImageDoneGifUrl { get; set; }

        /// <summary>Line after image is set (e.g. "Your vision is taking form."). If empty, default "✅ Image uploaded." style is used.</summary>
        public string SaintsMintImageDoneLine { get; set; }

        /// <summary>GIF shown when showing the confirm step (before "Type YES to mint").</summary>
        public string SaintsMintConfirmGifUrl { get; set; }

        /// <summary>Line before confirm summary (e.g. "One step to ascension."). If set, shown above the confirm block.</summary>
        public string SaintsMintConfirmLine { get; set; }

        /// <summary>Caption for the minting animation (e.g. "🪙 **Minting your NFT...**"). If empty, default is used.</summary>
        public string SaintsMintMintingCaption { get; set; }

        /// <summary>Line appended to success message (e.g. "You have ascended. Welcome to the SAINTS. Use /join_saints for the secret group.").</summary>
        public string SaintsMintSuccessLine { get; set; }

        /// <summary>Override for title step prompt (e.g. "Name your creation."). If empty, default "**Title** for your NFT?" is used.</summary>
        public string SaintsMintTitlePrompt { get; set; }

        /// <summary>Override for symbol step prompt (e.g. "Its symbol (e.g. SAINT, max 10 chars)?"). If empty, default is used.</summary>
        public string SaintsMintSymbolPrompt { get; set; }

        /// <summary>Override for description step prompt (e.g. "Describe it—or type skip."). If empty, default is used.</summary>
        public string SaintsMintDescriptionPrompt { get; set; }

        /// <summary>Override for wallet step prompt (e.g. "Send the Solana address where your NFT shall ascend."). If empty, default is used.</summary>
        public string SaintsMintWalletPrompt { get; set; }

        // --- Ordain: bless top token holders with Saint NFT (Telegram /ordain; /dropholders alias) ---

        /// <summary>Avatar ID whose Solana wallet pays for drop mints and sends. If empty, BotAvatarId is used.</summary>
        public string DropTreasuryAvatarId { get; set; }

        /// <summary>Maximum number of top holders per drop (e.g. 20). Keeps drops exclusive and limits rent/tx cost. Default 20.</summary>
        public int DropMaxHolders { get; set; } = 20;

        /// <summary>When true, only send baptise NFTs to wallets that also hold $SAINT (SaintTokenMint). Filters the top-holders list so recipients are always SAINTS holders. Default false.</summary>
        public bool OrdainRequireSaintHolder { get; set; }

        /// <summary>When OrdainRequireSaintHolder is true, minimum $SAINT balance (raw units) to qualify. Default 0 = any positive balance. Uses SaintTokenRequiredBalance if not set.</summary>
        public decimal OrdainSaintMinBalance { get; set; }

        /// <summary>Optional. Helius mainnet RPC URL tried first for top-token-holders; fallback is OASIS DNA MainnetConnectionString.</summary>
        public string HeliusMainnetRpcUrl { get; set; }

        /// <summary>Optional. Helius API key for Enhanced Transactions (newest token recipients). If empty, parsed from HeliusMainnetRpcUrl query string.</summary>
        public string HeliusApiKey { get; set; }

        /// <summary>Optional. Helius Enhanced API base URL (e.g. https://api-mainnet.helius-rpc.com). Used for newest-recipients lookup.</summary>
        public string HeliusEnhancedBaseUrl { get; set; }

        /// <summary>Telegram group link included in ordained NFT metadata (external_url). Utility: holders can join this group.</summary>
        public string OrdainNftTelegramGroupLink { get; set; }

        /// <summary>Secret message from SAINT included in ordained NFT metadata (attribute "Message from SAINT").</summary>
        public string OrdainNftSecretMessage { get; set; }

        // --- x402 revenue sharing (baptise/Saint NFTs can earn when payments are received) ---

        /// <summary>Enable x402 revenue sharing on baptise/Saint NFTs so holders can earn from payments. Default true when X402PaymentEndpoint is set.</summary>
        public bool X402Enabled { get; set; }

        /// <summary>x402 payment endpoint URL for revenue distribution webhooks (e.g. https://api.oasisweb4.com/api/x402/revenue/saint). If empty and X402Enabled is true, derived from OasisApiBaseUrl + "/api/x402/revenue/saint".</summary>
        public string X402PaymentEndpoint { get; set; }

        /// <summary>x402 revenue distribution model: "equal", "weighted", or "creator-split". Default "equal".</summary>
        public string X402RevenueModel { get; set; } = "equal";

        /// <summary>Optional treasury wallet address for x402 distribution.</summary>
        public string X402TreasuryWallet { get; set; }

        /// <summary>Minimum $SAINT balance (raw units) to qualify for drip. Default 0 = any positive balance.</summary>
        public decimal SaintDripMinBalance { get; set; }

        /// <summary>Optional pool avatar ID whose wallet funds the Saint drip. Documented in SAINTS_X402_POOL_AND_DRIP.md.</summary>
        public string SaintDripPoolAvatarId { get; set; }
    }
}
