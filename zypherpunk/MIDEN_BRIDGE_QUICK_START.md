# Miden ↔ Zcash Bridge - Quick Start

## Overview

The Miden ↔ Zcash bridge enables private cross-chain transfers between Zcash testnet and Miden testnet using:
- **Zcash**: Shielded transactions with viewing keys
- **Miden**: Private notes with STARK proofs

## Setup

### 1. Environment Variables

```bash
# Miden Configuration
export MIDEN_API_URL="https://testnet.miden.xyz"
export MIDEN_API_KEY="your_api_key_if_required"
export MIDEN_BRIDGE_POOL_ADDRESS="miden_bridge_pool"

# Zcash Configuration
export ZCASH_RPC_URL="http://localhost:8232"
export ZCASH_BRIDGE_POOL_ADDRESS="zt1bridgepool"
```

### 2. Provider Registration

Add to OASIS DNA configuration:

```json
{
  "Providers": {
    "MidenOASIS": {
      "IsEnabled": true,
      "ApiUrl": "${MIDEN_API_URL}",
      "ApiKey": "${MIDEN_API_KEY}",
      "Network": "testnet"
    },
    "ZcashOASIS": {
      "IsEnabled": true,
      "RpcUrl": "${ZCASH_RPC_URL}",
      "Network": "testnet"
    }
  },
  "Bridges": {
    "ZEC": "ZcashBridgeService",
    "MIDEN": "MidenBridgeService"
  }
}
```

## Usage

### Bridge Zcash → Miden

```csharp
var request = new CreateBridgeOrderRequest
{
    FromToken = "ZEC",
    ToToken = "MIDEN",
    Amount = 1.0m,
    FromAddress = "zs1your_zcash_address",
    DestinationAddress = "miden1your_miden_address",
    UserId = userId,
    EnableViewingKeyAudit = true,
    RequireProofVerification = true
};

var result = await bridgeManager.CreateBridgeOrderAsync(request);
if (!result.IsError)
{
    Console.WriteLine($"Bridge completed! Tx: {result.Result.TransactionId}");
}
```

### Bridge Miden → Zcash

```csharp
var request = new CreateBridgeOrderRequest
{
    FromToken = "MIDEN",
    ToToken = "ZEC",
    Amount = 1.0m,
    FromAddress = "miden1your_miden_address",
    DestinationAddress = "zs1your_zcash_address",
    UserId = userId,
    RequireProofVerification = true
};

var result = await bridgeManager.CreateBridgeOrderAsync(request);
```

## API Endpoints

### Create Bridge Order

```
POST /api/bridge/orders
Content-Type: application/json

{
  "fromToken": "ZEC",
  "toToken": "MIDEN",
  "amount": 1.0,
  "fromAddress": "zs1...",
  "destinationAddress": "miden1...",
  "userId": "user-guid",
  "enableViewingKeyAudit": true,
  "requireProofVerification": true
}
```

### Check Order Status

```
GET /api/bridge/orders/{orderId}/balance
```

## Features

✅ **Bi-directional**: Works both Zcash → Miden and Miden → Zcash
✅ **Private**: All operations maintain privacy
✅ **STARK Proofs**: Zero-knowledge verification on Miden
✅ **Viewing Keys**: Auditability without revealing amounts
✅ **Atomic**: Automatic rollback on failure

## Status

✅ Provider: Complete
✅ Bridge Service: Complete
✅ Bridge Manager: Complete
⏳ Testing: Pending testnet access
⏳ UI: Pending integration

## Next Steps

1. Obtain Miden testnet access
2. Test bridge operations
3. Integrate with wallet UI
4. Prepare demo

