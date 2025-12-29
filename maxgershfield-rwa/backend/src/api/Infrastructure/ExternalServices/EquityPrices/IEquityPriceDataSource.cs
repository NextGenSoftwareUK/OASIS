namespace Infrastructure.ExternalServices.EquityPrices;

/// <summary>
/// Interface for equity price data sources
/// </summary>
public interface IEquityPriceDataSource
{
    /// <summary>
    /// Name of the data source
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Reliability score (0-1 scale) for this data source
    /// </summary>
    decimal ReliabilityScore { get; }

    /// <summary>
    /// Get current price for a symbol
    /// </summary>
    Task<SourcePriceResult?> GetPriceAsync(
        string symbol,
        CancellationToken token = default);

    /// <summary>
    /// Get prices for multiple symbols
    /// </summary>
    Task<List<SourcePriceResult>> GetPricesAsync(
        List<string> symbols,
        CancellationToken token = default);

    /// <summary>
    /// Get historical price for a symbol at a specific date
    /// </summary>
    Task<SourcePriceResult?> GetPriceAtDateAsync(
        string symbol,
        DateTime date,
        CancellationToken token = default);
}

/// <summary>
/// Result from a data source
/// </summary>
public sealed record SourcePriceResult
{
    public string Symbol { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public DateTime Timestamp { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public int LatencyMs { get; init; }
}

