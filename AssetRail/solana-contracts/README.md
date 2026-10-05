# AssetRail Solana Smart Contracts

**Programmable Digital Asset Treasuries for the Solana Blockchain**

## Overview

This repository contains two core Solana programs built with the Anchor framework:

### 1. **DAT Integration** - Digital Asset Treasury
The revolutionary contract that transforms plain vanilla DATs into dynamic, yield-generating investment vehicles.

**Key Features:**
- **Enhanced Yield:** Combines SOL staking (5-7% APY) with tokenized asset returns (10-20% APY)
- **Multi-Asset Support:** Music IP, Real Estate, Sports, Wine, Film tokenization
- **Programmable Utility:** Access rights, exclusive content, experiences
- **Wyoming Trust Compatible:** Built-in legal compliance structures
- **Flexible Lockup:** Customizable staking periods with early withdrawal penalties
- **Transparent Accounting:** Real-time yield tracking and distribution

**Target APY:** 15-22% (SOL staking + asset-specific returns)

### 2. **NFT Airdrop** - Batch NFT Distribution
Efficiently mint and distribute NFTs to multiple wallets with advanced features.

**Key Features:**
- **Batch Operations:** Airdrop to multiple wallets in single transactions
- **Whitelist Support:** Pre-approve recipients for claim-based drops
- **Campaign Management:** Organize drops with limits and analytics
- **Claim Mechanism:** Allow users to claim their NFTs when ready
- **Pause/Resume:** Emergency controls for campaign management
- **Statistics Tracking:** Real-time campaign metrics

## Architecture

```
AssetRail DAT System
├── SOL Staking (Base Layer)
│   ├── 5-7% APY from network
│   └── Flexible lockup periods
├── Tokenized Assets (Yield Layer)
│   ├── Music IP (15% APY) - Royalty streams
│   ├── Real Estate (8-12% APY) - Rental income
│   ├── Sports (10-15% APY) - Memorabilia appreciation
│   ├── Wine (12-18% APY) - Vintage appreciation
│   └── Film (10-20% APY) - Revenue sharing
└── Enhanced Yield Distribution
    ├── Proportional to stake
    ├── Weighted by asset performance
    └── Claimable on demand
```

## Installation

### Prerequisites
- Rust 1.75+
- Solana CLI 1.18+
- Anchor 0.30.1+
- Node.js 18+
- Yarn or npm

### Setup

```bash
# Install dependencies
yarn install

# Build contracts
anchor build

# Run tests
anchor test

# Deploy to localnet
./scripts/deploy-localnet.sh

# Deploy to devnet
./scripts/deploy-devnet.sh
```

## Usage Examples

### DAT Integration

#### Initialize a Treasury
```typescript
import * as anchor from "@coral-xyz/anchor";
import { Program } from "@coral-xyz/anchor";
import { DatIntegration } from "./target/types/dat_integration";

const program = anchor.workspace.DatIntegration as Program<DatIntegration>;

await program.methods
  .initializeTreasury(
    "My Asset Treasury",
    500,  // 5% SOL staking APY
    new anchor.BN(1_000_000_000),  // 1 SOL minimum
    new anchor.BN(86400 * 30)  // 30-day lockup
  )
  .accounts({
    treasury: treasuryPda,
    treasuryVault: vault.publicKey,
    authority: wallet.publicKey,
    systemProgram: SystemProgram.programId,
  })
  .rpc();
```

#### Add Tokenized Asset
```typescript
await program.methods
  .addAsset(
    { musicIp: {} },  // Asset type
    "Quantum Beats Album",
    new anchor.BN(50_000_000_000_000),  // Asset value
    1500,  // 15% expected APY
    "ipfs://QmMusicMetadata"
  )
  .accounts({
    treasury: treasuryPda,
    asset: assetPda,
    authority: wallet.publicKey,
    systemProgram: SystemProgram.programId,
  })
  .rpc();
```

#### Stake SOL
```typescript
await program.methods
  .stakeSol(new anchor.BN(5_000_000_000))  // 5 SOL
  .accounts({
    treasury: treasuryPda,
    stakeAccount: stakePda,
    treasuryVault: vault.publicKey,
    user: wallet.publicKey,
    systemProgram: SystemProgram.programId,
  })
  .rpc();
```

#### Claim Enhanced Yield
```typescript
await program.methods
  .claimYield()
  .accounts({
    treasury: treasuryPda,
    stakeAccount: stakePda,
    treasuryVault: vault.publicKey,
    user: wallet.publicKey,
  })
  .rpc();
```

### NFT Airdrop

#### Initialize Campaign
```typescript
import { NftAirdrop } from "./target/types/nft_airdrop";

const program = anchor.workspace.NftAirdrop as Program<NftAirdrop>;

await program.methods
  .initializeCampaign(
    "AssetRail Genesis Drop",
    "ipfs://QmCollectionMetadata",
    1000  // Max recipients
  )
  .accounts({
    campaign: campaignPda,
    authority: wallet.publicKey,
    systemProgram: SystemProgram.programId,
  })
  .rpc();
```

#### Add to Whitelist
```typescript
const recipients = [wallet1.publicKey, wallet2.publicKey, wallet3.publicKey];

await program.methods
  .addToWhitelist(recipients)
  .accounts({
    campaign: campaignPda,
    whitelist: whitelistPda,
    authority: wallet.publicKey,
    systemProgram: SystemProgram.programId,
  })
  .rpc();
```

#### Batch Airdrop
```typescript
await program.methods
  .airdropBatch(
    recipients,
    metadataUris,
    nftNames
  )
  .accounts({
    campaign: campaignPda,
    authority: wallet.publicKey,
  })
  .rpc();
```

#### Claim NFT (Recipient Side)
```typescript
await program.methods
  .claimNft("AssetRail #1", "ARAIL", "ipfs://QmNFT1")
  .accounts({
    campaign: campaignPda,
    whitelist: whitelistPda,
    mint: mint.publicKey,
    tokenAccount: tokenAccountPda,
    recipient: wallet.publicKey,
    authority: campaignAuthority,
    tokenProgram: TOKEN_PROGRAM_ID,
    associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
    systemProgram: SystemProgram.programId,
    rent: SYSVAR_RENT_PUBKEY,
  })
  .signers([mint])
  .rpc();
```

## Testing

```bash
# Run all tests
anchor test

# Run specific test suite
anchor test -- --grep "DAT Integration"
anchor test -- --grep "NFT Airdrop"

# Test with logs
anchor test --skip-build -- --nocapture
```

## Security Considerations

### DAT Integration
- ✅ Reentrancy guards on all financial operations
- ✅ Overflow checks on all mathematical operations
- ✅ PDA-based access control
- ✅ Lockup period enforcement
- ✅ Emergency pause functionality (to be added)

### NFT Airdrop
- ✅ Whitelist verification
- ✅ Batch size limits (prevent DoS)
- ✅ Campaign limits enforcement
- ✅ Authority checks
- ✅ Pause/resume controls

### Recommended Audits
Before mainnet deployment:
1. External security audit by reputable firm
2. Economic model review
3. Formal verification of critical functions
4. Stress testing with large datasets

## Deployment

### Program IDs

**Localnet:**
- DAT Integration: `DAtYXMpDEZL8RqQ7KVZp9YvKj8TqPSxKjHD2nNKFZC3L`
- NFT Airdrop: `NftAirDRqP5vZXMpDEZL8RqQ7KVZp9YvKj8TqPSxKj`

**Devnet:** (Update after deployment)
- DAT Integration: TBD
- NFT Airdrop: TBD

**Mainnet:** (Update after deployment)
- DAT Integration: TBD
- NFT Airdrop: TBD

### Deployment Checklist

- [ ] Update program IDs in `Anchor.toml` and `declare_id!()` macros
- [ ] Run full test suite (`anchor test`)
- [ ] Build for production (`anchor build --verifiable`)
- [ ] Deploy to devnet first
- [ ] Verify on Solana Explorer
- [ ] Complete external audit
- [ ] Test on devnet with real assets
- [ ] Deploy to mainnet
- [ ] Verify deployment
- [ ] Initialize monitoring

## Integration with AssetRail Platform

These contracts are part of the larger AssetRail ecosystem:

1. **Frontend Dashboard** - React/Next.js UI for treasury management
2. **OASIS API** - Cross-chain provider integration
3. **Wyoming Trust Framework** - Legal compliance layer
4. **Asset Tokenization Pipeline** - Multi-asset onboarding

## Pitch Deck Alignment

This implementation directly supports the AssetRail pitch deck (Solana Colosseum Hackathon 2024):

- ✅ **Slide 3:** Programmable DATs with business logic
- ✅ **Slide 4:** Smart contract generation (template-ready)
- ✅ **Slide 5:** Multi-asset support (Music, Property, Sports, Wine, Film)
- ✅ **Slide 6:** Enterprise-grade infrastructure
- ✅ **Slide 8:** Demo flow ready

**Target Metrics:**
- 2-3x higher yields vs plain SOL staking
- 15-22% APY (SOL + asset returns)
- Support for 5+ asset verticals
- Production-ready smart contracts

## Contributing

This is part of the AssetRail hackathon submission. For production use:

1. Complete security audit
2. Add comprehensive error handling
3. Implement admin/governance controls
4. Add emergency pause mechanisms
5. Optimize compute units
6. Add upgrade authority management

## License

MIT License - AssetRail Team 2024

## Contact

For questions about these contracts or the AssetRail platform:
- **Website:** [assetrail.io](https://assetrail.io)
- **Hackathon:** Solana Colosseum 2024
- **Demo:** Available on request

---

**Built with ❤️ for the Solana Colosseum Hackathon**






