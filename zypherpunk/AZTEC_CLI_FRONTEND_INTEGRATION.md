# 🔗 Aztec CLI Service - Frontend Integration Guide

## Architecture Overview

The Aztec CLI service runs on the **backend server** and is accessed by the frontend through REST API endpoints. The CLI never runs in the browser - it executes on the server where Aztec CLI is installed.

---

## 📊 Complete Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         FRONTEND (Browser)                       │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Bridge Swap UI Component                                 │  │
│  │  - User selects: ZEC → AZTEC                             │  │
│  │  - Amount: 0.5 ZEC                                        │  │
│  │  - Clicks "Bridge"                                        │  │
│  └──────────────────────────────────────────────────────────┘  │
│                           │                                     │
│                           │ HTTP POST                            │
│                           ▼                                     │
└─────────────────────────────────────────────────────────────────┘
                           │
                           │ POST /api/v1/orders
                           │ {
                           │   "fromToken": "ZEC",
                           │   "toToken": "AZTEC",
                           │   "amount": 0.5,
                           │   "fromChain": "Zcash",
                           │   "toChain": "Aztec"
                           │ }
                           │
┌─────────────────────────────────────────────────────────────────┐
│                    BACKEND API SERVER                          │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  BridgeController.cs                                      │  │
│  │  POST /api/v1/orders                                      │  │
│  └──────────────────────────────────────────────────────────┘  │
│                           │                                     │
│                           │ CreateOrderAsync()                   │
│                           ▼                                     │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  BridgeService.cs                                         │  │
│  │  - Validates request                                      │  │
│  │  - Gets exchange rate                                     │  │
│  │  - Routes to AztecBridgeService                          │  │
│  └──────────────────────────────────────────────────────────┘  │
│                           │                                     │
│                           │ DepositAsync(amount, receiver)       │
│                           ▼                                     │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  AztecBridgeService.cs                                   │  │
│  │  - Prepares bridge transaction                           │  │
│  │  - Calls AztecCLIService                                 │  │
│  └──────────────────────────────────────────────────────────┘  │
│                           │                                     │
│                           │ SendTransactionAsync()              │
│                           ▼                                     │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  AztecCLIService.cs                                       │  │
│  │  - Executes: aztec-wallet send deposit ...               │  │
│  │  - Parses transaction hash from output                   │  │
│  └──────────────────────────────────────────────────────────┘  │
│                           │                                     │
│                           │ Process.Start()                     │
│                           │ aztec-wallet CLI                    │
│                           ▼                                     │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Aztec CLI (aztec-wallet)                                 │  │
│  │  - Generates proofs                                       │  │
│  │  - Submits to Aztec testnet                               │  │
│  │  - Returns transaction hash                               │  │
│  └──────────────────────────────────────────────────────────┘  │
│                           │                                     │
│                           │ Transaction Hash                     │
│                           │ "0xabc123..."                       │
│                           ▼                                     │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Response: BridgeTransactionResponse                      │  │
│  │  {                                                        │  │
│  │    "transactionHash": "0xabc123...",                     │  │
│  │    "status": "Pending",                                   │  │
│  │    "message": "Aztec deposit submitted"                  │  │
│  │  }                                                        │  │
│  └──────────────────────────────────────────────────────────┘  │
│                           │                                     │
│                           │ HTTP 200 OK                          │
│                           │ JSON Response                        │
└─────────────────────────────────────────────────────────────────┘
                           │
                           │ JSON Response
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                         FRONTEND (Browser)                       │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Bridge Swap UI Component                                 │  │
│  │  - Displays: "Transaction submitted!"                     │  │
│  │  - Shows transaction hash                                 │  │
│  │  - Polls for status updates                               │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 Implementation Details

### 1. **Frontend Component** (React/Next.js)

The frontend needs a bridge swap component that calls the Bridge API:

```typescript
// components/bridge/BridgeSwapModal.tsx
import { useState } from 'react';
import { oasisWalletAPI } from '@/lib/api';

export const BridgeSwapModal = () => {
  const [fromToken, setFromToken] = useState('ZEC');
  const [toToken, setToToken] = useState('AZTEC');
  const [amount, setAmount] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [txHash, setTxHash] = useState<string | null>(null);

  const handleBridge = async () => {
    setIsLoading(true);
    try {
      // Call Bridge API endpoint
      const response = await fetch('http://api.oasisplatform.world/api/v1/orders', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${authToken}` // If auth required
        },
        body: JSON.stringify({
          fromToken: fromToken,
          toToken: toToken,
          amount: parseFloat(amount),
          fromChain: 'Zcash',
          toChain: 'Aztec',
          fromWalletAddress: userWalletAddress,
          toWalletAddress: receiverAddress
        })
      });

      const result = await response.json();
      
      if (result.transactionHash) {
        setTxHash(result.transactionHash);
        // Poll for transaction status
        pollTransactionStatus(result.transactionHash);
      } else {
        throw new Error(result.error || 'Bridge transaction failed');
      }
    } catch (error) {
      console.error('Bridge error:', error);
      alert(`Bridge failed: ${error.message}`);
    } finally {
      setIsLoading(false);
    }
  };

  const pollTransactionStatus = async (hash: string) => {
    // Poll /api/v1/orders/{orderId}/check-balance
    // or use WebSocket for real-time updates
  };

  return (
    <div>
      <input 
        type="text" 
        value={amount} 
        onChange={(e) => setAmount(e.target.value)}
        placeholder="Amount"
      />
      <select value={fromToken} onChange={(e) => setFromToken(e.target.value)}>
        <option value="ZEC">ZEC</option>
        <option value="AZTEC">AZTEC</option>
      </select>
      <select value={toToken} onChange={(e) => setToToken(e.target.value)}>
        <option value="ZEC">ZEC</option>
        <option value="AZTEC">AZTEC</option>
      </select>
      <button onClick={handleBridge} disabled={isLoading}>
        {isLoading ? 'Bridging...' : 'Bridge'}
      </button>
      {txHash && (
        <div>
          <p>Transaction Hash: {txHash}</p>
          <a href={`https://aztec-explorer.com/tx/${txHash}`} target="_blank">
            View on Explorer
          </a>
        </div>
      )}
    </div>
  );
};
```

---

### 2. **Backend API Endpoint** (Already Implemented)

The `BridgeController` already has the endpoint:

```csharp
// BridgeController.cs
[HttpPost("orders")]
public async Task<IActionResult> CreateOrder(
    [FromBody] CreateBridgeOrderRequest request,
    CancellationToken cancellationToken = default)
{
    var result = await _bridgeService.CreateOrderAsync(request, cancellationToken);
    if (result.IsError)
    {
        return BadRequest(new { error = result.Message });
    }
    return Ok(result.Result);
}
```

---

### 3. **CLI Service Execution** (Backend Only)

The CLI service runs on the server:

```csharp
// AztecCLIService.cs
public async Task<OASISResult<string>> SendTransactionAsync(
    string accountAlias,
    string contractAddress,
    string functionName,
    object[] functionArgs)
{
    // Execute aztec-wallet CLI command
    var processStartInfo = new ProcessStartInfo
    {
        FileName = _aztecCliPath, // "/Users/maxgershfield/.aztec/bin/aztec-wallet"
        Arguments = $"send {functionName} --node-url {_nodeUrl} --from accounts:{accountAlias} ...",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    using var process = Process.Start(processStartInfo);
    var output = await process.StandardOutput.ReadToEndAsync();
    
    // Parse transaction hash from output
    var txHash = ExtractTransactionHash(output);
    return new OASISResult<string> { Result = txHash };
}
```

**Important**: The CLI must be installed on the server where the API runs, not in the browser.

---

## 🔑 Key Points

### ✅ What Works

1. **Frontend → API**: Frontend calls REST API endpoints
2. **API → CLI Service**: Backend executes CLI commands
3. **CLI → Aztec Network**: CLI submits real transactions
4. **Response Chain**: Transaction hash flows back to frontend

### ⚠️ Requirements

1. **Aztec CLI on Server**: Must be installed where API runs
   ```bash
   # On the server
   export PATH="$HOME/.aztec/bin:$PATH"
   aztec-wallet --version  # Should work
   ```

2. **Account Setup**: Account must exist in CLI wallet
   ```bash
   # On the server
   aztec-wallet list-accounts --node-url $NODE_URL
   # Should show: maxgershfield
   ```

3. **Bridge Contract**: Contract must be deployed
   ```bash
   # Deploy bridge contract first
   aztec-wallet deploy BridgeContract --from maxgershfield
   ```

---

## 🎯 Frontend API Integration

### Create Bridge Order

```typescript
POST /api/v1/orders
Content-Type: application/json

{
  "fromToken": "ZEC",
  "toToken": "AZTEC",
  "amount": 0.5,
  "fromChain": "Zcash",
  "toChain": "Aztec",
  "fromWalletAddress": "zs1...",
  "toWalletAddress": "0x09d16dbfac70e06fc61cbd984190ac9d385131f1011faeb436da4e17eaa2a686"
}

Response:
{
  "orderId": "guid-here",
  "transactionHash": "0xabc123...",
  "status": "Pending",
  "message": "Aztec deposit transaction submitted"
}
```

### Check Order Status

```typescript
GET /api/v1/orders/{orderId}/check-balance

Response:
{
  "orderId": "guid-here",
  "status": "Completed",
  "balance": 0.5,
  "transactionHash": "0xabc123..."
}
```

### Get Exchange Rate

```typescript
GET /api/v1/exchange-rate?fromToken=ZEC&toToken=AZTEC

Response:
{
  "rate": 1.0,
  "fromToken": "ZEC",
  "toToken": "AZTEC",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

## 🚀 Next Steps

### 1. Create Frontend Bridge Component

Create a React component that:
- Shows token selection (ZEC ↔ AZTEC)
- Displays exchange rate
- Handles amount input
- Submits bridge order
- Shows transaction status
- Polls for completion

### 2. Add Real-Time Updates

Use WebSocket or polling to update transaction status:
```typescript
// Poll every 5 seconds
setInterval(async () => {
  const status = await checkOrderStatus(orderId);
  if (status === 'Completed') {
    // Show success
  }
}, 5000);
```

### 3. Error Handling

Handle common errors:
- Insufficient balance
- Network errors
- Transaction failures
- CLI not available

---

## 📝 Summary

**The CLI service works with the frontend through the REST API:**

1. **Frontend** calls `/api/v1/orders` (REST API)
2. **Backend** processes request and calls `AztecCLIService`
3. **CLI Service** executes `aztec-wallet` command on server
4. **Aztec CLI** submits transaction to Aztec network
5. **Response** flows back: CLI → Service → API → Frontend

**The CLI never runs in the browser** - it's a server-side service that the frontend accesses via HTTP.

---

**Last Updated**: 2024-01-15
**Status**: Architecture complete, awaiting frontend component implementation

