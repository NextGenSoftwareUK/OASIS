# MetaBricks NFT Metadata Update Plan for x402

## 🎯 **Goal:**
Update MetaBricks NFT metadata to include x402 revenue distribution information

---

## 📊 **Current Situation:**

### **NFTs Minted:** 0 (starting fresh ✅)
### **Metadata Location:** Pinata IPFS
### **Current Metadata Structure:**

Based on the existing metadata URLs, current MetaBricks metadata looks like:
```json
{
  "name": "MetaBrick #1",
  "description": "A unique MetaBrick NFT for the metaverse",
  "image": "https://gateway.pinata.cloud/ipfs/...",
  "attributes": [
    {
      "trait_type": "Brick Type",
      "value": "Regular"
    },
    {
      "trait_type": "Brick Number",
      "value": "1"
    }
  ]
}
```

---

## 🆕 **Updated Metadata with x402:**

### **New Structure:**

```json
{
  "name": "MetaBrick #1",
  "description": "MetaBrick NFT: Brick #1. Earn passive income from AssetRail Smart Contract Generator API revenue. This NFT distributes 50% of SC-Gen revenue equally among all 432 brick holders.",
  "image": "https://gateway.pinata.cloud/ipfs/...",
  "attributes": [
    {
      "trait_type": "Brick Type",
      "value": "Regular"
    },
    {
      "trait_type": "Brick Number",
      "value": "1"
    },
    {
      "trait_type": "Revenue Sharing",
      "value": "Enabled"
    },
    {
      "trait_type": "Distribution Model",
      "value": "Equal Split"
    },
    {
      "trait_type": "Holder Share",
      "value": "50%"
    },
    {
      "trait_type": "Revenue Source",
      "value": "SC-Gen API"
    }
  ],
  "x402": {
    "enabled": true,
    "version": "1.0",
    "revenueSource": "AssetRail Smart Contract Generator",
    "distributionModel": "equal",
    "distributionPercentage": 50,
    "treasuryAddress": "H8tjfEMYNkTDdSbRBekMBTcE966vGRF2WnCThRRoQJ1",
    "totalSupply": 432,
    "webhookEndpoint": "https://metabricks-backend.herokuapp.com/api/metabricks/sc-gen-webhook"
  },
  "external_url": "https://metabricks.xyz"
}
```

---

## 🔧 **How NFT Minting Currently Works:**

### **Code Location:** `/meta-bricks-main/backend/server.js` (lines 646-702)

### **Current Flow:**

```javascript
// When user buys a brick:
POST /api/mint-nft

// Backend prepares request:
const oasisRequest = {
  Title: "MetaBrick #42",
  Description: "MetaBrick NFT: Brick #42. Earn passive income from...",
  Symbol: 'MBRICK',
  OnChainProvider: { value: 3, name: 'SolanaOASIS' },
  OffChainProvider: { value: 23, name: 'MongoDBOASIS' },
  NFTOffChainMetaType: { value: 3, name: 'ExternalJsonURL' },
  NFTStandardType: { value: 2, name: 'SPL' },
  
  // THIS is the key - points to Pinata metadata
  JSONMetaDataURL: "https://gateway.pinata.cloud/ipfs/QmXsv...",
  
  ImageUrl: 'https://gateway.pinata.cloud/ipfs/...',
  SendToAddressAfterMinting: userWalletAddress,
  
  // x402 config is ALREADY HERE (lines 666-673)
  x402Config: {
    enabled: true,
    nftCollection: 'METABRICKS',
    revenueSource: 'AssetRail Smart Contract Generator API',
    revenueModel: 'equal',
    distributionPercentage: 50,  // ✅ Already 50%
    totalNFTs: 432
  }
};

// Send to OASIS API
await makeOASISRequest('/api/nft/mint-nft', oasisRequest);
```

### **Key Points:**

1. **Metadata on Pinata** - Each brick has its own IPFS hash
2. **x402 Config** - Already in minting request (lines 666-673)
3. **OASIS Handles Minting** - MetaBricks → OASIS API → Solana blockchain
4. **Holder Address** - NFT sent to user's Phantom wallet

---

## 🎨 **Updating Metadata for x402:**

### **Option A: Upload New Metadata to Pinata** ✅ Recommended

For each of the 432 bricks, upload updated JSON to Pinata with x402 info:

```javascript
// Script to generate and upload metadata
const fs = require('fs');
const FormData = require('form-data');
const axios = require('axios');

const PINATA_API_KEY = process.env.PINATA_API_KEY;
const PINATA_SECRET = process.env.PINATA_SECRET;

for (let i = 1; i <= 432; i++) {
  const metadata = {
    name: `MetaBrick #${i}`,
    description: `MetaBrick NFT: Brick #${i}. Earn passive income from AssetRail Smart Contract Generator API revenue. This NFT distributes 50% of SC-Gen revenue equally among all 432 brick holders.`,
    image: getBrickImageUrl(i),
    attributes: [
      { trait_type: "Brick Number", value: i.toString() },
      { trait_type: "Brick Type", value: getBrickType(i) },
      { trait_type: "Revenue Sharing", value: "Enabled" },
      { trait_type: "Distribution Model", value: "Equal Split" },
      { trait_type: "Holder Share", value: "50%" },
      { trait_type: "Revenue Source", value: "SC-Gen API" },
      { trait_type: "Treasury Wallet", value: "H8tjfEMYNkTDdSbRBekMBTcE966vGRF2WnCThRRoQJ1" }
    ],
    x402: {
      enabled: true,
      version: "1.0",
      revenueSource: "AssetRail Smart Contract Generator",
      distributionModel: "equal",
      distributionPercentage: 50,
      treasuryAddress: "H8tjfEMYNkTDdSbRBekMBTcE966vGRF2WnCThRRoQJ1",
      totalSupply: 432,
      webhookEndpoint: "https://metabricks-backend.herokuapp.com/api/metabricks/sc-gen-webhook"
    },
    external_url: "https://metabricks.xyz"
  };
  
  // Upload to Pinata
  await uploadToPinata(metadata, `metabrick-${i}-x402.json`);
}
```

### **Option B: Use Dynamic Metadata** 

Store x402 config separately and merge at mint time (less work, but less transparent)

---

## 🏗️ **Complete NFT Minting Flow with x402:**

```
Step 1: User Visits metabricks.xyz
   └─> Selects brick (e.g., Brick #42)
   └─> Connects Phantom wallet
   └─> Pays 0.4 SOL (or $50 via Stripe)

Step 2: Frontend sends mint request to backend
   └─> POST /api/mint-nft
   └─> Body: { walletAddress, brickId: 42, ... }

Step 3: Backend prepares OASIS request
   └─> Gets metadata URL for brick #42
   └─> Adds x402 config to request
   └─> Calls OASIS API: POST /api/nft/mint-nft

Step 4: OASIS mints NFT on Solana
   └─> Creates SPL token
   └─> Sets metadata URI → Pinata
   └─> Transfers to user's wallet
   └─> **Stores x402 config in OASIS database**

Step 5: User receives NFT
   └─> MetaBrick #42 appears in Phantom wallet
   └─> Metadata shows "Revenue Sharing: Enabled"
   └─> NFT is now ready to receive distributions

---

Later: When SC-Gen receives payment
   └─> SC-Gen triggers webhook
   └─> MetaBricks backend queries NFT holders
   └─> **User's wallet address is returned**
   └─> Distribution executed
   └─> User receives SOL!
```

---

## 🔍 **How to Get Real Holder Addresses:**

### **After NFTs Are Minted:**

**Method 1: Query Solana Blockchain Directly**

```javascript
const { Connection, PublicKey } = require('@solana/web3.js');

async function getMetaBricksHolders() {
  const connection = new Connection('https://api.devnet.solana.com');
  
  // Get all SPL token accounts for MetaBricks collection
  // This requires knowing the mint addresses of all 432 NFTs
  
  const holders = [];
  
  for (const mintAddress of allMetaBricksMints) {
    const accounts = await connection.getTokenLargestAccounts(
      new PublicKey(mintAddress)
    );
    
    // Get the owner of the largest account (the holder)
    for (const account of accounts.value) {
      const accountInfo = await connection.getParsedAccountInfo(account.address);
      const owner = accountInfo.value?.data?.parsed?.info?.owner;
      
      if (owner) {
        holders.push({
          walletAddress: owner,
          mintAddress: mintAddress,
          balance: 1
        });
      }
    }
  }
  
  return holders;
}
```

**Method 2: Query OASIS API** (Easier!)

```javascript
async function getMetaBricksHoldersFromOASIS() {
  const token = await authenticateWithOASIS();
  
  const response = await axios.post(
    'http://oasisweb4.one/api/nft/get-nfts-by-collection',
    {
      CollectionSymbol: 'MBRICK',
      Provider: 'SolanaOASIS'
    },
    {
      headers: { 'Authorization': `Bearer ${token}` }
    }
  );
  
  // Extract holder addresses from NFT records
  const holders = response.data.result.nfts.map(nft => ({
    walletAddress: nft.OwnerAddress,
    mintAddress: nft.MintAddress,
    balance: 1,
    brickNumber: nft.Metadata?.brickNumber
  }));
  
  return holders;
}
```

**Method 3: Store During Minting** (Simplest!)

```javascript
// In /api/mint-nft endpoint, after successful mint:
await storageUtils.recordNFTHolder({
  walletAddress: mintData.walletAddress,
  brickId: mintData.brickId,
  mintAddress: result.mintAccount,
  timestamp: Date.now()
});

// Then query from local storage:
async function getMetaBricksHolders() {
  return await storageUtils.getAllNFTHolders('METABRICKS');
}
```

---

## 📋 **Action Plan:**

### **Step 1: Treasury Wallet** ✅ DONE
```
Address: H8tjfEMYNkTDdSbRBekMBTcE966vGRF2WnCThRRoQJ1
Keypair: /Volumes/Storage 2/OASIS_CLEAN/x402/metabricks-integration/treasury-devnet.json
Network: Devnet
Balance: 5 SOL (airdropped)
```

### **Step 2: Update Metadata** (Choose approach)

**Option A: Update Pinata** (30 min per 432 bricks)
- Upload new JSON with x402 info
- Update BRICK_METADATA_URLS in server.js

**Option B: Keep Current Metadata** (0 min)
- x402 config is in OASIS minting request
- Metadata on Pinata can stay as-is
- x402 info stored in OASIS database

### **Step 3: Track Holders** (Choose approach)

**Option A: Store During Minting** ✅ EASIEST
```javascript
// Add to /api/mint-nft after line 826:
await storageUtils.recordNFTHolder({
  walletAddress: mintData.walletAddress,
  brickId: mintData.brickId,
  mintAddress: result.mintAccount
});
```

**Option B: Query OASIS API** (when needed)
```javascript
// Query OASIS for all MetaBricks NFTs
const nfts = await oasisApi.getNFTsByCollection('MBRICK');
const holders = nfts.map(nft => nft.ownerAddress);
```

**Option C: Query Solana Directly** (most complex)

---

## 🚀 **Recommended Quick Path:**

### **1. Use Existing Metadata** (No Pinata updates needed)
   - x402 config is already in minting request ✅
   - Description already mentions "Earn passive income" ✅

### **2. Store Holders During Minting**
   - Add 5 lines to `/api/mint-nft` endpoint
   - Track holder addresses in local storage
   - Query from storage for distributions

### **3. Enable Real Distributions**
   - Use new treasury wallet: `H8tjfEMYNkTDdSbRBekMBTcE966vGRF2WnCThRRoQJ1`
   - Switch to REAL routes
   - Test with 1 NFT first

---

## 🧪 **Testing Flow:**

```
1. Mint 1 Test NFT
   └─> To your Phantom wallet
   └─> x402 config included
   └─> Wallet address stored

2. Trigger SC-Gen Payment
   └─> SC-Gen webhook fires
   └─> Backend queries holder (your wallet)
   └─> Creates Solana transaction

3. You Receive SOL
   └─> 0.5 SOL appears in Phantom
   └─> Transaction visible on Solscan
   └─> Proof of concept complete!

4. Mint Remaining 431 NFTs
   └─> All holders tracked
   └─> Future distributions go to all 432

5. Production Ready
   └─> Deploy to Heroku
   └─> Configure SC-Gen mainnet
   └─> Real revenue distribution begins
```

---

## 💡 **Want me to:**

1. **Add holder tracking to minting** (~5 min)
2. **Enable real Solana distributions** (~10 min)
3. **Create metadata upload script** (~30 min)
4. **Test with 1 NFT end-to-end** (~15 min)

**Which should we tackle first?** 🎯

