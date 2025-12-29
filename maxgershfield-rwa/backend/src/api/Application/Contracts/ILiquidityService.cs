namespace Application.Contracts;

/// <summary>
/// Service for calculating liquidity scores for equity symbols.
/// Liquidity scores are used in funding rate calculations to adjust for low liquidity periods.
/// </summary>
public interface ILiquidityService
{
    /// <summary>
    /// Get liquidity score for a symbol (0-1 scale, where 1 is highest liquidity)
    /// </summary>
    Task<Result<decimal>> GetLiquidityScoreAsync(
        string symbol,
        CancellationToken token = default);

    /// <summary>
    /// Get liquidity scores for multiple symbols
    /// </summary>
    Task<Result<Dictionary<string, decimal>>> GetBatchLiquidityScoresAsync(
        List<string> symbols,
        CancellationToken token = default);
}

