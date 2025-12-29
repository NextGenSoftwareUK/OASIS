namespace RadixBridgeTest.Services;

/// <summary>
/// Service for testing interactions with the RadixBridge API.
/// Includes methods for creating accounts, checking balances, restoring accounts,
/// making withdrawals, deposits, and checking transaction statuses.
/// </summary>
public class TestService : ITestService
{
    private readonly IRadixBridge _bridge;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestService"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the RadixBridge.</param>
    /// <param name="httpClient">HTTP client used for API calls.</param>
    /// <param name="logger"></param>
    public TestService(
        IOptions<RadixTechnicalAccountBridgeOptions> options,
        HttpClient httpClient,
        ILogger<RadixBridge.RadixBridge> logger)
        => _bridge = new RadixBridge.RadixBridge(options.Value, httpClient, logger);


    /// <summary>
    /// Creates a new account by calling the RadixBridge API, logs the generated keys and seed phrase.
    /// </summary>
    public async Task CreateAccountTestAsync()
    {
        TryExecute(() =>
        {
            Result<(PublicKey PublicKey, PrivateKey PrivateKey, string SeedPhrase)> response =
                _bridge.CreateAccountAsync();
            if (!response.IsSuccess)
            {
                Console.WriteLine($"ErrorType:{response.Error.ErrorType.ToString()}");
                Console.WriteLine($"ErrorCode:{response.Error.Code}");
                Console.WriteLine($"ErrorMessage:{response.Error.Message}");
            }
            else
            {
                LogAccountDetails(response.Value.PrivateKey, response.Value.SeedPhrase);
            }
        });
        await Task.CompletedTask;
    }

    /// <summary>
    /// Retrieves the account balance for a specified account.
    /// </summary>
    /// <param name="accountAddress">The address of the account to check the balance of.</param>
    public async Task GetAccountBalanceTestAsync(string accountAddress)
    {
        await TryExecuteAsync(async () =>
        {
            Result<decimal> response = await _bridge.GetAccountBalanceAsync(accountAddress);
            if (!response.IsSuccess)
            {
                Console.WriteLine($"ErrorType:{response.Error.ErrorType.ToString()}");
                Console.WriteLine($"ErrorCode:{response.Error.Code}");
                Console.WriteLine($"ErrorMessage:{response.Error.Message}");
            }
            else
            {
                Console.WriteLine($"Account Balance: {response.Value} XRD");
            }
        });
    }

    /// <summary>
    /// Restores an account from a given seed phrase, logs the restored keys.
    /// </summary>
    /// <param name="seedPhrase">The seed phrase used to restore the account.</param>
    public async Task RestoreAccountTestAsync(string seedPhrase)
    {
        TryExecute(() =>
        {
            Result<(PublicKey PublicKey, PrivateKey PrivateKey)> response = _bridge.RestoreAccountAsync(seedPhrase);

            if (!response.IsSuccess)
            {
                Console.WriteLine($"ErrorType:{response.Error.ErrorType.ToString()}");
                Console.WriteLine($"ErrorCode:{response.Error.Code}");
                Console.WriteLine($"ErrorMessage:{response.Error.Message}");
            }
            else
            {
                LogAccountDetails(response.Value.PrivateKey, null);
            }
        });
        await Task.CompletedTask;
    }

    /// <summary>
    /// Initiates a withdrawal process for the specified amount and logs transaction details.
    /// </summary>
    /// <param name="amount">The amount to withdraw.</param>
    /// <param name="accountAddress">The address of the account to withdraw from.</param>
    /// <param name="clientPrivateKey">The private key used to authorize the withdrawal.</param>
    public async Task WithdrawTestAsync(decimal amount, string accountAddress, string clientPrivateKey)
    {
        await TryExecuteAsync(async () =>
        {
            Result<TransactionResponse>
                response = await _bridge.WithdrawAsync(amount, accountAddress, clientPrivateKey);
            if (!response.IsSuccess)
            {
                Console.WriteLine($"ErrorType:{response.Error.ErrorType.ToString()}");
                Console.WriteLine($"ErrorCode:{response.Error.Code}");
                Console.WriteLine($"ErrorMessage:{response.Error.Message}");
            }
            else
            {
                LogTransactionDetails(response.Value!);
            }
        });
    }

    /// <summary>
    /// Initiates a deposit process for the specified amount and logs transaction details.
    /// </summary>
    /// <param name="amount">The amount to deposit.</param>
    /// <param name="accountAddress">The address of the account to deposit to.</param>
    public async Task DepositTestAsync(decimal amount, string accountAddress)
    {
        await TryExecuteAsync(async () =>
        {
            Result<TransactionResponse>
                response = await _bridge.DepositAsync(amount, accountAddress);
            if (!response.IsSuccess)
            {
                Console.WriteLine($"ErrorType:{response.Error.ErrorType.ToString()}");
                Console.WriteLine($"ErrorCode:{response.Error.Code}");
                Console.WriteLine($"ErrorMessage:{response.Error.Message}");
            }
            else
            {
                LogTransactionDetails(response.Value!);
            }
        });
    }

    /// <summary>
    /// Retrieves and logs the status of a transaction given its transaction hash.
    /// </summary>
    /// <param name="transactionHash">The unique hash of the transaction to check.</param>
    public async Task GetTransactionStatusTestAsync(string transactionHash)
    {
        await TryExecuteAsync(async () =>
        {
            Result<BridgeTransactionStatus> response = await _bridge.GetTransactionStatusAsync(transactionHash);
            if (!response.IsSuccess)
            {
                Console.WriteLine($"ErrorType:{response.Error.ErrorType.ToString()}");
                Console.WriteLine($"ErrorCode:{response.Error.Code}");
                Console.WriteLine($"ErrorMessage:{response.Error.Message}");
            }
            else
            {
                Console.WriteLine($"Transaction Status: {response.Value.ToString()}");
            }
        });
    }

    /// <summary>
    /// Generates an address based on the public key, address type, and network type, and logs the address.
    /// </summary>
    /// <param name="publicKey">The public key to generate the address from.</param>
    /// <param name="addressType">The type of address to generate (e.g., Account, Identity).</param>
    /// <param name="networkType">The network type (e.g., MainNet, StokeNet).</param>
    public async Task GetAddressTestAsync(PublicKey publicKey, AddressType addressType, NetworkType networkType)
    {
        TryExecute(() =>
        {
            Result<string> response = _bridge.GetAddressAsync(publicKey, addressType, networkType);
            if (!response.IsSuccess)
            {
                Console.WriteLine($"ErrorType:{response.Error.ErrorType.ToString()}");
                Console.WriteLine($"ErrorCode:{response.Error.Code}");
                Console.WriteLine($"ErrorMessage:{response.Error.Message}");
            }
            else
            {
                Console.WriteLine($"Generated Address: {response.Value}");
                Console.WriteLine($"Address Type: {addressType}");
                Console.WriteLine($"Network Type: {networkType}");
            }
        });
        await Task.CompletedTask;
    }

    #region Helper Methods

    /// <summary>
    /// Helper method to execute actions with exception handling for synchronous calls.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    private void TryExecute(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to execute actions with exception handling for asynchronous calls.
    /// </summary>
    /// <param name="action">The asynchronous action to execute.</param>
    private async Task TryExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Logs account details (public and private keys, seed phrase).
    /// </summary>
    /// <param name="privateKey">The private key to log.</param>
    /// <param name="seedPhrase">The seed phrase to log.</param>
    private void LogAccountDetails(PrivateKey privateKey, string? seedPhrase)
    {
        Console.WriteLine($"PublicKey: {Encoders.Hex.EncodeData(privateKey.PublicKeyBytes())}");
        Console.WriteLine($"PrivateKey: {privateKey.RawHex()}");
        if (seedPhrase != null)
        {
            Console.WriteLine($"SeedPhrase: {seedPhrase}");
        }
    }

    /// <summary>
    /// Logs transaction details (TransactionId, Success, ErrorMessage).
    /// </summary>
    /// <param name="transactionResponse">The transaction response to log.</param>
    private void LogTransactionDetails(TransactionResponse transactionResponse)
    {
        Console.WriteLine($"TransactionId: {transactionResponse.TransactionId}");
        Console.WriteLine($"Success: {transactionResponse.Success}");
        Console.WriteLine($"ErrorMessage: {transactionResponse.ErrorMessage}");
        Console.WriteLine($"Status: {transactionResponse.Status.ToString()}");
        Console.WriteLine($"Data: {transactionResponse.Data}");
    }

    #endregion
}