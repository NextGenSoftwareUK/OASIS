namespace Application.Contracts;

/// <summary>
/// Defines the contract for role-related operations such as querying, creation, update, and deletion.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Retrieves a paginated list of roles based on the provided filtering criteria.
    /// </summary>
    /// <param name="filter">Filter options including name, keyword, and description.</param>
    /// <param name="token">Optional cancellation token for the operation.</param>
    /// <returns>A result containing a paginated list of roles.</returns>
    Task<Result<PagedResponse<IEnumerable<GetRolesResponse>>>>
        GetRolesAsync(RoleFilter filter, CancellationToken token = default);

    /// <summary>
    /// Retrieves detailed information of a specific role.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role.</param>
    /// <param name="token">Optional cancellation token for the operation.</param>
    /// <returns>A result containing the role's detail if found; otherwise, a failure result.</returns>
    Task<Result<GetRoleDetailResponse>>
        GetRoleDetailAsync(Guid roleId, CancellationToken token);

    /// <summary>
    /// Creates a new role based on the provided request data.
    /// </summary>
    /// <param name="request">The request object containing role data.</param>
    /// <param name="token">Optional cancellation token for the operation.</param>
    /// <returns>A result containing the ID of the created role or failure details.</returns>
    Task<Result<CreateRoleResponse>>
        CreateRoleAsync(CreateRoleRequest request, CancellationToken token);

    /// <summary>
    /// Updates an existing role with the specified identifier.
    /// </summary>
    /// <param name="roleId">The ID of the role to update.</param>
    /// <param name="request">The request object containing updated values.</param>
    /// <param name="token">Optional cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure of the update operation.</returns>
    Task<Result<UpdateRoleResponse>>
        UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, CancellationToken token = default);

    /// <summary>
    /// Deletes the role with the specified identifier.
    /// </summary>
    /// <param name="roleId">The ID of the role to delete.</param>
    /// <param name="token">Optional cancellation token for the operation.</param>
    /// <returns>A base result indicating success or failure of the delete operation.</returns>
    Task<BaseResult> DeleteRoleAsync(Guid roleId, CancellationToken token = default);
}
