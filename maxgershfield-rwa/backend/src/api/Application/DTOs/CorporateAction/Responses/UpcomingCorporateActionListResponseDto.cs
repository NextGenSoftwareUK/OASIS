namespace Application.DTOs.CorporateAction.Responses;

/// <summary>
/// Response DTO for a list of upcoming corporate actions
/// </summary>
public sealed record UpcomingCorporateActionListResponseDto(
    string Symbol,
    IEnumerable<UpcomingCorporateActionResponseDto> UpcomingActions
);

