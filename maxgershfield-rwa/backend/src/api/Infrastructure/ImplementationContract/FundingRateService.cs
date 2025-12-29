using Application.Contracts;
using Application.DTOs.FundingRate;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ImplementationContract;

/// <summary>
/// Service for calculating funding rates for perpetual futures on RWAs/equities.
/// Funding rates account for premium to spot, corporate action windows, volatility, and liquidity.
/// </summary>
public sealed class FundingRateService(
    DataContext dbContext,
    IEquityPriceService equityPriceService,
    ICorporateActionService corporateActionService,
    ILiquidityService liquidityService,
    IVolatilityService volatilityService,
    ILogger<FundingRateService> logger) : IFundingRateService
{
    private const decimal FundingMultiplier = 0.1m; // Configurable multiplier for base rate
    private const decimal CorporateActionAdjustment7Days = 0.5m; // 0.5% boost if CA within 7 days
    private const decimal CorporateActionAdjustment3Days = 1.0m; // 1.0% boost if CA within 3 days
    private const decimal LiquidityAdjustmentMultiplier = 0.3m; // Up to 0.3% for low liquidity
    private const decimal VolatilityThreshold = 0.2m; // 20% volatility threshold
    private const decimal VolatilityAdjustmentMultiplier = 0.2m; // 0.2% per 1% vol above threshold
    private const decimal MaxAnnualizedRate = 100m; // Maximum 100% annualized
    private const decimal MinAnnualizedRate = -100m; // Minimum -100% annualized
    private const int HoursPerYear = 365 * 24;

    /// <summary>
    /// Calculate funding rate for a symbol given a mark price
    /// </summary>
    public async Task<Result<FundingRateResponse>> CalculateFundingRateAsync(
        string symbol,
        decimal markPrice,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(CalculateFundingRateAsync), startTime);

        try
        {
            // 1. Get adjusted spot price
            var spotPriceResult = await equityPriceService.GetAdjustedPriceAsync(symbol, token);
            if (!spotPriceResult.IsSuccess || spotPriceResult.Value is null)
            {
                logger.LogWarning("Failed to get spot price for {Symbol}", symbol);
                return Result<FundingRateResponse>.Failure(
                    ResultPatternError.NotFound($"Spot price not available for {symbol}"));
            }

            var spotPriceResponse = spotPriceResult.Value;
            decimal spotPrice = spotPriceResponse.RawPrice;
            decimal adjustedSpotPrice = spotPriceResponse.AdjustedPrice;

            // 2. Calculate premium/discount
            decimal premium = markPrice - adjustedSpotPrice;
            decimal premiumPercentage = adjustedSpotPrice != 0
                ? (premium / adjustedSpotPrice) * 100m
                : 0m;

            // 3. Calculate base funding rate
            decimal baseRate = premiumPercentage * FundingMultiplier;

            // 4. Check for upcoming corporate actions
            decimal corporateActionAdjustment = await GetCorporateActionAdjustmentAsync(symbol, token);

            // 5. Get liquidity adjustment
            var liquidityResult = await liquidityService.GetLiquidityScoreAsync(symbol, token);
            decimal liquidityScore = liquidityResult.IsSuccess && liquidityResult.Value != null
                ? liquidityResult.Value
                : 0.5m; // Default moderate liquidity
            decimal liquidityAdjustment = (1.0m - liquidityScore) * LiquidityAdjustmentMultiplier;

            // 6. Get volatility adjustment
            var volatilityResult = await volatilityService.GetVolatilityAsync(symbol, days: 30, token);
            decimal volatility = volatilityResult.IsSuccess && volatilityResult.Value != null
                ? volatilityResult.Value
                : 0.25m; // Default 25% volatility
            decimal volatilityAdjustment = 0m;
            if (volatility > VolatilityThreshold)
            {
                volatilityAdjustment = (volatility - VolatilityThreshold) * VolatilityAdjustmentMultiplier;
            }

            // 7. Calculate final funding rate
            decimal finalRate = baseRate + corporateActionAdjustment + liquidityAdjustment + volatilityAdjustment;

            // 8. Apply rate caps
            finalRate = Math.Max(MinAnnualizedRate, Math.Min(MaxAnnualizedRate, finalRate));

            // 9. Calculate hourly rate
            decimal hourlyRate = finalRate / HoursPerYear;

            // 10. Create factors breakdown
            var factors = new FundingRateFactorsDto
            {
                BaseRate = baseRate,
                CorporateActionAdjustment = corporateActionAdjustment,
                LiquidityAdjustment = liquidityAdjustment,
                VolatilityAdjustment = volatilityAdjustment,
                FinalRate = finalRate
            };

            DateTime calculatedAt = DateTime.UtcNow;
            DateTime validUntil = calculatedAt.AddHours(1); // Valid for 1 hour

            // 11. Store in database
            var fundingRateEntity = new FundingRate
            {
                Symbol = symbol,
                Rate = finalRate,
                HourlyRate = hourlyRate,
                MarkPrice = markPrice,
                SpotPrice = spotPrice,
                AdjustedSpotPrice = adjustedSpotPrice,
                Premium = premium,
                PremiumPercentage = premiumPercentage,
                BaseRate = baseRate,
                CorporateActionAdjustment = corporateActionAdjustment,
                LiquidityAdjustment = liquidityAdjustment,
                VolatilityAdjustment = volatilityAdjustment,
                CalculatedAt = calculatedAt,
                ValidUntil = validUntil
            };

            dbContext.FundingRates.Add(fundingRateEntity);
            await dbContext.SaveChangesAsync(token);

            // 12. Create response
            var response = new FundingRateResponse
            {
                Symbol = symbol,
                Rate = finalRate,
                HourlyRate = hourlyRate,
                MarkPrice = markPrice,
                SpotPrice = spotPrice,
                AdjustedSpotPrice = adjustedSpotPrice,
                Premium = premium,
                PremiumPercentage = premiumPercentage,
                Factors = factors,
                CalculatedAt = calculatedAt,
                ValidUntil = validUntil,
                OnChainTransactionHash = null // Will be set when published on-chain
            };

            logger.OperationCompleted(nameof(CalculateFundingRateAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<FundingRateResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating funding rate for {Symbol}", symbol);
            return Result<FundingRateResponse>.Failure(
                ResultPatternError.InternalServerError($"Error calculating funding rate: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get the current funding rate for a symbol (from database)
    /// </summary>
    public async Task<Result<FundingRateResponse>> GetCurrentFundingRateAsync(
        string symbol,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetCurrentFundingRateAsync), startTime);

        try
        {
            var fundingRate = await dbContext.FundingRates
                .AsNoTracking()
                .Where(fr => fr.Symbol == symbol && fr.ValidUntil > DateTime.UtcNow)
                .OrderByDescending(fr => fr.CalculatedAt)
                .FirstOrDefaultAsync(token);

            if (fundingRate is null)
            {
                logger.LogWarning("No current funding rate found for {Symbol}", symbol);
                return Result<FundingRateResponse>.Failure(
                    ResultPatternError.NotFound($"No current funding rate found for {symbol}"));
            }

            var factors = new FundingRateFactorsDto
            {
                BaseRate = fundingRate.BaseRate,
                CorporateActionAdjustment = fundingRate.CorporateActionAdjustment,
                LiquidityAdjustment = fundingRate.LiquidityAdjustment,
                VolatilityAdjustment = fundingRate.VolatilityAdjustment,
                FinalRate = fundingRate.Rate
            };

            var response = new FundingRateResponse
            {
                Symbol = fundingRate.Symbol,
                Rate = fundingRate.Rate,
                HourlyRate = fundingRate.HourlyRate,
                MarkPrice = fundingRate.MarkPrice,
                SpotPrice = fundingRate.SpotPrice,
                AdjustedSpotPrice = fundingRate.AdjustedSpotPrice,
                Premium = fundingRate.Premium,
                PremiumPercentage = fundingRate.PremiumPercentage,
                Factors = factors,
                CalculatedAt = fundingRate.CalculatedAt,
                ValidUntil = fundingRate.ValidUntil,
                OnChainTransactionHash = fundingRate.OnChainTransactionHash
            };

            logger.OperationCompleted(nameof(GetCurrentFundingRateAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<FundingRateResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting current funding rate for {Symbol}", symbol);
            return Result<FundingRateResponse>.Failure(
                ResultPatternError.InternalServerError($"Error getting funding rate: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get funding rate history for a symbol
    /// </summary>
    public async Task<Result<List<FundingRateHistoryItem>>> GetFundingRateHistoryAsync(
        string symbol,
        int hours = 24,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetFundingRateHistoryAsync), startTime);

        try
        {
            DateTime fromDate = DateTime.UtcNow.AddHours(-hours);

            var fundingRates = await dbContext.FundingRates
                .AsNoTracking()
                .Where(fr => fr.Symbol == symbol && fr.CalculatedAt >= fromDate)
                .OrderByDescending(fr => fr.CalculatedAt)
                .ToListAsync(token);

            var history = fundingRates.Select(fr => new FundingRateHistoryItem
            {
                Rate = fr.Rate,
                HourlyRate = fr.HourlyRate,
                MarkPrice = fr.MarkPrice,
                SpotPrice = fr.SpotPrice,
                AdjustedSpotPrice = fr.AdjustedSpotPrice,
                Premium = fr.Premium,
                PremiumPercentage = fr.PremiumPercentage,
                CalculatedAt = fr.CalculatedAt,
                ValidUntil = fr.ValidUntil
            }).ToList();

            logger.OperationCompleted(nameof(GetFundingRateHistoryAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<List<FundingRateHistoryItem>>.Success(history);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting funding rate history for {Symbol}", symbol);
            return Result<List<FundingRateHistoryItem>>.Failure(
                ResultPatternError.InternalServerError($"Error getting funding rate history: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get funding rates for multiple symbols in a single call
    /// </summary>
    public async Task<Result<Dictionary<string, FundingRateResponse>>> GetBatchFundingRatesAsync(
        List<string> symbols,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetBatchFundingRatesAsync), startTime);

        var results = new Dictionary<string, FundingRateResponse>();

        foreach (var symbol in symbols)
        {
            var rateResult = await GetCurrentFundingRateAsync(symbol, token);
            if (rateResult.IsSuccess && rateResult.Value is not null)
            {
                results[symbol] = rateResult.Value;
            }
        }

        logger.OperationCompleted(nameof(GetBatchFundingRatesAsync), DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow - startTime);

        return Result<Dictionary<string, FundingRateResponse>>.Success(results);
    }

    /// <summary>
    /// Get funding rate factors breakdown for a symbol
    /// </summary>
    public async Task<Result<FundingRateFactorsResponse>> GetFundingRateFactorsAsync(
        string symbol,
        CancellationToken token = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetFundingRateFactorsAsync), startTime);

        try
        {
            var currentRateResult = await GetCurrentFundingRateAsync(symbol, token);
            if (!currentRateResult.IsSuccess || currentRateResult.Value is null)
            {
                return Result<FundingRateFactorsResponse>.Failure(
                    ResultPatternError.NotFound($"No funding rate found for {symbol}"));
            }

            var currentRate = currentRateResult.Value;

            var response = new FundingRateFactorsResponse
            {
                Symbol = symbol,
                Factors = currentRate.Factors,
                MarkPrice = currentRate.MarkPrice,
                SpotPrice = currentRate.SpotPrice,
                AdjustedSpotPrice = currentRate.AdjustedSpotPrice,
                Premium = currentRate.Premium,
                CalculatedAt = currentRate.CalculatedAt
            };

            logger.OperationCompleted(nameof(GetFundingRateFactorsAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - startTime);

            return Result<FundingRateFactorsResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting funding rate factors for {Symbol}", symbol);
            return Result<FundingRateFactorsResponse>.Failure(
                ResultPatternError.InternalServerError($"Error getting funding rate factors: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get corporate action adjustment based on upcoming corporate actions
    /// </summary>
    private async Task<decimal> GetCorporateActionAdjustmentAsync(
        string symbol,
        CancellationToken token)
    {
        try
        {
            var upcomingActions = await corporateActionService.GetUpcomingCorporateActionsAsync(
                symbol,
                daysAhead: 7,
                token);

            if (upcomingActions.Count == 0)
            {
                return 0m;
            }

            DateTime now = DateTime.UtcNow;
            var actionsWithin3Days = upcomingActions.Where(ca => 
                ca.EffectiveDate <= now.AddDays(3) && ca.EffectiveDate > now).ToList();

            if (actionsWithin3Days.Any())
            {
                return CorporateActionAdjustment3Days;
            }

            // Actions within 7 days but not within 3 days
            return CorporateActionAdjustment7Days;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error getting corporate action adjustment for {Symbol}", symbol);
            return 0m; // Default to no adjustment on error
        }
    }
}

