namespace SolanaBridgeTest;

/// <summary>
/// Main program class that initializes and runs tests for the Solana Bridge API.
/// Tests include account creation, balance checking, account restoration,
/// withdrawal, deposit, and transaction status checks.
/// </summary>
public class Program
{
    /// <summary>
    /// Configuration options for SolanaTechnicalAccountBridge, including 
    /// API host URI, public key, and private key for authentication.
    /// </summary>
    private static readonly SolanaTechnicalAccountBridgeOptions Options = new()
    {
        HostUri = TechAccountData.HostUri, // API URI for Solana Bridge.
        PrivateKey = TechAccountData.PrivateKey, // Private key for authentication.
        PublicKey = TechAccountData.PublicKey // Public key used for transactions.
    };

    static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
    });

    static readonly ILogger<SolanaBridge.SolanaBridge> Logger = LoggerFactory.CreateLogger<SolanaBridge.SolanaBridge>();

    private static readonly IRpcClient RpcClient = ClientFactory.GetClient(Options.HostUri);

    /// <summary>
    /// TestService instance that runs the Solana Bridge API tests using the provided options.
    /// </summary>
    private static readonly ITestService TestService =
        new TestService(Options, Logger, RpcClient);

    /// <summary>
    /// Main method that executes the tests for the Solana Bridge operations.
    /// </summary>
    public static async Task Main()
    {
        // Informing that the tests are starting.
        Console.WriteLine("Starting Solana Bridge Tests...");

        // Uncomment to run each test:
        // Test account creation.
        //ok// await TestService.CreateAccountTestAsync();
        // Test balance retrieval for the specified public key.
        //ok// await TestService.GetAccountBalanceTestAsync(Options.PublicKey);
        // Test account restoration using the provided seed phrase.
        //ok// await TestService.RestoreAccountTestAsync(TechAccountData.SeedPhrase);
        // Test withdrawal operation using a specified amount and keys.
        //ok// await TestService.WithdrawTestAsync(3, ClientAccounts.PublicKey1, ClientAccounts.PrivateKey1);
        // Test deposit operation for a specified amount and key.
        //ok// await TestService.DepositTestAsync(3, ClientAccounts.PublicKey1);
        // Test checking transaction status by providing the transaction ID.
        //ok// await TestService.GetTransactionStatusTestAsync("4VmGWf8T99P66YsUa6Y5HAN2dsBJXfgC6zGN29BrApPhogFvPKWXx5JUFhYiu3DmVSxafHJkDavbm4C7QMUsEso8");

        // Informing that the tests have been completed.
        Console.WriteLine("Tests Completed.");
        await Task.CompletedTask;
    }
}

/// <summary>
/// Contains technical account data, including public/private keys, 
/// seed phrase, and Solana API URI.
/// Used for authentication and interactions with the Solana API.
/// </summary>
file static class TechAccountData
{
    /// <summary>
    /// Seed phrase used for account recovery.
    /// </summary>
    public const string SeedPhrase = "produce link borrow junior leisure small fiscal lens grief idea balcony resemble";

    /// <summary>
    /// Public key for accessing the technical account.
    /// </summary>
    public const string PublicKey = "AfpSpMjNyoHTZWMWkog6Znf57KV82MGzkpDUUjLtmHwG";

    /// <summary>
    /// Private key for authenticating and signing transactions.
    /// </summary>
    public const string PrivateKey =
        "z5mQD+vgwzrmzSmrXicfY2rVgS3FTSVYWDNNdmg1SoePquZBys9GbCn5tEl8GvbzrWCHE87qoGj5f+PrmaiLew==";

    /// <summary>
    /// URI for the Solana API used to perform operations.
    /// </summary>
    public static readonly string HostUri = new("https://api.devnet.solana.com");
}

/// <summary>
/// Contains client account details for testing purposes, including public 
/// and private keys used to simulate real-world transactions for testing.
/// </summary>
file static class ClientAccounts
{
    /// <summary>
    /// Public key of the first client account.
    /// </summary>
    public const string PublicKey1 = "2Gtzh4ywuvxNWmtLkS8zqJ3CJpbguquuqRWJCdeZF1Jm";

    /// <summary>
    /// Private key of the first client account.
    /// </summary>
    public const string PrivateKey1 =
        "Bc1ZqYsQeepp8s5XTKM6g+cz1XMHkcimT1dBc40Ouc4S7jCLH7XQg+zZOOFAUC258ELS5etWblYas96EhyDr1g==";

    /// <summary>
    /// Public key of the second client account.
    /// </summary>
    public const string PublicKey2 = "EaRtN6BAoyfYe5aQ9MpxD3dJwFBsnP3MN9AEz2f7Mc4k";

    /// <summary>
    /// Private key of the second client account.
    /// </summary>
    public const string PrivateKey2 =
        "iqf7gS8uuDrgNygLf0zVLdDNO3kYYJxYYOkw7qle3pTJuDwmGtgS8kdMZ7FixxF6MNMYpSi1qMUVT7ZEVlHXxQ==";
}