namespace API.Controllers.V1;

/// <summary>
/// Controller for managing linked wallet accounts.
/// Provides endpoints to create linked wallet accounts and retrieve the current user's linked accounts.
/// </summary>
[Route($"{ApiAddresses.Base}/linked-accounts")]
public sealed class WalletLinkedAccountController(IWalletLinkedAccountService service) : V1BaseController
{
    /// <summary>
    /// Creates a new linked wallet account.
    /// </summary>
    /// <param name="request">The request object containing the details for the linked account.</param>
    /// <param name="token">A cancellation token for the operation.</param>
    /// <returns>Action result indicating the success or failure of the operation.</returns>
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateWalletLinkedAccountRequest request,
        CancellationToken token = default)
        => (await service.CreateAsync(request, token)).ToActionResult();

    /// <summary>
    /// Retrieves the current user's linked wallet accounts.
    /// </summary>
    /// <param name="token">A cancellation token for the operation.</param>
    /// <returns>Action result containing the list of linked wallet accounts for the current user.</returns>
    [HttpGet("me")]
    public async Task<IActionResult> GetAsync(CancellationToken token = default)
        => (await service.GetAsync(token)).ToActionResult();
}