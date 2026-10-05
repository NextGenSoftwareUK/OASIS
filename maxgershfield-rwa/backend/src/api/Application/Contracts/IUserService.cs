namespace Application.Contracts;

/// <summary>
/// Contract for user-related operations such as querying user data,
/// updating user profile, and retrieving virtual accounts.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves a paginated list of users based on filter criteria.
    /// </summary>
    /// <param name="filter">Filtering options such as name, email, etc.</param>
    /// <param name="token">Cancellation token for the async operation.</param>
    /// <returns>Paged result of user data.</returns>
    Task<Result<PagedResponse<IEnumerable<GetAllUserResponse>>>>
        GetUsersAsync(UserFilter filter, CancellationToken token = default);

    /// <summary>
    /// Retrieves public user details by user ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Public user information or not found result.</returns>
    Task<Result<GetUserDetailPublicResponse>>
        GetByIdForUser(Guid userId, CancellationToken token = default);

    /// <summary>
    /// Retrieves all virtual accounts associated with the current user,
    /// including token types, network names, and balances.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>List of virtual accounts or not found result.</returns>
    Task<Result<IEnumerable<GetVirtualAccountDetailResponse>>>
        GetVirtualAccountsAsync(CancellationToken token = default);

    /// <summary>
    /// Retrieves private profile details for the current authenticated user.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Private user information or not found result.</returns>
    Task<Result<GetUserDetailPrivateResponse>>
        GetByIdForSelf(CancellationToken token = default);

    /// <summary>
    /// Updates profile information of the current user.
    /// Ensures uniqueness of email, phone number, and username.
    /// </summary>
    /// <param name="request">New profile values.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Update result or validation error.</returns>
    Task<Result<UpdateUserResponse>>
        UpdateProfileAsync(UpdateUserProfileRequest request, CancellationToken token = default);
}