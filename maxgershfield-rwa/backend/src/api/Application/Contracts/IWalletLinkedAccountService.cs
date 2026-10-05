namespace Application.Contracts;

/// <summary>
/// Interface for managing wallet linked accounts for users.
/// Provides functionality to create and retrieve linked wallet accounts.
/// </summary>
public interface IWalletLinkedAccountService
{
    /// <summary>
    /// Creates a new wallet linked account for the user.
    /// </summary>
    /// <param name="request">The request containing the details of the wallet linked account.</param>
    /// <param name="token">The cancellation token for the asynchronous operation.</param>
    /// <returns>Returns the result of the operation.</returns>
    Task<BaseResult> CreateAsync(CreateWalletLinkedAccountRequest request, CancellationToken token = default!);

    /// <summary>
    /// Retrieves a list of wallet linked accounts for the current user.
    /// </summary>
    /// <param name="token">The cancellation token for the asynchronous operation.</param>
    /// <returns>A list of wallet linked accounts for the user.</returns>
    Task<Result<IEnumerable<GetWalletLinkedAccountDetailResponse>>> GetAsync(CancellationToken token = default!);
}