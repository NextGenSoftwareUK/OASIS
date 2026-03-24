using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.ChatBridge;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// Central routing hub for the chat bridge.
    /// Receives a message from one platform, resolves the sender's OASIS avatar display name,
    /// formats it, and relays it to the other platform.
    /// Also handles /link commands for avatar linking from either platform.
    /// Maintains an in-memory history of the last 200 bridged messages for the IDE panel.
    /// </summary>
    public class ChatBridgeRouter
    {
        private const int MaxHistory = 200;

        private readonly IChatBridgeAvatarLinkStore _linkStore;
        private readonly TelegramBridgeSender _telegramSender;
        private readonly DiscordWebhookSender _discordSender;
        private readonly ChatBridgeOptions _options;
        private readonly ILogger<ChatBridgeRouter> _logger;

        // Thread-safe ring buffer for message history
        private readonly ConcurrentQueue<BridgeMessage> _history = new();
        private readonly object _historyLock = new();

        public ChatBridgeRouter(
            IChatBridgeAvatarLinkStore linkStore,
            TelegramBridgeSender telegramSender,
            DiscordWebhookSender discordSender,
            IOptions<ChatBridgeOptions> options,
            ILogger<ChatBridgeRouter> logger)
        {
            _linkStore = linkStore;
            _telegramSender = telegramSender;
            _discordSender = discordSender;
            _options = options.Value;
            _logger = logger;
        }

        // ── History ───────────────────────────────────────────────────────────

        private void RecordMessage(string platform, string senderName, string text)
        {
            lock (_historyLock)
            {
                _history.Enqueue(new BridgeMessage
                {
                    Platform = platform,
                    SenderName = senderName,
                    Text = text
                });
                while (_history.Count > MaxHistory)
                    _history.TryDequeue(out _);
            }
        }

        /// <summary>Returns the most recent messages (up to <paramref name="limit"/>), oldest first.</summary>
        public IReadOnlyList<BridgeMessage> GetRecentMessages(int limit = 50)
        {
            lock (_historyLock)
            {
                return _history.TakeLast(Math.Min(limit, MaxHistory)).ToList();
            }
        }

        // ── Discord → Telegram ────────────────────────────────────────────────

        /// <summary>
        /// Called when a regular (non-command) message arrives from Discord.
        /// Resolves the avatar display name and relays to the Telegram bridged group.
        /// </summary>
        public async Task RouteFromDiscordAsync(ulong discordUserId, string discordUsername, string text)
        {
            if (!_options.Enabled || string.IsNullOrWhiteSpace(text)) return;

            var link = await _linkStore.GetByDiscordUserIdAsync(discordUserId).ConfigureAwait(false);
            var displayName = link?.OasisUsername ?? discordUsername ?? discordUserId.ToString();

            RecordMessage("discord", displayName, text);

            var relayed = $"🎮 *{EscapeMarkdown(displayName)}* (Discord)\n{text}";

            _logger.LogInformation("[ChatBridge] Discord→Telegram from {User}: {Text}", displayName, text.Length > 80 ? text[..80] + "…" : text);
            await _telegramSender.SendToBridgedGroupAsync(relayed).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles /link &lt;oasis-username&gt; from Discord.
        /// Stores the Discord user → OASIS avatar link and confirms via the webhook.
        /// Returns a reply string to be sent back via the Discord gateway.
        /// </summary>
        public async Task<string> HandleDiscordLinkCommandAsync(ulong discordUserId, string discordUsername, string oasisUsername)
        {
            if (string.IsNullOrWhiteSpace(oasisUsername))
                return "Usage: `/link <your-oasis-username>`";

            oasisUsername = oasisUsername.Trim();

            var doc = new BridgedAvatarLinkDocument
            {
                Id = $"discord:{discordUserId}",
                Platform = "discord",
                PlatformUserId = discordUserId.ToString(),
                PlatformUsername = discordUsername,
                OasisAvatarId = Guid.Empty,      // no API lookup in v1 — username only
                OasisUsername = oasisUsername,
                LinkedAtUtc = DateTime.UtcNow
            };

            await _linkStore.UpsertAsync(doc).ConfigureAwait(false);
            _logger.LogInformation("[ChatBridge] Discord user {DiscordUser} linked to OASIS avatar '{Avatar}'", discordUsername, oasisUsername);

            return $"✅ Linked! Your messages will now appear as **{oasisUsername}** in the bridge.";
        }

        // ── Telegram → Discord ────────────────────────────────────────────────

        /// <summary>
        /// Called when a regular (non-command) message arrives from Telegram.
        /// Resolves the avatar display name and relays to the Discord channel via webhook.
        /// </summary>
        public async Task RouteFromTelegramAsync(long telegramUserId, string telegramUsername, string text)
        {
            if (!_options.Enabled || string.IsNullOrWhiteSpace(text)) return;

            var link = await _linkStore.GetByTelegramUserIdAsync(telegramUserId).ConfigureAwait(false);
            var displayName = link?.OasisUsername ?? telegramUsername ?? telegramUserId.ToString();

            RecordMessage("telegram", displayName, text);

            var relayed = $"📱 **{displayName}** (Telegram)\n{text}";

            _logger.LogInformation("[ChatBridge] Telegram→Discord from {User}: {Text}", displayName, text.Length > 80 ? text[..80] + "…" : text);
            await _discordSender.SendAsync(relayed).ConfigureAwait(false);
        }

        // ── IDE → Both platforms ─────────────────────────────────────────────

        /// <summary>
        /// Called when a message is sent from the IDE BridgePanel.
        /// Broadcasts to both Discord and Telegram, and stores in local history.
        /// </summary>
        public async Task RouteFromIdeAsync(string senderName, string text)
        {
            if (!_options.Enabled || string.IsNullOrWhiteSpace(text)) return;

            RecordMessage("ide", senderName, text);

            var toTelegram = $"💻 *{EscapeMarkdown(senderName)}* (IDE)\n{EscapeMarkdown(text)}";
            var toDiscord  = $"💻 **{senderName}** (IDE)\n{text}";

            _logger.LogInformation("[ChatBridge] IDE→Both from {User}: {Text}", senderName, text.Length > 80 ? text[..80] + "…" : text);

            await Task.WhenAll(
                _telegramSender.SendToBridgedGroupAsync(toTelegram),
                _discordSender.SendAsync(toDiscord)
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles /link &lt;oasis-username&gt; from Telegram.
        /// Stores the Telegram user → OASIS avatar link.
        /// Returns a reply string to be sent back to the user.
        /// </summary>
        public async Task<string> HandleTelegramLinkCommandAsync(long telegramUserId, string telegramUsername, string oasisUsername)
        {
            if (string.IsNullOrWhiteSpace(oasisUsername))
                return "Usage: /link <your-oasis-username>";

            oasisUsername = oasisUsername.Trim();

            var doc = new BridgedAvatarLinkDocument
            {
                Id = $"telegram:{telegramUserId}",
                Platform = "telegram",
                PlatformUserId = telegramUserId.ToString(),
                PlatformUsername = telegramUsername,
                OasisAvatarId = Guid.Empty,      // no API lookup in v1 — username only
                OasisUsername = oasisUsername,
                LinkedAtUtc = DateTime.UtcNow
            };

            await _linkStore.UpsertAsync(doc).ConfigureAwait(false);
            _logger.LogInformation("[ChatBridge] Telegram user {TelegramUser} linked to OASIS avatar '{Avatar}'", telegramUsername, oasisUsername);

            return $"✅ Linked! Your messages will now appear as *{oasisUsername}* in the bridge.";
        }

        // ── Unlink ────────────────────────────────────────────────────────────

        public async Task<string> HandleDiscordUnlinkCommandAsync(ulong discordUserId)
        {
            var removed = await _linkStore.RemoveByDiscordUserIdAsync(discordUserId).ConfigureAwait(false);
            return removed ? "✅ Your avatar link has been removed." : "No link found to remove.";
        }

        public async Task<string> HandleTelegramUnlinkCommandAsync(long telegramUserId)
        {
            var removed = await _linkStore.RemoveByTelegramUserIdAsync(telegramUserId).ConfigureAwait(false);
            return removed ? "✅ Your avatar link has been removed." : "No link found to remove.";
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string EscapeMarkdown(string text)
        {
            // Escape Telegram MarkdownV1 special chars in display names
            return text?.Replace("_", "\\_").Replace("*", "\\*").Replace("[", "\\[").Replace("`", "\\`") ?? "";
        }
    }
}
