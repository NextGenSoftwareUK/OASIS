namespace Application.DTOs.CorporateAction.Responses;

/// <summary>
/// Response DTO after creating a corporate action
/// </summary>
public sealed record CreateCorporateActionResponseDto(
    Guid Id,
    string Symbol,
    string Type,
    DateTimeOffset CreatedAt
);

