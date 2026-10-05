using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ExternalServices.EquityPrices;

/// <summary>
/// Alpha Vantage equity price data source
/// </summary>
public sealed class AlphaVantagePriceSource(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<AlphaVantagePriceSource> logger) : IEquityPriceDataSource
{
    private readonly string? _apiKey = configuration["EquityPriceSources:AlphaVantage:ApiKey"];
    private readonly string _baseUrl = "https://www.alphavantage.co/query";
    private const decimal ReliabilityScoreValue = 0.75m; // Free tier, rate limited

    public string SourceName => "Alpha Vantage";
    public decimal ReliabilityScore => ReliabilityScoreValue;

    public async Task<SourcePriceResult?> GetPriceAsync(
        string symbol,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("Alpha Vantage API key not configured");
            return null;
        }

        var startTime = DateTime.UtcNow;

        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var url = $"{_baseUrl}?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";

            var response = await httpClient.GetAsync(url, token);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(token);
            var latencyMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            var result = ParseResponse(json, symbol, latencyMs);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching price from Alpha Vantage for symbol {Symbol}", symbol);
            return null;
        }
    }

    public async Task<List<SourcePriceResult>> GetPricesAsync(
        List<string> symbols,
        CancellationToken token = default)
    {
        var results = new List<SourcePriceResult>();

        // Alpha Vantage free tier has rate limits, so fetch sequentially
        foreach (var symbol in symbols)
        {
            var result = await GetPriceAsync(symbol, token);
            if (result != null)
            {
                results.Add(result);
            }

            // Rate limit: 5 calls per minute for free tier
            await Task.Delay(12000, token); // 12 seconds between calls
        }

        return results;
    }

    public async Task<SourcePriceResult?> GetPriceAtDateAsync(
        string symbol,
        DateTime date,
        CancellationToken token = default)
    {
        // Alpha Vantage doesn't have a direct historical endpoint in free tier
        // This would require TIME_SERIES_DAILY which has different rate limits
        // For now, return null - can be enhanced later
        logger.LogWarning("Alpha Vantage historical price lookup not implemented");
        return null;
    }

    private SourcePriceResult? ParseResponse(string json, string symbol, int latencyMs)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("Global Quote", out var quote))
            {
                if (quote.TryGetProperty("05. price", out var priceElement))
                {
                    if (decimal.TryParse(priceElement.GetString(), out var price))
                    {
                        return new SourcePriceResult
                        {
                            Symbol = symbol,
                            Price = price,
                            Timestamp = DateTime.UtcNow,
                            SourceName = SourceName,
                            LatencyMs = latencyMs
                        };
                    }
                }
            }

            logger.LogWarning("Failed to parse Alpha Vantage response for symbol {Symbol}", symbol);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing Alpha Vantage response for symbol {Symbol}", symbol);
            return null;
        }
    }
}

