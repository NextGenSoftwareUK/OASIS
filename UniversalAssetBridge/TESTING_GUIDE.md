# 🌉 Universal Asset Bridge - Testing Guide

**Date:** December 2024  
**Status:** Ready for Testing

---

## 🎯 Overview

The Universal Asset Bridge supports cross-chain token swaps across multiple blockchains. This guide covers all available testing methods.

---

## ✅ What's Ready to Test

### Currently Supported Chains
1. **Solana (SOL)** - ✅ Fully functional on Devnet
2. **Arbitrum (ARB)** - ✅ Bridge service implemented
3. **Ethereum (ETH)** - ✅ Bridge service implemented  
4. **Polygon (MATIC)** - ✅ Bridge service implemented
5. **Base (BASE)** - ✅ Bridge service implemented
6. **Radix (XRD)** - ⏳ Partial (40% complete)

### Available Test Methods

1. **CLI Demo** - Interactive console app (Solana only)
2. **Test Harness** - Full OASIS integration (requires compilation)
3. **REST API** - HTTP endpoints (if ONODE WebAPI is running)
4. **Frontend** - Web UI (if frontend is running)

---

## 🚀 Method 1: CLI Demo (Easiest - Recommended)

### Quick Start

```bash
cd /Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/cli-demo
dotnet run
```

### Available Tests

**Menu Options:**
- **[1]** Create New Solana Wallet - Generate wallet with seed phrase
- **[2]** Check Solana Balance - Query any Solana address
- **[3]** View Bridge Architecture - See how the bridge works
- **[4]** Simulate Bridge Swap Flow - Understand atomic swaps
- **[5]** Full OASIS Bridge Information - Complete documentation

### Test Workflow

1. **Create a Test Wallet:**
   ```
   Select [1]
   Save the seed phrase and public key
   ```

2. **Fund the Wallet (Devnet):**
   - Visit: https://faucet.solana.com
   - Paste your public key
   - Request devnet SOL (test tokens only)

3. **Check Balance:**
   ```
   Select [2]
   Enter your public key
   Verify balance appears
   ```

4. **Explore Architecture:**
   ```
   Select [3] or [4] or [5]
   Learn how the bridge works
   ```

---

## 🧪 Method 2: Test Harness (Full Integration)

### Prerequisites
- Full OASIS solution compiled
- All dependencies resolved

### Run Test Harness

```bash
cd /Volumes/Storage/OASIS_CLEAN/NextGenSoftware.OASIS.API.Bridge.TestHarness
dotnet restore
dotnet build
dotnet run
```

### Available Tests

**Menu Options:**
- **[1]** Create New Solana Account
- **[2]** Create New Radix Account (pending)
- **[3]** Check Solana Balance
- **[4]** Check Radix Balance (pending)
- **[5]** Test SOL → XRD Swap (pending)
- **[6]** Test XRD → SOL Swap (pending)
- **[7]** View Configuration

### Current Status
- ✅ Solana bridge fully functional
- ⏳ Radix bridge pending (compilation issues)
- ⏳ Cross-chain swaps pending Radix completion

---

## 🌐 Method 3: REST API Testing

### Prerequisites
- ONODE WebAPI running
- Bridge endpoints enabled

### Start ONODE WebAPI

```bash
cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run
```

### Available Endpoints

**Create Bridge Order:**
```bash
POST /api/v1/bridge/orders
Content-Type: application/json

{
  "fromToken": "SOL",
  "toToken": "ETH",
  "amount": 1.0,
  "destinationAddress": "0x...",
  "userId": "user-id"
}
```

**Check Order Status:**
```bash
GET /api/v1/bridge/orders/{orderId}
```

**Get Exchange Rate:**
```bash
GET /api/v1/bridge/exchange-rate?from=SOL&to=ETH
```

**List Supported Networks:**
```bash
GET /api/v1/bridge/networks
```

### Test with curl

```bash
# Get exchange rate
curl http://localhost:5000/api/v1/bridge/exchange-rate?from=SOL&to=ETH

# List networks
curl http://localhost:5000/api/v1/bridge/networks
```

---

## 🎨 Method 4: Frontend Testing

### Prerequisites
- Frontend dependencies installed
- Backend API running

### Start Frontend

```bash
cd /Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/frontend
npm install
npm run dev
```

### Access Frontend
- Open: http://localhost:3000
- Navigate to Bridge section
- Select source and destination tokens
- Enter amount
- Execute swap

---

## 🔍 Testing Specific Features

### 1. Account Creation

**Test:** Create accounts on different chains

**Expected Result:**
- Public key generated
- Private key generated
- Seed phrase provided (12 words)
- All keys valid for respective blockchain

### 2. Balance Checking

**Test:** Query balances on different chains

**Expected Result:**
- Balance returned in native token units
- Zero balance handled gracefully
- Invalid addresses rejected

### 3. Exchange Rates

**Test:** Get exchange rates between tokens

**Expected Result:**
- Real-time rates from CoinGecko
- Rate calculation correct
- Invalid pairs handled

### 4. Cross-Chain Swaps

**Test:** Execute SOL → ETH swap (when both chains ready)

**Expected Flow:**
1. Validate request
2. Check source balance
3. Get exchange rate
4. Withdraw from source chain
5. Deposit to destination chain
6. Verify transactions
7. Return order details

**Expected Result:**
- Atomic operation (all or nothing)
- Automatic rollback on failure
- Transaction hashes returned
- Order status tracked

---

## 🧪 Automated Testing Script

### Basic Connection Test

```bash
#!/bin/bash
cd /Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/cli-demo

echo "Testing Solana Connection..."
dotnet run <<EOF
2
9WzDXwBbmkg8ZTbNMqUxvQRAyrZzDsGYdLVL9zYtAWWM
0
EOF
```

### Create Wallet Test

```bash
#!/bin/bash
cd /Volumes/Storage/OASIS_CLEAN/UniversalAssetBridge/cli-demo

echo "Creating Test Wallet..."
dotnet run <<EOF
1
0
EOF
```

---

## ✅ Test Checklist

### Basic Functionality
- [ ] CLI demo runs without errors
- [ ] Solana connection successful
- [ ] Account creation works
- [ ] Balance checking works
- [ ] Architecture viewer displays correctly

### Bridge Operations (When Ready)
- [ ] Exchange rates retrieved
- [ ] SOL → ETH swap works
- [ ] ETH → SOL swap works
- [ ] Rollback on failure works
- [ ] Transaction verification works

### Integration
- [ ] REST API endpoints respond
- [ ] Frontend connects to backend
- [ ] Error handling works
- [ ] Logging works

---

## 🐛 Troubleshooting

### "Failed to connect to Solana Devnet"
**Solution:** 
- Check internet connection
- Verify Solana status: https://status.solana.com
- Try again (Devnet may be temporarily down)

### "Account has no balance"
**Solution:**
- Fund account from faucet: https://faucet.solana.com
- Wait a few seconds for airdrop to process
- Check balance again

### "Radix provider not initialized"
**Solution:**
- This is expected - RadixOASIS has compilation issues
- Use Solana-only tests for now
- Radix will be available once SDK issues are fixed

### "Compilation errors"
**Solution:**
- Run `dotnet restore`
- Check all dependencies installed
- Verify .NET SDK version (8.0+)

---

## 📊 Test Results Template

```
Test Date: [Date]
Tester: [Name]
Environment: [Devnet/Testnet/Mainnet]

✅ Passed Tests:
- [List passed tests]

❌ Failed Tests:
- [List failed tests]

⚠️  Known Issues:
- [List known issues]

📝 Notes:
- [Additional notes]
```

---

## 🎯 Next Steps

1. **Complete Radix Integration** - Fix compilation issues
2. **Add More Chains** - Ethereum, Polygon, Base, etc.
3. **Test Real Swaps** - Execute actual cross-chain swaps
4. **Performance Testing** - Measure swap times
5. **Security Audit** - Review atomic swap logic
6. **Mainnet Deployment** - Deploy to production

---

## 📚 Additional Resources

- **Quick Start:** `BRIDGE_QUICKSTART.md`
- **Architecture:** `BRIDGE_ARCHITECTURE_EXPLAINED.md`
- **Adding Chains:** `ADDING_BRIDGE_SUPPORT_TO_PROVIDERS.md`
- **Status:** `MULTI_CHAIN_BRIDGE_COMPLETE.md`

---

**Ready to test? Start with Method 1 (CLI Demo) - it's the easiest!**

