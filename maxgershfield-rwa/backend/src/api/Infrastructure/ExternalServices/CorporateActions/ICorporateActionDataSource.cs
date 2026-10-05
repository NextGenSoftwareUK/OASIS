using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.ExternalServices.CorporateActions;

/// <summary>
/// Interface for corporate action data sources
/// </summary>
public interface ICorporateActionDataSource
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
    /// Fetch stock splits for a symbol
    /// </summary>
    Task<List<CorporateAction>> FetchSplitsAsync(
        string symbol,
        CancellationToken token = default);

    /// <summary>
    /// Fetch dividends for a symbol
    /// </summary>
    Task<List<CorporateAction>> FetchDividendsAsync(
        string symbol,
        CancellationToken token = default);

    /// <summary>
    /// Fetch all corporate actions for a symbol (optionally from a date)
    /// </summary>
    Task<List<CorporateAction>> FetchAllActionsAsync(
        string symbol,
        DateTime? fromDate = null,
        CancellationToken token = default);
}

