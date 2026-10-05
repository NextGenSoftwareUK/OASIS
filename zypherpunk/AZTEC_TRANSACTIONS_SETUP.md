# 🔐 Aztec Transactions - Setup Guide

## ✅ What We've Implemented

### 1. **AztecCLIService** (✅ Complete)
- Uses Aztec CLI (`aztec-wallet`) to submit real transactions
- Falls back to Node.js service if CLI fails
- **NO MOCKS** - All transactions are real

### 2. **Node.js Bridge Service** (✅ Complete)
- Uses Aztec.js SDK for transaction submission
- HTTP API for .NET to call
- Handles proof generation and transaction submission

### 3. **Updated Bridge Service** (✅ Complete)
- Uses `AztecCLIService` for deposit/withdraw
- Real transaction submission - NO MOCKS

---

## 🚀 How to Enable Transactions

### Option A: Use Node.js Bridge Service (Recommended)

**Step 1: Install Dependencies**
```bash
cd /Volumes/Storage/OASIS_CLEAN/aztec-bridge-service
npm install
```

**Step 2: Start Service**
```bash
export AZTEC_NODE_URL=https://aztec-testnet-fullnode.zkv.xyz
npm start
```

**Step 3: Update .NET Configuration**
```json
"AztecBridge": {
  "NodeUrl": "https://aztec-testnet-fullnode.zkv.xyz",
  "NodeJsServiceUrl": "http://localhost:3002"
}
```

**Step 4: Test**
```bash
curl -X POST http://localhost:3002/api/health
```

---

### Option B: Use Aztec CLI Directly

**Requirements:**
- Aztec CLI installed (✅ Done)
- Account created (✅ Done - `maxgershfield`)
- Bridge contract deployed (⚠️ **Need to deploy**)

**Test Transaction:**
```bash
export NODE_URL=https://aztec-testnet-fullnode.zkv.xyz
aztec-wallet send deposit \
    --node-url $NODE_URL \
    --from accounts:maxgershfield \
    --payment method=fpc-sponsored,fpc=contracts:sponsoredfpc \
    --contract-address <BRIDGE_CONTRACT_ADDRESS> \
    --args <RECEIVER_ADDRESS> <AMOUNT>
```

---

## 🔧 Current Status

### ✅ Working
- Account created: `maxgershfield`
- CLI service implemented
- Node.js bridge service created
- Bridge service updated to use CLI

### ⚠️ Pending
- **Bridge Contract Deployment**: Need to deploy bridge contract first
- **Account Deployment**: Testnet returned "Transactions are not permitted"
- **Node.js Service**: Needs `npm install` and `npm start`

---

## 📝 Next Steps

1. **Start Node.js Bridge Service**:
   ```bash
   cd /Volumes/Storage/OASIS_CLEAN/aztec-bridge-service
   npm install
   npm start
   ```

2. **Deploy Bridge Contract** (when testnet allows):
   - Write contract in Noir
   - Compile: `aztec-nargo compile`
   - Deploy: `aztec-wallet deploy --from maxgershfield BridgeContract`

3. **Test Transaction**:
   - Once contract is deployed, test deposit/withdraw
   - Verify transaction hashes are returned

---

## 🎯 Transaction Flow

```
OASIS Bridge API
    ↓
AztecBridgeService
    ↓
AztecCLIService
    ↓
[Option A] Aztec CLI (aztec-wallet send)
    OR
[Option B] Node.js Service (Aztec.js SDK)
    ↓
Aztec Testnet
    ↓
Real Transaction Hash ✅
```

---

**Status**: Infrastructure ready, awaiting contract deployment
**Last Updated**: 2024-01-15

