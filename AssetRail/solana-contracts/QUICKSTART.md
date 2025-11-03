# AssetRail Solana Contracts - Quick Start Guide

## 🚀 Getting Started in 5 Minutes

### Prerequisites Check
```bash
# Check installations
solana --version      # Should be 1.18+
anchor --version      # Should be 0.30+
node --version        # Should be 18+
```

### 1. Install Dependencies
```bash
cd /Volumes/Storage/OASIS_CLEAN/AssetRail/solana-contracts
yarn install
```

### 2. Build Contracts
```bash
anchor build
```

### 3. Run Tests
```bash
# Start local validator in separate terminal
solana-test-validator

# Run tests
anchor test --skip-local-validator
```

### 4. Deploy to Devnet
```bash
# Configure Solana CLI for devnet
solana config set --url devnet

# Request airdrop (if needed)
solana airdrop 2

# Deploy
./scripts/deploy-devnet.sh
```

---

## 📋 What You Just Built

### 1. **DAT Integration Contract**
**Purpose:** Revolutionary Digital Asset Treasury combining SOL staking with tokenized asset yields

**Use Cases:**
- **Music Labels:** Stake SOL + tokenize music IP royalties → 15-20% APY
- **Real Estate:** Stake SOL + fractional property tokens → 13-19% APY  
- **Sports:** Stake SOL + memorabilia NFTs → 15-22% APY
- **Wine Collectors:** Stake SOL + vintage tracking → 17-25% APY
- **Film Studios:** Stake SOL + revenue sharing → 15-25% APY

**Key Functions:**
```typescript
// Initialize treasury
initializeTreasury(name, apy, minStake, lockup)

// Add tokenized assets
addAsset(assetType, name, value, yieldBps, metadataUri)

// User stakes SOL
stakeSol(amount)

// User claims enhanced yield
claimYield()

// Asset manager distributes returns
distributeAssetYield(amount)
```

### 2. **NFT Airdrop Contract**
**Purpose:** Batch mint and distribute NFTs to multiple wallets efficiently

**Use Cases:**
- **Community Rewards:** Airdrop to DAO contributors
- **Marketing Campaigns:** Whitelist-based claim drops
- **Event Tickets:** Batch distribute to attendees
- **Loyalty Programs:** Tiered NFT distributions
- **Genesis Collections:** Launch with batch minting

**Key Functions:**
```typescript
// Create airdrop campaign
initializeCampaign(name, collectionUri, maxRecipients)

// Add whitelist
addToWhitelist(recipients[])

// Batch airdrop
airdropBatch(recipients[], uris[], names[])

// User claims NFT
claimNft(name, symbol, uri)

// Manage campaign
pauseCampaign() / resumeCampaign()
```

---

## 🎯 Demo Scenarios

### Scenario 1: Music Label DAT
```typescript
// 1. Initialize treasury
await initializeTreasury("SoundWave Records DAT", 500, 1_SOL, 30_days);

// 2. Add tokenized music catalog
await addAsset(
  AssetType.MusicIP,
  "Hit Singles Catalog 2024",
  50_000_SOL,  // Portfolio value
  1500,        // 15% expected APY from royalties
  "ipfs://catalog-metadata"
);

// 3. Investors stake SOL
await stakeSol(100_SOL);  // Get enhanced yield

// 4. Music generates royalties
await distributeAssetYield(0.5_SOL);  // From streaming

// 5. Investor claims combined yield
await claimYield();  // SOL staking + music royalties
```

**Result:** ~17% APY (5% SOL + 12% music royalties)

### Scenario 2: Genesis NFT Airdrop
```typescript
// 1. Create campaign
await initializeCampaign("AssetRail Genesis", "ipfs://collection", 1000);

// 2. Add early supporters
await addToWhitelist([wallet1, wallet2, wallet3, ...]);

// 3. Batch airdrop to first 10
await airdropBatch(
  [wallet1, wallet2, ...],
  ["ipfs://nft1", "ipfs://nft2", ...],
  ["Genesis #1", "Genesis #2", ...]
);

// 4. Others claim when ready
await claimNft("Genesis #11", "ARAIL", "ipfs://nft11");
```

---

## 📊 Expected Performance

### DAT Integration
| Metric | Value |
|--------|-------|
| Base SOL APY | 5-7% |
| Asset Boost | +10-15% |
| **Total APY** | **15-22%** |
| Gas Cost (stake) | ~0.00001 SOL |
| Gas Cost (claim) | ~0.00001 SOL |

### NFT Airdrop
| Metric | Value |
|--------|-------|
| Batch Size | Up to 10 per tx |
| Gas Cost (batch) | ~0.0001 SOL |
| Campaign Setup | ~0.001 SOL |
| Whitelist (100) | ~0.01 SOL |

---

## 🔧 Troubleshooting

### Build Fails
```bash
# Clean and rebuild
anchor clean
anchor build
```

### Test Fails: "insufficient funds"
```bash
# Request airdrop
solana airdrop 2

# Or start validator with more SOL
solana-test-validator --reset
```

### Deployment Error: "custom program error: 0x1"
```bash
# Check wallet balance
solana balance

# Request more SOL
solana airdrop 2 --url devnet
```

---

## 📚 Next Steps

1. **Customize Parameters**
   - Modify APY rates in `initializeTreasury`
   - Adjust lockup periods
   - Set minimum stake amounts

2. **Add Asset Types**
   - Edit `AssetType` enum in `dat-integration/src/lib.rs`
   - Add new variants (Art, Collectibles, etc.)

3. **Integrate with Frontend**
   - Use Anchor TypeScript client
   - Connect to wallet (Phantom, Solflare)
   - Display real-time yields

4. **Production Deployment**
   - Complete security audit
   - Update program IDs
   - Deploy to mainnet
   - Set up monitoring

---

## 🎓 Learn More

**Anchor Documentation:** https://www.anchor-lang.com
**Solana Cookbook:** https://solanacookbook.com
**AssetRail Pitch Deck:** See `AssetRail_Pitch_Deck_Outline.md`

---

## 💡 Pro Tips

1. **Testing Locally**
   ```bash
   # Terminal 1: Start validator
   solana-test-validator
   
   # Terminal 2: Watch logs
   solana logs
   
   # Terminal 3: Run tests
   anchor test --skip-local-validator
   ```

2. **Debugging**
   ```bash
   # Add this to your test:
   .rpc({ skipPreflight: true })  // See full error
   ```

3. **Monitoring**
   ```bash
   # Watch program logs
   solana logs <PROGRAM_ID> --url devnet
   ```

---

**Ready to revolutionize Digital Asset Treasuries! 🚀**

For questions: Check `README.md` or AssetRail documentation






