namespace Application.DTOs.UserRole.Requests;

/// <summary>
/// Request object used for updating the user role association.
/// </summary>
/// <param name="UserId">
/// The unique identifier of the user whose role is being updated.
/// </param>
/// <param name="RoleId">
/// The unique identifier of the new role to assign to the user.
/// </param>
public sealed record UpdateUserRoleRequest(
    Guid UserId,
    Guid RoleId) : BaseUserRoleRequest(UserId, RoleId);