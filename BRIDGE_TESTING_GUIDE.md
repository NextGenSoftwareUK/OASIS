# 🌉 OASIS Universal Asset Bridge - Testing Guide

## Overview

This guide covers testing the OASIS Universal Asset Bridge, which enables cross-chain token swaps between:
- **Solana** (SOL) ↔ **Radix** (XRD) 
- **Zcash** (ZEC) ↔ **Aztec** (AZTEC) - Private bridge with viewing keys
- Future: Ethereum, Polygon, and more

## Prerequisites

1. **OASIS API Running**: The ONODE WebAPI must be running
   ```bash
   cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
   dotnet run
   ```

2. **Test Script**: Use the provided test script
   ```bash
   cd /Volumes/Storage/OASIS_CLEAN
   ./test-bridge.sh
   ```

## Test Endpoints

### 1. Get Supported Networks
**Endpoint**: `GET /api/v1/networks`

**Expected Response**:
```json
[
  { "name": "Solana", "symbol": "SOL", "network": "devnet", "status": "active" },
  { "name": "Zcash", "symbol": "ZEC", "network": "testnet", "status": "active" },
  { "name": "Aztec", "symbol": "AZTEC", "network": "sandbox", "status": "active" }
]
```

**Test Command**:
```bash
curl http://localhost:5000/api/v1/networks
```

---

### 2. Get Exchange Rate
**Endpoint**: `GET /api/v1/exchange-rate?fromToken=SOL&toToken=XRD`

**Expected Response**:
```json
{
  "rate": 12.5,
  "fromToken": "SOL",
  "toToken": "XRD",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Test Commands**:
```bash
# Solana to Radix
curl "http://localhost:5000/api/v1/exchange-rate?fromToken=SOL&toToken=XRD"

# Zcash to Aztec
curl "http://localhost:5000/api/v1/exchange-rate?fromToken=ZEC&toToken=AZTEC"
```

**Note**: Exchange rates use CoinGecko API with 5-minute caching. ZEC is supported, but AZTEC may need a custom oracle if not listed on CoinGecko.

---

### 3. Create Bridge Order (Public)
**Endpoint**: `POST /api/v1/orders`

**Request Body**:
```json
{
  "fromToken": "SOL",
  "toToken": "XRD",
  "amount": 1.0,
  "fromAddress": "SolanaWalletAddress123",
  "toAddress": "RadixWalletAddress456",
  "fromChain": "Solana",
  "toChain": "Radix"
}
```

**Expected Response**:
```json
{
  "orderId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "pending",
  "fromToken": "SOL",
  "toToken": "XRD",
  "amount": 1.0,
  "estimatedAmount": 12.5,
  "exchangeRate": 12.5,
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Test Command**:
```bash
curl -X POST http://localhost:5000/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{
    "fromToken": "SOL",
    "toToken": "XRD",
    "amount": 1.0,
    "fromAddress": "test-solana-address",
    "toAddress": "test-radix-address",
    "fromChain": "Solana",
    "toChain": "Radix"
  }'
```

---

### 4. Create Private Bridge Order (Zcash ↔ Aztec)
**Endpoint**: `POST /api/v1/orders/private`

**Request Body**:
```json
{
  "fromToken": "ZEC",
  "toToken": "AZTEC",
  "amount": 0.5,
  "fromAddress": "zt1test123...",
  "toAddress": "aztec-test-address",
  "fromChain": "Zcash",
  "toChain": "Aztec"
}
```

**Expected Response**: Same as public order, but with:
- `enableViewingKeyAudit: true`
- `requireProofVerification: true`

**Test Command**:
```bash
curl -X POST http://localhost:5000/api/v1/orders/private \
  -H "Content-Type: application/json" \
  -d '{
    "fromToken": "ZEC",
    "toToken": "AZTEC",
    "amount": 0.5,
    "fromAddress": "zt1test123",
    "toAddress": "aztec-test-address",
    "fromChain": "Zcash",
    "toChain": "Aztec"
  }'
```

**Features**:
- ✅ Viewing key audit trail stored as Holons
- ✅ STARK proof verification
- ✅ Privacy-preserving transaction flow

---

### 5. Check Order Balance
**Endpoint**: `GET /api/v1/orders/{orderId}/check-balance`

**Expected Response**:
```json
{
  "orderId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "completed",
  "fromTokenBalance": 0.0,
  "toTokenBalance": 12.5,
  "lastUpdated": "2024-01-15T10:35:00Z"
}
```

**Test Command**:
```bash
curl http://localhost:5000/api/v1/orders/{ORDER_ID}/check-balance
```

---

### 6. Record Viewing Key (Audit)
**Endpoint**: `POST /api/v1/viewing-keys/audit`

**Request Body**:
```json
{
  "transactionId": "zcash-tx-123",
  "viewingKey": "viewing-key-string",
  "address": "zt1test123",
  "submittedBy": "auditor@example.com",
  "purpose": "bridge-audit"
}
```

**Expected Response**:
```json
{
  "success": true
}
```

**Test Command**:
```bash
curl -X POST http://localhost:5000/api/v1/viewing-keys/audit \
  -H "Content-Type: application/json" \
  -d '{
    "transactionId": "test-tx-123",
    "viewingKey": "test-viewing-key",
    "address": "zt1test123",
    "submittedBy": "test-user",
    "purpose": "bridge-audit"
  }'
```

**Note**: Viewing keys are stored as Holons in OASIS for compliance and auditability.

---

### 7. Verify Proof
**Endpoint**: `POST /api/v1/proofs/verify`

**Request Body**:
```json
{
  "proofPayload": "proof-data-here",
  "proofType": "STARK"
}
```

**Expected Response**:
```json
{
  "success": true
}
```

**Test Command**:
```bash
curl -X POST http://localhost:5000/api/v1/proofs/verify \
  -H "Content-Type: application/json" \
  -d '{
    "proofPayload": "test-proof-payload",
    "proofType": "STARK"
  }'
```

---

## Bridge Implementation Status

### ✅ Solana Bridge
- **Status**: Fully Implemented
- **Features**:
  - Account creation/restoration
  - Balance checking
  - Deposit/Withdraw transactions
  - Transaction status tracking
- **Testnet**: Solana Devnet (public, no auth needed)

### ⚠️ Zcash Bridge
- **Status**: Implementation Complete, Node Setup In Progress
- **Features**:
  - Shielded transaction support
  - Viewing key generation
  - Partial notes
  - Bridge lock/unlock operations
- **Testnet**: Zcash Testnet (requires local `zcashd` node)
- **Current**: Building `zcashd` from source (30-60 min build time)

### ⚠️ Aztec Bridge
- **Status**: Implementation Complete, Requires Aztec SDK
- **Features**:
  - Private note creation
  - STARK proof generation
  - Bridge deposit/withdraw
  - Event synchronization
- **Testnet**: Aztec Sandbox (requires Aztec API endpoint)
- **Note**: Currently uses placeholder/mock responses until Aztec SDK is integrated

---

## Testing Workflow

### Step 1: Start OASIS API
```bash
cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run
```

### Step 2: Run Test Script
```bash
cd /Volumes/Storage/OASIS_CLEAN
./test-bridge.sh
```

### Step 3: Manual Testing
Use the curl commands above or the test script to verify each endpoint.

---

## Expected Test Results

### ✅ Should Work Immediately
1. **Get Supported Networks** - Returns list of networks
2. **Get Exchange Rate (SOL/XRD)** - Returns rate from CoinGecko
3. **Create Bridge Order (SOL/XRD)** - Creates order (may fail on actual swap without funded accounts)

### ⚠️ Requires Setup
1. **Zcash Bridge** - Needs `zcashd` testnet node running
2. **Aztec Bridge** - Needs Aztec API endpoint configured
3. **Private Orders** - Will work once Zcash node is ready

---

## Next Steps

1. **Wait for Zcash Build**: Once `zcashd` is built, configure and sync testnet node
2. **Configure Aztec**: Set up Aztec sandbox API endpoint
3. **Test End-to-End**: Run complete bridge workflow with real transactions
4. **Frontend Integration**: Connect React/Next.js frontend to bridge API

---

## Troubleshooting

### API Not Responding
- Check if ONODE WebAPI is running: `dotnet run` in the ONODE directory
- Verify port: Default is `5000` (HTTP) or `5001` (HTTPS)

### Exchange Rate Failing
- CoinGecko API may be rate-limited (free tier: 10-50 calls/minute)
- Check internet connection
- Verify token symbols are in the mapping (ZEC is supported, AZTEC may need custom oracle)

### Bridge Order Failing
- Verify bridge implementations are initialized in `BridgeService.cs`
- Check logs for specific error messages
- Ensure testnet accounts have sufficient balance (for real transactions)

---

## Architecture Notes

- **Bridge Manager**: Orchestrates all bridge operations
- **Exchange Rate Service**: Uses CoinGecko with 5-minute caching
- **Viewing Key Audit**: Stores audit entries as Holons in OASIS
- **Proof Verification**: Validates STARK proofs for private bridges
- **MPC Service**: Hook for Multi-Party Computation (future enhancement)

---

**Last Updated**: 2024-01-15
**Status**: Ready for testing (Solana bridge), Zcash/Aztec pending node setup

