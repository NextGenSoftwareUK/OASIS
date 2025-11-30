# Testing Instructions - End-to-End MetaBricks x402

## 🎯 **What We're Testing:**
SC-Gen receives payment → Triggers webhook → MetaBricks backend distributes SOL → Real wallets receive SOL

---

## ✅ **What's Now Configured:**

### **1. SC-Gen (Smart Contract Generator)**
- ✅ Treasury wallet: `3BTEJ9uANDQ5DqSZwmjQm2CsnGuofojBgViKRpVZco5X`
- ✅ Webhook URL: `http://localhost:3001/api/metabricks/sc-gen-webhook`
- ✅ Distribution: 50% to holders, 50% to treasury
- ✅ Webhook triggers after every payment verification

**Files updated:**
- `appsettings.json` - Set DistributionWebhookUrl and TreasuryAddress
- `X402PaymentService.cs` - Changed distributionPercentage to 50%, added nftCollection

---

### **2. MetaBricks Backend**
- ✅ x402 service integrated (as library)
- ✅ MetaBricks webhook endpoint ready
- ✅ Real Solana transaction code added (new file)
- ✅ File storage for distribution history

**Files created/updated:**
- `server.js` - Initialize x402 service
- `routes/metabricks-x402-routes.js` - Mock distributions (current)
- `routes/metabricks-x402-routes-REAL.js` - Real Solana transfers (ready to use)

---

## 🧪 **Testing Options:**

### **Option A: Test with Mock Data** (Works NOW)
**No wallet needed, just tests the webhook flow**

1. **Start MetaBricks backend:**
   ```bash
   cd "/Volumes/Storage 2/OASIS_CLEAN/meta-bricks-main/backend"
   npm start
   ```

2. **Start SC-Gen API:**
   ```bash
   cd "/Volumes/Storage 2/QS_Asset_Rail/smart-contract-generator/src/SmartContractGen/ScGen.API"
   dotnet run
   ```

3. **Simulate a payment in SC-Gen:**
   ```bash
   # Call the verify payment endpoint
   curl -X POST http://localhost:5000/api/v1/payments/verify \
     -H "Content-Type: application/json" \
     -d '{
       "signature": "TEST_PAYMENT_001",
       "amount": 0.15,
       "operation": "compile",
       "blockchain": "rust"
     }'
   ```

4. **Check if webhook was triggered:**
   ```bash
   # Watch MetaBricks backend logs - should see:
   # 🧱 MetaBricks distribution webhook triggered
   # 💰 Distribution Details: Total payment: 0.15 SOL
   # ✅ Distributed 0.075 SOL to 432 MetaBricks holders
   ```

5. **Check MetaBricks stats:**
   ```bash
   curl http://localhost:3001/api/metabricks/stats
   ```

**This proves:** SC-Gen → MetaBricks webhook flow works ✅

---

### **Option B: Test with Real SOL Transfers** (Requires wallet setup)
**Actually sends SOL to real wallet addresses**

#### **Prerequisites:**

1. **Fund the treasury wallet:**
   ```bash
   # Airdrop SOL to treasury (devnet)
   solana airdrop 5 3BTEJ9uANDQ5DqSZwmjQm2CsnGuofojBgViKRpVZco5X --url devnet
   
   # Check balance
   solana balance 3BTEJ9uANDQ5DqSZwmjQm2CsnGuofojBgViKRpVZco5X --url devnet
   ```

2. **Get treasury keypair:**
   You need the private key for `3BTEJ9uANDQ5DqSZwmjQm2CsnGuofojBgViKRpVZco5X`
   
   Save it to: `~/.config/solana/id.json`
   
   Format:
   ```json
   [123, 45, 67, ..., 255]  // Array of 64 bytes
   ```

3. **Set environment variables:**
   ```bash
   export TREASURY_KEYPAIR_PATH=~/.config/solana/id.json
   export SOLANA_RPC_URL=https://api.devnet.solana.com
   export X402_ENABLE_REAL_DISTRIBUTIONS=true
   export X402_QUERY_REAL_HOLDERS=false  # Use mock holders for testing
   ```

4. **Switch MetaBricks backend to use real routes:**
   ```bash
   cd "/Volumes/Storage 2/OASIS_CLEAN/meta-bricks-main/backend/routes"
   
   # Backup current file
   mv metabricks-x402-routes.js metabricks-x402-routes-MOCK.js
   
   # Use real distribution file
   mv metabricks-x402-routes-REAL.js metabricks-x402-routes.js
   ```

#### **Testing Steps:**

1. **Start backends:**
   ```bash
   # Terminal 1: MetaBricks
   cd meta-bricks-main/backend
   npm start
   
   # Terminal 2: SC-Gen
   cd QS_Asset_Rail/smart-contract-generator/src/SmartContractGen/ScGen.API
   dotnet run
   ```

2. **Create test holder wallet:**
   ```bash
   # Generate new wallet
   solana-keygen new -o test-brick-holder.json
   
   # Get address
   HOLDER_ADDRESS=$(solana-keygen pubkey test-brick-holder.json)
   echo "Holder wallet: $HOLDER_ADDRESS"
   
   # Fund with 0.1 SOL for rent
   solana airdrop 0.1 $HOLDER_ADDRESS --url devnet
   ```

3. **Update mock holders to use real wallet:**
   
   Edit `metabricks-x402-routes.js` temporarily:
   ```javascript
   // Replace mock holders with just your test wallet
   const holders = [{
     walletAddress: '<YOUR_HOLDER_ADDRESS>',  // Use your actual wallet
     balance: 1,
     brickNumber: 1
   }];
   ```

4. **Trigger SC-Gen payment:**
   ```bash
   curl -X POST http://localhost:5000/api/v1/payments/verify \
     -H "Content-Type: application/json" \
     -d '{
       "signature": "REAL_TEST_001",
       "amount": 1.0,
       "operation": "deploy",
       "blockchain": "rust"
     }'
   ```

5. **Watch MetaBricks logs:**
   ```
   💎 Executing REAL Solana distribution transaction...
   🔗 Connecting to Solana RPC: https://api.devnet.solana.com
   💰 Treasury balance: 5.0 SOL
   💸 Creating multi-recipient transaction for 1 holders...
   📝 Transaction created with 1 transfers
   📡 Sending transaction to Solana blockchain...
   ⏳ Confirming transaction: 5xYz...abc123
   ✅ Transaction confirmed: 5xYz...abc123
   ```

6. **Check holder wallet:**
   ```bash
   # Check balance (should have +0.5 SOL)
   solana balance $HOLDER_ADDRESS --url devnet
   
   # View transaction
   solana confirm <TRANSACTION_SIGNATURE> --url devnet
   
   # Or check on Solscan
   open "https://solscan.io/tx/<TRANSACTION_SIGNATURE>?cluster=devnet"
   ```

**Expected:** Your test wallet now has 0.5 SOL more! 💰

---

## 🎬 **Complete End-to-End Flow:**

```
1. User buys SC-Gen credits
   └─> Sends 1.0 SOL to treasury (3BTEJ9uANDQ5DqSZwmjQm2CsnGuofojBgViKRpVZco5X)

2. SC-Gen verifies payment
   └─> Calls /api/v1/payments/verify
   └─> Verifies transaction on Solana blockchain
   
3. SC-Gen triggers webhook (line 62 in X402PaymentService.cs)
   └─> POST http://localhost:3001/api/metabricks/sc-gen-webhook
   └─> Body: { signature, amount: 1.0, distributionPercentage: 50, ... }

4. MetaBricks backend receives webhook
   └─> Calculates: 0.5 SOL to holders, 0.5 SOL stays in treasury
   └─> Queries holders (mock: 432, or real: your test wallet)
   └─> Calculates per-holder: 0.5 SOL / 432 = 0.001157 SOL each
   
5. MetaBricks executes Solana transaction
   └─> Creates multi-recipient transfer
   └─> Signs with treasury keypair
   └─> Sends to Solana blockchain
   └─> Waits for confirmation
   
6. Holder receives SOL
   └─> Transaction confirmed on blockchain
   └─> 0.5 SOL appears in holder's Phantom wallet
   └─> Visible on Solscan
```

---

## 🔧 **Quick Setup Script:**

```bash
#!/bin/bash

echo "🚀 Setting up End-to-End x402 Testing"
echo ""

# 1. Set environment variables
export TREASURY_KEYPAIR_PATH=~/.config/solana/id.json
export SOLANA_RPC_URL=https://api.devnet.solana.com
export X402_ENABLE_REAL_DISTRIBUTIONS=true
export X402_USE_MOCK_DATA=true  # Use mock holders for now

# 2. Start MetaBricks backend
cd "/Volumes/Storage 2/OASIS_CLEAN/meta-bricks-main/backend"
echo "Starting MetaBricks backend..."
npm start &
METABRICKS_PID=$!

sleep 5

# 3. Start SC-Gen
cd "/Volumes/Storage 2/QS_Asset_Rail/smart-contract-generator/src/SmartContractGen/ScGen.API"
echo "Starting SC-Gen API..."
dotnet run &
SCGEN_PID=$!

sleep 10

# 4. Test the flow
echo ""
echo "✅ Both services running!"
echo ""
echo "MetaBricks: http://localhost:3001"
echo "SC-Gen: http://localhost:5000"
echo ""
echo "To test, run:"
echo "  curl -X POST http://localhost:5000/api/v1/payments/verify \\"
echo "    -H 'Content-Type: application/json' \\"
echo "    -d '{\"signature\":\"TEST\",\"amount\":0.1,\"operation\":\"compile\",\"blockchain\":\"rust\"}'"
echo ""

# Keep running
wait
```

---

## 🚨 **Current Limitations:**

### **What Works:**
- ✅ SC-Gen → MetaBricks webhook
- ✅ 50/50 split calculation
- ✅ Distribution recording
- ✅ Stats API
- ✅ Real Solana transaction code (ready)

### **What Needs Real Data:**
- ❌ Real holder wallet addresses (using mock 432 for now)
- ❌ Treasury keypair (need private key for `3BTEJ9uANDQ5DqSZwmjQm2CsnGuofojBgViKRpVZco5X`)
- ❌ Real MetaBricks NFTs in wallets

---

## 💡 **Recommended Test Path:**

### **TODAY: Test webhook flow with mock data**
1. Start both services
2. Trigger SC-Gen payment
3. Verify webhook received
4. Check distribution recorded
5. **DONE in 10 minutes**

### **THIS WEEK: Test with real Solana transactions**
1. Get treasury wallet private key
2. Create 1-3 test wallets
3. Mint test MetaBricks to them
4. Trigger payment
5. See SOL arrive in wallets
6. **Full proof-of-concept in 2 hours**

### **PRODUCTION: Deploy with 432 real holders**
1. Query all real MetaBricks holders from OASIS
2. Deploy to Heroku mainnet
3. Configure SC-Gen production webhook
4. Real revenue distribution begins!

---

## 📞 **What Do You Need From Me?**

To enable real SOL transfers, I need you to:

**Option 1: Give me the treasury private key**
- Save it as `~/.config/solana/id.json`
- Format: `[123, 45, 67, ..., 255]` (array of 64 bytes)

**Option 2: Create a new test treasury wallet**
```bash
solana-keygen new -o treasury-test.json
solana-keygen pubkey treasury-test.json
# Update TreasuryAddress in SC-Gen appsettings.json
# Fund with: solana airdrop 5 <address> --url devnet
```

**Which option works better for you?** 🔑

