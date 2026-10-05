namespace Application.DTOs.Role.Responses;

/// <summary>
/// Detailed response DTO representing a role entity.
/// </summary>
/// <param name="Id">Unique identifier of the role.</param>
/// <param name="RoleName">The display name of the role, visible to users and administrators.</param>
/// <param name="RoleKey">The unique internal key used to identify the role within the system.</param>
/// <param name="Description">An optional description that elaborates on the role’s responsibilities or usage.</param>
public record GetRoleDetailResponse(
    Guid Id,
    string RoleName,
    string RoleKey,
    string? Description);