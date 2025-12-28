using Application.DTOs.PriceAdjustment;
using Domain.Entities;

namespace Application.Contracts;

/// <summary>
/// Service for applying corporate action adjustments to prices
/// This is a minimal interface for Task 18 dependencies.
/// Full implementation should be done in Task 17.
/// </summary>
public interface IPriceAdjustmentService
{
    /// <summary>
    /// Get adjusted price for a given raw price and date
    /// </summary>
    Task<decimal> GetAdjustedPriceAsync(
        string symbol,
        decimal rawPrice,
        DateTime priceDate,
        CancellationToken token = default);

    /// <summary>
    /// Get adjustment factor (multiplier) for a date range
    /// </summary>
    Task<decimal> GetAdjustmentFactorAsync(
        string symbol,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken token = default);

    /// <summary>
    /// Get all adjustments applied between two dates
    /// </summary>
    Task<List<PriceAdjustmentInfo>> GetAdjustmentHistoryAsync(
        string symbol,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken token = default);
}

/// <summary>
/// Information about a price adjustment
/// </summary>
public sealed record PriceAdjustmentInfo
{
    public CorporateAction CorporateAction { get; init; } = null!;
    public decimal PriceBefore { get; init; }
    public decimal PriceAfter { get; init; }
    public decimal AdjustmentFactor { get; init; }
    public DateTime AppliedAt { get; init; }
}

