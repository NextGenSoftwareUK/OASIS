namespace Application.DTOs.Role.Requests;

/// <summary>
/// Request DTO used for updating an existing role's properties.
/// </summary>
/// <param name="RoleName">The new name of the role. Must be unique across the system.</param>
/// <param name="RoleKey">The new unique key used for identifying the role in the system.</param>
/// <param name="Description">An optional textual description providing more context about the role.</param>
public sealed record UpdateRoleRequest(
    string RoleName,
    string RoleKey,
    string? Description);