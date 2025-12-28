using System.Text.Json;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ExternalServices.CorporateActions;

/// <summary>
/// Polygon.io corporate action data source
/// </summary>
public sealed class PolygonCorporateActionSource(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<PolygonCorporateActionSource> logger) : ICorporateActionDataSource
{
    private readonly string? _apiKey = configuration["CorporateActionSources:Polygon:ApiKey"];
    private readonly string _baseUrl = "https://api.polygon.io/v2/reference";
    private const decimal ReliabilityScoreValue = 0.90m; // Paid service, reliable

    public string SourceName => "Polygon.io";
    public decimal ReliabilityScore => ReliabilityScoreValue;

    public async Task<List<CorporateAction>> FetchSplitsAsync(
        string symbol,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("Polygon.io API key not configured");
            return [];
        }

        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "OASIS-RWA-Oracle/1.0");

            var url = $"{_baseUrl}/splits/{symbol}?apikey={_apiKey}";

            var response = await httpClient.GetAsync(url, token);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(token);
            return ParseSplitsResponse(json, symbol);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching splits from Polygon.io for symbol {Symbol}", symbol);
            return [];
        }
    }

    public async Task<List<CorporateAction>> FetchDividendsAsync(
        string symbol,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("Polygon.io API key not configured");
            return [];
        }

        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "OASIS-RWA-Oracle/1.0");

            var url = $"{_baseUrl}/dividends/{symbol}?apikey={_apiKey}";

            var response = await httpClient.GetAsync(url, token);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(token);
            return ParseDividendsResponse(json, symbol);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching dividends from Polygon.io for symbol {Symbol}", symbol);
            return [];
        }
    }

    public async Task<List<CorporateAction>> FetchAllActionsAsync(
        string symbol,
        DateTime? fromDate = null,
        CancellationToken token = default)
    {
        var allActions = new List<CorporateAction>();

        // Fetch splits and dividends in parallel
        var splitsTask = FetchSplitsAsync(symbol, token);
        var dividendsTask = FetchDividendsAsync(symbol, token);

        await Task.WhenAll(splitsTask, dividendsTask);

        allActions.AddRange(await splitsTask);
        allActions.AddRange(await dividendsTask);

        // Filter by fromDate if provided
        if (fromDate.HasValue)
        {
            allActions = allActions
                .Where(ca => ca.EffectiveDate >= fromDate.Value)
                .ToList();
        }

        return allActions;
    }

    private List<CorporateAction> ParseSplitsResponse(string json, string symbol)
    {
        var actions = new List<CorporateAction>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("status", out var statusElement) &&
                statusElement.GetString() != "OK")
            {
                logger.LogWarning("Polygon.io API returned non-OK status for symbol {Symbol}", symbol);
                return actions;
            }

            if (root.TryGetProperty("results", out var resultsElement) &&
                resultsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var split in resultsElement.EnumerateArray())
                {
                    try
                    {
                        var action = new CorporateAction
                        {
                            Id = Guid.NewGuid(),
                            Symbol = symbol,
                            Type = CorporateActionType.StockSplit,
                            DataSource = SourceName,
                            CreatedAt = DateTimeOffset.UtcNow
                        };

                        if (split.TryGetProperty("exDate", out var exDateElement))
                        {
                            if (DateTime.TryParse(exDateElement.GetString(), out var exDate))
                            {
                                action.ExDate = exDate;
                            }
                        }

                        if (split.TryGetProperty("recordDate", out var recordDateElement))
                        {
                            if (DateTime.TryParse(recordDateElement.GetString(), out var recordDate))
                            {
                                action.RecordDate = recordDate;
                            }
                        }

                        if (split.TryGetProperty("payableDate", out var payableDateElement))
                        {
                            if (DateTime.TryParse(payableDateElement.GetString(), out var effectiveDate))
                            {
                                action.EffectiveDate = effectiveDate;
                            }
                        }

                        // Use exDate as effective date if not specified
                        if (action.EffectiveDate == default && action.ExDate != default)
                        {
                            action.EffectiveDate = action.ExDate;
                        }

                        if (split.TryGetProperty("ratio", out var ratioElement))
                        {
                            // Polygon returns ratio as a number (e.g., 2.0 for 2:1 split)
                            if (ratioElement.ValueKind == JsonValueKind.Number)
                            {
                                action.SplitRatio = ratioElement.GetDecimal();
                            }
                            else if (ratioElement.ValueKind == JsonValueKind.String)
                            {
                                var ratioStr = ratioElement.GetString() ?? "";
                                if (decimal.TryParse(ratioStr, out var ratio))
                                {
                                    action.SplitRatio = ratio;
                                }
                            }
                        }

                        if (action.EffectiveDate != default)
                        {
                            actions.Add(action);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Error parsing split entry for symbol {Symbol}", symbol);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing Polygon.io splits response for symbol {Symbol}", symbol);
        }

        return actions;
    }

    private List<CorporateAction> ParseDividendsResponse(string json, string symbol)
    {
        var actions = new List<CorporateAction>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("status", out var statusElement) &&
                statusElement.GetString() != "OK")
            {
                logger.LogWarning("Polygon.io API returned non-OK status for symbol {Symbol}", symbol);
                return actions;
            }

            if (root.TryGetProperty("results", out var resultsElement) &&
                resultsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var dividend in resultsElement.EnumerateArray())
                {
                    try
                    {
                        var action = new CorporateAction
                        {
                            Id = Guid.NewGuid(),
                            Symbol = symbol,
                            Type = CorporateActionType.Dividend,
                            DataSource = SourceName,
                            CreatedAt = DateTimeOffset.UtcNow
                        };

                        if (dividend.TryGetProperty("exDate", out var exDateElement))
                        {
                            if (DateTime.TryParse(exDateElement.GetString(), out var exDate))
                            {
                                action.ExDate = exDate;
                            }
                        }

                        if (dividend.TryGetProperty("recordDate", out var recordDateElement))
                        {
                            if (DateTime.TryParse(recordDateElement.GetString(), out var recordDate))
                            {
                                action.RecordDate = recordDate;
                            }
                        }

                        if (dividend.TryGetProperty("payableDate", out var payableDateElement))
                        {
                            if (DateTime.TryParse(payableDateElement.GetString(), out var effectiveDate))
                            {
                                action.EffectiveDate = effectiveDate;
                            }
                        }

                        // Use exDate as effective date if not specified
                        if (action.EffectiveDate == default && action.ExDate != default)
                        {
                            action.EffectiveDate = action.ExDate;
                        }

                        if (dividend.TryGetProperty("amount", out var amountElement))
                        {
                            if (amountElement.ValueKind == JsonValueKind.Number)
                            {
                                action.DividendAmount = amountElement.GetDecimal();
                            }
                            else if (amountElement.ValueKind == JsonValueKind.String)
                            {
                                if (decimal.TryParse(amountElement.GetString(), out var amount))
                                {
                                    action.DividendAmount = amount;
                                }
                            }
                        }

                        if (dividend.TryGetProperty("currency", out var currencyElement))
                        {
                            action.DividendCurrency = currencyElement.GetString() ?? "USD";
                        }
                        else
                        {
                            action.DividendCurrency = "USD";
                        }

                        if (action.EffectiveDate != default)
                        {
                            actions.Add(action);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Error parsing dividend entry for symbol {Symbol}", symbol);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing Polygon.io dividends response for symbol {Symbol}", symbol);
        }

        return actions;
    }
}

