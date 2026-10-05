namespace RadixBridgeTest.Services;

/// <summary>
/// Defines essential operations for testing interactions with the Radix Bridge API.
/// Operations include account creation, balance retrieval, account restoration, withdrawals, deposits, and transaction status checks.
/// </summary>
public interface ITestService
{
    /// <summary>
    /// Asynchronously tests account creation on the Radix network.
    /// </summary>
    Task CreateAccountTestAsync();

    /// <summary>
    /// Asynchronously retrieves the balance of an account.
    /// </summary>
    Task GetAccountBalanceTestAsync(string accountAddress);

    /// <summary>
    /// Asynchronously restores an account from a seed phrase.
    /// </summary>
    Task RestoreAccountTestAsync(string seedPhrase);

    /// <summary>
    /// Asynchronously tests the withdrawal process from an account.
    /// </summary>
    Task WithdrawTestAsync(decimal amount, string accountAddress, string clientPrivateKey);

    /// <summary>
    /// Asynchronously tests the deposit process into an account.
    /// </summary>
    Task DepositTestAsync(decimal amount, string accountAddress);

    /// <summary>
    /// Asynchronously retrieves the status of a transaction.
    /// </summary>
    Task GetTransactionStatusTestAsync(string transactionHash);

    /// <summary>
    /// Asynchronously generates an address based on the public key, address type, and network type.
    /// </summary>
    Task GetAddressTestAsync(PublicKey publicKey, AddressType addressType, NetworkType networkType);
}