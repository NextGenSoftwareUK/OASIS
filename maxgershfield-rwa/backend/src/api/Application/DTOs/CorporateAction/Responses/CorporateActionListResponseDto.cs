namespace Application.DTOs.CorporateAction.Responses;

/// <summary>
/// Response DTO for a paginated list of corporate actions
/// </summary>
public sealed record CorporateActionListResponseDto(
    string Symbol,
    IEnumerable<CorporateActionResponseDto> CorporateActions,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

