namespace Application.DTOs.UserRole.Requests;

/// <summary>
/// Request object used to create a new user role association.
/// </summary>
/// <param name="UserId">
/// The unique identifier of the user to be associated with the role.
/// </param>
/// <param name="RoleId">
/// The unique identifier of the role to be assigned to the user.
/// </param>
public sealed record CreateUserRoleRequest(
    Guid UserId,
    Guid RoleId) : BaseUserRoleRequest(UserId, RoleId);