using Solnet.Rpc;

namespace SolanaBridgeTest.Services;

/// <summary>
/// Service for testing interactions with the SolanaBridge API.
/// Includes creating an account, checking balance, restoring an account,
/// making withdrawals, deposits, and checking transaction statuses.
/// Uses SolanaTechnicalAccountBridgeOptions for SolanaBridge configuration.
/// </summary>
public class TestService : ITestService
{
    /// <summary>
    /// Instance of SolanaBridge, initialized with the provided options.
    /// </summary>
    private readonly SolanaBridge.SolanaBridge _bridge;

    /// <summary>
    /// Primary constructor that initializes the SolanaBridge instance with the provided options.
    /// </summary>
    /// <param name="options">Configuration options for the SolanaBridge.</param>
    /// <param name="logger"></param>
    public TestService(SolanaTechnicalAccountBridgeOptions options, ILogger<SolanaBridge.SolanaBridge> logger, IRpcClient rpcClient)
        => _bridge = new(logger, options, rpcClient);


    /// <summary>
    /// Asynchronously tests the process of creating a new account on the Solana network.
    /// This includes generating keys and a seed phrase.
    /// </summary>
    public async Task CreateAccountTestAsync()
    {
        try
        {
            // Creating a new account via the SolanaBridge
            Result<(string PublicKey, string PrivateKey, string SeedPhrase)> response =
                await _bridge.CreateAccountAsync();

            if (response.IsSuccess)
            {
                // Displaying the generated account details
                Console.WriteLine($"PublicKey: {response.Value.PublicKey}");
                Console.WriteLine($"PrivateKey: {response.Value.PrivateKey}");
                Console.WriteLine($"SeedPhrase: {response.Value.SeedPhrase}");
            }
            else
            {
                Console.WriteLine($"ErrorType:{response.Error.ErrorType.ToString()}");
                Console.WriteLine($"ErrorCode:{response.Error.Code}");
                Console.WriteLine($"ErrorMessage:{response.Error.Message}");
            }
        }
        catch (Exception ex)
        {
            // Handling errors during account creation
            Console.WriteLine($"Error in CreateAccountTestAsync: {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronously gets the balance of an account by its address.
    /// </summary>
    /// <param name="accountAddress">The account address for balance retrieval.</param>
    public async Task GetAccountBalanceTestAsync(string accountAddress)
    {
        try
        {
            // Fetching the account balance
            Result<decimal> response = await _bridge.GetAccountBalanceAsync(accountAddress);

            if (response.IsSuccess)
            {
                // Displaying the balance in SOL (Solana tokens)
                Console.WriteLine($"Account Balance: {response.Value} SOL");
            }
            else
            {
                Console.WriteLine($"ErrorType:{response.Error.ErrorType.ToString()}");
                Console.WriteLine($"ErrorCode:{response.Error.Code}");
                Console.WriteLine($"ErrorMessage:{response.Error.Message}");
            }
        }
        catch (Exception ex)
        {
            // Handling errors while fetching balance
            Console.WriteLine($"Error in GetAccountBalanceTestAsync: {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronously restores an account using a seed phrase.
    /// </summary>
    /// <param name="seedPhrase">The seed phrase to restore the account.</param>
    public async Task RestoreAccountTestAsync(string seedPhrase)
    {
        try
        {
            // Restoring the account using the provided seed phrase
            Result<(string PublicKey, string PrivateKey)> response = await _bridge.RestoreAccountAsync(seedPhrase);

            if (response.IsSuccess)
            {
                // Displaying the restored account details
                Console.WriteLine($"Restored PublicKey: {response.Value.PublicKey}");
                Console.WriteLine($"Restored PrivateKey: {response.Value.PrivateKey}");
            }
            else
            {
                Console.WriteLine($"ErrorType:{response.Error.ErrorType.ToString()}");
                Console.WriteLine($"ErrorCode:{response.Error.Code}");
                Console.WriteLine($"ErrorMessage:{response.Error.Message}");
            }
        }
        catch (Exception ex)
        {
            // Handling errors during account restoration
            Console.WriteLine($"Error in RestoreAccountTestAsync: {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronously withdraws a specified amount from an account.
    /// </summary>
    /// <param name="amount">The amount to withdraw.</param>
    /// <param name="accountAddress">The account address for withdrawal.</param>
    /// <param name="clientPrivateKey">The client's private key for authorization.</param>
    public async Task WithdrawTestAsync(decimal amount, string accountAddress, string clientPrivateKey)
    {
        try
        {
            // Performing the withdrawal operation
            Result<TransactionResponse> response =
                await _bridge.WithdrawAsync(amount, accountAddress, clientPrivateKey);

            if (response.IsSuccess)
            {
                // Displaying the transaction details
                Console.WriteLine($"TransactionId: {response.Value?.TransactionId}");
                Console.WriteLine($"ErrorMessage: {response.Value?.ErrorMessage}");
                Console.WriteLine($"Success: {response.Value?.Success}");
                Console.WriteLine($"Data: {response.Value?.Data}");
                Console.WriteLine($"TransactionStatus: {response.Value?.Status.ToString()}");
            }
            else
            {
                Console.WriteLine($"ErrorType:{response.Error.ErrorType.ToString()}");
                Console.WriteLine($"ErrorCode:{response.Error.Code}");
                Console.WriteLine($"ErrorMessage:{response.Error.Message}");
            }
        }
        catch (Exception ex)
        {
            // Handling errors during withdrawal
            Console.WriteLine($"Error in WithdrawTestAsync: {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronously deposits a specified amount into an account.
    /// </summary>
    /// <param name="amount">The amount to deposit.</param>
    /// <param name="accountAddress">The account address for deposit.</param>
    public async Task DepositTestAsync(decimal amount, string accountAddress)
    {
        try
        {
            // Performing the deposit operation
            Result<TransactionResponse> response = await _bridge.DepositAsync(amount, accountAddress);

            if (response.IsSuccess)
            {
                // Displaying the transaction details
                Console.WriteLine($"TransactionId: {response.Value?.TransactionId}");
                Console.WriteLine($"ErrorMessage: {response.Value?.ErrorMessage}");
                Console.WriteLine($"Success: {response.Value?.Success}");
                Console.WriteLine($"Data: {response.Value?.Data}");
                Console.WriteLine($"TransactionStatus: {response.Value?.Status.ToString()}");
            }
            else
            {
                Console.WriteLine($"ErrorType:{response.Error.ErrorType.ToString()}");
                Console.WriteLine($"ErrorCode:{response.Error.Code}");
                Console.WriteLine($"ErrorMessage:{response.Error.Message}");
            }
        }
        catch (Exception ex)
        {
            // Handling errors during deposit
            Console.WriteLine($"Error in DepositTestAsync: {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronously gets the status of a transaction by its hash.
    /// </summary>
    /// <param name="transactionHash">The transaction hash to check the status of.</param>
    public async Task GetTransactionStatusTestAsync(string transactionHash)
    {
        try
        {
            // Fetching the transaction status
            Result<BridgeTransactionStatus> response = await _bridge.GetTransactionStatusAsync(transactionHash);

            if (response.IsSuccess)
            {
                // Displaying the transaction status
                Console.WriteLine($"Transaction Status: {response.Value.ToString()}");
            }
            else
            {
                Console.WriteLine($"ErrorType:{response.Error.ErrorType.ToString()}");
                Console.WriteLine($"ErrorCode:{response.Error.Code}");
                Console.WriteLine($"ErrorMessage:{response.Error.Message}");
            }
        }
        catch (Exception ex)
        {
            // Handling errors while fetching the transaction status
            Console.WriteLine($"Error in GetTransactionStatusTestAsync: {ex.Message}");
        }
    }
}