using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.ChatBridge;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Telegram;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// Receives Telegram webhook updates and runs the NFT mint flow.
    /// Configure your bot with: SetWebhook URL = https://your-domain/api/telegram/webhook
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TelegramWebhookController : ControllerBase
    {
        private readonly TelegramNftMintFlowService _flowService;
        private readonly ISaintMintRecordService _saintMintRecordService;
        private readonly ITokenMetadataByMintService _tokenMetadataByMintService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TelegramWebhookController> _logger;
        private readonly ChatBridgeRouter _bridgeRouter;
        private readonly ChatBridgeOptions _bridgeOptions;

        public TelegramWebhookController(
            TelegramNftMintFlowService flowService,
            ISaintMintRecordService saintMintRecordService,
            ITokenMetadataByMintService tokenMetadataByMintService,
            IConfiguration configuration,
            ILogger<TelegramWebhookController> logger,
            ChatBridgeRouter bridgeRouter = null,
            IOptions<ChatBridgeOptions> bridgeOptions = null)
        {
            _flowService = flowService;
            _saintMintRecordService = saintMintRecordService;
            _tokenMetadataByMintService = tokenMetadataByMintService;
            _configuration = configuration;
            _logger = logger;
            _bridgeRouter = bridgeRouter;
            _bridgeOptions = bridgeOptions?.Value;
        }

        /// <summary>
        /// Backfill historical mints into SaintMintRecords. Requires X-Saint-Backfill-Key header.
        /// Set TelegramNftMint:SaintBackfillKey in config. If empty, backfill is disabled.
        /// </summary>
        [HttpPost("saint-mints/backfill")]
        [Route("~/api/telegram/saint-mints/backfill")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> SaintMintsBackfill([FromBody] SaintMintBackfillRequest request)
        {
            var key = _configuration["TelegramNftMint:SaintBackfillKey"] ?? _configuration["SaintMintRecords:BackfillKey"];
            var provided = Request.Headers["X-Saint-Backfill-Key"].FirstOrDefault();
            if (string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("[SaintMintsBackfill] Backfill disabled: TelegramNftMint:SaintBackfillKey not set");
                return NotFound();
            }
            if (provided != key)
            {
                return Unauthorized();
            }
            if (request?.Mints == null || request.Mints.Count == 0)
                return BadRequest(new { error = "Mints array is required and must not be empty" });

            var records = request.Mints
                .Where(m => !string.IsNullOrWhiteSpace(m.NftMintAddress))
                .Select(m => new SaintMintRecord
                {
                    TelegramUserId = 0,
                    WalletAddress = m.WalletAddress ?? "",
                    NftMintAddress = m.NftMintAddress.Trim(),
                    TxHash = m.TxHash ?? "",
                    CreatedAtUtc = m.CreatedAtUtc ?? DateTime.UtcNow
                })
                .ToList();

            var inserted = await _saintMintRecordService.BackfillMintsAsync(records).ConfigureAwait(false);
            _logger.LogInformation("[SaintMintsBackfill] Inserted {Count} of {Total} mints", inserted, records.Count);
            return Ok(new { inserted, total = records.Count });
        }

        /// <summary>
        /// Returns recent mints from the SAINT bot with NFT metadata. No auth required.
        /// Used by saints.fun gallery.
        /// </summary>
        [HttpGet("saint-mints")]
        [Route("~/api/telegram/saint-mints")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> SaintMints([FromQuery] int limit = 48)
        {
            if (limit < 1 || limit > 100) limit = 48;
            if (_saintMintRecordService == null)
                return Ok(new { mints = Array.Empty<object>() });

            var records = await _saintMintRecordService.ListRecentMintsAsync(limit).ConfigureAwait(false);
            var mints = new List<object>();
            foreach (var r in records)
            {
                var item = new
                {
                    nftMintAddress = r.NftMintAddress,
                    txHash = r.TxHash,
                    walletAddress = r.WalletAddress,
                    createdAtUtc = r.CreatedAtUtc.ToString("o"),
                    image = (string)null,
                    name = (string)null,
                    symbol = (string)null
                };
                if (_tokenMetadataByMintService != null && !string.IsNullOrWhiteSpace(r.NftMintAddress))
                {
                    try
                    {
                        var meta = await _tokenMetadataByMintService.GetMetadataAsync(r.NftMintAddress).ConfigureAwait(false);
                        if (meta != null)
                        {
                            item = new
                            {
                                nftMintAddress = r.NftMintAddress,
                                txHash = r.TxHash,
                                walletAddress = r.WalletAddress,
                                createdAtUtc = r.CreatedAtUtc.ToString("o"),
                                image = meta.Image ?? "",
                                name = meta.Name ?? "",
                                symbol = meta.Symbol ?? ""
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "[SaintMints] Metadata lookup failed for mint {Mint}", r.NftMintAddress);
                    }
                }
                mints.Add(item);
            }
            return Ok(new { mints });
        }

        /// <summary>
        /// Returns SAINT bot mint counts (today UTC and total). No auth required.
        /// </summary>
        [HttpGet("saint-mint-stats")]
        [Route("~/api/telegram/saint-mint-stats")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> SaintMintStats()
        {
            var now = DateTime.UtcNow;
            var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            var todayEnd = todayStart.AddDays(1);
            var mintsToday = _saintMintRecordService != null ? await _saintMintRecordService.CountMintsAsync(todayStart, todayEnd).ConfigureAwait(false) : 0;
            var mintsTotal = _saintMintRecordService != null ? await _saintMintRecordService.CountMintsAsync(null, null).ConfigureAwait(false) : 0;
            return Ok(new { mintsToday, mintsTotal, dateUtc = todayStart.ToString("yyyy-MM-dd") });
        }

        /// <summary>
        /// Telegram sends POST here with Update JSON. We process the message and reply via Telegram API.
        /// Route accepts both api/TelegramWebhook/webhook and api/telegram/webhook (case-sensitive hosts may use lowercase).
        /// </summary>
        [ApiExplorerSettings(IgnoreApi = true)] // Exclude from Swagger to avoid "Ambiguous HTTP method" (action has multiple route templates)
        [HttpPost("webhook")]
        [Route("~/api/telegram/webhook")] // absolute route so .../api/telegram/webhook (lowercase) works
        public async Task<IActionResult> Webhook([FromBody] TelegramWebhookUpdate update)
        {
            if (update?.Message == null)
                return Ok();

            var chatId = update.Message.Chat.Id;
            var userId = update.Message.From?.Id ?? 0;
            var username = update.Message.From?.Username ?? update.Message.From?.FirstName ?? userId.ToString();
            var text = update.Message.Text ?? "";
            _logger.LogInformation("[TelegramWebhook] Update {UpdateId} chat {ChatId} text: {Text}", update.UpdateId, chatId, text.Length > 100 ? text.Substring(0, 100) + "..." : text);

            // ── Chat bridge: handle before NFT flow so /link is caught first ──
            if (_bridgeRouter != null && _bridgeOptions != null && _bridgeOptions.Enabled)
            {
                // /link command: works in any chat (DM or group)
                if (text.StartsWith("/link ", StringComparison.OrdinalIgnoreCase))
                {
                    var oasisUsername = text.Substring("/link ".Length).Trim();
                    var reply = await _bridgeRouter.HandleTelegramLinkCommandAsync(userId, username, oasisUsername).ConfigureAwait(false);
                    await _flowService.SendMessageAsync(chatId, reply).ConfigureAwait(false);
                    return Ok();
                }

                if (text.Equals("/unlink", StringComparison.OrdinalIgnoreCase))
                {
                    var reply = await _bridgeRouter.HandleTelegramUnlinkCommandAsync(userId).ConfigureAwait(false);
                    await _flowService.SendMessageAsync(chatId, reply).ConfigureAwait(false);
                    return Ok();
                }

                // Route regular messages from the bridged group to Discord (non-blocking)
                if (chatId == _bridgeOptions.TelegramBridgeChatId
                    && !text.StartsWith("/")        // skip all bot commands
                    && !string.IsNullOrWhiteSpace(text))
                {
                    _ = _bridgeRouter.RouteFromTelegramAsync(userId, username, text)
                        .ContinueWith(t => _logger.LogError(t.Exception, "[ChatBridge] RouteFromTelegram faulted"),
                            System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
                }
            }

            // ── Existing NFT mint / SAINTS flow ──────────────────────────────
            try
            {
                var reply = await _flowService.HandleUpdateAsync(update).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(reply))
                {
                    var sent = await _flowService.SendMessageAsync(chatId, reply).ConfigureAwait(false);
                    if (!sent)
                        _logger.LogWarning("[TelegramWebhook] SendMessage failed for chat {ChatId} (check BotToken and Telegram API)", chatId);
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "[TelegramWebhook] HandleUpdate failed for update {UpdateId} chat {ChatId}", update.UpdateId, chatId);
                try
                {
                    await _flowService.SendMessageAsync(chatId, "Something went wrong. Please try again or contact support.").ConfigureAwait(false);
                }
                catch { /* best effort */ }
            }

            return Ok();
        }
    }
}
