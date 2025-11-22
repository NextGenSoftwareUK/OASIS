# End-to-End Testing Plan
## Real MetaBricks NFT → Real SC-Gen Revenue → Real SOL in Wallet

---

## 🎯 **Goal:**
Show a MetaBricks NFT holder receiving **actual SOL** in their wallet from SC-Gen revenue

---

## 📊 **Current Status vs. Required:**

### **What We Have Now:**
- ✅ Backend receives SC-Gen webhook
- ✅ Backend calculates 50/50 split
- ✅ Backend records distributions
- ❌ Backend uses MOCK holders (not real wallets)
- ❌ Backend doesn't send actual Solana transactions
- ❌ No real SOL being transferred

### **What We Need:**
- ✅ Real MetaBricks NFT in a real wallet
- ✅ Real SC-Gen payment transaction
- ✅ Query real NFT holders from blockchain
- ✅ Execute real Solana multi-recipient transfer
- ✅ Holder sees SOL arrive in their wallet

---

## 🚀 **End-to-End Testing Steps**

### **Phase 1: Set Up Test Environment** (30 min)

#### **1.1: Create Test Wallets**

```bash
# Install Solana CLI
brew install solana

# Create treasury wallet (for distributions)
solana-keygen new -o ~/.config/solana/x402-treasury-devnet.json

# Get the public key
solana-keygen pubkey ~/.config/solana/x402-treasury-devnet.json

# Create test holder wallet
solana-keygen new -o ~/.config/solana/test-brick-holder.json
solana-keygen pubkey ~/.config/solana/test-brick-holder.json

# Fund wallets with devnet SOL
solana airdrop 2 <TREASURY_PUBKEY> --url devnet
solana airdrop 1 <HOLDER_PUBKEY> --url devnet
```

**You'll have:**
- Treasury wallet: For holding/distributing revenue (2 SOL)
- Holder wallet: To receive distributions (1 SOL for gas)

---

#### **1.2: Mint Test MetaBricks NFT**

**Option A: Use your existing MetaBricks minting flow**
```bash
# Start MetaBricks backend
cd meta-bricks-main/backend
npm start

# In another terminal, mint to test wallet
curl -X POST http://localhost:3001/api/mint-nft \
  -H "Content-Type: application/json" \
  -d '{
    "walletAddress": "<HOLDER_WALLET_ADDRESS>",
    "brickId": "1",
    "brickName": "Test Brick #1",
    "paymentNetwork": "solana"
  }'
```

**Option B: Mint directly via OASIS API**
```bash
# Use the OASIS Web4 NFT minting endpoint at oasisweb4.one
```

**Result:** You'll have MetaBrick #1 NFT in your test wallet

---

### **Phase 2: Update Backend for Real Transactions** (1 hour)

#### **2.1: Add Real Solana Transaction Logic**

Update `/meta-bricks-main/backend/routes/metabricks-x402-routes.js`:

```javascript
// Add at top
const { Connection, PublicKey, Transaction, SystemProgram, LAMPORTS_PER_SOL, Keypair } = require('@solana/web3.js');
const fs = require('fs');

// Load treasury wallet
const TREASURY_KEYPAIR_PATH = process.env.TREASURY_KEYPAIR_PATH || 
                              `${process.env.HOME}/.config/solana/x402-treasury-devnet.json`;

let treasuryKeypair = null;
try {
  const keypairData = JSON.parse(fs.readFileSync(TREASURY_KEYPAIR_PATH));
  treasuryKeypair = Keypair.fromSecretKey(Uint8Array.from(keypairData));
  console.log('✅ Treasury wallet loaded:', treasuryKeypair.publicKey.toString());
} catch (error) {
  console.warn('⚠️  Treasury wallet not found - real distributions disabled');
}

// In sc-gen-webhook route, replace mock distribution with:
async function executeRealDistribution(holders, amountPerHolder, connection) {
  if (!treasuryKeypair) {
    throw new Error('Treasury wallet not configured');
  }

  console.log('💰 Creating real Solana distribution transaction...');
  
  // Create transaction with multiple transfers
  const transaction = new Transaction();
  
  for (const holder of holders) {
    transaction.add(
      SystemProgram.transfer({
        fromPubkey: treasuryKeypair.publicKey,
        toPubkey: new PublicKey(holder.walletAddress),
        lamports: amountPerHolder
      })
    );
  }
  
  // Send transaction
  const signature = await connection.sendTransaction(transaction, [treasuryKeypair]);
  await connection.confirmTransaction(signature);
  
  console.log(`✅ Real distribution transaction: ${signature}`);
  return signature;
}
```

---

#### **2.2: Query Real NFT Holders from OASIS**

Update `queryMetaBricksHolders` function:

```javascript
async function queryMetaBricksHolders(distributor) {
  console.log('🔍 Querying REAL MetaBricks holders from OASIS API...');
  
  try {
    // Query OASIS API for MetaBricks collection holders
    const oasisApiUrl = process.env.OASIS_API_URL || 'http://oasisweb4.one';
    const token = process.env.OASIS_TOKEN; // You'd get this from authenticateWithOASIS()
    
    const response = await axios.post(
      `${oasisApiUrl}/api/nft/get-nft-holders`,
      {
        CollectionSymbol: 'MBRICK',
        Provider: 'SolanaOASIS'
      },
      {
        headers: { 'Authorization': `Bearer ${token}` }
      }
    );
    
    if (response.data?.result?.holders) {
      console.log(`✅ Found ${response.data.result.holders.length} real holders`);
      return response.data.result.holders;
    }
  } catch (error) {
    console.error('❌ Failed to query OASIS for holders:', error.message);
  }
  
  // Fallback to mock data
  return generateMockHolders();
}
```

---

### **Phase 3: End-to-End Test Flow** (15 min)

#### **Step 1: Verify Setup**

```bash
# Check treasury wallet balance
solana balance <TREASURY_PUBKEY> --url devnet

# Should show: 2.0 SOL (or whatever you airdropped)
```

---

#### **Step 2: Verify NFT Ownership**

```bash
# Query wallet for NFTs
spl-token accounts <HOLDER_WALLET_ADDRESS> --url devnet

# Or use OASIS API
curl -X POST http://oasisweb4.one/api/nft/get-nfts-owned-by-address \
  -H "Content-Type: application/json" \
  -d '{"walletAddress": "<HOLDER_WALLET_ADDRESS>", "provider": "SolanaOASIS"}'
```

**Expected:** Should see MetaBrick #1 NFT

---

#### **Step 3: Simulate SC-Gen Payment**

```bash
# Send webhook to your backend
curl -X POST http://localhost:3001/api/metabricks/sc-gen-webhook \
  -H "Content-Type: application/json" \
  -d '{
    "signature": "REAL_TEST_TX_001",
    "amount": 1.0,
    "operation": "purchase-credits",
    "distributionPercentage": 50,
    "nftCollection": "METABRICKS",
    "metadata": {
      "packName": "Test Pack",
      "credits": 100
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
    "holders": 1,              // Just your test wallet
    "amountPerHolder": 0.5,
    "distributionTx": "5xYz...abc123",  // Real Solana tx signature
    "fundingTx": "REAL_TEST_TX_001",
    "timestamp": 1762118000000
  }
}
```

---

#### **Step 4: Verify SOL Received**

```bash
# Check holder wallet balance (should have increased)
solana balance <HOLDER_WALLET_ADDRESS> --url devnet

# Check transaction on Solscan
echo "https://solscan.io/tx/<DISTRIBUTION_TX>?cluster=devnet"

# Or check via Solana CLI
solana confirm <DISTRIBUTION_TX> --url devnet
```

**Expected:** 
- Wallet balance increased by 0.5 SOL
- Transaction confirmed on blockchain
- Visible in Phantom wallet

---

### **Phase 4: Full Production Flow** (with real SC-Gen)

#### **4.1: Configure SC-Gen Webhook**

In SC-Gen's configuration:

```json
{
  "X402": {
    "WebhookUrl": "http://localhost:3001/api/metabricks/sc-gen-webhook",
    "DistributionPercentage": 50,
    "NFTCollection": "METABRICKS"
  }
}
```

---

#### **4.2: Make Real Purchase in SC-Gen**

```bash
# Developer buys credits in SC-Gen
# SC-Gen processes payment
# SC-Gen sends webhook automatically
```

---

#### **4.3: Monitor Distribution**

```bash
# Watch backend logs
tail -f /tmp/metabricks-server.log | grep "MetaBricks"

# You should see:
# 🧱 MetaBricks distribution webhook triggered
# 💰 Distribution Details: Total payment: 1.0 SOL
# 🔍 Querying MetaBricks NFT holders...
# 👥 Found 1 MetaBricks holders
# ✅ Distributed 0.5 SOL to 1 MetaBricks holders
```

---

#### **4.4: Verify in Wallet**

**Open Phantom Wallet:**
1. Connect to Devnet (Settings → Developer Settings → Network → Devnet)
2. View wallet transactions
3. Should see incoming SOL transaction
4. From: Treasury wallet
5. Amount: 0.5 SOL
6. Memo: "MetaBricks Revenue Distribution"

---

## 🔧 **Implementation Checklist**

### **Required Code Changes:**

- [ ] **1. Add treasury wallet loading** (20 lines)
  - Location: `metabricks-x402-routes.js`
  - Load Solana keypair from file

- [ ] **2. Implement real transaction execution** (50 lines)
  - Location: `metabricks-x402-routes.js`
  - Create multi-recipient Solana transaction
  - Send and confirm on blockchain

- [ ] **3. Query real holders from OASIS** (30 lines)
  - Location: `queryMetaBricksHolders()` function
  - Call OASIS API to get actual NFT holders
  - Fallback to mock if query fails

- [ ] **4. Add environment variables**
  ```bash
  TREASURY_KEYPAIR_PATH=~/.config/solana/x402-treasury-devnet.json
  SOLANA_RPC_URL=https://api.devnet.solana.com
  X402_ENABLE_REAL_DISTRIBUTIONS=true
  X402_USE_MOCK_DATA=false
  ```

---

## 🧪 **Testing Scenarios**

### **Scenario 1: Single Holder (Your Test)**
```
NFT Holders: 1 (your test wallet)
SC-Gen Payment: 1.0 SOL
Distribution: 0.5 SOL → your wallet
Treasury: 0.5 SOL kept
```

### **Scenario 2: Multiple Holders**
```
NFT Holders: 3 test wallets
SC-Gen Payment: 3.0 SOL
Distribution: 1.5 SOL split 3 ways → 0.5 SOL each
Treasury: 1.5 SOL kept
```

### **Scenario 3: Production (432 holders)**
```
NFT Holders: 432 real wallets
SC-Gen Payment: 100 SOL
Distribution: 50 SOL split 432 ways → 0.1157 SOL each
Treasury: 50 SOL kept
```

---

## 💰 **Cost to Test:**

### **Devnet (FREE):**
- ✅ Free SOL via airdrop
- ✅ Free transactions
- ✅ Same code as mainnet
- ✅ Perfect for testing

### **Mainnet (REAL MONEY):**
- ⚠️  Need real SOL (~$100 to test properly)
- ⚠️  Transaction fees (~$0.0001 per transaction)
- ⚠️  Irreversible distributions

**Recommendation: Test on devnet first!**

---

## 🔍 **What's Missing Right Now:**

### **1. Real Solana Transaction Execution**

**Current code:**
```javascript
// Generate distribution transaction (mock for now)
const distributionTx = `metabricks_dist_${Date.now().toString(36)}`;
```

**Needed:**
```javascript
// Execute REAL Solana transaction
const connection = new Connection('https://api.devnet.solana.com');
const signature = await executeMultiRecipientTransfer(
  treasuryKeypair,
  holders,
  amountPerHolder,
  connection
);
```

---

### **2. Real NFT Holder Query**

**Current code:**
```javascript
// Mock holders
for (let i = 1; i <= 432; i++) {
  holders.push({
    walletAddress: `MetaBrick${i}Holder${Math.random().toString(36).substring(7)}`,
    balance: 1
  });
}
```

**Needed:**
```javascript
// Query OASIS API for actual MetaBricks holders
const response = await oasisApi.getNFTHolders({
  collectionSymbol: 'MBRICK',
  provider: 'SolanaOASIS'
});

const realHolders = response.holders.map(h => ({
  walletAddress: h.walletAddress,  // Real Solana address
  balance: h.tokenBalance
}));
```

---

### **3. Treasury Wallet with Funds**

**Current:** No wallet configured

**Needed:**
```bash
# Create and fund treasury wallet
solana-keygen new -o treasury.json
solana airdrop 10 $(solana-keygen pubkey treasury.json) --url devnet

# Set in environment
export TREASURY_KEYPAIR_PATH=./treasury.json
```

---

## 🎬 **Complete End-to-End Flow**

```
┌─────────────────────────────────────────────────────────────┐
│ STEP 1: Initial Setup                                      │
└─────────────────────────────────────────────────────────────┘

You (testing):
  1. Create Phantom wallet
  2. Mint MetaBrick #1 NFT to your wallet (via metabricks.xyz or API)
  3. Verify NFT is in your wallet (check Phantom on devnet)

Treasury Setup:
  4. Create treasury wallet with solana-keygen
  5. Fund treasury with 10 SOL (via airdrop on devnet)
  6. Configure backend with treasury keypair path


┌─────────────────────────────────────────────────────────────┐
│ STEP 2: Configure Backend for Real Distributions           │
└─────────────────────────────────────────────────────────────┘

Update metabricks-x402-routes.js:
  7. Add treasury wallet loading code
  8. Add real Solana transaction execution
  9. Update holder query to check blockchain
  10. Set environment variables

Restart backend:
  11. npm start
  12. Verify logs show "✅ Treasury wallet loaded"


┌─────────────────────────────────────────────────────────────┐
│ STEP 3: Trigger SC-Gen Payment (Simulated)                 │
└─────────────────────────────────────────────────────────────┘

Simulate SC-Gen sending webhook:
  13. curl -X POST http://localhost:3001/api/metabricks/sc-gen-webhook \
        -d '{"amount": 1.0, "distributionPercentage": 50, ...}'

Backend processes:
  14. Receives webhook ✅
  15. Calculates: 0.5 SOL to holders, 0.5 SOL to treasury ✅
  16. Queries blockchain for MetaBricks holders → finds your wallet ✅
  17. Creates Solana transaction sending 0.5 SOL to your wallet ✅
  18. Signs with treasury wallet ✅
  19. Submits to Solana blockchain ✅
  20. Returns transaction signature ✅


┌─────────────────────────────────────────────────────────────┐
│ STEP 4: Verify You Received SOL                            │
└─────────────────────────────────────────────────────────────┘

Check your Phantom wallet:
  21. Open Phantom → Switch to Devnet
  22. Check balance (should have +0.5 SOL)
  23. View transaction history
  24. See incoming transfer from treasury wallet

Check on Solscan:
  25. Visit https://solscan.io/tx/<SIGNATURE>?cluster=devnet
  26. See: Treasury → Your Wallet (0.5 SOL)
  27. Status: Confirmed ✅

Check backend stats:
  28. curl http://localhost:3001/api/metabricks/stats
  29. Should show: totalDistributed += 0.5 SOL
```

---

## 📋 **Quick Setup Commands**

### **Run This to Set Up Everything:**

```bash
#!/bin/bash

echo "🚀 Setting up End-to-End Testing"

# 1. Create wallets
echo "1. Creating wallets..."
solana-keygen new -o treasury-devnet.json --no-bip39-passphrase
solana-keygen new -o holder-devnet.json --no-bip39-passphrase

TREASURY_PUBKEY=$(solana-keygen pubkey treasury-devnet.json)
HOLDER_PUBKEY=$(solana-keygen pubkey holder-devnet.json)

echo "   Treasury: $TREASURY_PUBKEY"
echo "   Holder: $HOLDER_PUBKEY"

# 2. Fund wallets
echo "2. Funding wallets with devnet SOL..."
solana airdrop 10 $TREASURY_PUBKEY --url devnet
solana airdrop 1 $HOLDER_PUBKEY --url devnet

# 3. Check balances
echo "3. Wallet balances:"
echo "   Treasury: $(solana balance $TREASURY_PUBKEY --url devnet)"
echo "   Holder: $(solana balance $HOLDER_PUBKEY --url devnet)"

# 4. Set environment variables
echo "4. Setting environment..."
export TREASURY_KEYPAIR_PATH=./treasury-devnet.json
export SOLANA_RPC_URL=https://api.devnet.solana.com
export X402_ENABLE_REAL_DISTRIBUTIONS=true
export X402_USE_MOCK_DATA=false

echo ""
echo "✅ Setup complete!"
echo ""
echo "Next steps:"
echo "  1. Mint MetaBricks NFT to: $HOLDER_PUBKEY"
echo "  2. Update backend code for real transactions"
echo "  3. Start backend: npm start"
echo "  4. Trigger SC-Gen webhook"
echo "  5. Check holder wallet for SOL!"
```

---

## 🎯 **Simplified Test (No Real Transactions Yet)**

### **What You Can Test RIGHT NOW (No Code Changes):**

1. **Check backend is working:**
   ```bash
   curl http://localhost:3001/api/metabricks/stats
   ```

2. **Simulate payment:**
   ```bash
   curl -X POST http://localhost:3001/api/metabricks/sc-gen-webhook \
     -H "Content-Type: application/json" \
     -d '{"signature":"TEST","amount":1.0,"distributionPercentage":50,"nftCollection":"METABRICKS"}'
   ```

3. **See it recorded:**
   ```bash
   cat data/x402/x402-distributions.json | python3 -m json.tool
   ```

**This proves:**
- ✅ Webhook endpoint works
- ✅ Distribution calculation works (50/50 split)
- ✅ Data storage works
- ✅ Stats API works

**What it doesn't prove (yet):**
- ❌ Real SOL being transferred
- ❌ Real wallets receiving funds

---

## 🚀 **To Make It FULLY Real (Next 1-2 Hours):**

I need to:
1. ✅ Add Solana transaction execution code
2. ✅ Add treasury wallet loading
3. ✅ Query real NFT holders (or use test wallet for now)
4. ✅ Execute real blockchain transfers

**Want me to implement the real transaction code now?**

Or would you prefer to:
- Test with mock data more first?
- Review the architecture diagram I can create?
- Deploy to Heroku as-is (with mock distributions)?

Let me know which path you want to take! 🎯

