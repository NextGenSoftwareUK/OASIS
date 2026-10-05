using Application.Contracts;
using Application.DTOs.EquityPrice;
using Infrastructure.ExternalServices.EquityPrices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Infrastructure.ImplementationContract;

/// <summary>
/// Service for aggregating equity prices from multiple sources with corporate action adjustments
/// </summary>
public sealed class EquityPriceService(
    DataContext dbContext,
    ICorporateActionService corporateActionService,
    IPriceAdjustmentService priceAdjustmentService,
    IEnumerable<IEquityPriceDataSource> dataSources,
    IMemoryCache memoryCache,
    ILogger<EquityPriceService> logger) : IEquityPriceService
{
    private const int CacheExpirationMinutes = 1; // Cache raw prices for 1 minute
    private const int HistoricalCacheExpirationHours = 24; // Cache historical prices for 24 hours

    public async Task<Result<EquityPriceResponse>> GetAdjustedPriceAsync(
        string symbol,
        CancellationToken token = default)
    {
        return await GetPriceAsync(symbol, adjusted: true, token);
    }

    public async Task<Result<EquityPriceResponse>> GetRawPriceAsync(
        string symbol,
        CancellationToken token = default)
    {
        return await GetPriceAsync(symbol, adjusted: false, token);
    }

    public async Task<Result<List<EquityPriceResponse>>> GetBatchPricesAsync(
        List<string> symbols,
        bool adjusted = true,
        CancellationToken token = default)
    {
        var results = new ConcurrentBag<EquityPriceResponse>();
        var tasks = symbols.Select(async symbol =>
        {
            var result = await GetPriceAsync(symbol, adjusted, token);
            if (result.IsSuccess && result.Value != null)
            {
                results.Add(result.Value);
            }
        });

        await Task.WhenAll(tasks);

        return Result<List<EquityPriceResponse>>.Success(results.ToList());
    }

    public async Task<Result<EquityPriceHistoryResponse>> GetPriceHistoryAsync(
        string symbol,
        DateTime fromDate,
        DateTime toDate,
        bool adjusted = true,
        CancellationToken token = default)
    {
        var cacheKey = $"price_history_{symbol}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}_{adjusted}";

        if (memoryCache.TryGetValue(cacheKey, out EquityPriceHistoryResponse? cached))
        {
            return Result<EquityPriceHistoryResponse>.Success(cached!);
        }

        // Get prices from database
        var prices = await dbContext.Set<EquityPrice>()
            .Where(ep => ep.Symbol == symbol
                         && ep.PriceDate >= fromDate
                         && ep.PriceDate <= toDate
                         && !ep.IsDeleted)
            .OrderBy(ep => ep.PriceDate)
            .ToListAsync(token);

        var dataPoints = prices.Select(p => new EquityPriceDataPointDto
        {
            PriceDate = p.PriceDate,
            RawPrice = p.RawPrice,
            AdjustedPrice = adjusted ? p.AdjustedPrice : p.RawPrice,
            Confidence = p.Confidence
        }).ToList();

        var response = new EquityPriceHistoryResponse
        {
            Symbol = symbol,
            Prices = dataPoints,
            FromDate = fromDate,
            ToDate = toDate,
            Adjusted = adjusted
        };

        memoryCache.Set(cacheKey, response, TimeSpan.FromHours(HistoricalCacheExpirationHours));

        return Result<EquityPriceHistoryResponse>.Success(response);
    }

    public async Task<Result<EquityPriceResponse>> GetPriceAtDateAsync(
        string symbol,
        DateTime date,
        bool adjusted = true,
        CancellationToken token = default)
    {
        // Try to get from database first
        var cachedPrice = await dbContext.Set<EquityPrice>()
            .Where(ep => ep.Symbol == symbol
                         && ep.PriceDate.Date == date.Date
                         && !ep.IsDeleted)
            .OrderByDescending(ep => ep.PriceDate)
            .FirstOrDefaultAsync(token);

        if (cachedPrice != null)
        {
            var sources = ParseSourceBreakdown(cachedPrice.SourceBreakdownJson);
            var corporateActions = await GetCorporateActionsAppliedAsync(symbol, date, token);

            return Result<EquityPriceResponse>.Success(new EquityPriceResponse
            {
                Symbol = symbol,
                RawPrice = cachedPrice.RawPrice,
                AdjustedPrice = adjusted ? cachedPrice.AdjustedPrice : cachedPrice.RawPrice,
                Confidence = cachedPrice.Confidence,
                PriceDate = cachedPrice.PriceDate,
                Sources = sources,
                CorporateActionsApplied = corporateActions,
                LastUpdated = cachedPrice.UpdatedAt?.DateTime ?? cachedPrice.CreatedAt.DateTime
            });
        }

        // If not in database, try to fetch from data sources
        // For historical dates, use Polygon.io if available
        var polygonSource = dataSources.FirstOrDefault(ds => ds.SourceName == "Polygon.io");
        if (polygonSource != null)
        {
            var historicalPrice = await polygonSource.GetPriceAtDateAsync(symbol, date, token);
            if (historicalPrice != null)
            {
                var rawPrice = historicalPrice.Price;
                var adjustedPrice = adjusted
                    ? await priceAdjustmentService.GetAdjustedPriceAsync(symbol, rawPrice, date, token)
                    : rawPrice;

                var corporateActions = await GetCorporateActionsAppliedAsync(symbol, date, token);

                return Result<EquityPriceResponse>.Success(new EquityPriceResponse
                {
                    Symbol = symbol,
                    RawPrice = rawPrice,
                    AdjustedPrice = adjustedPrice,
                    Confidence = polygonSource.ReliabilityScore,
                    PriceDate = date,
                    Sources = new List<SourcePriceDto>
                    {
                        new()
                        {
                            SourceName = historicalPrice.SourceName,
                            Price = historicalPrice.Price,
                            Timestamp = historicalPrice.Timestamp,
                            ReliabilityScore = polygonSource.ReliabilityScore,
                            LatencyMs = historicalPrice.LatencyMs
                        }
                    },
                    CorporateActionsApplied = corporateActions,
                    LastUpdated = DateTime.UtcNow
                });
            }
        }

        return Result<EquityPriceResponse>.Failure(
            ResultPatternError.NotFound($"Price not found for symbol {symbol} on date {date:yyyy-MM-dd}"));
    }

    private async Task<Result<EquityPriceResponse>> GetPriceAsync(
        string symbol,
        bool adjusted,
        CancellationToken token = default)
    {
        var cacheKey = $"price_{symbol}_{adjusted}";

        if (memoryCache.TryGetValue(cacheKey, out EquityPriceResponse? cached))
        {
            return Result<EquityPriceResponse>.Success(cached!);
        }

        // Fetch prices from all data sources in parallel
        var sourceTasks = dataSources.Select(async ds =>
        {
            try
            {
                return await ds.GetPriceAsync(symbol, token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching price from {Source} for symbol {Symbol}", ds.SourceName, symbol);
                return null;
            }
        });

        var sourceResults = await Task.WhenAll(sourceTasks);
        var validPrices = sourceResults.Where(r => r != null).ToList();

        if (validPrices.Count == 0)
        {
            // Try to get last known price from database
            var lastPrice = await dbContext.Set<EquityPrice>()
                .Where(ep => ep.Symbol == symbol && !ep.IsDeleted)
                .OrderByDescending(ep => ep.PriceDate)
                .FirstOrDefaultAsync(token);

            if (lastPrice != null)
            {
                var sources = ParseSourceBreakdown(lastPrice.SourceBreakdownJson);
                var corporateActions = await GetCorporateActionsAppliedAsync(symbol, DateTime.UtcNow, token);

                return Result<EquityPriceResponse>.Success(new EquityPriceResponse
                {
                    Symbol = symbol,
                    RawPrice = lastPrice.RawPrice,
                    AdjustedPrice = adjusted ? lastPrice.AdjustedPrice : lastPrice.RawPrice,
                    Confidence = lastPrice.Confidence * 0.5m, // Lower confidence for stale data
                    PriceDate = lastPrice.PriceDate,
                    Sources = sources,
                    CorporateActionsApplied = corporateActions,
                    LastUpdated = lastPrice.UpdatedAt?.DateTime ?? lastPrice.CreatedAt.DateTime
                });
            }

            return Result<EquityPriceResponse>.Failure(
                ResultPatternError.NotFound($"Price not found for symbol {symbol}"));
        }

        // Calculate consensus price
        var consensusResult = CalculateConsensus(validPrices!);
        var consensusPrice = consensusResult.ConsensusPrice;
        var confidence = consensusResult.Confidence;
        var sourceDtos = consensusResult.SourceDtos;

        // Apply corporate action adjustments if needed
        decimal adjustedPrice = consensusPrice;
        List<CorporateActionInfoDto> corporateActions = new();

        if (adjusted)
        {
            adjustedPrice = await priceAdjustmentService.GetAdjustedPriceAsync(
                symbol, consensusPrice, DateTime.UtcNow, token);

            corporateActions = await GetCorporateActionsAppliedAsync(symbol, DateTime.UtcNow, token);
        }

        // Save to database for caching
        var equityPrice = new EquityPrice
        {
            Symbol = symbol,
            RawPrice = consensusPrice,
            AdjustedPrice = adjustedPrice,
            Confidence = confidence,
            PriceDate = DateTime.UtcNow,
            Source = "Multi-Source Consensus",
            SourceBreakdownJson = JsonSerializer.Serialize(sourceDtos)
        };

        dbContext.Set<EquityPrice>().Add(equityPrice);
        await dbContext.SaveChangesAsync(token);

        var response = new EquityPriceResponse
        {
            Symbol = symbol,
            RawPrice = consensusPrice,
            AdjustedPrice = adjustedPrice,
            Confidence = confidence,
            PriceDate = DateTime.UtcNow,
            Sources = sourceDtos,
            CorporateActionsApplied = corporateActions,
            LastUpdated = DateTime.UtcNow
        };

        memoryCache.Set(cacheKey, response, TimeSpan.FromMinutes(CacheExpirationMinutes));

        return Result<EquityPriceResponse>.Success(response);
    }

    private ConsensusResult CalculateConsensus(List<SourcePriceResult> prices)
    {
        if (prices.Count == 0)
        {
            throw new InvalidOperationException("Cannot calculate consensus from empty price list");
        }

        if (prices.Count == 1)
        {
            var price = prices[0];
            var source = GetDataSource(price.SourceName);
            return new ConsensusResult
            {
                ConsensusPrice = price.Price,
                Confidence = source?.ReliabilityScore ?? 0.5m,
                SourceDtos = new List<SourcePriceDto>
                {
                    new()
                    {
                        SourceName = price.SourceName,
                        Price = price.Price,
                        Timestamp = price.Timestamp,
                        ReliabilityScore = source?.ReliabilityScore ?? 0.5m,
                        LatencyMs = price.LatencyMs
                    }
                }
            };
        }

        // Remove outliers (prices >3 standard deviations from median)
        var priceValues = prices.Select(p => p.Price).ToList();
        var median = CalculateMedian(priceValues);
        var stdDev = CalculateStandardDeviation(priceValues, median);

        var filteredPrices = prices.Where(p =>
        {
            var deviation = Math.Abs(p.Price - median);
            return deviation <= 3 * stdDev;
        }).ToList();

        if (filteredPrices.Count == 0)
        {
            filteredPrices = prices; // If all are outliers, use all
        }

        // Calculate weighted average based on reliability scores
        decimal totalWeight = 0;
        decimal weightedSum = 0;

        var sourceDtos = new List<SourcePriceDto>();

        foreach (var price in filteredPrices)
        {
            var source = GetDataSource(price.SourceName);
            var reliability = source?.ReliabilityScore ?? 0.5m;

            weightedSum += price.Price * reliability;
            totalWeight += reliability;

            sourceDtos.Add(new SourcePriceDto
            {
                SourceName = price.SourceName,
                Price = price.Price,
                Timestamp = price.Timestamp,
                ReliabilityScore = reliability,
                LatencyMs = price.LatencyMs
            });
        }

        var consensusPrice = totalWeight > 0 ? weightedSum / totalWeight : median;

        // Calculate confidence score
        var agreementPercentage = CalculateAgreementPercentage(filteredPrices, consensusPrice);
        var averageReliability = filteredPrices
            .Select(p => GetDataSource(p.SourceName)?.ReliabilityScore ?? 0.5m)
            .Average();
        var sourceCountFactor = Math.Min(1.0m, filteredPrices.Count / 3.0m);

        var confidence = agreementPercentage * averageReliability * sourceCountFactor;

        return new ConsensusResult
        {
            ConsensusPrice = consensusPrice,
            Confidence = Math.Min(1.0m, Math.Max(0m, confidence)),
            SourceDtos = sourceDtos
        };
    }

    private decimal CalculateMedian(List<decimal> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var count = sorted.Count;

        if (count % 2 == 0)
        {
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
        }

        return sorted[count / 2];
    }

    private decimal CalculateStandardDeviation(List<decimal> values, decimal mean)
    {
        var variance = values.Average(v => (decimal)Math.Pow((double)(v - mean), 2));
        return (decimal)Math.Sqrt((double)variance);
    }

    private decimal CalculateAgreementPercentage(List<SourcePriceResult> prices, decimal consensusPrice)
    {
        if (prices.Count == 0) return 0;

        var withinOnePercent = prices.Count(p =>
        {
            var deviation = Math.Abs(p.Price - consensusPrice);
            var percentage = consensusPrice > 0 ? deviation / consensusPrice : 0;
            return percentage <= 0.01m; // Within 1%
        });

        return (decimal)withinOnePercent / prices.Count;
    }

    private IEquityPriceDataSource? GetDataSource(string sourceName)
    {
        return dataSources.FirstOrDefault(ds => ds.SourceName == sourceName);
    }

    private async Task<List<CorporateActionInfoDto>> GetCorporateActionsAppliedAsync(
        string symbol,
        DateTime date,
        CancellationToken token)
    {
        var actions = await corporateActionService.GetCorporateActionsAsync(symbol, null, token);
        var appliedActions = actions
            .Where(ca => ca.EffectiveDate <= date)
            .OrderBy(ca => ca.EffectiveDate)
            .ToList();

        var result = new List<CorporateActionInfoDto>();

        foreach (var action in appliedActions)
        {
            var factor = action.Type switch
            {
                CorporateActionType.StockSplit => action.SplitRatio.HasValue
                    ? 1.0m / action.SplitRatio.Value
                    : 1.0m,
                CorporateActionType.ReverseSplit => action.SplitRatio ?? 1.0m,
                CorporateActionType.Merger => action.ExchangeRatio ?? 1.0m,
                _ => 1.0m
            };

            result.Add(new CorporateActionInfoDto
            {
                Type = action.Type,
                EffectiveDate = action.EffectiveDate,
                AdjustmentFactor = factor
            });
        }

        return result;
    }

    private List<SourcePriceDto> ParseSourceBreakdown(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<SourcePriceDto>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<SourcePriceDto>>(json) ?? new List<SourcePriceDto>();
        }
        catch
        {
            return new List<SourcePriceDto>();
        }
    }

    private sealed class ConsensusResult
    {
        public decimal ConsensusPrice { get; init; }
        public decimal Confidence { get; init; }
        public List<SourcePriceDto> SourceDtos { get; init; } = new();
    }
}

