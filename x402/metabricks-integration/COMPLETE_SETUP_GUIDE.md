# Complete x402 Setup Guide
## From Zero to Real SOL Distributions

---

## ✅ **What We've Done So Far:**

### **1. Treasury Wallet Created**
```
Address: H8tjfEMYNkTDdSbRBekMBTcE966vGRF2WnCThRRoQJ1
Keypair: /Volumes/Storage 2/OASIS_CLEAN/x402/metabricks-integration/treasury-devnet.json
Network: Devnet
Seed Phrase: split devote scrap finger drip risk climb regret loyal pyramid history ability
```

**⚠️  SAVE THIS SEED PHRASE!** You'll need it to recover the wallet.

---

### **2. Backend Integration Complete**
- ✅ x402 service integrated into MetaBricks backend
- ✅ 50/50 split configured
- ✅ Webhook endpoint working
- ✅ SC-Gen configured to trigger webhook
- ✅ Holder tracking added (new file)

---

### **3. Code Updated**
**Files changed:**
1. `meta-bricks-main/backend/server.js` - Initialize x402, track holders
2. `meta-bricks-main/backend/routes/metabricks-x402-routes.js` - Distribution logic
3. `meta-bricks-main/backend/storage/nft-holders-storage.js` - NEW - Track holder wallets
4. `QS_Asset_Rail/smart-contract-generator/.../appsettings.json` - Webhook URL
5. `QS_Asset_Rail/smart-contract-generator/.../X402PaymentService.cs` - 50% split

---

## 🚀 **How It Works Now:**

### **NFT Minting Flow (Updated):**

```
1. User mints MetaBricks NFT
   └─> Frontend → Backend → OASIS API → Solana blockchain
   
2. NFT minted with x402 config (lines 694-701 in server.js)
   └─> x402Config included in OASIS request
   └─> Description mentions "Earn passive income"
   
3. NFT transferred to user's wallet
   └─> User receives MetaBrick in Phantom
   
4. Holder address RECORDED (NEW - lines 862-873)
   └─> Saved to: backend/storage/metabricks-holders.json
   └─> Format: { walletAddress, brickId, mintAddress, timestamp }
```

### **Distribution Flow:**

```
1. Developer uses SC-Gen API
   └─> Pays 0.15 SOL to compile Rust contract
   
2. SC-Gen verifies payment on blockchain
   └─> Calls internal VerifyPaymentAsync
   
3. SC-Gen triggers webhook (line 62 in X402PaymentService.cs)
   └─> POST http://localhost:3001/api/metabricks/sc-gen-webhook
   └─> Body: { amount: 0.15, distributionPercentage: 50, ... }
   
4. MetaBricks backend receives webhook
   └─> Queries holders from storage
   └─> Calculates: 0.075 SOL to holders, 0.075 SOL to treasury
   
5. Backend queries REAL holders
   └─> Reads metabricks-holders.json
   └─> Gets actual wallet addresses
   
6. Backend executes Solana transaction
   └─> Creates multi-recipient transfer
   └─> Signs with treasury wallet
   └─> Sends to blockchain
   
7. Holders receive SOL
   └─> Transaction confirmed
   └─> SOL appears in Phantom wallets
```

---

## 🧪 **Testing Steps (Complete End-to-End):**

### **Phase 1: Fund Treasury**

```bash
# Already done!
# Address: H8tjfEMYNkTDdSbRBekMBTcE966vGRF2WnCThRRoQJ1
# Balance: 5 SOL (devnet)
```

---

### **Phase 2: Enable Real Distributions**

```bash
cd "/Volumes/Storage 2/OASIS_CLEAN/meta-bricks-main/backend"

# Set environment variables
export TREASURY_KEYPAIR_PATH="/Volumes/Storage 2/OASIS_CLEAN/x402/metabricks-integration/treasury-devnet.json"
export SOLANA_RPC_URL=https://api.devnet.solana.com
export X402_ENABLE_REAL_DISTRIBUTIONS=true

# Switch to REAL distribution routes
cp routes/metabricks-x402-routes.js routes/metabricks-x402-routes-BACKUP.js
cp routes/metabricks-x402-routes-REAL.js routes/metabricks-x402-routes.js

# Restart backend
npm start
```

---

### **Phase 3: Mint Test NFT to YOUR Wallet**

```bash
# Option A: Use MetaBricks frontend at metabricks.xyz
# - Connect your Phantom wallet (on devnet)
# - Select Brick #1
# - Pay with Solana (devnet SOL)
# - Receive NFT

# Option B: Direct API call
curl -X POST http://localhost:3001/api/mint-nft \
  -H "Content-Type: application/json" \
  -d '{
    "walletAddress": "<YOUR_PHANTOM_WALLET_ADDRESS>",
    "brickId": "1",
    "brickName": "MetaBrick #1",
    "paymentNetwork": "solana",
    "brickType": "regular"
  }'
```

**Result:**
- MetaBrick #1 NFT in your wallet
- Your wallet address saved to `storage/metabricks-holders.json`

---

### **Phase 4: Verify Holder Tracking**

```bash
# Check if your wallet was recorded
curl http://localhost:3001/api/metabricks/holders

# Should show:
# {
#   "success": true,
#   "totalHolders": 1,
#   "holders": [
#     {
#       "walletAddress": "<YOUR_WALLET>",
#       "balance": 1
#     }
#   ]
# }
```

---

### **Phase 5: Trigger SC-Gen Payment**

**Option A: Start SC-Gen and use it:**

```bash
cd "/Volumes/Storage 2/QS_Asset_Rail/smart-contract-generator/src/SmartContractGen/ScGen.API"
dotnet run

# Then visit http://localhost:5000 and use the API
# Or call the payment endpoint directly
```

**Option B: Simulate SC-Gen webhook:**

```bash
curl -X POST http://localhost:3001/api/metabricks/sc-gen-webhook \
  -H "Content-Type: application/json" \
  -d '{
    "signature": "REAL_TEST_001",
    "amount": 1.0,
    "operation": "compile",
    "blockchain": "rust",
    "distributionPercentage": 50,
    "nftCollection": "METABRICKS",
    "metadata": {
      "test": "End-to-end real distribution"
    }
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "Revenue distributed to MetaBricks holders",
  "distribution": {
    "totalAmount": 0.5,
    "holders": 1,
    "amountPerHolder": 0.5,
    "distributionTx": "5xYz...abc123",  // REAL Solana signature!
    "solscanUrl": "https://solscan.io/tx/5xYz...?cluster=devnet"
  }
}
```

---

### **Phase 6: Verify SOL Received**

```bash
# Check your Phantom wallet
# - Switch to Devnet in settings
# - Check balance (should have +0.5 SOL)
# - View transactions
# - See incoming transfer from treasury

# Or check via CLI
solana balance <YOUR_WALLET> --url devnet

# View transaction
open "https://solscan.io/tx/<DISTRIBUTION_TX>?cluster=devnet"
```

**You should see:** 0.5 SOL transferred from treasury to YOUR wallet! 💰

---

## 📊 **Current vs. Target State:**

### **Current (After These Changes):**

```
✅ Treasury wallet: H8tjfEMYNkTDdSbRBekMBTcE966vGRF2WnCThRRoQJ1 (5 SOL)
✅ Backend tracks holders during minting
✅ Backend queries REAL holders for distributions
✅ REAL Solana transaction code ready
❌ No NFTs minted yet (starting fresh)
❌ Real distributions not enabled (need to switch routes)
```

### **Target (In 30 Minutes):**

```
✅ 1-3 test NFTs minted to real wallets
✅ Real holders tracked in storage
✅ Real Solana distributions enabled
✅ SC-Gen payment triggers real SOL transfer
✅ Holder sees SOL in Phantom wallet
✅ Transaction visible on Solscan
```

---

## 🎯 **Next Immediate Steps:**

### **Step 1: Fund Treasury** ✅ DONE
```
solana airdrop 5 H8tjfEMYNkTDdSbRBekMBTcE966vGRF2WnCThRRoQJ1 --url devnet
```

### **Step 2: Enable Real Distributions** (I'll do this)
- Switch to REAL routes file
- Configure treasury keypair path
- Update holder query to use storage

### **Step 3: Mint 1 Test NFT** (You'll do this)
- Connect Phantom to devnet
- Mint MetaBrick #1 to your wallet
- Verify it appears in your wallet

### **Step 4: Test Distribution** (Together)
- Trigger SC-Gen webhook
- Watch backend logs
- See SOL arrive in your wallet!

---

## 💡 **About the Metadata:**

**Good news:** You don't need to update Pinata metadata!

**Why?**
1. x402 config is in the OASIS minting request (lines 694-701)
2. OASIS stores this in its database
3. The on-chain metadata URI still points to Pinata
4. But the x402 config is associated with the NFT in OASIS

**Optional Enhancement:**
You could update Pinata metadata to show x402 info in marketplaces, but it's not required for distributions to work.

---

## 📁 **New Files Created:**

```
meta-bricks-main/backend/
├── storage/
│   ├── nft-holders-storage.js          # NEW - Track holder wallets
│   └── metabricks-holders.json         # NEW - Auto-created when first NFT mints
└── routes/
    └── metabricks-x402-routes-REAL.js  # NEW - Real Solana transfers

x402/metabricks-integration/
└── treasury-devnet.json                # NEW - Treasury wallet keypair
```

---

## ✅ **Ready to Test?**

**Want me to:**
1. **Enable real distributions now** - Switch to REAL routes, configure treasury
2. **Create a simple minting test** - Script to mint 1 NFT
3. **Both** - Full end-to-end setup

Which would you like? 🚀


