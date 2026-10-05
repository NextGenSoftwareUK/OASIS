namespace Common.Contracts;

/// <summary>
/// Defines a generic interface for interacting with blockchain bridges, allowing operations such as
/// creating and restoring accounts, checking balances, making deposits and withdrawals, and checking
/// transaction statuses. This interface is designed to be adaptable for any blockchain network (e.g., Solana, Ethereum, etc.).
/// </summary>
public interface IBridge
{
    /// <summary>
    /// Retrieves the balance of an account based on its address.
    /// </summary>
    /// <param name="accountAddress">The address of the account to check.</param>
    /// <param name="token">A cancellation token to support cancellation of the operation.</param>
    /// <returns>The balance of the account.</returns>
    Task<Result<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default);

    /// <summary>
    /// Creates a new account and returns the public key, private key, and seed phrase.
    /// </summary>
    /// <param name="token">A cancellation token to support cancellation of the operation.</param>
    /// <returns>A tuple containing the public key, private key, and seed phrase of the created account.</returns>
    Task<Result<(string PublicKey, string PrivateKey, string SeedPhrase)>>
        CreateAccountAsync(CancellationToken token = default);

    /// <summary>
    /// Restores an existing account using a seed phrase, returning the public and private keys.
    /// </summary>
    /// <param name="seedPhrase">The seed phrase used to restore the account.</param>
    /// <param name="token">A cancellation token to support cancellation of the operation.</param>
    /// <returns>A tuple containing the public key and private key of the restored account.</returns>
    Task<Result<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase,
        CancellationToken token = default);

    /// <summary>
    /// Initiates a withdrawal of a specified amount from an account.
    /// </summary>
    /// <param name="amount">The amount to withdraw.</param>
    /// <param name="senderAccountAddress">The address of the account to withdraw from.</param>
    /// <param name="senderPrivateKey">The private key of the client initiating the withdrawal.</param>
    /// <returns>A response containing the transaction details of the withdrawal.</returns>
    Task<Result<TransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey);

    /// <summary>
    /// Initiates a deposit of a specified amount into an account.
    /// </summary>
    /// <param name="amount">The amount to deposit.</param>
    /// <param name="receiverAccountAddress">The address of the account to deposit to.</param>
    /// <returns>A response containing the transaction details of the deposit.</returns>
    Task<Result<TransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress);

    /// <summary>
    /// Retrieves the status of a transaction using its transaction hash.
    /// </summary>
    /// <param name="transactionHash">The hash of the transaction to check the status of.</param>
    /// <param name="token">A cancellation token to support cancellation of the operation.</param>
    /// <returns>The status of the transaction.</returns>
    Task<Result<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default);
}