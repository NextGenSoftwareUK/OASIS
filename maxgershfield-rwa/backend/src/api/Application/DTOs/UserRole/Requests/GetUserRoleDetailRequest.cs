namespace Application.DTOs.UserRole.Requests;

/// <summary>
/// Represents the request to get the details of a user role.
/// This class contains the user ID and role ID to fetch the specific role details of a user.
/// The unique identifier for the user whose role details are requested.
/// The unique identifier for the role whose details are requested for the user.
/// </summary>
public sealed record GetUserRoleDetailRequest(
    Guid UserId,
    Guid RoleId) : BaseUserRoleRequest(UserId, RoleId);