using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.Services;

/// <summary>
/// Exchange rate service using CoinGecko API (free tier, no API key required)
/// </summary>
public class CoinGeckoExchangeRateService : IExchangeRateService
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, string> _coinIds;
    private readonly Dictionary<string, CachedRate> _rateCache;
    private readonly TimeSpan _cacheExpiration;

    private class CachedRate
    {
        public decimal Rate { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public CoinGeckoExchangeRateService(HttpClient httpClient = null, int cacheExpirationMinutes = 5)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
        _cacheExpiration = TimeSpan.FromMinutes(cacheExpirationMinutes);
        _rateCache = new Dictionary<string, CachedRate>();

        // Map common token symbols to CoinGecko IDs
        _coinIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "SOL", "solana" },
            { "XRD", "radix" },
            { "BTC", "bitcoin" },
            { "ETH", "ethereum" },
            { "ARB", "arbitrum" },
            { "MATIC", "matic-network" },
            { "AVAX", "avalanche-2" },
            { "OP", "optimism" },
            { "BASE", "base" },
            { "BNB", "binancecoin" },
            { "FTM", "fantom" },
            { "NEAR", "near" },
            { "ADA", "cardano" },
            { "DOT", "polkadot" },
            { "ATOM", "cosmos" },
            { "USDC", "usd-coin" },
            { "USDT", "tether" }
        };
    }

    public async Task<OASISResult<decimal>> GetExchangeRateAsync(
        string fromToken, 
        string toToken, 
        CancellationToken token = default)
    {
        var result = new OASISResult<decimal>();

        try
        {
            // Check cache first
            string cacheKey = $"{fromToken}_{toToken}";
            if (_rateCache.TryGetValue(cacheKey, out var cachedRate))
            {
                if (DateTime.UtcNow - cachedRate.Timestamp < _cacheExpiration)
                {
                    result.Result = cachedRate.Rate;
                    result.IsError = false;
                    result.Message = $"Cached exchange rate: 1 {fromToken} = {cachedRate.Rate} {toToken}";
                    return result;
                }
                else
                {
                    // Remove expired cache entry
                    _rateCache.Remove(cacheKey);
                }
            }

            // Get CoinGecko IDs for tokens
            if (!_coinIds.TryGetValue(fromToken, out string fromCoinId))
            {
                result.IsError = true;
                result.Message = $"Unsupported token: {fromToken}";
                return result;
            }

            if (!_coinIds.TryGetValue(toToken, out string toCoinId))
            {
                result.IsError = true;
                result.Message = $"Unsupported token: {toToken}";
                return result;
            }

            // Fetch rates from CoinGecko
            string url = $"simple/price?ids={fromCoinId},{toCoinId}&vs_currencies=usd";
            var response = await _httpClient.GetAsync(url, token);

            if (!response.IsSuccessStatusCode)
            {
                result.IsError = true;
                result.Message = $"CoinGecko API error: {response.StatusCode}";
                return result;
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(jsonResponse);

            // Get USD prices for both tokens
            var fromTokenData = json[fromCoinId];
            var toTokenData = json[toCoinId];

            if (fromTokenData == null || toTokenData == null)
            {
                result.IsError = true;
                result.Message = "Failed to get price data from CoinGecko";
                return result;
            }

            decimal fromPriceUsd = fromTokenData["usd"]?.Value<decimal>() ?? 0;
            decimal toPriceUsd = toTokenData["usd"]?.Value<decimal>() ?? 0;

            if (fromPriceUsd == 0 || toPriceUsd == 0)
            {
                result.IsError = true;
                result.Message = "Invalid price data received";
                return result;
            }

            // Calculate exchange rate
            decimal exchangeRate = fromPriceUsd / toPriceUsd;

            // Cache the result
            _rateCache[cacheKey] = new CachedRate
            {
                Rate = exchangeRate,
                Timestamp = DateTime.UtcNow
            };

            result.Result = exchangeRate;
            result.IsError = false;
            result.Message = $"Live exchange rate: 1 {fromToken} = {exchangeRate} {toToken}";

            return result;
        }
        catch (Exception ex)
        {
            result.IsError = true;
            result.Message = $"Error fetching exchange rate: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Adds a custom token mapping to CoinGecko ID
    /// </summary>
    public void AddTokenMapping(string tokenSymbol, string coinGeckoId)
    {
        _coinIds[tokenSymbol] = coinGeckoId;
    }

    /// <summary>
    /// Clears the rate cache
    /// </summary>
    public void ClearCache()
    {
        _rateCache.Clear();
    }
}

