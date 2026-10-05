namespace RadixBridge;

/// <summary>
/// Defines the interface for Radix blockchain bridge interactions.
/// </summary>
public interface IRadixBridge : IBridge
{
    /// <summary>
    /// Retrieves the Radix address associated with a given public key.
    /// </summary>
    /// <param name="publicKey">The public key used to derive the address.</param>
    /// <param name="addressType">The type of address to generate.</param>
    /// <param name="networkType">The network type (e.g., mainnet, testnet).</param>
    /// <param name="token">Cancellation token for async operations.</param>
    /// <returns>The generated Radix address as a string.</returns>
    Result<string> GetAddressAsync(PublicKey publicKey, AddressType addressType, NetworkType networkType,
        CancellationToken token = default);

    /// <summary>
    /// Creates a new Radix account, including a public key, private key, and seed phrase.
    /// </summary>
    /// <param name="token">Cancellation token for async operations.</param>
    /// <returns>A tuple containing the public key, private key, and seed phrase.</returns>
    new Result<(PublicKey PublicKey, PrivateKey PrivateKey, string SeedPhrase)> CreateAccountAsync(
        CancellationToken token = default);

    /// <summary>
    /// Restores an existing Radix account using a seed phrase.
    /// </summary>
    /// <param name="seedPhrase">The seed phrase used to restore the account.</param>
    /// <param name="token">Cancellation token for async operations.</param>
    /// <returns>A tuple containing the public key and private key of the restored account.</returns>
    new Result<(PublicKey PublicKey, PrivateKey PrivateKey)> RestoreAccountAsync(string seedPhrase,
        CancellationToken token = default);
}