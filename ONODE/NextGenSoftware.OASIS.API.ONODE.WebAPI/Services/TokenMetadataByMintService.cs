using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFTs;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// Resolves Solana token metadata by mint address (on-chain + JSON at uri).
    /// Used by NftController and TelegramNftMintFlowService so the bot can call in-process (no HTTP/base URL needed).
    /// </summary>
    public interface ITokenMetadataByMintService
    {
        Task<TokenMetadataByMintResponse> GetMetadataAsync(string mint);
    }

    public class TokenMetadataByMintService : ITokenMetadataByMintService
    {
        private readonly ILogger<TokenMetadataByMintService> _logger;

        public TokenMetadataByMintService(ILogger<TokenMetadataByMintService> logger)
        {
            _logger = logger;
        }

        public async Task<TokenMetadataByMintResponse> GetMetadataAsync(string mint)
        {
            if (string.IsNullOrWhiteSpace(mint))
                return null;

            try
            {
                var nftManager = new NFTManager(Guid.Empty);
                var providerResult = nftManager.GetNFTProvider(ProviderType.SolanaOASIS);
                if (providerResult?.Result == null || providerResult.IsError)
                {
                    _logger?.LogWarning("[TokenMetadataByMint] Solana provider not available: {Message}", providerResult?.Message);
                    return null;
                }

                var solanaProvider = providerResult.Result as IOASISNFTProvider;
                if (solanaProvider == null)
                {
                    _logger?.LogWarning("[TokenMetadataByMint] Solana provider does not support LoadOnChainNFTDataAsync");
                    return null;
                }

                // Memecoins / Solscan tokens are on mainnet: use MainnetConnectionString when set so lookup succeeds.
                var mainnetRpc = OASISDNAManager.OASISDNA?.OASIS?.StorageProviders?.SolanaOASIS?.MainnetConnectionString
                    ?? OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.StorageProviders?.SolanaOASIS?.MainnetConnectionString;

                // Use standard LoadOnChainNFTDataAsync (provider uses its configured RPC; mainnet when set in OASIS_DNA).
                if (string.IsNullOrWhiteSpace(mainnetRpc))
                    _logger?.LogWarning("[TokenMetadataByMint] MainnetConnectionString not set; memecoin lookup may fail for mainnet tokens. Mint: {Mint}", mint);
                else
                    _logger?.LogInformation("[TokenMetadataByMint] Using mainnet RPC for mint {Mint}", mint);
                OASISResult<IWeb3NFT> loadResult = await solanaProvider.LoadOnChainNFTDataAsync(mint).ConfigureAwait(false);
                if (loadResult?.Result == null || loadResult.IsError)
                {
                    _logger?.LogWarning("[TokenMetadataByMint] Metaplex lookup failed for {Mint}: {Message}", mint, loadResult?.Message);
                    // Option A: Helius DAS API fallback for pump.fun / memecoins that don't have Metaplex metadata
                    if (!string.IsNullOrWhiteSpace(mainnetRpc))
                    {
                        var dasResult = await TryGetMetadataViaHeliusDasAsync(mint, mainnetRpc.Trim()).ConfigureAwait(false);
                        if (dasResult != null)
                        {
                            _logger?.LogInformation("[TokenMetadataByMint] Resolved via DAS fallback for mint {Mint}", mint);
                            return dasResult;
                        }
                    }
                    return null;
                }

                var nft = loadResult.Result;
                var uri = nft.JSONMetaDataURL;
                string image = null;
                string description = null;

                if (!string.IsNullOrWhiteSpace(uri))
                {
                    try
                    {
                        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                        var json = await client.GetStringAsync(uri).ConfigureAwait(false);
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("image", out var imgEl))
                            image = imgEl.GetString();
                        if (string.IsNullOrEmpty(image) && root.TryGetProperty("image_url", out var imgUrlEl))
                            image = imgUrlEl.GetString();
                        if (root.TryGetProperty("description", out var descEl))
                            description = descEl.GetString();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogDebug(ex, "[TokenMetadataByMint] Failed to fetch URI JSON for {Mint}", mint);
                    }
                }

                return new TokenMetadataByMintResponse
                {
                    Mint = mint,
                    Name = nft.Title ?? "",
                    Symbol = nft.Symbol ?? "",
                    Uri = uri ?? "",
                    Image = image ?? "",
                    Description = description ?? ""
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[TokenMetadataByMint] GetMetadata failed for mint {Mint}", mint);
                return null;
            }
        }

        /// <summary>
        /// Fallback for tokens without Metaplex metadata (e.g. pump.fun memecoins).
        /// Calls Helius (or any DAS-capable RPC) getAsset and maps the response to our metadata shape.
        /// </summary>
        private async Task<TokenMetadataByMintResponse> TryGetMetadataViaHeliusDasAsync(string mint, string rpcUrl)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                var body = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "getAsset",
                    @params = new { id = mint, options = new { showFungible = true } }
                };
                var json = JsonSerializer.Serialize(body);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await http.PostAsync(rpcUrl, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;
                if (!root.TryGetProperty("result", out var resultEl) || resultEl.ValueKind == JsonValueKind.Null)
                    return null;
                // Some RPCs return result.asset, others return the asset object directly as result
                var assetEl = resultEl.TryGetProperty("asset", out var a) ? a : resultEl;
                if (!assetEl.TryGetProperty("content", out var contentEl))
                    return null;
                contentEl.TryGetProperty("metadata", out var metaEl);
                var name = metaEl.TryGetProperty("name", out var n) ? n.GetString()?.Trim() : null;
                var symbol = metaEl.TryGetProperty("symbol", out var s) ? s.GetString()?.Trim() : null;
                var description = metaEl.TryGetProperty("description", out var d) ? d.GetString()?.Trim() : null;
                var jsonUri = contentEl.TryGetProperty("json_uri", out var u) ? u.GetString()?.Trim() : null;
                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(symbol) && string.IsNullOrEmpty(jsonUri))
                    return null;
                // Image: content.metadata.image, or first content.files[].uri, or from json_uri JSON
                var image = metaEl.TryGetProperty("image", out var img) ? img.GetString()?.Trim() : null;
                if (string.IsNullOrEmpty(image) && contentEl.TryGetProperty("files", out var filesEl) && filesEl.GetArrayLength() > 0)
                {
                    var first = filesEl[0];
                    if (first.TryGetProperty("uri", out var uriEl))
                        image = uriEl.GetString()?.Trim();
                }
                if (string.IsNullOrEmpty(image) && !string.IsNullOrWhiteSpace(jsonUri))
                {
                    try
                    {
                        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                        var jsonStr = await client.GetStringAsync(jsonUri).ConfigureAwait(false);
                        using var uriDoc = JsonDocument.Parse(jsonStr);
                        var uriRoot = uriDoc.RootElement;
                        if (uriRoot.TryGetProperty("image", out var i))
                            image = i.GetString()?.Trim();
                        if (string.IsNullOrEmpty(image) && uriRoot.TryGetProperty("image_url", out var iu))
                            image = iu.GetString()?.Trim();
                        if (string.IsNullOrEmpty(description) && uriRoot.TryGetProperty("description", out var desc))
                            description = desc.GetString()?.Trim();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogDebug(ex, "[TokenMetadataByMint] DAS: failed to fetch json_uri for {Mint}", mint);
                    }
                }
                return new TokenMetadataByMintResponse
                {
                    Mint = mint,
                    Name = name ?? "",
                    Symbol = symbol ?? "",
                    Uri = jsonUri ?? "",
                    Image = image ?? "",
                    Description = description ?? ""
                };
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "[TokenMetadataByMint] DAS getAsset failed for mint {Mint}", mint);
                return null;
            }
        }
    }
}
