using Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ImplementationContract;

/// <summary>
/// Minimal implementation of IPriceAdjustmentService for Task 18 dependencies
/// Full implementation should be done in Task 17
/// </summary>
public sealed class PriceAdjustmentService(
    DataContext dbContext,
    ILogger<PriceAdjustmentService> logger) : IPriceAdjustmentService
{
    public async Task<decimal> GetAdjustedPriceAsync(
        string symbol,
        decimal rawPrice,
        DateTime priceDate,
        CancellationToken token = default)
    {
        // Get all corporate actions before or on the price date
        var actions = await dbContext.Set<CorporateAction>()
            .Where(ca => ca.Symbol == symbol
                         && ca.EffectiveDate <= priceDate
                         && !ca.IsDeleted)
            .OrderBy(ca => ca.EffectiveDate)
            .ToListAsync(token);

        decimal adjustedPrice = rawPrice;

        // Apply adjustments chronologically
        foreach (var action in actions)
        {
            adjustedPrice = ApplyCorporateAction(adjustedPrice, action);
        }

        return adjustedPrice;
    }

    public async Task<decimal> GetAdjustmentFactorAsync(
        string symbol,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken token = default)
    {
        // Get all corporate actions between the dates
        var actions = await dbContext.Set<CorporateAction>()
            .Where(ca => ca.Symbol == symbol
                         && ca.EffectiveDate > fromDate
                         && ca.EffectiveDate <= toDate
                         && !ca.IsDeleted)
            .OrderBy(ca => ca.EffectiveDate)
            .ToListAsync(token);

        decimal factor = 1.0m;

        foreach (var action in actions)
        {
            factor *= GetAdjustmentFactor(action);
        }

        return factor;
    }

    public async Task<List<PriceAdjustmentInfo>> GetAdjustmentHistoryAsync(
        string symbol,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken token = default)
    {
        var actions = await dbContext.Set<CorporateAction>()
            .Where(ca => ca.Symbol == symbol
                         && ca.EffectiveDate >= fromDate
                         && ca.EffectiveDate <= toDate
                         && !ca.IsDeleted)
            .OrderBy(ca => ca.EffectiveDate)
            .ToListAsync(token);

        var history = new List<PriceAdjustmentInfo>();
        decimal currentPrice = 100m; // Base price for calculation

        foreach (var action in actions)
        {
            var priceBefore = currentPrice;
            currentPrice = ApplyCorporateAction(currentPrice, action);
            var factor = GetAdjustmentFactor(action);

            history.Add(new PriceAdjustmentInfo
            {
                CorporateAction = action,
                PriceBefore = priceBefore,
                PriceAfter = currentPrice,
                AdjustmentFactor = factor,
                AppliedAt = action.EffectiveDate
            });
        }

        return history;
    }

    private decimal ApplyCorporateAction(decimal price, CorporateAction action)
    {
        return action.Type switch
        {
            CorporateActionType.StockSplit => action.SplitRatio.HasValue
                ? price / action.SplitRatio.Value
                : price,
            CorporateActionType.ReverseSplit => action.SplitRatio.HasValue
                ? price * action.SplitRatio.Value
                : price,
            CorporateActionType.Merger => action.ExchangeRatio.HasValue
                ? price * action.ExchangeRatio.Value
                : price,
            _ => price // Dividends don't adjust price
        };
    }

    private decimal GetAdjustmentFactor(CorporateAction action)
    {
        return action.Type switch
        {
            CorporateActionType.StockSplit => action.SplitRatio.HasValue
                ? 1.0m / action.SplitRatio.Value
                : 1.0m,
            CorporateActionType.ReverseSplit => action.SplitRatio ?? 1.0m,
            CorporateActionType.Merger => action.ExchangeRatio ?? 1.0m,
            _ => 1.0m
        };
    }
}

