# StarknetOASIS Provider

This provider wraps Starknet tooling inside the OASIS storage provider abstraction. It is the foundation for ZEC ↔ Starknet atomic swap flows, Starknet account management, and any future Starknet-specific privacy helpers.

## Overview

The StarknetOASIS provider enables:
- Real Starknet RPC transaction submission
- Account creation and restoration with seed phrase support
- Atomic swap intent creation and tracking via Holons
- Balance queries and transaction status monitoring
- Holon persistence for auditable swap history

## Provider Registration

The provider is registered in `OASIS_DNA.json` and uses `ProviderType.StarknetOASIS`. To activate:

1. Ensure the provider is configured in `OASIS_DNA.json`:
```json
{
  "StorageProviders": {
    "StarknetOASIS": {
      "Network": "alpha-goerli",
      "ApiBaseUrl": "https://alpha4.starknet.io"
    }
  }
}
```

2. Activate the provider via API or programmatically:
```csharp
var provider = new StarknetOASIS("alpha-goerli", "https://alpha4.starknet.io");
await provider.ActivateProviderAsync();
```

## Configuration

### Environment Variables

- `STARKNET_NETWORK`: Network name (default: "alpha-goerli")
- `STARKNET_API_BASE_URL`: RPC endpoint URL (default: "https://alpha4.starknet.io")

### Supported Networks

- `alpha-goerli`: Starknet testnet (default)
- `alpha-mainnet`: Starknet mainnet (when available)

## Key Features

### 1. Atomic Swap Intent Creation

Create and track atomic swaps between Zcash and Starknet:

```csharp
var swapId = await provider.CreateAtomicSwapIntentAsync(
    starknetAddress: "0x123...",
    amount: 1.5m,
    zcashAddress: "zs1..."
);
```

This creates a Holon tracking the swap with status, addresses, and timestamps.

### 2. Balance Queries

Get account balances:

```csharp
var balance = await provider.GetBalanceAsync("0x123...");
```

### 3. Swap Status Tracking

Query and update swap status:

```csharp
// Get status
var status = await provider.GetSwapStatusAsync(swapId);

// Update status
await provider.UpdateSwapStatusAsync(
    swapId, 
    BridgeTransactionStatus.Completed,
    transactionHash: "0xabc..."
);
```

### 4. Holon Persistence

The provider implements `SaveHolonAsync` and `LoadHolonAsync` to persist swap data:

```csharp
// Save a Holon
var holon = new Holon { /* ... */ };
await provider.SaveHolonAsync(holon);

// Load a Holon
var loaded = await provider.LoadHolonAsync(holonId);
```

## RPC Client

The `StarknetRpcClient` handles:
- `GetBlockNumberAsync()`: Get current block number
- `GetBalanceAsync(address)`: Get account balance
- `SubmitTransactionAsync(payload)`: Submit transactions to Starknet
- `GetTransactionStatusAsync(hash)`: Check transaction status

### Transaction Submission

Transactions are submitted via `starknet_addInvokeTransaction` RPC method. The client includes:
- Proper transaction payload construction
- Error handling and fallback mechanisms
- Deterministic transaction hash generation for tracking

## Account Management

The `StarknetBridge` provides:
- `CreateAccountAsync()`: Generate new Starknet account with seed phrase
- `RestoreAccountAsync(seedPhrase)`: Restore account from seed phrase

**Note**: Current implementation uses simplified key derivation. For production, integrate with a proper Starknet SDK (e.g., StarknetSharp) for full cryptographic support.

## Holon Structure

Atomic swap Holons contain:

```json
{
  "Name": "Atomic Swap {swapId}",
  "Description": "Zcash to Starknet atomic swap",
  "HolonType": "BridgeTransaction",
  "MetaData": {
    "swapId": "...",
    "starknetAddress": "0x...",
    "zcashAddress": "zs1...",
    "amount": "1.5",
    "status": "Pending|Completed|Canceled",
    "fromChain": "Zcash",
    "toChain": "Starknet",
    "createdAt": "2024-...",
    "transactionHash": "0x..." // optional
  }
}
```

## Integration with Zypherpunk

This provider supports the Zypherpunk hackathon bridge:
- Atomic swap intents are tracked in Holons
- Status updates enable UI to show swap progress
- Viewing keys and oracle feeds can be stored in Holon metadata

## API Surface

### Public Methods

- `CreateAtomicSwapIntentAsync(starknetAddress, amount, zcashAddress)`: Create swap intent
- `GetBalanceAsync(accountAddress)`: Get account balance
- `GetSwapStatusAsync(swapId)`: Get swap status
- `UpdateSwapStatusAsync(swapId, status, transactionHash)`: Update swap status
- `SaveHolonAsync(holon)`: Persist Holon
- `LoadHolonAsync(id)`: Load Holon

## Error Handling

All methods return `OASISResult<T>` with:
- `IsError`: Boolean indicating success/failure
- `Message`: Human-readable message
- `Exception`: Exception details (if error occurred)
- `Result`: Method-specific result data

## Development Notes

- Transaction submission uses RPC calls with fallback to deterministic hash generation
- Account creation uses simplified key derivation (upgrade to full SDK recommended)
- Holon persistence delegates to `ProviderManager` for storage provider abstraction
- Provider follows OASIS patterns: inherits from `OASISStorageProviderBase`, implements required interfaces

## Future Enhancements

- Integration with full Starknet SDK for proper cryptographic operations
- Support for Starknet smart contract interactions
- Enhanced viewing key management
- Oracle feed integration for price data
- Multi-signature support for atomic swaps

