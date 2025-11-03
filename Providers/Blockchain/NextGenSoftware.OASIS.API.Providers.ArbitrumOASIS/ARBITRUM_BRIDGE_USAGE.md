# Arbitrum OASIS Bridge Usage Guide

## Overview

The **ArbitrumOASIS Bridge** provides full cross-chain bridge functionality for the Arbitrum blockchain, enabling seamless integration with the OASIS cross-chain bridge system.

## Features

- ✅ **Account Management** - Create and restore Arbitrum accounts
- ✅ **Balance Queries** - Check account balances in ETH
- ✅ **Withdrawals** - Transfer ETH to technical account for bridge swaps
- ✅ **Deposits** - Receive ETH from bridge swaps
- ✅ **Transaction Status** - Track transaction confirmations
- ✅ **Atomic Swaps** - Fully integrated with CrossChainBridgeManager

## Architecture

```
ArbitrumOASIS
    └── BridgeService (IArbitrumBridgeService)
        ├── GetAccountBalanceAsync()
        ├── CreateAccountAsync()
        ├── RestoreAccountAsync()
        ├── WithdrawAsync()
        ├── DepositAsync()
        └── GetTransactionStatusAsync()
```

## Quick Start

### 1. Initialize Arbitrum OASIS Provider

```csharp
using NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;

// Initialize provider with connection details
var arbitrumProvider = new ArbitrumOASIS(
    hostUri: "https://arb1.arbitrum.io/rpc",
    chainPrivateKey: "YOUR_TECHNICAL_ACCOUNT_PRIVATE_KEY",
    chainId: 42161, // Arbitrum One mainnet
    contractAddress: "YOUR_OASIS_CONTRACT_ADDRESS"
);

// Activate the provider
var activationResult = await arbitrumProvider.ActivateProviderAsync();
if (activationResult.IsError)
{
    Console.WriteLine($"Failed to activate: {activationResult.Message}");
    return;
}
```

### 2. Access Bridge Service

```csharp
// Get the bridge service from the provider
var bridgeService = arbitrumProvider.BridgeService;
```

### 3. Create a New Arbitrum Account

```csharp
var createResult = await bridgeService.CreateAccountAsync();
if (!createResult.IsError)
{
    var (publicKey, privateKey, seedPhrase) = createResult.Result;
    Console.WriteLine($"Address: {publicKey}");
    Console.WriteLine($"Private Key: {privateKey}");
    Console.WriteLine($"Seed Phrase: {seedPhrase}");
    
    // IMPORTANT: Store these securely!
}
```

### 4. Check Account Balance

```csharp
string accountAddress = "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb";

var balanceResult = await bridgeService.GetAccountBalanceAsync(accountAddress);
if (!balanceResult.IsError)
{
    Console.WriteLine($"Balance: {balanceResult.Result} ETH");
}
```

### 5. Perform a Withdrawal (Bridge Swap)

```csharp
// Withdraw 0.5 ETH from user account to bridge
var withdrawResult = await bridgeService.WithdrawAsync(
    amount: 0.5m,
    senderAccountAddress: "0xUSER_ADDRESS",
    senderPrivateKey: "USER_PRIVATE_KEY"
);

if (!withdrawResult.IsError)
{
    var transaction = withdrawResult.Result;
    Console.WriteLine($"Withdrawal TxID: {transaction.TransactionId}");
    Console.WriteLine($"Status: {transaction.Status}");
    Console.WriteLine($"Successful: {transaction.IsSuccessful}");
}
```

### 6. Perform a Deposit (Complete Bridge Swap)

```csharp
// Deposit 10.5 XRD equivalent as ETH to destination account
var depositResult = await bridgeService.DepositAsync(
    amount: 0.048m, // Calculated from exchange rate
    receiverAccountAddress: "0xDESTINATION_ADDRESS"
);

if (!depositResult.IsError)
{
    var transaction = depositResult.Result;
    Console.WriteLine($"Deposit TxID: {transaction.TransactionId}");
    Console.WriteLine($"Status: {transaction.Status}");
}
```

### 7. Check Transaction Status

```csharp
string txHash = "0xTRANSACTION_HASH";

var statusResult = await bridgeService.GetTransactionStatusAsync(txHash);
if (!statusResult.IsError)
{
    Console.WriteLine($"Transaction Status: {statusResult.Result}");
    // Possible values: Pending, Completed, NotFound
}
```

## Full Atomic Swap Example

```csharp
using NextGenSoftware.OASIS.API.Core.Managers.Bridge;
using NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;
using NextGenSoftware.OASIS.API.Providers.SolanaOASIS;

// Initialize both providers
var arbitrumProvider = new ArbitrumOASIS(
    "https://arb1.arbitrum.io/rpc",
    "TECHNICAL_PRIVATE_KEY",
    42161,
    "CONTRACT_ADDRESS"
);

var solanaProvider = new SolanaOASIS(
    "https://api.mainnet-beta.solana.com",
    "TECHNICAL_KEYPAIR_JSON"
);

await arbitrumProvider.ActivateProviderAsync();
await solanaProvider.ActivateProviderAsync();

// Initialize bridge manager with real exchange rate service
var bridgeManager = new CrossChainBridgeManager(
    solanaOASIS: solanaProvider.BridgeService,
    arbitrumBridge: arbitrumProvider.BridgeService
);

// Perform atomic swap: SOL → ARB (ETH)
var swapResult = await bridgeManager.SwapFromSolanaToRadixAsync(
    amount: 1.0m, // 1 SOL
    senderSolanaAddress: "USER_SOLANA_ADDRESS",
    senderSolanaPrivateKey: "USER_SOLANA_PRIVATE_KEY",
    receiverRadixAddress: "0xUSER_ARBITRUM_ADDRESS"
);

if (!swapResult.IsError)
{
    Console.WriteLine($"Swap completed successfully!");
    Console.WriteLine($"Withdraw TxID: {swapResult.Result.WithdrawTransactionId}");
    Console.WriteLine($"Deposit TxID: {swapResult.Result.DepositTransactionId}");
    Console.WriteLine($"Exchange Rate: {swapResult.Result.ExchangeRate}");
    Console.WriteLine($"Amount Received: {swapResult.Result.ReceivedAmount} ETH");
}
else
{
    Console.WriteLine($"Swap failed: {swapResult.Message}");
}
```

## Integration with CrossChainBridgeManager

The Arbitrum bridge seamlessly integrates with the `CrossChainBridgeManager` for atomic swaps:

```csharp
// The bridge manager handles:
// 1. Getting real-time exchange rates (SOL/ARB)
// 2. Withdrawing from source chain
// 3. Calculating destination amount
// 4. Depositing to destination chain
// 5. Automatic rollback on failure

var bridgeManager = new CrossChainBridgeManager(
    solanaBridge: solanaProvider.BridgeService,
    radixBridge: arbitrumProvider.BridgeService  // Works with any IOASISBridge
);

// Get current exchange rate
var rateResult = await bridgeManager.GetExchangeRateAsync("ARB", "SOL");
Console.WriteLine($"1 ARB = {rateResult.Result} SOL");
```

## Supported Networks

### Arbitrum One (Mainnet)
- Chain ID: `42161`
- RPC: `https://arb1.arbitrum.io/rpc`
- Native Token: ETH
- Block Explorer: https://arbiscan.io

### Arbitrum Nova
- Chain ID: `42170`
- RPC: `https://nova.arbitrum.io/rpc`
- Native Token: ETH
- Block Explorer: https://nova.arbiscan.io

### Arbitrum Goerli (Testnet)
- Chain ID: `421613`
- RPC: `https://goerli-rollup.arbitrum.io/rpc`
- Native Token: ETH (testnet)
- Block Explorer: https://goerli.arbiscan.io

## Exchange Rate Support

The Arbitrum bridge is integrated with the `CoinGeckoExchangeRateService` which supports:

- **ARB** ↔ **SOL**
- **ARB** ↔ **XRD**
- **ARB** ↔ **ETH**
- **ARB** ↔ **BTC**
- **ARB** ↔ **USDC**
- **ARB** ↔ **USDT**

Exchange rates are cached for 5 minutes to reduce API calls.

## Error Handling

All bridge methods return `OASISResult<T>` which includes:

```csharp
var result = await bridgeService.GetAccountBalanceAsync(address);

if (result.IsError)
{
    Console.WriteLine($"Error: {result.Message}");
    if (result.Exception != null)
    {
        Console.WriteLine($"Exception: {result.Exception.Message}");
    }
}
else
{
    Console.WriteLine($"Success: {result.Result}");
}
```

## Transaction Response Structure

```csharp
public class BridgeTransactionResponse
{
    public string TransactionId { get; set; }           // Transaction hash
    public string? DuplicateTransactionId { get; set; } // Optional duplicate ID
    public bool IsSuccessful { get; set; }              // True if confirmed
    public string? ErrorMessage { get; set; }           // Error details if failed
    public BridgeTransactionStatus Status { get; set; } // Completed, Pending, etc.
}
```

## Transaction Status Values

```csharp
public enum BridgeTransactionStatus
{
    Completed,           // Transaction succeeded
    Pending,             // Waiting for confirmation
    Canceled,            // Transaction canceled
    Expired,             // Transaction expired
    InsufficientFunds,   // Not enough balance
    SufficientFunds,     // Balance verified
    InsufficientFundsForFee, // Can't pay gas
    NotFound             // Transaction not found or failed
}
```

## Security Considerations

1. **Private Keys**: Never hardcode private keys. Use secure key management (Azure Key Vault, AWS KMS, etc.)

2. **Technical Account**: The bridge service requires a "technical account" that holds funds temporarily during swaps
   - This account should have sufficient ETH for gas fees
   - Implement proper monitoring and alerts
   - Use multi-sig for production deployments

3. **Gas Limits**: Default gas limit is 500,000 units. Adjust based on network conditions:
   ```csharp
   var bridgeService = new ArbitrumBridgeService(
       web3, 
       technicalAccount, 
       gasLimit: new HexBigInteger(800000) // Higher gas limit
   );
   ```

4. **Transaction Monitoring**: Always check transaction status before considering a swap complete

5. **Rollback Mechanism**: The `CrossChainBridgeManager` automatically returns funds if either leg of the swap fails

## Limitations & Known Issues

1. **Seed Phrase Generation**: Current implementation uses a simplified seed phrase. For production, consider adding the `Nethereum.HdWallet` package for proper BIP39 mnemonic support.

2. **Gas Price**: Uses network default gas price. For faster transactions, implement dynamic gas price calculation.

3. **Network Errors**: Implement retry logic for network failures:
   ```csharp
   int maxRetries = 3;
   for (int i = 0; i < maxRetries; i++)
   {
       var result = await bridgeService.GetAccountBalanceAsync(address);
       if (!result.IsError) break;
       await Task.Delay(1000 * (i + 1)); // Exponential backoff
   }
   ```

## Testing

For testing, use Arbitrum Goerli testnet:

```csharp
var testProvider = new ArbitrumOASIS(
    hostUri: "https://goerli-rollup.arbitrum.io/rpc",
    chainPrivateKey: "TEST_PRIVATE_KEY",
    chainId: 421613,
    contractAddress: "TEST_CONTRACT"
);
```

Get testnet ETH from:
- https://faucet.triangleplatform.com/arbitrum/goerli
- https://goerlifaucet.com/

## Support

For issues or questions:
- GitHub: https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK
- Email: support@nextgensoftware.co.uk

## License

MIT License - Copyright © NextGen Software Ltd 2025





