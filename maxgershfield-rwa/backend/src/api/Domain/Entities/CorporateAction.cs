using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a corporate action (split, dividend, merger, etc.) that affects equity prices
/// </summary>
public sealed class CorporateAction : BaseEntity
{
    /// <summary>
    /// Stock ticker symbol (e.g., "AAPL")
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Type of corporate action
    /// </summary>
    public CorporateActionType Type { get; set; }

    /// <summary>
    /// Ex-dividend/split date
    /// </summary>
    public DateTime ExDate { get; set; }

    /// <summary>
    /// Record date for the corporate action
    /// </summary>
    public DateTime RecordDate { get; set; }

    /// <summary>
    /// Effective date when the corporate action takes effect
    /// </summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Split ratio (e.g., 2.0 for 2-for-1 split, 0.2 for 1-for-5 reverse split)
    /// </summary>
    public decimal? SplitRatio { get; set; }

    /// <summary>
    /// Dividend amount per share
    /// </summary>
    public decimal? DividendAmount { get; set; }

    /// <summary>
    /// Currency for dividend payment
    /// </summary>
    public string? DividendCurrency { get; set; }

    /// <summary>
    /// Symbol of acquiring company (for mergers/acquisitions)
    /// </summary>
    public string? AcquiringSymbol { get; set; }

    /// <summary>
    /// Exchange ratio (shares of acquiring per target share)
    /// </summary>
    public decimal? ExchangeRatio { get; set; }

    /// <summary>
    /// Data source that provided this corporate action
    /// </summary>
    public string DataSource { get; set; } = string.Empty;

    /// <summary>
    /// External ID from the data source
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Whether this corporate action has been verified by multiple sources
    /// </summary>
    public bool IsVerified { get; set; }
}

