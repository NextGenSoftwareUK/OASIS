namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.ChatBridge
{
    /// <summary>
    /// Configuration for the Discord ↔ Telegram chat bridge.
    /// Bind to appsettings.json section "ChatBridge".
    /// </summary>
    public class ChatBridgeOptions
    {
        public const string SectionName = "ChatBridge";

        /// <summary>When false the bridge is completely disabled (no Discord bot started, no routing).</summary>
        public bool Enabled { get; set; } = true;

        // ── Discord ──────────────────────────────────────────────────────────

        /// <summary>Discord bot token from the Discord Developer Portal.</summary>
        public string DiscordBotToken { get; set; }

        /// <summary>Numeric snowflake ID of the Discord channel to bridge.</summary>
        public ulong DiscordBridgeChannelId { get; set; }

        /// <summary>
        /// Discord Incoming Webhook URL used to post Telegram→Discord messages.
        /// Create one in Discord: Channel Settings → Integrations → Webhooks.
        /// </summary>
        public string DiscordWebhookUrl { get; set; }

        // ── Telegram ─────────────────────────────────────────────────────────

        /// <summary>
        /// Telegram bot token used for sending bridge messages and DM confirmations.
        /// Can share the existing TelegramNftMint:BotToken or be a separate bot.
        /// </summary>
        public string TelegramBotToken { get; set; }

        /// <summary>
        /// Telegram chat ID of the group to bridge (negative number, e.g. -1001234567890).
        /// The bot must be an admin or at least a member of this group.
        /// </summary>
        public long TelegramBridgeChatId { get; set; }
    }
}
