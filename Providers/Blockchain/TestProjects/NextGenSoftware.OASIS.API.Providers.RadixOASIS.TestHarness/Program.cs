using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Oracle;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.TestHarness;

internal static class TestData
{
    // Radix Stokenet (Testnet) configuration - SAFE FOR TESTING
    // For Mainnet, use: "https://mainnet.radixdlt.com" and NetworkId = 1
    public const string HostUri = "https://stokenet.radixdlt.com";
    public const byte NetworkId = 2; // Stokenet network ID (use 1 for Mainnet)
    
    // Stokenet test account
    // Format for Stokenet: account_tdx_2_1...
    // Format for Mainnet: account_rdx1...
    // 
    // To get a Stokenet account:
    // 1. Install Radix Wallet: https://wallet.radixdlt.com/
    // 2. Create account on Stokenet network
    // 3. Export private key from wallet
    // 4. Fund at: https://faucet.radixdlt.com/
    // 5. Add details below:
    public const string AccountAddress = ""; // Add your Stokenet account address here
    
    // Private key for the test account
    public const string PrivateKey = ""; // Add your private key here (keep secure!)
}

internal static class Program
{
    private static void WriteWithTime(ConsoleColor color, string message)
    {
        Console.ForegroundColor = color;
        Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    private static void WriteSuccess(string message) => WriteWithTime(ConsoleColor.Green, $"✅ {message}");
    private static void WriteError(string message) => WriteWithTime(ConsoleColor.Red, $"❌ {message}");
    private static void WriteInfo(string message) => WriteWithTime(ConsoleColor.Cyan, $"ℹ️  {message}");
    private static void WriteWarning(string message) => WriteWithTime(ConsoleColor.Yellow, $"⚠️  {message}");

    private static void WriteColored(string label, string value, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write($"[{DateTime.Now:HH:mm:ss}] {label}: ");
        Console.ResetColor();
        Console.WriteLine(string.IsNullOrWhiteSpace(value) ? "<empty>" : value);
    }

    /// <summary>
    /// Test provider activation and basic connection
    /// </summary>
    private static async Task Run_TestProviderActivation()
    {
        WriteInfo("=== Testing Provider Activation ===");
        
        RadixOASIS radixOASIS = new(TestData.HostUri, TestData.NetworkId, TestData.AccountAddress, TestData.PrivateKey);
        
        WriteInfo("Activating Radix provider...");
        var activateResult = await radixOASIS.ActivateProviderAsync();
        
        if (activateResult.IsError)
        {
            WriteError($"Failed to activate provider: {activateResult.Message}");
            return;
        }
        
        WriteSuccess("Provider activated successfully!");
        WriteColored("Provider Name", radixOASIS.ProviderName, ConsoleColor.Green);
        WriteColored("Provider Type", radixOASIS.ProviderType?.Name ?? "Unknown", ConsoleColor.Cyan);
        WriteColored("Is Activated", radixOASIS.IsProviderActivated.ToString(), ConsoleColor.Magenta);
        
        WriteInfo("Deactivating provider...");
        await radixOASIS.DeActivateProviderAsync();
        WriteSuccess("Provider deactivated.");
    }

    /// <summary>
    /// Test getting account balance
    /// </summary>
    private static async Task Run_TestGetAccountBalance()
    {
        WriteInfo("=== Testing Get Account Balance ===");
        
        RadixOASIS radixOASIS = new(TestData.HostUri, TestData.NetworkId, TestData.AccountAddress, TestData.PrivateKey);
        
        WriteInfo("Activating Radix provider...");
        await radixOASIS.ActivateProviderAsync();
        
        if (radixOASIS.RadixBridgeService == null)
        {
            WriteError("RadixBridgeService is null!");
            return;
        }
        
        WriteInfo($"Getting balance for account: {TestData.AccountAddress}...");
        var balanceResult = await radixOASIS.RadixBridgeService.GetAccountBalanceAsync(TestData.AccountAddress);
        
        if (balanceResult.IsError)
        {
            WriteError($"Failed to get balance: {balanceResult.Message}");
            return;
        }
        
        WriteSuccess($"Balance retrieved successfully!");
        WriteColored("Balance", balanceResult.Result?.ToString() ?? "0", ConsoleColor.Green);
        
        await radixOASIS.DeActivateProviderAsync();
    }

    /// <summary>
    /// Test sending a transaction
    /// </summary>
    private static async Task Run_TestSendTransaction()
    {
        WriteInfo("=== Testing Send Transaction ===");
        
        RadixOASIS radixOASIS = new(TestData.HostUri, TestData.NetworkId, TestData.AccountAddress, TestData.PrivateKey);
        
        WriteInfo("Activating Radix provider...");
        await radixOASIS.ActivateProviderAsync();
        
        // TODO: Replace with a valid recipient address for testing
        string recipientAddress = "account_tdx_2_1yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy";
        decimal amount = 0.1m; // Small test amount
        
        WriteInfo($"Sending {amount} XRD to {recipientAddress}...");
        var sendResult = await radixOASIS.SendTransactionAsync(
            fromWalletAddress: TestData.AccountAddress,
            toWalletAddress: recipientAddress,
            amount: amount,
            memoText: "Test transaction from RadixOASIS TestHarness"
        );
        
        if (sendResult.IsError)
        {
            WriteError($"Failed to send transaction: {sendResult.Message}");
            return;
        }
        
        WriteSuccess("Transaction sent successfully!");
        WriteColored("Transaction Hash", sendResult.Result?.TransactionResult ?? "Unknown", ConsoleColor.Green);
        
        await radixOASIS.DeActivateProviderAsync();
    }

    /// <summary>
    /// Test Oracle Node - Chain State
    /// </summary>
    private static async Task Run_TestOracleChainState()
    {
        WriteInfo("=== Testing Oracle Node - Chain State ===");
        
        RadixOASIS radixOASIS = new(TestData.HostUri, TestData.NetworkId, TestData.AccountAddress, TestData.PrivateKey);
        
        WriteInfo("Activating Radix provider...");
        await radixOASIS.ActivateProviderAsync();
        
        if (radixOASIS.OracleNode == null)
        {
            WriteError("OracleNode is null!");
            return;
        }
        
        WriteInfo("Starting Oracle Node...");
        var startResult = await radixOASIS.OracleNode.StartAsync();
        
        if (startResult.IsError)
        {
            WriteError($"Failed to start Oracle Node: {startResult.Message}");
            return;
        }
        
        WriteSuccess("Oracle Node started!");
        
        WriteInfo("Getting chain state...");
        var chainStateRequest = new RadixOracleNode.OracleDataRequest
        {
            DataType = "chainstate"
        };
        
        var chainStateResult = await radixOASIS.OracleNode.GetOracleDataAsync(chainStateRequest);
        
        if (chainStateResult.IsError)
        {
            WriteError($"Failed to get chain state: {chainStateResult.Message}");
        }
        else
        {
            WriteSuccess("Chain state retrieved successfully!");
            WriteColored("Chain State Result", chainStateResult.Result?.ToString() ?? "Unknown", ConsoleColor.Green);
        }
        
        WriteInfo("Stopping Oracle Node...");
        await radixOASIS.OracleNode.StopAsync();
        WriteSuccess("Oracle Node stopped.");
        
        await radixOASIS.DeActivateProviderAsync();
    }

    /// <summary>
    /// Test Oracle Node - Latest Epoch
    /// </summary>
    private static async Task Run_TestOracleLatestEpoch()
    {
        WriteInfo("=== Testing Oracle Node - Latest Epoch ===");
        
        RadixOASIS radixOASIS = new(TestData.HostUri, TestData.NetworkId, TestData.AccountAddress, TestData.PrivateKey);
        
        WriteInfo("Activating Radix provider...");
        await radixOASIS.ActivateProviderAsync();
        
        if (radixOASIS.ChainObserver == null)
        {
            WriteError("ChainObserver is null!");
            return;
        }
        
        WriteInfo("Connecting Chain Observer...");
        var connectResult = await radixOASIS.ChainObserver.ConnectAsync();
        
        if (connectResult.IsError)
        {
            WriteError($"Failed to connect Chain Observer: {connectResult.Message}");
            return;
        }
        
        WriteSuccess("Chain Observer connected!");
        
        WriteInfo("Getting latest epoch...");
        var epochResult = await radixOASIS.ChainObserver.GetLatestEpochAsync();
        
        if (epochResult.IsError)
        {
            WriteError($"Failed to get latest epoch: {epochResult.Message}");
        }
        else
        {
            WriteSuccess($"Latest epoch retrieved: {epochResult.Result}");
            WriteColored("Latest Epoch", epochResult.Result.ToString(), ConsoleColor.Green);
        }
        
        await radixOASIS.ChainObserver.DisconnectAsync();
        await radixOASIS.DeActivateProviderAsync();
    }

    /// <summary>
    /// Test Oracle Node - Price Feed
    /// </summary>
    private static async Task Run_TestOraclePriceFeed()
    {
        WriteInfo("=== Testing Oracle Node - Price Feed ===");
        
        RadixOASIS radixOASIS = new(TestData.HostUri, TestData.NetworkId, TestData.AccountAddress, TestData.PrivateKey);
        
        WriteInfo("Activating Radix provider...");
        await radixOASIS.ActivateProviderAsync();
        
        if (radixOASIS.OracleNode == null)
        {
            WriteError("OracleNode is null!");
            return;
        }
        
        WriteInfo("Starting Oracle Node...");
        var startResult = await radixOASIS.OracleNode.StartAsync();
        
        if (startResult.IsError)
        {
            WriteError($"Failed to start Oracle Node: {startResult.Message}");
            return;
        }
        
        WriteSuccess("Oracle Node started!");
        
        WriteInfo("Getting XRD price feed...");
        var priceRequest = new RadixOracleNode.OracleDataRequest
        {
            DataType = "price",
            TokenSymbol = "XRD",
            Currency = "USD"
        };
        
        var priceResult = await radixOASIS.OracleNode.GetOracleDataAsync(priceRequest);
        
        if (priceResult.IsError)
        {
            WriteWarning($"Price feed not available (may need external integration): {priceResult.Message}");
        }
        else
        {
            WriteSuccess("Price feed retrieved successfully!");
            WriteColored("Price Feed Result", priceResult.Result?.ToString() ?? "Unknown", ConsoleColor.Green);
        }
        
        WriteInfo("Stopping Oracle Node...");
        await radixOASIS.OracleNode.StopAsync();
        WriteSuccess("Oracle Node stopped.");
        
        await radixOASIS.DeActivateProviderAsync();
    }

    /// <summary>
    /// Test Oracle Node - Chain Health
    /// </summary>
    private static async Task Run_TestOracleChainHealth()
    {
        WriteInfo("=== Testing Oracle Node - Chain Health ===");
        
        RadixOASIS radixOASIS = new(TestData.HostUri, TestData.NetworkId, TestData.AccountAddress, TestData.PrivateKey);
        
        WriteInfo("Activating Radix provider...");
        await radixOASIS.ActivateProviderAsync();
        
        if (radixOASIS.ChainObserver == null)
        {
            WriteError("ChainObserver is null!");
            return;
        }
        
        WriteInfo("Connecting Chain Observer...");
        var connectResult = await radixOASIS.ChainObserver.ConnectAsync();
        
        if (connectResult.IsError)
        {
            WriteError($"Failed to connect Chain Observer: {connectResult.Message}");
            return;
        }
        
        WriteSuccess("Chain Observer connected!");
        
        WriteInfo("Getting chain health...");
        var healthResult = await radixOASIS.ChainObserver.GetChainHealthAsync();
        
        if (healthResult.IsError)
        {
            WriteError($"Failed to get chain health: {healthResult.Message}");
        }
        else
        {
            WriteSuccess("Chain health retrieved successfully!");
            if (healthResult.Result != null)
            {
                WriteColored("Chain Name", healthResult.Result.ChainName, ConsoleColor.Green);
                WriteColored("Is Healthy", healthResult.Result.IsHealthy.ToString(), ConsoleColor.Cyan);
                WriteColored("Status Message", healthResult.Result.StatusMessage, ConsoleColor.Magenta);
                WriteColored("Last Checked", healthResult.Result.LastChecked.ToString(), ConsoleColor.Blue);
            }
        }
        
        await radixOASIS.ChainObserver.DisconnectAsync();
        await radixOASIS.DeActivateProviderAsync();
    }

    /// <summary>
    /// Test Bridge Operations - Deposit
    /// </summary>
    private static async Task Run_TestBridgeDeposit()
    {
        WriteInfo("=== Testing Bridge Operations - Deposit ===");
        
        RadixOASIS radixOASIS = new(TestData.HostUri, TestData.NetworkId, TestData.AccountAddress, TestData.PrivateKey);
        
        WriteInfo("Activating Radix provider...");
        await radixOASIS.ActivateProviderAsync();
        
        if (radixOASIS.RadixBridgeService == null)
        {
            WriteError("RadixBridgeService is null!");
            return;
        }
        
        // TODO: Replace with a valid recipient address for testing
        string recipientAddress = "account_tdx_2_1yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy";
        decimal amount = 0.1m;
        
        WriteInfo($"Depositing {amount} XRD to {recipientAddress}...");
        var depositResult = await radixOASIS.RadixBridgeService.DepositAsync(amount, recipientAddress);
        
        if (depositResult.IsError)
        {
            WriteError($"Failed to deposit: {depositResult.Message}");
            return;
        }
        
        WriteSuccess("Deposit completed successfully!");
        if (depositResult.Result != null)
        {
            WriteColored("Transaction Hash", depositResult.Result.TransactionHash ?? depositResult.Result.IntentHash ?? "Unknown", ConsoleColor.Green);
            WriteColored("Intent Hash", depositResult.Result.IntentHash ?? "Unknown", ConsoleColor.Cyan);
        }
        
        await radixOASIS.DeActivateProviderAsync();
    }

    /// <summary>
    /// Run all tests
    /// </summary>
    private static async Task Main(string[] args)
    {
        WriteInfo("=== Starting RadixOASIS Test Harness ===");
        WriteInfo($"Host URI: {TestData.HostUri}");
        WriteInfo($"Network ID: {TestData.NetworkId}");
        WriteInfo($"Account Address: {TestData.AccountAddress}");
        WriteInfo("");
        
        // Check if test data is configured
        if (string.IsNullOrEmpty(TestData.AccountAddress) || string.IsNullOrEmpty(TestData.PrivateKey))
        {
            WriteWarning("⚠️  WARNING: Test account not configured!");
            WriteInfo("");
            WriteInfo("To get a Stokenet account:");
            WriteInfo("  1. Install Radix Wallet: https://wallet.radixdlt.com/");
            WriteInfo("  2. Create account on Stokenet (testnet) network");
            WriteInfo("  3. Export private key from wallet settings");
            WriteInfo("  4. Fund account at: https://faucet.radixdlt.com/");
            WriteInfo("  5. Update TestData in Program.cs with:");
            WriteInfo("     - AccountAddress: your Stokenet address (account_tdx_2_1...)");
            WriteInfo("     - PrivateKey: your exported private key");
            WriteInfo("");
            WriteInfo("See STOKENET_ACCOUNT_SETUP.md for detailed instructions.");
            WriteInfo("");
            return; // Exit early since we need account details
        }
        
        WriteSuccess("✅ Test account configured!");
        WriteInfo($"Using Stokenet (testnet) - safe for testing");
        WriteInfo("");
        
        try
        {
            // Basic provider tests
            await Run_TestProviderActivation();
            WriteInfo("");
            
            // Account operations
            await Run_TestGetAccountBalance();
            WriteInfo("");
            
            // Transaction tests
            // await Run_TestSendTransaction(); // Uncomment when you have a valid recipient address
            // WriteInfo("");
            
            // Oracle tests
            await Run_TestOracleChainState();
            WriteInfo("");
            
            await Run_TestOracleLatestEpoch();
            WriteInfo("");
            
            await Run_TestOracleChainHealth();
            WriteInfo("");
            
            await Run_TestOraclePriceFeed();
            WriteInfo("");
            
            // Bridge tests
            // await Run_TestBridgeDeposit(); // Uncomment when you have a valid recipient address
            // WriteInfo("");
            
            WriteSuccess("=== All Tests Completed ===");
        }
        catch (Exception ex)
        {
            WriteError($"Unexpected error: {ex.Message}");
            WriteError($"Stack trace: {ex.StackTrace}");
        }
    }
}
