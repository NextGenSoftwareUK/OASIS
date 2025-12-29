namespace Application.DTOs.Role.Requests;

/// <summary>
/// Request DTO used for creating a new role within the system.
/// </summary>
/// <param name="RoleName">The human-readable name of the role. Must be unique within the system.</param>
/// <param name="RoleKey">The internal key that uniquely identifies the role programmatically. Used in authorization and business logic.</param>
/// <param name="Description">An optional field for describing the purpose or usage of the role.</param>
public sealed record CreateRoleRequest(
    string RoleName,
    string RoleKey,
    string? Description);