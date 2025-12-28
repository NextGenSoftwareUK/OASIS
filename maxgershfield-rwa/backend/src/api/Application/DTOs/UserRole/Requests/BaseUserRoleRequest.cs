namespace Application.DTOs.UserRole.Requests;

/// <summary>
/// Represents the base request for creating or updating a user role.
/// This class contains the user ID and role ID as basic information.
/// The unique identifier for the user to which the role will be assigned.
/// The unique identifier for the role to be assigned to the user.
/// </summary>
public abstract record BaseUserRoleRequest(
    Guid UserId,
    Guid RoleId);