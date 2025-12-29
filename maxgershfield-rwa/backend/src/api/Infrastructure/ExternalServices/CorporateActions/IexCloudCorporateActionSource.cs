using System.Text.Json;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ExternalServices.CorporateActions;

/// <summary>
/// IEX Cloud corporate action data source
/// </summary>
public sealed class IexCloudCorporateActionSource(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<IexCloudCorporateActionSource> logger) : ICorporateActionDataSource
{
    private readonly string? _apiKey = configuration["CorporateActionSources:IexCloud:ApiKey"];
    private readonly string _baseUrl = "https://cloud.iexapis.com/stable";
    private const decimal ReliabilityScoreValue = 0.95m; // Paid service, highly reliable

    public string SourceName => "IEX Cloud";
    public decimal ReliabilityScore => ReliabilityScoreValue;

    public async Task<List<CorporateAction>> FetchSplitsAsync(
        string symbol,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("IEX Cloud API key not configured");
            return [];
        }

        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "OASIS-RWA-Oracle/1.0");

            var url = $"{_baseUrl}/stock/{symbol}/splits?token={_apiKey}";

            var response = await httpClient.GetAsync(url, token);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(token);
            return ParseSplitsResponse(json, symbol);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching splits from IEX Cloud for symbol {Symbol}", symbol);
            return [];
        }
    }

    public async Task<List<CorporateAction>> FetchDividendsAsync(
        string symbol,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("IEX Cloud API key not configured");
            return [];
        }

        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "OASIS-RWA-Oracle/1.0");

            var url = $"{_baseUrl}/stock/{symbol}/dividends?token={_apiKey}";

            var response = await httpClient.GetAsync(url, token);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(token);
            return ParseDividendsResponse(json, symbol);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching dividends from IEX Cloud for symbol {Symbol}", symbol);
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

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var split in root.EnumerateArray())
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

                        if (split.TryGetProperty("paymentDate", out var paymentDateElement) ||
                            split.TryGetProperty("declaredDate", out paymentDateElement))
                        {
                            if (DateTime.TryParse(paymentDateElement.GetString(), out var effectiveDate))
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
                            var ratioStr = ratioElement.GetString() ?? "";
                            // Parse "2:1" or "2/1" format
                            if (ratioStr.Contains(':'))
                            {
                                var parts = ratioStr.Split(':');
                                if (parts.Length == 2 &&
                                    decimal.TryParse(parts[0], out var numerator) &&
                                    decimal.TryParse(parts[1], out var denominator) &&
                                    denominator > 0)
                                {
                                    action.SplitRatio = numerator / denominator;
                                }
                            }
                            else if (ratioStr.Contains('/'))
                            {
                                var parts = ratioStr.Split('/');
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
            logger.LogError(ex, "Error parsing IEX Cloud splits response for symbol {Symbol}", symbol);
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

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var dividend in root.EnumerateArray())
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

                        if (dividend.TryGetProperty("paymentDate", out var paymentDateElement))
                        {
                            if (DateTime.TryParse(paymentDateElement.GetString(), out var effectiveDate))
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
                            if (decimal.TryParse(amountElement.GetString(), out var amount))
                            {
                                action.DividendAmount = amount;
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
            logger.LogError(ex, "Error parsing IEX Cloud dividends response for symbol {Symbol}", symbol);
        }

        return actions;
    }
}

