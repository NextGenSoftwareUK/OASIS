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
    /// Sends messages to Discord via an Incoming Webhook URL (no library required).
    /// Used to relay Telegram → Discord bridge messages.
    /// Create a webhook in Discord: Channel Settings → Integrations → Webhooks → New Webhook.
    /// </summary>
    public class DiscordWebhookSender
    {
        private readonly HttpClient _http;
        private readonly ChatBridgeOptions _options;
        private readonly ILogger<DiscordWebhookSender> _logger;

        public DiscordWebhookSender(IHttpClientFactory httpClientFactory, IOptions<ChatBridgeOptions> options, ILogger<DiscordWebhookSender> logger)
        {
            _http = httpClientFactory.CreateClient("DiscordWebhook");
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>Posts a message to the configured Discord channel via Incoming Webhook.</summary>
        public async Task<bool> SendAsync(string content)
        {
            if (string.IsNullOrEmpty(_options.DiscordWebhookUrl))
            {
                _logger.LogWarning("[DiscordWebhookSender] DiscordWebhookUrl not configured; skipping send.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(content))
                return true;

            // Discord webhook has a 2000-char limit per message
            if (content.Length > 2000)
                content = content.Substring(0, 1997) + "...";

            try
            {
                var payload = new { content };
                var response = await _http.PostAsJsonAsync(_options.DiscordWebhookUrl, payload).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    _logger.LogWarning("[DiscordWebhookSender] Discord webhook returned {Status}: {Body}", response.StatusCode, body);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DiscordWebhookSender] Failed to send message to Discord webhook");
                return false;
            }
        }
    }
}
