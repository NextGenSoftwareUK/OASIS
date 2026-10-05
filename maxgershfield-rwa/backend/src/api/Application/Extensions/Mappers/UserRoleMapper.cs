namespace Application.Extensions.Mappers;

/// <summary>
/// Provides mapping functionality for converting UserRole-related request models into domain entities.
/// This class includes methods for mapping UserRole entities to response models and updating user roles.
/// </summary>
public static class UserRoleMapper
{
    /// <summary>
    /// Maps a UserRole entity to a GetUserRolesResponse, including details about the associated user and role.
    /// </summary>
    /// <param name="userRole">The UserRole entity to be mapped.</param>
    /// <returns>A GetUserRolesResponse containing the mapped data.</returns>
    public static GetUserRolesResponse ToRead(this UserRole userRole)
        => new(
            userRole.Id,
            userRole.UserId,
            userRole.RoleId,
            userRole.User.ToReadPublicDetail(),
            userRole.Role.ToReadDetail());

    /// <summary>
    /// Maps a UserRole entity to a GetUserRoleDetailResponse, which includes detailed information about the user and role.
    /// </summary>
    /// <param name="userRole">The UserRole entity to be mapped.</param>
    /// <returns>A GetUserRoleDetailResponse containing the mapped data.</returns>
    public static GetUserRoleDetailResponse ToReadDetail(this UserRole userRole)
        => new(
            userRole.UserId,
            userRole.RoleId,
            userRole.User.ToReadPublicDetail(),
            userRole.Role.ToReadDetail());

    /// <summary>
    /// Maps a CreateUserRoleRequest to a new UserRole entity.
    /// </summary>
    /// <param name="request">The CreateUserRoleRequest containing the new user role information.</param>
    /// <param name="accessor">The IHttpContextAccessor to retrieve the current user and IP address information.</param>
    /// <returns>A new UserRole entity with the provided data.</returns>
    public static UserRole ToEntity(this CreateUserRoleRequest request, IHttpContextAccessor accessor)
        => new()
        {
            UserId = request.UserId,
            RoleId = request.RoleId,
            CreatedBy = accessor.GetId(),
            CreatedByIp = accessor.GetRemoteIpAddress()
        };

    /// <summary>
    /// Marks a UserRole entity as deleted, setting the appropriate deletion information.
    /// </summary>
    /// <param name="userRole">The UserRole entity to be deleted.</param>
    /// <param name="accessor">The IHttpContextAccessor to retrieve the current user and IP address information.</param>
    /// <returns>The deleted UserRole entity.</returns>
    public static UserRole ToEntity(this UserRole userRole, IHttpContextAccessor accessor)
    {
        userRole.Delete(accessor.GetId());
        userRole.DeletedByIp = accessor.GetRemoteIpAddress();
        return userRole;
    }

    /// <summary>
    /// Updates an existing UserRole entity with new data from an UpdateUserRoleRequest.
    /// </summary>
    /// <param name="userRole">The UserRole entity to be updated.</param>
    /// <param name="accessor">The IHttpContextAccessor to retrieve the current user and IP address information.</param>
    /// <param name="request">The UpdateUserRoleRequest containing the updated user role information.</param>
    /// <returns>The updated UserRole entity.</returns>
    public static UserRole ToEntity(this UserRole userRole, IHttpContextAccessor accessor,
        UpdateUserRoleRequest request)
    {
        userRole.Update(accessor.GetId());
        userRole.UserId = request.UserId;
        userRole.RoleId = request.RoleId;
        userRole.UpdatedByIp!.Add(accessor.GetRemoteIpAddress());
        return userRole;
    }
}
