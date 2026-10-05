namespace Application.Contracts;

/// <summary>
/// Service for fetching and aggregating equity prices from multiple sources
/// with corporate action adjustments and confidence scoring
/// </summary>
public interface IEquityPriceService
{
    /// <summary>
    /// Get adjusted price for a symbol (defaults to adjusted price)
    /// </summary>
    Task<Result<EquityPriceResponse>> GetAdjustedPriceAsync(
        string symbol,
        CancellationToken token = default);

    /// <summary>
    /// Get raw price for a symbol (without corporate action adjustments)
    /// </summary>
    Task<Result<EquityPriceResponse>> GetRawPriceAsync(
        string symbol,
        CancellationToken token = default);

    /// <summary>
    /// Get prices for multiple symbols in a single call
    /// </summary>
    Task<Result<List<EquityPriceResponse>>> GetBatchPricesAsync(
        List<string> symbols,
        bool adjusted = true,
        CancellationToken token = default);

    /// <summary>
    /// Get historical prices for a symbol within a date range
    /// </summary>
    Task<Result<EquityPriceHistoryResponse>> GetPriceHistoryAsync(
        string symbol,
        DateTime fromDate,
        DateTime toDate,
        bool adjusted = true,
        CancellationToken token = default);

    /// <summary>
    /// Get price at a specific date
    /// </summary>
    Task<Result<EquityPriceResponse>> GetPriceAtDateAsync(
        string symbol,
        DateTime date,
        bool adjusted = true,
        CancellationToken token = default);
}

