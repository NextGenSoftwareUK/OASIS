namespace Application.Extensions.Mappers;

/// <summary>
/// Provides mapping functionality for converting role-related request models into domain entities.
/// This class contains methods for mapping Create, Update, and Delete role requests to corresponding Role entity instances.
/// </summary>
public static class RoleMapper
{
    /// <summary>
    /// Maps a Role entity to a GetRolesResponse.
    /// </summary>
    /// <param name="role">The Role entity to be mapped.</param>
    /// <returns>A GetRolesResponse containing the mapped data.</returns>
    public static GetRolesResponse ToRead(this Role role)
        => new(
            role.Id,
            role.Name,
            role.RoleKey,
            role.Description);

    /// <summary>
    /// Maps a Role entity to a GetRoleDetailResponse.
    /// </summary>
    /// <param name="role">The Role entity to be mapped.</param>
    /// <returns>A GetRoleDetailResponse containing the mapped data.</returns>
    public static GetRoleDetailResponse ToReadDetail(this Role role)
        => new(
            role.Id,
            role.Name,
            role.RoleKey,
            role.Description);

    /// <summary>
    /// Maps an UpdateRoleRequest to an existing Role entity.
    /// </summary>
    /// <param name="role">The Role entity to be updated.</param>
    /// <param name="accessor">The IHttpContextAccessor to retrieve the current user and IP address information.</param>
    /// <param name="request">The UpdateRoleRequest containing the new data.</param>
    /// <returns>The updated Role entity.</returns>
    public static Role ToEntity(this Role role, IHttpContextAccessor accessor, UpdateRoleRequest request)
    {
        role.Update(accessor.GetId());
        role.RoleKey = request.RoleKey;
        role.Description = request.Description;
        role.Name = request.RoleName;
        role.UpdatedByIp!.Add(accessor.GetRemoteIpAddress());
        return role;
    }

    /// <summary>
    /// Maps a CreateRoleRequest to a new Role entity.
    /// </summary>
    /// <param name="request">The CreateRoleRequest containing the data for the new Role entity.</param>
    /// <param name="accessor">The IHttpContextAccessor to retrieve the current user and IP address information.</param>
    /// <returns>A new Role entity.</returns>
    public static Role ToEntity(this CreateRoleRequest request, IHttpContextAccessor accessor)
        => new()
        {
            Name = request.RoleName,
            RoleKey = request.RoleKey,
            Description = request.Description,
            CreatedBy = accessor.GetId(),
            CreatedByIp = accessor.GetRemoteIpAddress()
        };

    /// <summary>
    /// Maps a Role entity to an entity indicating deletion.
    /// </summary>
    /// <param name="role">The Role entity to be marked as deleted.</param>
    /// <param name="accessor">The IHttpContextAccessor to retrieve the current user and IP address information.</param>
    /// <returns>The updated Role entity indicating deletion.</returns>
    public static Role ToEntity(this Role role, IHttpContextAccessor accessor)
    {
        role.Delete(accessor.GetId());
        role.DeletedByIp = accessor.GetRemoteIpAddress();
        return role;
    }
}
