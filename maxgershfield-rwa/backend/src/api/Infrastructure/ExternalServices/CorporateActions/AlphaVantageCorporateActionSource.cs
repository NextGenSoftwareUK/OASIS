using System.Text.Json;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ExternalServices.CorporateActions;

/// <summary>
/// Alpha Vantage corporate action data source
/// </summary>
public sealed class AlphaVantageCorporateActionSource(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<AlphaVantageCorporateActionSource> logger) : ICorporateActionDataSource
{
    private readonly string? _apiKey = configuration["CorporateActionSources:AlphaVantage:ApiKey"];
    private readonly string _baseUrl = "https://www.alphavantage.co/query";
    private const decimal ReliabilityScoreValue = 0.75m; // Free tier, rate limited

    public string SourceName => "Alpha Vantage";
    public decimal ReliabilityScore => ReliabilityScoreValue;

    public async Task<List<CorporateAction>> FetchSplitsAsync(
        string symbol,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("Alpha Vantage API key not configured");
            return [];
        }

        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var url = $"{_baseUrl}?function=SPLIT&symbol={symbol}&apikey={_apiKey}";

            var response = await httpClient.GetAsync(url, token);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(token);
            return ParseSplitsResponse(json, symbol);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching splits from Alpha Vantage for symbol {Symbol}", symbol);
            return [];
        }
    }

    public async Task<List<CorporateAction>> FetchDividendsAsync(
        string symbol,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("Alpha Vantage API key not configured");
            return [];
        }

        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var url = $"{_baseUrl}?function=DIVIDEND&symbol={symbol}&apikey={_apiKey}";

            var response = await httpClient.GetAsync(url, token);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(token);
            return ParseDividendsResponse(json, symbol);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching dividends from Alpha Vantage for symbol {Symbol}", symbol);
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

            // Check for error message
            if (root.TryGetProperty("Error Message", out var errorMsg) ||
                root.TryGetProperty("Note", out _))
            {
                logger.LogWarning("Alpha Vantage API error or rate limit for symbol {Symbol}", symbol);
                return actions;
            }

            if (root.TryGetProperty("splits", out var splitsElement))
            {
                foreach (var split in splitsElement.EnumerateArray())
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

                        if (split.TryGetProperty("date", out var dateElement))
                        {
                            if (DateTime.TryParse(dateElement.GetString(), out var date))
                            {
                                action.ExDate = date;
                                action.RecordDate = date;
                                action.EffectiveDate = date;
                            }
                        }

                        if (split.TryGetProperty("split", out var splitElement))
                        {
                            var splitStr = splitElement.GetString() ?? "";
                            // Parse "2:1" format
                            if (splitStr.Contains(':'))
                            {
                                var parts = splitStr.Split(':');
                                if (parts.Length == 2 &&
                                    decimal.TryParse(parts[0], out var numerator) &&
                                    decimal.TryParse(parts[1], out var denominator) &&
                                    denominator > 0)
                                {
                                    action.SplitRatio = numerator / denominator;
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
            logger.LogError(ex, "Error parsing Alpha Vantage splits response for symbol {Symbol}", symbol);
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

            // Check for error message
            if (root.TryGetProperty("Error Message", out var errorMsg) ||
                root.TryGetProperty("Note", out _))
            {
                logger.LogWarning("Alpha Vantage API error or rate limit for symbol {Symbol}", symbol);
                return actions;
            }

            if (root.TryGetProperty("dividends", out var dividendsElement))
            {
                foreach (var dividend in dividendsElement.EnumerateArray())
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

                        if (dividend.TryGetProperty("date", out var dateElement))
                        {
                            if (DateTime.TryParse(dateElement.GetString(), out var date))
                            {
                                action.ExDate = date;
                                action.RecordDate = date;
                                action.EffectiveDate = date;
                            }
                        }

                        if (dividend.TryGetProperty("dividend", out var dividendElement))
                        {
                            if (decimal.TryParse(dividendElement.GetString(), out var amount))
                            {
                                action.DividendAmount = amount;
                                action.DividendCurrency = "USD"; // Default, Alpha Vantage doesn't specify
                            }
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
            logger.LogError(ex, "Error parsing Alpha Vantage dividends response for symbol {Symbol}", symbol);
        }

        return actions;
    }
}

