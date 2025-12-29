using Application.Contracts;
using Application.DTOs.EquityPrice;

namespace Infrastructure.ImplementationContract;

/// <summary>
/// Service for calculating volatility for equity symbols.
/// Calculates 30-day rolling volatility using historical price data.
/// </summary>
public sealed class VolatilityService(
    IEquityPriceService equityPriceService,
    ILogger<VolatilityService> logger) : IVolatilityService
{
    private const int DefaultDays = 30;
    private const int TradingDaysPerYear = 252;

    /// <summary>
    /// Get volatility for a symbol (returns as decimal, e.g., 0.25 = 25%)
    /// Calculates 30-day rolling volatility by default
    /// </summary>
    public async Task<Result<decimal>> GetVolatilityAsync(
        string symbol,
        int days = DefaultDays,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetVolatilityAsync), startTime);

        try
        {
            // Get historical prices for the specified number of days
            DateTime toDate = DateTime.UtcNow;
            DateTime fromDate = toDate.AddDays(-days);

            var priceHistoryResult = await equityPriceService.GetPriceHistoryAsync(
                symbol,
                fromDate,
                toDate,
                adjusted: true, // Use adjusted prices for accurate volatility
                token);

            if (!priceHistoryResult.IsSuccess || priceHistoryResult.Value is null)
            {
                logger.LogWarning("Failed to get price history for {Symbol} to calculate volatility", symbol);
                return Result<decimal>.Failure(
                    ResultPatternError.NotFound($"Price history not available for {symbol}"));
            }

            var priceHistory = priceHistoryResult.Value;
            var priceDataPoints = priceHistory.Prices;

            if (priceDataPoints.Count < 2)
            {
                logger.LogWarning("Insufficient price data for {Symbol} to calculate volatility", symbol);
                return Result<decimal>.Failure(
                    ResultPatternError.ValidationError($"Insufficient price data for {symbol}"));
            }

            // Calculate daily returns
            var returns = new List<decimal>();
            for (int i = 1; i < priceDataPoints.Count; i++)
            {
                if (priceDataPoints[i - 1].AdjustedPrice > 0)
                {
                    decimal dailyReturn = (priceDataPoints[i].AdjustedPrice - priceDataPoints[i - 1].AdjustedPrice) / priceDataPoints[i - 1].AdjustedPrice;
                    returns.Add(dailyReturn);
                }
            }

            if (returns.Count < 2)
            {
                logger.LogWarning("Insufficient returns data for {Symbol} to calculate volatility", symbol);
                return Result<decimal>.Failure(
                    ResultPatternError.ValidationError($"Insufficient returns data for {symbol}"));
            }

            // Calculate mean return
            decimal meanReturn = returns.Average();

            // Calculate variance
            decimal variance = returns.Sum(r => (r - meanReturn) * (r - meanReturn)) / (returns.Count - 1);

            // Calculate standard deviation (volatility)
            decimal standardDeviation = (decimal)Math.Sqrt((double)variance);

            // Annualize volatility: StdDev * sqrt(trading days per year)
            decimal annualizedVolatility = standardDeviation * (decimal)Math.Sqrt(TradingDaysPerYear);

            logger.OperationCompleted(nameof(GetVolatilityAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<decimal>.Success(annualizedVolatility);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating volatility for {Symbol}", symbol);
            return Result<decimal>.Failure(
                ResultPatternError.InternalServerError($"Error calculating volatility: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get volatility for multiple symbols
    /// </summary>
    public async Task<Result<Dictionary<string, decimal>>> GetBatchVolatilityAsync(
        List<string> symbols,
        int days = DefaultDays,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetBatchVolatilityAsync), startTime);

        var results = new Dictionary<string, decimal>();

        foreach (var symbol in symbols)
        {
            var volatilityResult = await GetVolatilityAsync(symbol, days, token);
            if (volatilityResult.IsSuccess && volatilityResult.Value != null)
            {
                results[symbol] = volatilityResult.Value;
            }
        }

        logger.OperationCompleted(nameof(GetBatchVolatilityAsync), DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow - startTime);

        return Result<Dictionary<string, decimal>>.Success(results);
    }
}

