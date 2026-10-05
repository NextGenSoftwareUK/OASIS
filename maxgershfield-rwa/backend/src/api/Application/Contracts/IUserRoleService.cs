namespace Application.Contracts;

/// <summary>
/// Defines the contract for user-role related operations, such as querying, creation, update, and deletion.
/// </summary>
public interface IUserRoleService
{
    /// <summary>
    /// Retrieves a paginated list of user roles based on the provided filtering criteria.
    /// </summary>
    /// <param name="filter">Filter options including role name, description, etc.</param>
    /// <param name="token">Optional cancellation token for the operation.</param>
    /// <returns>A result containing a paginated list of user roles.</returns>
    Task<Result<PagedResponse<IEnumerable<GetUserRolesResponse>>>>
        GetUserRolesAsync(UserRoleFilter filter, CancellationToken token = default);

    /// <summary>
    /// Retrieves detailed information for a specific user role.
    /// </summary>
    /// <param name="id">The unique identifier of the user role.</param>
    /// <param name="token">Optional cancellation token for the operation.</param>
    /// <returns>A result containing the user role details if found; otherwise, a failure result.</returns>
    Task<Result<GetUserRoleDetailResponse>>
        GetUserRoleDetailAsync(Guid id, CancellationToken token = default);

    /// <summary>
    /// Creates a new user role based on the provided request data.
    /// </summary>
    /// <param name="request">The request object containing data for the new user role.</param>
    /// <param name="token">Optional cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure of the create operation, including the created role's details.</returns>
    Task<Result<CreateUserRoleResponse>>
        CreateUserRoleAsync(CreateUserRoleRequest request, CancellationToken token = default);

    /// <summary>
    /// Updates an existing user role with the specified identifier.
    /// </summary>
    /// <param name="id">The ID of the user role to update.</param>
    /// <param name="request">The request object containing the updated user role data.</param>
    /// <param name="token">Optional cancellation token for the operation.</param>
    /// <returns>A result indicating the success or failure of the update operation.</returns>
    Task<Result<UpdateUserRoleResponse>>
        UpdateUserRoleAsync(Guid id, UpdateUserRoleRequest request, CancellationToken token = default);

    /// <summary>
    /// Deletes the user role with the specified identifier.
    /// </summary>
    /// <param name="id">The ID of the user role to delete.</param>
    /// <param name="token">Optional cancellation token for the operation.</param>
    /// <returns>A base result indicating the success or failure of the delete operation.</returns>
    Task<BaseResult> DeleteUserRoleAsync(Guid id, CancellationToken token = default);
}