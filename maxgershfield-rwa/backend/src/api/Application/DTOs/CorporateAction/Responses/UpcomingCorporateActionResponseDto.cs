namespace Application.DTOs.CorporateAction.Responses;

/// <summary>
/// Response DTO for upcoming corporate actions
/// </summary>
public sealed record UpcomingCorporateActionResponseDto(
    Guid Id,
    string Type,
    DateTime ExDate,
    DateTime EffectiveDate,
    decimal? SplitRatio,
    decimal? DividendAmount,
    int DaysUntil
);

