# x402 Integration - Final Action Plan

## ✅ **What's Complete:**

1. **Treasury Wallet:** `H8tjfEMYNkTDdSbRBekMBTcE966vGRF2WnCThRRoQJ1`
2. **50/50 Split:** Configured everywhere
3. **SC-Gen Webhook:** Pointing to MetaBricks
4. **Blockchain Query:** Code queries CURRENT owners (not database)
5. **Real Solana Transfers:** Code ready

---

## 🎯 **Critical Issue Resolved:**

### **THE INSIGHT:**
x402 distribution rights MUST be embedded IN the NFT so they transfer with ownership.

### **THE FIX:**
Updated code to query blockchain for CURRENT holders, not a static database.

```javascript
// OLD (WRONG):
const holders = database.getHolders(); // Static list of original minters

// NEW (CORRECT):
const holders = await oasisApi.getNFTsByCollection('MBRICK');
// Returns CURRENT owners from Solana blockchain
// Automatically reflects transfers!
```

---

## 🔍 **Two Questions to Answer:**

### **Question 1: Does OASIS Store x402 Config?**

Your minting request includes:
```javascript
x402Config: {
  enabled: true,
  distributionPercentage: 50,
  // ...
}
```

**Check:** Does OASIS:
- ✓ Store this config in its database?
- ✓ Link it to the NFT on-chain?
- ✓ Return it when querying NFTs?

**How to check:**
1. Mint 1 test NFT with x402 config
2. Query OASIS: `GET /api/nft/get-nft/{mintAddress}`
3. See if response includes x402Config

**If YES:** OASIS handles everything! ✅  
**If NO:** Need to update Pinata metadata ❌

---

### **Question 2: Does OASIS Return Current Owners?**

**Check:** When querying NFTs by collection, does OASIS return:
- Original minter? ❌
- Current owner (from blockchain)? ✅

**How to check:**
1. Mint NFT to Wallet A
2. Transfer NFT to Wallet B
3. Query OASIS for NFT
4. Does it show Wallet A or Wallet B?

**If Wallet B:** Perfect! ✅  
**If Wallet A:** Need to query Solana directly ❌

---

## 🚀 **Action Plan:**

### **Step 1: Test OASIS x402 Support** (10 min)

```bash
# 1. Mint 1 test NFT
curl -X POST http://localhost:3001/api/mint-nft \
  -H "Content-Type: application/json" \
  -d '{
    "walletAddress": "<YOUR_WALLET>",
    "brickId": "1",
    "paymentNetwork": "solana"
  }'

# 2. Get the mint address from response
MINT_ADDRESS="<from response>"

# 3. Query OASIS for this NFT
curl -X POST http://oasisweb4.one/api/nft/get-nft \
  -H "Authorization: Bearer <token>" \
  -d '{"mintAddress": "'$MINT_ADDRESS'", "provider": "SolanaOASIS"}'

# 4. Check response:
# - Does it include x402Config? 
# - Does it show current owner?
```

---

### **Step 2A: If OASIS Supports x402** ✅

**Then we're DONE!** Just need to:
1. Enable `X402_QUERY_REAL_HOLDERS=true`
2. OASIS returns current holders with x402 config
3. Distributions work automatically

---

### **Step 2B: If OASIS Doesn't Support x402** ❌

**Then we need to:**

**Option 1: Update Pinata Metadata** (2-3 hours)
```bash
# Run the metadata upload script
cd /meta-bricks-main/backend
node ../../../x402/metabricks-integration/METADATA_UPLOAD_SCRIPT.js

# Updates all 432 metadata files on Pinata
# Each includes x402 config
```

**Option 2: Use Token-2022 Extensions** (requires new minting)
- Mint with Solana Token-2022
- Use metadata extension to store x402 on-chain
- Most robust but requires smart contract changes

---

## 💡 **My Recommendation:**

### **DO THIS NOW:**

1. **Test what OASIS does with x402Config**
   - Mint 1 test NFT
   - Query it back from OASIS
   - See if x402 config is stored/returned

2. **If OASIS handles it:**
   - ✅ We're done!
   - Just enable blockchain queries
   - Deploy to production

3. **If OASIS doesn't handle it:**
   - Update Pinata metadata (automated script ready)
   - Takes 2-3 hours for 432 files
   - Then deploy

---

## 📊 **What Happens in Each Scenario:**

### **Scenario: NFT Transfer**

**With Database Approach (WRONG):**
```
Alice mints NFT #1
  └─> Database: Alice owns #1
  
Alice sells NFT #1 to Bob
  └─> Blockchain: Bob owns #1
  └─> Database: Still says Alice owns #1
  
Distribution:
  └─> Query database → Alice
  └─> Send to Alice ❌ (Bob should get it!)
```

**With Blockchain Query (CORRECT):**
```
Alice mints NFT #1
  └─> Blockchain: Alice owns #1
  └─> Metadata: x402 config embedded
  
Alice sells NFT #1 to Bob
  └─> Blockchain: Bob owns #1
  └─> Metadata: Same x402 config (transfers with NFT)
  
Distribution:
  └─> Query blockchain → Bob
  └─> Send to Bob ✅ (correct!)
```

---

## 🧪 **Quick Test to Verify:**

```bash
# 1. Mint NFT to your wallet
# 2. Check backend logs for x402 config being sent to OASIS
# 3. Query OASIS for the NFT back
# 4. See if x402Config is in the response

# If yes → Distribution rights are embedded!
# If no → Need to add to Pinata metadata
```

---

## 📞 **What Do You Want to Do?**

**Option A:** Test OASIS first (5 min)
- Mint 1 NFT
- Query it back
- See what OASIS returns

**Option B:** Update Pinata now (2 hours)
- Run metadata upload script
- Embed x402 in all 432 metadata files
- Guaranteed to work

**Option C:** Check OASIS documentation
- See if x402Config is a supported field
- Confirm before minting

**Which path?** 🎯


