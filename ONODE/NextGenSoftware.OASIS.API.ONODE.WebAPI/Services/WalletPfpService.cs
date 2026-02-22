using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Telegram;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// Resolves a Solana wallet's PFP URL via Helius DAS getAssetsByOwner (first NFT image).
    /// </summary>
    public class WalletPfpService : IWalletPfpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WalletPfpService> _logger;
        private readonly TelegramNftMintOptions _options;
        private const int TimeoutSeconds = 15;

        public WalletPfpService(
            IHttpClientFactory httpClientFactory,
            ILogger<WalletPfpService> logger,
            IOptions<TelegramNftMintOptions> options = null)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _options = options?.Value;
        }

        public async Task<string> GetPfpUrlAsync(string walletAddress, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(walletAddress))
                return null;

            var rpcUrl = _options?.HeliusMainnetRpcUrl?.Trim();
            if (string.IsNullOrEmpty(rpcUrl))
            {
                _logger?.LogDebug("[WalletPfp] HeliusMainnetRpcUrl not set; skipping PFP lookup.");
                return null;
            }

            try
            {
                // DAS getAssetsByOwner: params as single object (Helius accepts this)
                var body = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "getAssetsByOwner",
                    @params = new object[] { new { ownerAddress = walletAddress.Trim(), limit = 10, page = 1 } }
                };

                using var http = _httpClientFactory.CreateClient();
                http.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
                var json = JsonSerializer.Serialize(body);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await http.PostAsync(rpcUrl, content, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;
                if (!root.TryGetProperty("result", out var resultEl) || resultEl.ValueKind == JsonValueKind.Null)
                {
                    if (root.TryGetProperty("error", out var errEl))
                        _logger?.LogDebug("[WalletPfp] DAS error for {Wallet}: {Error}", walletAddress, errEl.GetRawText());
                    return null;
                }

                if (!resultEl.TryGetProperty("items", out var itemsEl) || itemsEl.ValueKind != JsonValueKind.Array)
                    return null;

                foreach (var item in itemsEl.EnumerateArray())
                {
                    var url = TryGetImageUrlFromAsset(item);
                    if (!string.IsNullOrEmpty(url))
                        return url;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "[WalletPfp] Failed to get PFP for {Wallet}", walletAddress);
                return null;
            }
        }

        private static string TryGetImageUrlFromAsset(JsonElement item)
        {
            if (!item.TryGetProperty("content", out var contentEl))
                return null;

            // Prefer content.files[0].cdn_uri (Helius CDN) then .uri
            if (contentEl.TryGetProperty("files", out var filesEl) && filesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var file in filesEl.EnumerateArray())
                {
                    var mime = file.TryGetProperty("mime", out var mimeEl) ? mimeEl.GetString() : null;
                    if (mime != null && !mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (file.TryGetProperty("cdn_uri", out var cdnEl))
                    {
                        var u = cdnEl.GetString();
                        if (!string.IsNullOrWhiteSpace(u)) return u;
                    }
                    if (file.TryGetProperty("uri", out var uriEl))
                    {
                        var u = uriEl.GetString();
                        if (!string.IsNullOrWhiteSpace(u)) return u;
                    }
                }
            }

            // Fallback: content.links?.image (some DAS responses use this)
            if (contentEl.TryGetProperty("links", out var linksEl) && linksEl.TryGetProperty("image", out var imgEl))
            {
                var u = imgEl.GetString();
                if (!string.IsNullOrWhiteSpace(u)) return u;
            }

            return null;
        }
    }
}
