# RadixOASIS Test Harness

Test harness for the RadixOASIS provider, allowing you to test various operations including provider activation, transactions, oracle operations, and bridge functionality.

## Prerequisites

1. **Radix Account**: You need a Radix account with:
   - Account address (format: `account_tdx_2_1...`)
   - Private key (hex format)

2. **Network Access**: Access to Radix Stokenet (testnet) or Mainnet

3. **.NET 8.0 SDK**: Required to build and run the test harness

## Configuration

### Option 1: Generate Account Using Radix Wallet (Recommended)

1. **Install Radix Wallet**: Download from https://wallet.radixdlt.com/
2. **Create Stokenet Account**:
   - Open Radix Wallet
   - Switch to Stokenet (testnet) network
   - Create a new account
   - Copy the account address (format: `account_tdx_2_1...`)
   - Export the private key (keep it secure!)
3. **Fund the Account**: Visit https://faucet.radixdlt.com/ to get free test XRD
4. **Update TestData** in `Program.cs`:

```csharp
internal static class TestData
{
    public const string HostUri = "https://stokenet.radixdlt.com";
    public const byte NetworkId = 2; // Stokenet
    
    // Your Stokenet account details
    public const string AccountAddress = "account_tdx_2_1..."; // Your address
    public const string PrivateKey = "your_private_key_here"; // Your private key
}
```

### Option 2: Use Pre-Generated Test Account

For quick testing, you can use this pre-generated Stokenet account:

**⚠️ WARNING: This is a TEST account only. Do NOT use for production!**

```csharp
public const string AccountAddress = "account_tdx_2_1xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
public const string PrivateKey = "your_private_key_here";
```

**Note**: You'll need to generate your own account and private key using the Radix Wallet.

## Available Tests

### 1. **Provider Activation Test** (`Run_TestProviderActivation`)
- Tests provider initialization and activation
- Verifies provider properties and state
- Tests provider deactivation

### 2. **Get Account Balance** (`Run_TestGetAccountBalance`)
- Retrieves XRD balance for the configured account
- Tests the RadixBridgeService integration

### 3. **Send Transaction** (`Run_TestSendTransaction`)
- Sends XRD from one account to another
- Tests the `SendTransactionAsync` method
- **Note**: Requires a valid recipient address

### 4. **Oracle Chain State** (`Run_TestOracleChainState`)
- Tests the first-party oracle node
- Retrieves current chain state
- Tests oracle node start/stop functionality

### 5. **Oracle Latest Epoch** (`Run_TestOracleLatestEpoch`)
- Tests the chain observer
- Retrieves the latest epoch number
- Tests chain observer connection/disconnection

### 6. **Oracle Chain Health** (`Run_TestOracleChainHealth`)
- Tests chain health monitoring
- Retrieves health status and metrics
- Tests chain observer health checks

### 7. **Oracle Price Feed** (`Run_TestOraclePriceFeed`)
- Tests price feed retrieval (XRD/USD)
- **Note**: May require external price source integration

### 8. **Bridge Deposit** (`Run_TestBridgeDeposit`)
- Tests bridge deposit operations
- Sends XRD via bridge service
- **Note**: Requires a valid recipient address

## Running Tests

### Build the Project

```bash
cd Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.RadixOASIS.TestHarness
dotnet build
```

### Run All Tests

```bash
dotnet run
```

### Run Specific Tests

Edit `Program.cs` in the `Main` method to uncomment specific test methods:

```csharp
private static async Task Main(string[] args)
{
    // Uncomment the tests you want to run:
    await Run_TestProviderActivation();
    await Run_TestGetAccountBalance();
    // await Run_TestSendTransaction();
    // etc...
}
```

## Test Output

The test harness provides color-coded output:

- ✅ **Green**: Success messages
- ❌ **Red**: Error messages
- ℹ️ **Cyan**: Information messages
- ⚠️ **Yellow**: Warning messages

Each test includes timestamps and detailed output for debugging.

## Example Output

```
[14:30:15] ℹ️  === Starting RadixOASIS Test Harness ===
[14:30:15] ℹ️  Host URI: https://stokenet.radixdlt.com
[14:30:15] ℹ️  Network ID: 2
[14:30:15] ℹ️  Account Address: account_tdx_2_1...
[14:30:15] ℹ️  
[14:30:15] ℹ️  === Testing Provider Activation ===
[14:30:16] ℹ️  Activating Radix provider...
[14:30:17] ✅ Provider activated successfully!
[14:30:17] Provider Name: RadixOASIS
[14:30:17] Provider Type: RadixOASIS
[14:30:17] Is Activated: True
```

## Important Notes

1. **Test Data**: Always use test accounts and testnet for development. Never use mainnet private keys in test code.

2. **Transaction Tests**: Some tests (like `SendTransaction` and `BridgeDeposit`) require valid recipient addresses. Update the recipient addresses in the test methods before running.

3. **Price Feeds**: The price feed test may show warnings if external price sources aren't configured. This is expected and doesn't indicate a failure.

4. **Network Selection**: 
   - **Stokenet (Testnet)**: `https://stokenet.radixdlt.com`, NetworkId = 2
   - **Mainnet**: `https://mainnet.radixdlt.com`, NetworkId = 1

5. **Error Handling**: All tests include error handling and will display helpful error messages if something goes wrong.

## Troubleshooting

### "Failed to activate provider"
- Check that your account address and private key are correct
- Verify network connectivity to Radix network
- Ensure the network ID matches the HostUri (2 for Stokenet, 1 for Mainnet)

### "RadixBridgeService is null"
- Ensure provider is activated before accessing services
- Check that `ActivateProviderAsync()` completed successfully

### "OracleNode is null"
- Ensure provider is activated
- Oracle node is initialized during provider activation

### Transaction Failures
- Verify you have sufficient XRD balance
- Check that recipient addresses are valid Radix addresses
- Ensure you're using the correct network (testnet vs mainnet)

## Next Steps

After running the test harness successfully, you can:

1. Integrate RadixOASIS into your applications
2. Use the oracle node for real-time chain data
3. Implement bridge operations for cross-chain functionality
4. Build on top of the first-party oracle capabilities

## Support

For issues or questions:
- Check the main OASIS documentation
- Review the RadixOASIS provider implementation
- Check Radix network status and documentation

---

**Version**: 1.0  
**Last Updated**: January 2025

