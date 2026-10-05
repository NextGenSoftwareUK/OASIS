namespace API.Controllers.V1
{
    /// <summary>
    /// Controller for managing user accounts within the system.
    /// Provides endpoints for retrieving, updating, and managing user profiles and virtual accounts.
    /// </summary>
    [Route($"{ApiAddresses.Base}/accounts")]
    public sealed class UserController(IUserService userService) : V1BaseController
    {
        /// <summary>
        /// Retrieves a list of users based on the provided filter parameters.
        /// </summary>
        /// <param name="filter">Filter parameters for querying users.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
        /// <returns>A list of users matching the filter criteria.</returns>
        [HttpGet]
        public async Task<IActionResult> GetUsersAsync([FromQuery] UserFilter filter,
            CancellationToken cancellationToken)
            => (await userService.GetUsersAsync(filter, cancellationToken)).ToActionResult();

        /// <summary>
        /// Retrieves a user profile by its unique user ID.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
        /// <returns>The profile of the specified user.</returns>
        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
            => (await userService.GetByIdForUser(userId, cancellationToken)).ToActionResult();

        /// <summary>
        /// Retrieves a list of virtual accounts associated with users.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
        /// <returns>A list of virtual accounts.</returns>
        [HttpGet("list")]
        public async Task<IActionResult> GetVirtualAccountsAsync(CancellationToken cancellationToken)
            => (await userService.GetVirtualAccountsAsync(cancellationToken)).ToActionResult();

        /// <summary>
        /// Retrieves the profile of the currently authenticated user.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
        /// <returns>The profile of the currently authenticated user.</returns>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfileAsync(CancellationToken cancellationToken)
            => (await userService.GetByIdForSelf(cancellationToken)).ToActionResult();

        /// <summary>
        /// Updates the profile of the currently authenticated user.
        /// </summary>
        /// <param name="request">The request containing updated user profile details.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
        /// <returns>A response indicating the success or failure of the profile update.</returns>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfileAsync([FromBody] UpdateUserProfileRequest request,
            CancellationToken cancellationToken)
            => (await userService.UpdateProfileAsync(request, cancellationToken)).ToActionResult();
    }
}