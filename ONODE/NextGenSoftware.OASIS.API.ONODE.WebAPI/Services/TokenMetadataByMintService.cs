using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFTs;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using NextGenSoftware.OASIS.Common;

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

                // When MainnetConnectionString is set in OASIS_DNA (SolanaOASIS), use mainnet for metadata lookup (e.g. Solscan memecoins are mainnet).
                var mainnetRpc = OASISDNAManager.OASISDNA?.OASIS?.StorageProviders?.SolanaOASIS?.MainnetConnectionString;
                OASISResult<IWeb3NFT> loadResult;
                if (!string.IsNullOrWhiteSpace(mainnetRpc) && solanaProvider is SolanaOASIS solanaOasis)
                {
                    _logger?.LogDebug("[TokenMetadataByMint] Using mainnet RPC for mint {Mint}", mint);
                    loadResult = await solanaOasis.LoadOnChainNFTDataWithRpcAsync(mint, mainnetRpc.Trim()).ConfigureAwait(false);
                }
                else
                {
                    loadResult = await solanaProvider.LoadOnChainNFTDataAsync(mint).ConfigureAwait(false);
                }
                if (loadResult?.Result == null || loadResult.IsError)
                {
                    _logger?.LogWarning("[TokenMetadataByMint] LoadOnChainNFTDataAsync failed for {Mint}: {Message}", mint, loadResult?.Message);
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
    }
}
