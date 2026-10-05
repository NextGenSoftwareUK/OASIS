namespace SolanaBridgeTest.Services;

// This interface defines the essential operations for interacting with the Solana Bridge API for testing purposes.
// It includes methods for account creation, balance retrieval, account restoration, withdrawals, deposits, and transaction status checks.
// The interface follows SOLID principles to ensure flexibility, maintainability, and separation of concerns.
public interface ITestService
{
    /// <summary>
    /// Asynchronously tests the process of creating a new account on the Solana network.
    /// This includes generating keys and a seed phrase.
    /// </summary>
    Task CreateAccountTestAsync();

    /// <summary>
    /// Asynchronously retrieves the balance of a given account by its address.
    /// This method fetches the account balance in SOL (the native token of the Solana network).
    /// </summary>
    /// <param name="accountAddress">The address of the account to check the balance of.</param>
    Task GetAccountBalanceTestAsync(string accountAddress);

    /// <summary>
    /// Asynchronously restores an account from a given seed phrase.
    /// This method helps recover a lost account or access it from a backup using the seed phrase.
    /// </summary>
    /// <param name="seedPhrase">The seed phrase used to restore the account.</param>
    Task RestoreAccountTestAsync(string seedPhrase);

    /// <summary>
    /// Asynchronously tests the withdrawal process from a given account.
    /// This method simulates withdrawing a specified amount from the account, using the provided private key for authorization.
    /// </summary>
    /// <param name="amount">The amount of SOL to withdraw.</param>
    /// <param name="accountAddress">The address of the account to withdraw from.</param>
    /// <param name="clientPrivateKey">The private key used to authorize the withdrawal.</param>
    Task WithdrawTestAsync(decimal amount, string accountAddress, string clientPrivateKey);

    /// <summary>
    /// Asynchronously tests the deposit process into a given account.
    /// This method simulates depositing a specified amount into the account.
    /// </summary>
    /// <param name="amount">The amount of SOL to deposit.</param>
    /// <param name="accountAddress">The address of the account to deposit to.</param>
    Task DepositTestAsync(decimal amount, string accountAddress);

    /// <summary>
    /// Asynchronously retrieves the status of a transaction using its unique transaction hash.
    /// This method helps track whether a transaction is confirmed, pending, or failed.
    /// </summary>
    /// <param name="transactionHash">The unique hash of the transaction to check.</param>
    Task GetTransactionStatusTestAsync(string transactionHash);
}