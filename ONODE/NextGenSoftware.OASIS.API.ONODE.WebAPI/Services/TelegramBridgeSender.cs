using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.ChatBridge;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// Sends messages to a Telegram group via the Bot API (no library dependency needed).
    /// Used to relay Discord → Telegram bridge messages.
    /// </summary>
    public class TelegramBridgeSender
    {
        private readonly HttpClient _http;
        private readonly ChatBridgeOptions _options;
        private readonly ILogger<TelegramBridgeSender> _logger;

        public TelegramBridgeSender(IHttpClientFactory httpClientFactory, IOptions<ChatBridgeOptions> options, ILogger<TelegramBridgeSender> logger)
        {
            _http = httpClientFactory.CreateClient("TelegramBridge");
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>Posts a plain text message to the configured bridged Telegram group.</summary>
        public async Task<bool> SendToBridgedGroupAsync(string text)
        {
            return await SendMessageAsync(_options.TelegramBridgeChatId, text).ConfigureAwait(false);
        }

        /// <summary>Sends a message to any Telegram chat by ID (used for DM confirmations).</summary>
        public async Task<bool> SendMessageAsync(long chatId, string text)
        {
            if (string.IsNullOrEmpty(_options.TelegramBotToken))
            {
                _logger.LogWarning("[TelegramBridgeSender] TelegramBotToken not configured; skipping send.");
                return false;
            }

            try
            {
                var url = $"https://api.telegram.org/bot{_options.TelegramBotToken}/sendMessage";
                var payload = new
                {
                    chat_id = chatId,
                    text,
                    parse_mode = "Markdown"
                };
                var response = await _http.PostAsJsonAsync(url, payload).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    _logger.LogWarning("[TelegramBridgeSender] Telegram API returned {Status} for chatId {ChatId}: {Body}", response.StatusCode, chatId, body);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TelegramBridgeSender] Failed to send message to Telegram chat {ChatId}", chatId);
                return false;
            }
        }
    }
}
