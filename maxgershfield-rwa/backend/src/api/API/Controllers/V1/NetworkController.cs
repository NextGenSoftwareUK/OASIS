namespace API.Controllers.V1;

/// <summary>
/// Controller that manages network-related operations, including retrieving, creating, updating, and deleting networks.
/// Accessible only by authorized users with the appropriate roles. This controller exposes actions for managing network
/// entities and is designed to interact with the network service layer for data processing and business logic.
/// </summary>
[Route($"{ApiAddresses.Base}/networks")]
public sealed class NetworkController(INetworkService networkService) : V1BaseController
{
    /// <summary>
    /// Retrieves a list of networks filtered by the provided filter criteria.
    /// </summary>
    /// <param name="filter">The filter parameters for querying networks.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response containing a list of networks that match the filter criteria.</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetNetworksAsync([FromQuery] NetworkFilter filter,
        CancellationToken cancellationToken)
        => (await networkService.GetNetworksAsync(filter, cancellationToken)).ToActionResult();

    /// <summary>
    /// Retrieves detailed information about a specific network identified by its unique ID.
    /// </summary>
    /// <param name="networkId">The unique identifier of the network to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response containing the details of the requested network.</returns>
    [HttpGet("{networkId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNetworkDetailAsync(Guid networkId, CancellationToken cancellationToken)
        => (await networkService.GetNetworkDetailAsync(networkId, cancellationToken)).ToActionResult();

    /// <summary>
    /// Creates a new network with the provided network details. Only accessible by users with the Admin role.
    /// </summary>
    /// <param name="request">The request containing the details for creating the network.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating the success or failure of the network creation process.</returns>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateNetworkAsync([FromBody] CreateNetworkRequest request,
        CancellationToken cancellationToken)
        => (await networkService.CreateNetworkAsync(request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Updates an existing network identified by its unique ID. Only accessible by users with the Admin role.
    /// </summary>
    /// <param name="networkId">The unique identifier of the network to update.</param>
    /// <param name="request">The request containing the updated network details.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating the result of the network update operation.</returns>
    [HttpPut("{networkId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateNetworkAsync(Guid networkId, [FromBody] UpdateNetworkRequest request,
        CancellationToken cancellationToken)
        => (await networkService.UpdateNetworkAsync(networkId, request, cancellationToken)).ToActionResult();

    /// <summary>
    /// Deletes an existing network identified by its unique ID. Only accessible by users with the Admin role.
    /// </summary>
    /// <param name="networkId">The unique identifier of the network to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A response indicating the result of the network deletion operation.</returns>
    [HttpDelete("{networkId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteNetworkAsync(Guid networkId, CancellationToken cancellationToken)
        => (await networkService.DeleteNetworkAsync(networkId, cancellationToken)).ToActionResult();
}