namespace Application.DTOs.UserRole.Responses;

/// <summary>
/// Response object containing details of a user's role.
/// </summary>
/// <param name="Id">
/// The unique identifier of the user-role mapping.
/// </param>
/// <param name="UserId">
/// The unique identifier of the user associated with the role.
/// </param>
/// <param name="RoleId">
/// The unique identifier of the role assigned to the user.
/// </param>
/// <param name="User">
/// The detailed information of the user, provided by the <see cref="GetUserDetailPublicResponse"/>.
/// </param>
/// <param name="Role">
/// The detailed information of the role, provided by the <see cref="GetRoleDetailResponse"/>.
/// </param>
public sealed record GetUserRolesResponse(
    Guid Id,
    Guid UserId,
    Guid RoleId,
    GetUserDetailPublicResponse User,
    GetRoleDetailResponse Role);