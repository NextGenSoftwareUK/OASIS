# x402 NFT Embedding - Proper Implementation

## 🎯 **Critical Requirement:**

**Distribution rights MUST be embedded in the NFT itself**, not stored in a separate database.

**Why?**
- NFT ownership = distribution rights
- Transfer NFT = transfer distribution rights automatically
- No manual tracking needed

---

## ❌ **Wrong Approach (What I Was Doing):**

### **Storing Holders in Database:**

```javascript
// When NFT mints
await database.save({ wallet: "Alice", nftId: "NFT1" });

// Later, NFT transfers from Alice to Bob
// But database still says Alice owns it!

// Distribution happens
query database → finds Alice → sends SOL to Alice ❌
// But Bob owns the NFT now!
```

**Problem:** Distribution rights don't transfer with NFT ownership

---

## ✅ **Correct Approach:**

### **Method 1: Embed in On-Chain Metadata (SPL Token Extensions)**

**Using Solana Token-2022 with metadata extension:**

```rust
// In Solana program (lib.rs)
use solana_program::program_pack::Pack;
use spl_token_2022::extension::metadata_pointer::MetadataPointer;

pub fn initialize_x402_nft(
    mint_account: &AccountInfo,
    metadata_account: &AccountInfo,
) -> ProgramResult {
    // Create NFT with x402 metadata extension
    let metadata = X402Metadata {
        enabled: true,
        distribution_percentage: 50,
        revenue_source: "SC-Gen API",
        distribution_model: "equal",
        treasury_address: "3BTEJ9...",
        webhook_endpoint: "https://metabricks.../webhook",
    };
    
    // Store on-chain
    MetadataPointer::pack(metadata, &mut mint_account.data.borrow_mut())?;
    
    Ok(())
}

// When distributing:
pub fn get_nft_holders_with_x402() -> Vec<Pubkey> {
    // Query all tokens with x402 metadata extension
    // Returns CURRENT holders from blockchain
    // Automatically reflects transfers!
}
```

---

### **Method 2: Embed in Metadata URI (Pinata + On-Chain Link)**

**Update Pinata metadata for ALL 432 bricks:**

```json
{
  "name": "MetaBrick #1",
  "description": "...",
  "image": "...",
  "attributes": [...],
  
  "x402": {
    "enabled": true,
    "version": "1.0",
    "distributionPercentage": 50,
    "revenueSource": "AssetRail Smart Contract Generator",
    "distributionModel": "equal",
    "treasuryAddress": "H8tjfEMYNkTDdSbRBekMBTcE966vGRF2WnCThRRoQJ1",
    "totalSupply": 432,
    "webhookEndpoint": "https://metabricks-backend.herokuapp.com/api/metabricks/sc-gen-webhook",
    "collectionIdentifier": "MBRICK_X402_V1"
  }
}
```

**Then on-chain, the NFT stores:**
- `metadataUri` → Points to Pinata JSON above
- When transferred, new owner gets NFT with same metadata
- x402 config stays with NFT forever

**Distribution query:**
```javascript
// Get ALL holders of MetaBricks collection from Solana blockchain
async function getCurrentHolders() {
  // 1. Get all NFTs in MBRICK collection
  const allNFTs = await queryNFTsByCollection("MBRICK");
  
  // 2. For each NFT, get CURRENT owner from blockchain
  const holders = [];
  for (const nft of allNFTs) {
    const tokenAccount = await connection.getTokenLargestAccounts(nft.mint);
    const owner = await connection.getParsedAccountInfo(tokenAccount.value[0].address);
    holders.push(owner.value.data.parsed.info.owner);
  }
  
  // 3. Returns CURRENT owners (reflects all transfers)
  return holders;
}
```

---

### **Method 3: Use OASIS's Built-in x402 Storage (If Supported)**

**Check if OASIS API supports x402 natively:**

```javascript
// When minting
const oasisRequest = {
  Title: "MetaBrick #1",
  Symbol: "MBRICK",
  x402Config: {
    enabled: true,
    distributionPercentage: 50,
    revenueSource: "SC-Gen",
    // ... other config
  }
};

// OASIS stores this and links it to the NFT on-chain

// When distributing
const nftsWithX402 = await oasisApi.getNFTsWithX402Config({
  collectionSymbol: "MBRICK"
});

// Returns CURRENT holders because OASIS queries blockchain
```

---

## 🔍 **How to Query Current Holders:**

### **Option A: Query Solana Directly**

```javascript
const { Connection, PublicKey } = require('@solana/web3.js');

async function getMetaBricksCurrentHolders() {
  const connection = new Connection('https://api.devnet.solana.com');
  
  // Get all MetaBricks mint addresses
  const metaBricksMints = await getAllMetaBricksMintAddresses();
  
  const holders = [];
  
  for (const mintAddress of metaBricksMints) {
    // Get largest token account (the holder)
    const largestAccounts = await connection.getTokenLargestAccounts(
      new PublicKey(mintAddress)
    );
    
    if (largestAccounts.value.length > 0) {
      // Get the owner of this token account
      const accountInfo = await connection.getParsedAccountInfo(
        largestAccounts.value[0].address
      );
      
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
  
  return holders; // These are CURRENT holders, updated automatically
}
```

---

### **Option B: Query via OASIS API**

```javascript
async function getMetaBricksCurrentHolders() {
  const token = await authenticateWithOASIS();
  
  // OASIS queries blockchain for current holders
  const response = await axios.post(
    'http://oasisweb4.one/api/nft/get-nfts-by-collection',
    {
      CollectionSymbol: 'MBRICK',
      Provider: 'SolanaOASIS',
      IncludeHolderInfo: true
    },
    {
      headers: { 'Authorization': `Bearer ${token}` }
    }
  );
  
  // Extract CURRENT holder from each NFT
  const holders = response.data.result.nfts.map(nft => ({
    walletAddress: nft.CurrentOwner, // OASIS queries blockchain
    mintAddress: nft.MintAddress,
    balance: 1,
    brickNumber: nft.Metadata?.brickNumber
  }));
  
  return holders; // Reflects transfers automatically
}
```

---

## 🏗️ **Correct Architecture:**

```
╔═══════════════════════════════════════════════════════════╗
║           ON-CHAIN (Solana Blockchain)                    ║
╚═══════════════════════════════════════════════════════════╝

NFT Mint: 5xYz...abc123
├─ Owner: Bob's Wallet (CURRENT, updated on transfer)
├─ Metadata URI: ipfs://Qm.../metadata.json
│  └─> Points to Pinata
│      └─> Contains x402 config
└─ Collection: MBRICK

When Bob sells to Charlie:
├─ Owner: Charlie's Wallet (automatically updated)
└─ x402 config: Still embedded (transfers with NFT)


╔═══════════════════════════════════════════════════════════╗
║           DISTRIBUTION QUERY (Always Current)             ║
╚═══════════════════════════════════════════════════════════╝

1. Query blockchain for all MBRICK NFTs
2. For each NFT, get CURRENT owner
3. Returns: Charlie's wallet (not Bob's!)
4. Send distribution to Charlie ✅
```

---

## 🔧 **What Needs to Change:**

### **1. Update Pinata Metadata (All 432 Bricks)**

**Script to generate updated metadata:**

```javascript
// upload-x402-metadata.js
const axios = require('axios');
const FormData = require('form-data');

const PINATA_API_KEY = process.env.PINATA_API_KEY;
const PINATA_SECRET = process.env.PINATA_SECRET;

for (let i = 1; i <= 432; i++) {
  const metadata = {
    name: `MetaBrick #${i}`,
    symbol: "MBRICK",
    description: `MetaBrick NFT #${i}. Earn passive income from AssetRail Smart Contract Generator API revenue. This NFT automatically distributes 50% of SC-Gen revenue equally among all 432 brick holders. Transfer this NFT to transfer the distribution rights.`,
    image: getBrickImageUrl(i),
    attributes: [
      { trait_type: "Brick Number", value: i.toString() },
      { trait_type: "Brick Type", value: getBrickType(i) },
      { trait_type: "Revenue Sharing", value: "x402 Enabled" },
      { trait_type: "Distribution Share", value: "50%" },
      { trait_type: "Distribution Model", value: "Equal (1/432)" },
      // ... existing attributes ...
    ],
    
    // CRITICAL: x402 configuration embedded in metadata
    x402: {
      enabled: true,
      version: "1.0",
      protocolVersion: "x402-v1",
      distributionPercentage: 50,
      revenueSource: "AssetRail Smart Contract Generator",
      distributionModel: "equal",
      treasuryAddress: "H8tjfEMYNkTDdSbRBekMBTcE966vGRF2WnCThRRoQJ1",
      totalSupply: 432,
      webhookEndpoint: "https://metabricks-backend.herokuapp.com/api/metabricks/sc-gen-webhook",
      collectionIdentifier: "MBRICK_X402_V1",
      verificationUrl: "https://metabricks.xyz/verify-x402",
      distributionHistory: "https://metabricks.xyz/api/metabricks/stats"
    },
    
    external_url: "https://metabricks.xyz",
    seller_fee_basis_points: 500,
    properties: {
      files: [{ uri: getBrickImageUrl(i), type: "image/png" }],
      category: "image"
    }
  };
  
  await uploadToPinata(metadata, `metabrick-${i}-x402-enabled.json`);
  console.log(`✅ Uploaded metadata for Brick #${i}`);
}
```

---

### **2. Update Holder Query (CRITICAL FIX)**

**Current (WRONG):**
```javascript
// Queries static database
const holders = await nftHoldersStorage.getAllNFTHolders();
// Returns: Original minters (doesn't reflect transfers)
```

**Fixed (CORRECT):**
```javascript
// Queries blockchain for CURRENT owners
const holders = await queryBlockchainForCurrentHolders();
// Returns: Current NFT owners (reflects all transfers)

async function queryBlockchainForCurrentHolders() {
  // Method 1: Via OASIS API
  const nfts = await oasisApi.getNFTsByCollection('MBRICK');
  return nfts.map(nft => ({
    walletAddress: nft.currentOwner, // ← CURRENT owner from blockchain
    mintAddress: nft.mintAddress
  }));
  
  // Method 2: Direct Solana query
  // Query all SPL tokens in MBRICK collection
  // Get current token account owner for each
}
```

---

## 📋 **Correct Implementation Checklist:**

### **☑️ Metadata (On-Chain or IPFS):**
- [ ] x402 config in metadata JSON
- [ ] Uploaded to Pinata for all 432 bricks
- [ ] NFT metadata URI points to updated JSON
- [ ] x402 info visible in wallet/marketplaces

### **☑️ Smart Contract (Optional but Ideal):**
- [ ] Use Solana Token-2022 with metadata extension
- [ ] Store x402 config directly on-chain
- [ ] Immutable and verifiable

### **☑️ Distribution Query:**
- [ ] Query blockchain for CURRENT NFT holders
- [ ] NOT a static database
- [ ] Automatically reflects transfers

### **☑️ OASIS Integration:**
- [ ] x402 config in minting request ✅ (already done)
- [ ] OASIS stores and links to NFT ✅
- [ ] Can query OASIS for current holders

---

## 🚀 **Quick Fix - What We Need to Change:**

Let me update the code to query CURRENT holders from blockchain instead of database:



