namespace Common.Contracts;

/// <summary>
///     Contract for providing wallet-related operations for NFT transactions.
/// </summary>
/// <remarks>
///     This interface is used to abstract away wallet management, including obtaining a wallet with a private/public key pair.
/// </remarks>
public interface INftWalletProvider
{
    /// <summary>
    ///     Asynchronously retrieves a wallet key pair (public/private key) associated with the current user.
    ///     Depending on the implementation, this may involve generating a new wallet or retrieving an existing one.
    /// </summary>
    /// <param name="network"></param>
    /// <param name="token">
    ///     The cancellation token used to signal that the operation should be cancelled.
    ///     This allows the operation to be aborted if necessary (e.g., user cancels the operation).
    /// </param>
    /// <returns>
    ///     A result containing the wallet key pair (private and public keys) if successful, or an error message if the operation fails.
    ///     The success value contains the `WalletKeyPair` containing the private and public keys of the wallet.
    /// </returns>
    ValueTask<Result<WalletKeyPair>> GetWalletAsync(string network, CancellationToken token = default);
}
