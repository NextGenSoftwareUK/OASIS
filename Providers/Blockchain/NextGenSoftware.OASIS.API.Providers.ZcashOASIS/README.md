# Zcash OASIS Provider

Zcash blockchain provider for OASIS with support for shielded transactions, viewing keys, and partial notes. Built for the Zypherpunk Hackathon.

## Features

- ✅ Shielded transaction support
- ✅ Viewing key generation (for auditability)
- ✅ Partial notes (for enhanced privacy)
- ✅ Cross-chain bridge operations
- ✅ Zcash RPC integration
- ✅ Testnet and mainnet support

## Configuration

Add to `OASIS_DNA.json`:

```json
{
  "StorageProviders": {
    "ZcashOASIS": {
      "ProviderType": "ZcashOASIS",
      "IsEnabled": true,
      "IsDefault": false,
      "ConnectionString": "http://localhost:8232",
      "RPCUser": "user",
      "RPCPassword": "password",
      "Network": "testnet",
      "BridgeAddresses": {
        "Aztec": "zt1...",
        "Miden": "zt1...",
        "Solana": "zt1..."
      }
    }
  }
}
```

## Environment Variables

- `ZCASH_RPC_URL` - Zcash RPC endpoint (default: http://localhost:8232)
- `ZCASH_RPC_USER` - RPC username
- `ZCASH_RPC_PASSWORD` - RPC password
- `ZCASH_AZTEC_BRIDGE_ADDRESS` - Bridge address for Aztec
- `ZCASH_MIDEN_BRIDGE_ADDRESS` - Bridge address for Miden
- `ZCASH_SOLANA_BRIDGE_ADDRESS` - Bridge address for Solana

## Usage

### Create Shielded Transaction

```csharp
var provider = new ZcashOASIS();
await provider.ActivateProviderAsync();

var result = await provider.CreateShieldedTransactionAsync(
    "zt1...", // from address
    "zt1...", // to address
    1.0m,     // amount
    "Memo"    // optional memo
);
```

### Generate Viewing Key

```csharp
var viewingKey = await provider.GenerateViewingKeyAsync("zt1...");
```

### Create Partial Note

```csharp
var partialNote = await provider.CreatePartialNoteAsync(1.0m, 3); // Split into 3 parts
```

### Lock ZEC for Bridge

```csharp
var txId = await provider.LockZECForBridgeAsync(
    1.0m,              // amount
    "Aztec",           // destination chain
    "aztec_address",   // destination address
    "viewing_key"      // optional viewing key
);
```

## Zypherpunk Hackathon

This provider is built for the Zypherpunk Hackathon tracks:
- Aztec Labs - Private Bridge
- Aztec Labs - Unified Wallet
- Aztec Labs - Zcash Stablecoin
- Miden - Private Bridge
- Solana ↔ Zcash Solutions

## License

MIT

