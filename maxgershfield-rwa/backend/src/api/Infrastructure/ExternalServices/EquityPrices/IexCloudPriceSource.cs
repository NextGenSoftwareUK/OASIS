using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ExternalServices.EquityPrices;

/// <summary>
/// IEX Cloud equity price data source
/// </summary>
public sealed class IexCloudPriceSource(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<IexCloudPriceSource> logger) : IEquityPriceDataSource
{
    private readonly string? _apiKey = configuration["EquityPriceSources:IexCloud:ApiKey"];
    private readonly string _baseUrl = "https://cloud.iexapis.com/stable";
    private const decimal ReliabilityScoreValue = 0.95m; // Paid tier, reliable

    public string SourceName => "IEX Cloud";
    public decimal ReliabilityScore => ReliabilityScoreValue;

    public async Task<SourcePriceResult?> GetPriceAsync(
        string symbol,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("IEX Cloud API key not configured");
            return null;
        }

        var startTime = DateTime.UtcNow;

        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var url = $"{_baseUrl}/stock/{symbol}/quote?token={_apiKey}";

            var response = await httpClient.GetAsync(url, token);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(token);
            var latencyMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            var result = ParseResponse(json, symbol, latencyMs);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching price from IEX Cloud for symbol {Symbol}", symbol);
            return null;
        }
    }

    public async Task<List<SourcePriceResult>> GetPricesAsync(
        List<string> symbols,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return new List<SourcePriceResult>();
        }

        var startTime = DateTime.UtcNow;

        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            // IEX Cloud supports batch quotes
            var symbolsParam = string.Join(",", symbols);
            var url = $"{_baseUrl}/stock/market/batch?symbols={symbolsParam}&types=quote&token={_apiKey}";

            var response = await httpClient.GetAsync(url, token);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(token);
            var latencyMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            var results = ParseBatchResponse(json, latencyMs);
            return results;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching batch prices from IEX Cloud");
            return new List<SourcePriceResult>();
        }
    }

    public async Task<SourcePriceResult?> GetPriceAtDateAsync(
        string symbol,
        DateTime date,
        CancellationToken token = default)
    {
        // IEX Cloud historical data requires different endpoint
        // For now, return null - can be enhanced later
        logger.LogWarning("IEX Cloud historical price lookup not implemented");
        return null;
    }

    private SourcePriceResult? ParseResponse(string json, string symbol, int latencyMs)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("latestPrice", out var priceElement))
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

            logger.LogWarning("Failed to parse IEX Cloud response for symbol {Symbol}", symbol);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing IEX Cloud response for symbol {Symbol}", symbol);
            return null;
        }
    }

    private List<SourcePriceResult> ParseBatchResponse(string json, int latencyMs)
    {
        var results = new List<SourcePriceResult>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            foreach (var property in root.EnumerateObject())
            {
                var symbol = property.Name;
                if (property.Value.TryGetProperty("quote", out var quote))
                {
                    if (quote.TryGetProperty("latestPrice", out var priceElement))
                    {
                        if (decimal.TryParse(priceElement.GetString(), out var price))
                        {
                            results.Add(new SourcePriceResult
                            {
                                Symbol = symbol,
                                Price = price,
                                Timestamp = DateTime.UtcNow,
                                SourceName = SourceName,
                                LatencyMs = latencyMs
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing IEX Cloud batch response");
        }

        return results;
    }
}

