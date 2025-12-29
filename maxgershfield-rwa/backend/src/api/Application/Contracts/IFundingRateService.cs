namespace Application.Contracts;

/// <summary>
/// Service for calculating funding rates for perpetual futures on RWAs/equities.
/// Funding rates account for premium to spot, corporate action windows, volatility, and liquidity.
/// </summary>
public interface IFundingRateService
{
    /// <summary>
    /// Calculate funding rate for a symbol given a mark price
    /// </summary>
    Task<Result<FundingRateResponse>> CalculateFundingRateAsync(
        string symbol,
        decimal markPrice,
        CancellationToken token = default);

    /// <summary>
    /// Get the current funding rate for a symbol (from database)
    /// </summary>
    Task<Result<FundingRateResponse>> GetCurrentFundingRateAsync(
        string symbol,
        CancellationToken token = default);

    /// <summary>
    /// Get funding rate history for a symbol
    /// </summary>
    Task<Result<List<FundingRateHistoryItem>>> GetFundingRateHistoryAsync(
        string symbol,
        int hours = 24,
        CancellationToken token = default);

    /// <summary>
    /// Get funding rates for multiple symbols in a single call
    /// </summary>
    Task<Result<Dictionary<string, FundingRateResponse>>> GetBatchFundingRatesAsync(
        List<string> symbols,
        CancellationToken token = default);

    /// <summary>
    /// Get funding rate factors breakdown for a symbol
    /// </summary>
    Task<Result<FundingRateFactorsResponse>> GetFundingRateFactorsAsync(
        string symbol,
        CancellationToken token = default);
}

