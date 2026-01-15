# OASIS API vs Direct RPC: When to Use Each

**Date:** 2026-01-07  
**Status:** ✅ Documented

## Overview

You have two options for sending SOL transactions:

1. **OASIS API** (`oasis_send_transaction`) - Uses OASIS infrastructure
2. **Direct RPC** (`solana_send_sol`) - Bypasses OASIS, direct blockchain access

## Why Use OASIS API? (Recommended)

### ✅ Advantages

1. **Integrated with OASIS Ecosystem**
   - Transactions are tracked in OASIS
   - Transaction history stored in OASIS database
   - Works with OASIS avatar wallets automatically
   - Can use avatar IDs instead of wallet addresses

2. **Wallet Management**
   - Automatically uses avatar's default wallet
   - No need to manage private keys directly
   - Secure key storage in OASIS
   - Supports multiple wallets per avatar

3. **Provider Abstraction**
   - Works across multiple blockchains (Solana, Ethereum, etc.)
   - Unified API for all providers
   - Automatic provider selection

4. **Security**
   - Private keys never exposed to client
   - JWT authentication required
   - Centralized security management

5. **Additional Features**
   - Transaction memos
   - Multi-provider support
   - Avatar-to-avatar transfers
   - Integration with OASIS karma system

### ⚠️ Requirements

- Requires authentication (JWT token)
- Needs wallet addresses or avatar IDs
- Requires provider type specification

## Why Use Direct RPC?

### ✅ Advantages

1. **Speed**
   - No API layer overhead
   - Direct blockchain communication
   - Faster transaction submission

2. **Simplicity**
   - No authentication required
   - Direct private key usage
   - Fewer dependencies

3. **Network Control**
   - Explicit network selection (devnet/mainnet)
   - Direct RPC endpoint control
   - No provider abstraction layer

4. **Debugging**
   - Direct error messages from blockchain
   - Easier to troubleshoot
   - Full transaction visibility

### ⚠️ Disadvantages

1. **No OASIS Integration**
   - Transactions not tracked in OASIS
   - No transaction history
   - No avatar integration

2. **Security Concerns**
   - Must manage private keys yourself
   - Keys exposed in requests
   - No centralized key management

3. **Limited Features**
   - Only basic SOL transfers
   - No multi-provider support
   - No avatar-based operations

## Fixed OASIS Endpoint

The `oasis_send_transaction` endpoint has been **fixed** to work correctly:

### Previous Issues (Now Fixed)

1. ❌ **Wrong endpoint**: Was calling `/api/wallet/send-transaction` (404)
   - ✅ **Fixed**: Now calls `/api/wallet/send_token`

2. ❌ **Wrong request format**: Was using simplified format
   - ✅ **Fixed**: Now uses proper `IWalletTransactionRequest` structure

3. ❌ **Missing provider types**: Provider types not specified
   - ✅ **Fixed**: Automatically sets SolanaOASIS (3) as default

### Current Implementation

```typescript
// OASIS API - Recommended
await oasis_send_transaction({
  fromWalletAddress: "Cwy7Xxg4HbwNrhPJoTwwyY9S52BkDev49XGPfoz4SD6h",
  toWalletAddress: "7N7GhumDJMktqHGRazsCHY797H6geiV2MFd2sUwvW6Vs",
  amount: 4,
  fromProvider: 3, // SolanaOASIS (optional, defaults to 3)
  toProvider: 3,   // SolanaOASIS (optional, defaults to 3)
  memoText: "Payment for services" // optional
});

// Or use avatar IDs
await oasis_send_transaction({
  fromAvatarId: "f489231f-73c8-4642-9fc9-11efeb9698fa",
  toAddress: "7N7GhumDJMktqHGRazsCHY797H6geiV2MFd2sUwvW6Vs",
  amount: 4
});
```

## Recommendation

**Use OASIS API (`oasis_send_transaction`) for:**
- ✅ Production applications
- ✅ When you want transaction history
- ✅ When working with OASIS avatars
- ✅ When you need multi-provider support
- ✅ When security is a priority

**Use Direct RPC (`solana_send_sol`) for:**
- ✅ Quick testing/development
- ✅ When you need maximum speed
- ✅ When you have private keys directly
- ✅ When you don't need OASIS integration
- ✅ When debugging blockchain issues

## Migration Path

If you're currently using `solana_send_sol`, consider migrating to `oasis_send_transaction`:

```typescript
// Old (Direct RPC)
await solana_send_sol({
  fromPrivateKey: "...",
  toAddress: "...",
  amount: 4,
  network: "devnet"
});

// New (OASIS API) - Better!
await oasis_send_transaction({
  fromWalletAddress: "...", // Get from OASIS wallet
  toWalletAddress: "...",
  amount: 4
  // No private key needed!
});
```

## Summary

**You're absolutely right** - we should use the OASIS API! The endpoint has been fixed and now works correctly. The direct RPC endpoints are still available for edge cases, but OASIS API is the recommended approach for most use cases.

The key fix was:
1. Correct endpoint: `/api/wallet/send_token` (not `/api/wallet/send-transaction`)
2. Proper request format matching `IWalletTransactionRequest`
3. Automatic provider type handling







