using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ExternalServices.EquityPrices;

/// <summary>
/// Polygon.io equity price data source
/// </summary>
public sealed class PolygonPriceSource(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<PolygonPriceSource> logger) : IEquityPriceDataSource
{
    private readonly string? _apiKey = configuration["EquityPriceSources:Polygon:ApiKey"];
    private readonly string _baseUrl = "https://api.polygon.io";
    private const decimal ReliabilityScoreValue = 0.90m; // Paid tier, reliable

    public string SourceName => "Polygon.io";
    public decimal ReliabilityScore => ReliabilityScoreValue;

    public async Task<SourcePriceResult?> GetPriceAsync(
        string symbol,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("Polygon.io API key not configured");
            return null;
        }

        var startTime = DateTime.UtcNow;

        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            // Use previous close endpoint for free tier
            var url = $"{_baseUrl}/v2/aggs/ticker/{symbol}/prev?adjusted=true&apikey={_apiKey}";

            var response = await httpClient.GetAsync(url, token);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(token);
            var latencyMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            var result = ParseResponse(json, symbol, latencyMs);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching price from Polygon.io for symbol {Symbol}", symbol);
            return null;
        }
    }

    public async Task<List<SourcePriceResult>> GetPricesAsync(
        List<string> symbols,
        CancellationToken token = default)
    {
        var results = new List<SourcePriceResult>();

        // Polygon.io doesn't have a batch endpoint in free tier, so fetch sequentially
        foreach (var symbol in symbols)
        {
            var result = await GetPriceAsync(symbol, token);
            if (result != null)
            {
                results.Add(result);
            }

            // Rate limit: 5 calls per minute for free tier
            await Task.Delay(12000, token);
        }

        return results;
    }

    public async Task<SourcePriceResult?> GetPriceAtDateAsync(
        string symbol,
        DateTime date,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return null;
        }

        var startTime = DateTime.UtcNow;

        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var dateStr = date.ToString("yyyy-MM-dd");
            var url = $"{_baseUrl}/v2/aggs/ticker/{symbol}/range/1/day/{dateStr}/{dateStr}?adjusted=true&apikey={_apiKey}";

            var response = await httpClient.GetAsync(url, token);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(token);
            var latencyMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            var result = ParseHistoricalResponse(json, symbol, date, latencyMs);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching historical price from Polygon.io for symbol {Symbol}", symbol);
            return null;
        }
    }

    private SourcePriceResult? ParseResponse(string json, string symbol, int latencyMs)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
            {
                var resultsArray = results.EnumerateArray();
                if (resultsArray.MoveNext())
                {
                    var result = resultsArray.Current;
                    if (result.TryGetProperty("c", out var closePrice)) // 'c' is close price
                    {
                        if (decimal.TryParse(closePrice.GetString(), out var price))
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
            }

            logger.LogWarning("Failed to parse Polygon.io response for symbol {Symbol}", symbol);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing Polygon.io response for symbol {Symbol}", symbol);
            return null;
        }
    }

    private SourcePriceResult? ParseHistoricalResponse(string json, string symbol, DateTime date, int latencyMs)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
            {
                var resultsArray = results.EnumerateArray();
                if (resultsArray.MoveNext())
                {
                    var result = resultsArray.Current;
                    if (result.TryGetProperty("c", out var closePrice))
                    {
                        if (decimal.TryParse(closePrice.GetString(), out var price))
                        {
                            return new SourcePriceResult
                            {
                                Symbol = symbol,
                                Price = price,
                                Timestamp = date,
                                SourceName = SourceName,
                                LatencyMs = latencyMs
                            };
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing Polygon.io historical response for symbol {Symbol}", symbol);
            return null;
        }
    }
}

