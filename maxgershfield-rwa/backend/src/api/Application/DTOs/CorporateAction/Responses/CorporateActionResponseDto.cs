namespace Application.DTOs.CorporateAction.Responses;

/// <summary>
/// Response DTO for a corporate action
/// </summary>
public sealed record CorporateActionResponseDto(
    Guid Id,
    string Symbol,
    string Type,
    DateTime ExDate,
    DateTime RecordDate,
    DateTime EffectiveDate,
    decimal? SplitRatio,
    decimal? DividendAmount,
    string? DividendCurrency,
    string? AcquiringSymbol,
    decimal? ExchangeRatio,
    string DataSource,
    bool IsVerified,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

