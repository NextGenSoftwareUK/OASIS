namespace Application.DTOs.Role.Responses;

/// <summary>
/// Response DTO representing a brief summary of a role entity.
/// </summary>
/// <param name="Id">Unique identifier of the role.</param>
/// <param name="RoleName">Display name of the role, used for user-facing purposes.</param>
/// <param name="RoleKey">Unique internal key used to reference the role in code or configuration.</param>
/// <param name="Description">Optional description providing additional details about the role's purpose.</param>
public record GetRolesResponse(
    Guid Id,
    string RoleName,
    string RoleKey,
    string? Description);