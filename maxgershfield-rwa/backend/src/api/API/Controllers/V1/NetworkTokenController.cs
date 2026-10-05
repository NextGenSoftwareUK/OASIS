namespace API.Controllers.V1;

/// <summary>
/// Controller that manages network token-related operations, including retrieving, creating, updating, and deleting network tokens.
/// The controller ensures that only authorized users with the appropriate roles (Admin) can modify network tokens.
/// Provides actions to interact with the network token service for business logic and data handling.
/// </summary>
[Route($"{ApiAddresses.Base}/network-tokens")]
public sealed class NetworkTokenController(INetworkTokenService networkTokenService) : V1BaseController
{
    /// <summary>
    /// Retrieves a list of network tokens filtered by the provided filter criteria.
    /// </summary>
    /// <param name="filter">The filter parameters for querying network tokens.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response containing a list of network tokens that match the filter criteria.</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetNetworkTokensAsync([FromQuery] NetworkTokenFilter filter,
        CancellationToken cancellationToken)
        => (await networkTokenService.GetNetworkTokensAsync(filter, cancellationToken)).ToActionResult();

    /// <summary>
    /// Retrieves detailed information about a specific network token identified by its unique ID.
    /// </summary>
    /// <param name="networkTokenId">The unique identifier of the network token to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response containing the details of the requested network token.</returns>
    [HttpGet("{networkTokenId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNetworkTokenDetailAsync(Guid networkTokenId,
        CancellationToken cancellationToken)
        => (await networkTokenService.GetNetworkTokenDetailAsync(networkTokenId, cancellationToken)).ToActionResult();

    /// <summary>
    /// Creates a new network token with the provided details. Only accessible by users with the Admin role.
    /// </summary>
    /// <param name="request">The request containing the details for creating the network token.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating the success or failure of the network token creation process.</returns>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateNetworkTokenAsync([FromBody] CreateNetworkTokenRequest request,
        CancellationToken cancellationToken)
        => (await networkTokenService.CreateNetworkTokenAsync(request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Updates an existing network token identified by its unique ID. Only accessible by users with the Admin role.
    /// </summary>
    /// <param name="networkTokenId">The unique identifier of the network token to update.</param>
    /// <param name="request">The request containing the updated network token details.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating the result of the network token update operation.</returns>
    [HttpPut("{networkTokenId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateNetworkTokenAsync(Guid networkTokenId,
        [FromBody] UpdateNetworkTokenRequest request, CancellationToken cancellationToken)
        => (await networkTokenService.UpdateNetworkTokenAsync(networkTokenId, request, cancellationToken))
            .ToActionResult();

    /// <summary>
    /// Deletes an existing network token identified by its unique ID. Only accessible by users with the Admin role.
    /// </summary>
    /// <param name="networkTokenId">The unique identifier of the network token to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating the result of the network token deletion operation.</returns>
    [HttpDelete("{networkTokenId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteNetworkTokenAsync(Guid networkTokenId, CancellationToken cancellationToken)
        => (await networkTokenService.DeleteNetworkTokenAsync(networkTokenId, cancellationToken)).ToActionResult();
}