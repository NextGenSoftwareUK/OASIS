using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.ChatBridge;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// REST API for the Discord ↔ Telegram chat bridge.
    /// Provides avatar link management and bridge status endpoints.
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class ChatBridgeController : ControllerBase
    {
        private readonly IChatBridgeAvatarLinkStore _linkStore;
        private readonly ChatBridgeRouter _router;
        private readonly ChatBridgeOptions _options;
        private readonly ILogger<ChatBridgeController> _logger;

        public ChatBridgeController(
            IChatBridgeAvatarLinkStore linkStore,
            ChatBridgeRouter router,
            IOptions<ChatBridgeOptions> options,
            ILogger<ChatBridgeController> logger)
        {
            _linkStore = linkStore;
            _router = router;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Returns the current bridge status and configuration summary (no secrets exposed).
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                enabled = _options.Enabled,
                discordConfigured = !string.IsNullOrEmpty(_options.DiscordBotToken),
                discordWebhookConfigured = !string.IsNullOrEmpty(_options.DiscordWebhookUrl),
                discordBridgeChannelId = _options.DiscordBridgeChannelId,
                telegramConfigured = !string.IsNullOrEmpty(_options.TelegramBotToken),
                telegramBridgeChatId = _options.TelegramBridgeChatId
            });
        }

        /// <summary>
        /// Look up the OASIS avatar linked to a Discord user.
        /// </summary>
        [HttpGet("link/discord/{discordUserId}")]
        public async Task<IActionResult> GetDiscordLink(ulong discordUserId)
        {
            var link = await _linkStore.GetByDiscordUserIdAsync(discordUserId).ConfigureAwait(false);
            if (link == null) return NotFound(new { message = $"No avatar link found for Discord user {discordUserId}" });
            return Ok(link);
        }

        /// <summary>
        /// Look up the OASIS avatar linked to a Telegram user.
        /// </summary>
        [HttpGet("link/telegram/{telegramUserId}")]
        public async Task<IActionResult> GetTelegramLink(long telegramUserId)
        {
            var link = await _linkStore.GetByTelegramUserIdAsync(telegramUserId).ConfigureAwait(false);
            if (link == null) return NotFound(new { message = $"No avatar link found for Telegram user {telegramUserId}" });
            return Ok(link);
        }

        /// <summary>
        /// Programmatically link a Discord user to an OASIS avatar username.
        /// Useful for admin tooling or onboarding flows.
        /// </summary>
        [HttpPost("link/discord")]
        public async Task<IActionResult> LinkDiscordUser([FromBody] LinkPlatformUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.OasisUsername))
                return BadRequest(new { message = "OasisUsername is required." });
            if (request.PlatformUserId == 0)
                return BadRequest(new { message = "PlatformUserId (Discord snowflake) is required." });

            var doc = new BridgedAvatarLinkDocument
            {
                Id = $"discord:{request.PlatformUserId}",
                Platform = "discord",
                PlatformUserId = request.PlatformUserId.ToString(),
                PlatformUsername = request.PlatformUsername ?? "",
                OasisAvatarId = Guid.Empty,
                OasisUsername = request.OasisUsername.Trim(),
                LinkedAtUtc = DateTime.UtcNow
            };

            await _linkStore.UpsertAsync(doc).ConfigureAwait(false);
            _logger.LogInformation("[ChatBridgeController] Linked Discord user {Id} to OASIS avatar '{Avatar}'", request.PlatformUserId, request.OasisUsername);
            return Ok(doc);
        }

        /// <summary>
        /// Programmatically link a Telegram user to an OASIS avatar username.
        /// </summary>
        [HttpPost("link/telegram")]
        public async Task<IActionResult> LinkTelegramUser([FromBody] LinkPlatformUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.OasisUsername))
                return BadRequest(new { message = "OasisUsername is required." });
            if (request.PlatformUserId == 0)
                return BadRequest(new { message = "PlatformUserId (Telegram numeric ID) is required." });

            var doc = new BridgedAvatarLinkDocument
            {
                Id = $"telegram:{request.PlatformUserId}",
                Platform = "telegram",
                PlatformUserId = request.PlatformUserId.ToString(),
                PlatformUsername = request.PlatformUsername ?? "",
                OasisAvatarId = Guid.Empty,
                OasisUsername = request.OasisUsername.Trim(),
                LinkedAtUtc = DateTime.UtcNow
            };

            await _linkStore.UpsertAsync(doc).ConfigureAwait(false);
            _logger.LogInformation("[ChatBridgeController] Linked Telegram user {Id} to OASIS avatar '{Avatar}'", request.PlatformUserId, request.OasisUsername);
            return Ok(doc);
        }

        /// <summary>
        /// Remove a Discord user's avatar link.
        /// </summary>
        [HttpDelete("link/discord/{discordUserId}")]
        public async Task<IActionResult> UnlinkDiscordUser(ulong discordUserId)
        {
            var removed = await _linkStore.RemoveByDiscordUserIdAsync(discordUserId).ConfigureAwait(false);
            return removed
                ? Ok(new { message = "Link removed." })
                : NotFound(new { message = "No link found to remove." });
        }

        /// <summary>
        /// Remove a Telegram user's avatar link.
        /// </summary>
        [HttpDelete("link/telegram/{telegramUserId}")]
        public async Task<IActionResult> UnlinkTelegramUser(long telegramUserId)
        {
            var removed = await _linkStore.RemoveByTelegramUserIdAsync(telegramUserId).ConfigureAwait(false);
            return removed
                ? Ok(new { message = "Link removed." })
                : NotFound(new { message = "No link found to remove." });
        }

        /// <summary>
        /// Returns the most recent bridged messages (default 50, max 200).
        /// Used by the IDE BridgePanel to populate the message feed.
        /// </summary>
        [HttpGet("messages")]
        public IActionResult GetMessages([FromQuery] int limit = 50)
        {
            limit = Math.Clamp(limit, 1, 200);
            var messages = _router.GetRecentMessages(limit);
            return Ok(messages);
        }

        /// <summary>
        /// Send a message from the IDE to both Discord and Telegram.
        /// Body: { senderName: string, text: string }
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendFromIde([FromBody] IdeSendRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Text))
                return BadRequest(new { message = "text is required." });

            var sender = string.IsNullOrWhiteSpace(request.SenderName) ? "IDE User" : request.SenderName;
            await _router.RouteFromIdeAsync(sender, request.Text).ConfigureAwait(false);
            return Ok(new { sent = true });
        }
    }

    public class LinkPlatformUserRequest
    {
        /// <summary>Discord snowflake or Telegram numeric user ID.</summary>
        public long PlatformUserId { get; set; }

        /// <summary>Platform username (display, optional).</summary>
        public string PlatformUsername { get; set; }

        /// <summary>OASIS avatar username to link to.</summary>
        public string OasisUsername { get; set; }
    }

    public class IdeSendRequest
    {
        public string SenderName { get; set; }
        public string Text { get; set; }
    }
}
