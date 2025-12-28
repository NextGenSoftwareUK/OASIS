using Application.Contracts;
using Application.DTOs.EquityPrice;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ImplementationContract;

/// <summary>
/// Service for calculating liquidity scores for equity symbols.
/// Liquidity scores are used in funding rate calculations to adjust for low liquidity periods.
/// </summary>
public sealed class LiquidityService(
    IEquityPriceService equityPriceService,
    DataContext dbContext,
    ILogger<LiquidityService> logger) : ILiquidityService
{
    /// <summary>
    /// Get liquidity score for a symbol (0-1 scale, where 1 is highest liquidity)
    /// 
    /// Calculation factors:
    /// - Average daily volume (normalized)
    /// - Price stability (lower volatility = higher liquidity score)
    /// - Recent price update frequency
    /// </summary>
    public async Task<Result<decimal>> GetLiquidityScoreAsync(
        string symbol,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetLiquidityScoreAsync), startTime);

        try
        {
            // Get recent price history (last 30 days) to assess liquidity
            DateTime toDate = DateTime.UtcNow;
            DateTime fromDate = toDate.AddDays(-30);

            var priceHistoryResult = await equityPriceService.GetPriceHistoryAsync(
                symbol,
                fromDate,
                toDate,
                adjusted: true,
                token);

            if (!priceHistoryResult.IsSuccess || priceHistoryResult.Value is null)
            {
                logger.LogWarning("Failed to get price history for {Symbol} to calculate liquidity", symbol);
                // Return a default moderate liquidity score if data unavailable
                return Result<decimal>.Success(0.5m);
            }

            var priceHistory = priceHistoryResult.Value;
            var prices = priceHistory.Prices;

            if (prices.Count == 0)
            {
                logger.LogWarning("No price data for {Symbol} to calculate liquidity", symbol);
                return Result<decimal>.Success(0.5m); // Default moderate liquidity
            }

            // Factor 1: Data availability (more data points = higher liquidity)
            // Normalize: 0-1 scale based on expected 30 trading days
            decimal dataAvailabilityScore = Math.Min(1.0m, prices.Count / 30.0m);

            // Factor 2: Price stability (lower price variance = higher liquidity)
            // Calculate coefficient of variation (std dev / mean)
            if (prices.Count > 1)
            {
                var priceValues = prices.Select(p => p.AdjustedPrice).ToList();
                decimal meanPrice = priceValues.Average();
                
                if (meanPrice > 0)
                {
                    decimal variance = priceValues.Sum(p => (p - meanPrice) * (p - meanPrice)) / priceValues.Count;
                    decimal stdDev = (decimal)Math.Sqrt((double)variance);
                    decimal coefficientOfVariation = stdDev / meanPrice;
                    
                    // Lower CV = higher stability = higher liquidity
                    // Normalize: CV of 0.1 (10%) = score of 1.0, CV of 0.5 (50%) = score of 0.0
                    decimal stabilityScore = Math.Max(0m, 1.0m - (coefficientOfVariation * 2.0m));
                    stabilityScore = Math.Min(1.0m, stabilityScore);

                    // Factor 3: Recent activity (more recent prices = higher liquidity)
                    // Check if we have prices in the last 7 days
                    var recentPrices = prices.Where(p => p.PriceDate >= toDate.AddDays(-7)).ToList();
                    decimal recentActivityScore = Math.Min(1.0m, recentPrices.Count / 7.0m);

                    // Weighted average of factors
                    // Data availability: 30%, Stability: 50%, Recent activity: 20%
                    decimal liquidityScore = (dataAvailabilityScore * 0.3m) +
                                           (stabilityScore * 0.5m) +
                                           (recentActivityScore * 0.2m);

                    logger.OperationCompleted(nameof(GetLiquidityScoreAsync), DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow - startTime);

                    return Result<decimal>.Success(liquidityScore);
                }
            }

            // Fallback: use data availability score only
            logger.OperationCompleted(nameof(GetLiquidityScoreAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<decimal>.Success(dataAvailabilityScore);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating liquidity score for {Symbol}", symbol);
            // Return default moderate liquidity on error
            return Result<decimal>.Success(0.5m);
        }
    }

    /// <summary>
    /// Get liquidity scores for multiple symbols
    /// </summary>
    public async Task<Result<Dictionary<string, decimal>>> GetBatchLiquidityScoresAsync(
        List<string> symbols,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetBatchLiquidityScoresAsync), startTime);

        var results = new Dictionary<string, decimal>();

        foreach (var symbol in symbols)
        {
            var liquidityResult = await GetLiquidityScoreAsync(symbol, token);
            if (liquidityResult.IsSuccess && liquidityResult.Value != null)
            {
                results[symbol] = liquidityResult.Value;
            }
        }

        logger.OperationCompleted(nameof(GetBatchLiquidityScoresAsync), DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow - startTime);

        return Result<Dictionary<string, decimal>>.Success(results);
    }
}

