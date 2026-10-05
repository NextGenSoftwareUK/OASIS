namespace Application.Contracts;

/// <summary>
/// Service for calculating volatility for equity symbols.
/// Volatility is used in funding rate calculations to adjust for high volatility periods.
/// </summary>
public interface IVolatilityService
{
    /// <summary>
    /// Get volatility for a symbol (returns as decimal, e.g., 0.25 = 25%)
    /// Calculates 30-day rolling volatility by default
    /// </summary>
    Task<Result<decimal>> GetVolatilityAsync(
        string symbol,
        int days = 30,
        CancellationToken token = default);

    /// <summary>
    /// Get volatility for multiple symbols
    /// </summary>
    Task<Result<Dictionary<string, decimal>>> GetBatchVolatilityAsync(
        List<string> symbols,
        int days = 30,
        CancellationToken token = default);
}

