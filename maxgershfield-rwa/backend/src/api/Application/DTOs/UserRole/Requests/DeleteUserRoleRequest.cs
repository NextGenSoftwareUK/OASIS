namespace Application.DTOs.UserRole.Requests;

/// <summary>
/// Represents the request to delete a user role.
/// This class contains the user ID and role ID to remove a specific role from a user.
/// The unique identifier for the user whose role will be removed.
/// The unique identifier for the role to be removed from the user.
/// </summary>
public sealed record DeleteUserRoleRequest(
    Guid UserId,
    Guid RoleId) : BaseUserRoleRequest(UserId, RoleId);