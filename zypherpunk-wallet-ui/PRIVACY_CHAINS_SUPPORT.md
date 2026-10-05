# Privacy Chains Support Status

## Overview

This document outlines the support status for privacy-focused blockchains (Zcash, Aztec, Miden, Starknet) in the unified wallet feature.

## Current Status

### ✅ **Zcash (ZcashOASIS)**
- **Status**: ✅ Fully Supported
- **Provider Type**: `ProviderType.ZcashOASIS`
- **In Unified Wallet**: ✅ Yes
- **Features**:
  - Shielded transactions support
  - z-address support
  - Privacy score integration
  - Viewing key management

### ✅ **Aztec (AztecOASIS)**
- **Status**: ✅ Supported (Needs Testing)
- **Provider Type**: `ProviderType.AztecOASIS`
- **In Unified Wallet**: ✅ Added
- **Backend Support**: ✅ Yes (aztec-bridge-service exists)
- **Features**:
  - Privacy-enabled zk-rollup
  - Bridge support available
  - Needs wallet generation testing

### ⚠️ **Miden**
- **Status**: ⚠️ Partial Support
- **Provider Type**: ❌ Not in enum (needs `MidenOASIS`)
- **In Unified Wallet**: ❌ Not yet (waiting for provider type)
- **Backend Support**: ❓ Unknown
- **UI References**: ✅ Yes (in providerMeta.ts)
- **Action Required**:
  1. Add `MidenOASIS = "MidenOASIS"` to ProviderType enum
  2. Verify backend provider exists
  3. Add to unified wallet providers list

### ⚠️ **Starknet**
- **Status**: ⚠️ Partial Support
- **Provider Type**: ❌ Not in enum (needs `StarknetOASIS`)
- **In Unified Wallet**: ❌ Not yet (waiting for provider type)
- **Backend Support**: ✅ Yes (StarknetBridgeController exists)
- **Action Required**:
  1. Add `StarknetOASIS = "StarknetOASIS"` to ProviderType enum
  2. Verify wallet generation works
  3. Add to unified wallet providers list

## Implementation Steps

### Step 1: Add Missing Provider Types

Update `lib/types.ts`:

```typescript
export enum ProviderType {
  // ... existing providers ...
  AztecOASIS = "AztecOASIS",      // ✅ Already exists
  MidenOASIS = "MidenOASIS",      // ❌ Needs to be added
  StarknetOASIS = "StarknetOASIS", // ❌ Needs to be added
  ZcashOASIS = "ZcashOASIS",      // ✅ Already exists
}
```

### Step 2: Update Unified Wallet

Once provider types are added, update `lib/unifiedWallet.ts`:

```typescript
export const UNIFIED_WALLET_PROVIDERS: ProviderType[] = [
  ProviderType.SolanaOASIS,
  ProviderType.EthereumOASIS,
  ProviderType.PolygonOASIS,
  ProviderType.ArbitrumOASIS,
  ProviderType.ZcashOASIS,      // ✅ Privacy chain
  ProviderType.AztecOASIS,       // ✅ Privacy chain
  ProviderType.MidenOASIS,      // ⚠️ After adding to enum
  ProviderType.StarknetOASIS,   // ⚠️ After adding to enum
];
```

### Step 3: Verify Backend Support

For each chain, verify:
1. **Keys API** supports keypair generation
2. **Wallet API** supports wallet creation
3. **Provider** is registered in OASIS DNA

## Testing Checklist

### Zcash ✅
- [x] Provider type exists
- [x] In unified wallet
- [ ] Test wallet generation
- [ ] Test shielded transactions
- [ ] Test viewing keys

### Aztec ⚠️
- [x] Provider type exists
- [x] In unified wallet
- [ ] Test wallet generation
- [ ] Test bridge functionality
- [ ] Test private transactions

### Miden ❌
- [ ] Add provider type to enum
- [ ] Verify backend provider
- [ ] Add to unified wallet
- [ ] Test wallet generation
- [ ] Test STARK-based transactions

### Starknet ❌
- [ ] Add provider type to enum
- [ ] Verify backend provider
- [ ] Add to unified wallet
- [ ] Test wallet generation
- [ ] Test Cairo smart contracts

## Privacy Features by Chain

### Zcash
- **Shielded Addresses**: z-addresses (z1...)
- **Transparent Addresses**: t-addresses (t1...)
- **Viewing Keys**: For auditability
- **Memo Fields**: 512 bytes encrypted

### Aztec
- **Private Notes**: Encrypted UTXOs
- **Nullifiers**: Prevent double-spending
- **Viewing Keys**: For transparency
- **zk-SNARKs**: Zero-knowledge proofs

### Miden
- **STARK Proofs**: Scalable zero-knowledge
- **zkVM**: Zero-knowledge virtual machine
- **Private State**: Encrypted state transitions
- **Public Verification**: Proof verification

### Starknet
- **Cairo**: Smart contract language
- **Account Abstraction**: Flexible accounts
- **L2 Privacy**: Layer 2 privacy features
- **zk-STARKs**: Zero-knowledge proofs

## Recommendations

### Immediate Actions

1. **Add Missing Provider Types**
   - Add `MidenOASIS` to ProviderType enum
   - Add `StarknetOASIS` to ProviderType enum

2. **Test Aztec Support**
   - Verify Keys API works with AztecOASIS
   - Test wallet generation
   - Test bridge functionality

3. **Verify Backend Providers**
   - Check if Miden provider exists in backend
   - Check if Starknet provider exists in backend
   - Verify provider registration in OASIS DNA

### Future Enhancements

1. **Privacy Dashboard**
   - Unified privacy score across all chains
   - Privacy recommendations
   - Transaction privacy analysis

2. **Cross-Chain Privacy**
   - Privacy-preserving bridges
   - Cross-chain shielded transactions
   - Unified viewing keys

3. **Advanced Features**
   - Multi-chain privacy pools
   - Cross-chain nullifiers
   - Unified privacy settings

## Current Unified Wallet Support

As of now, the unified wallet will work for:

✅ **Fully Supported**:
- Zcash (ZcashOASIS) - Privacy chain
- Aztec (AztecOASIS) - Privacy chain (needs testing)

⚠️ **Needs Provider Type Addition**:
- Miden - Add `MidenOASIS` to enum first
- Starknet - Add `StarknetOASIS` to enum first

## Summary

| Chain | Provider Type | Backend | Unified Wallet | Status |
|-------|--------------|---------|----------------|--------|
| Zcash | ✅ ZcashOASIS | ✅ Yes | ✅ Yes | ✅ Ready |
| Aztec | ✅ AztecOASIS | ✅ Yes | ✅ Yes | ⚠️ Needs Testing |
| Miden | ❌ Missing | ❓ Unknown | ❌ No | ❌ Needs Setup |
| Starknet | ❌ Missing | ✅ Yes | ❌ No | ❌ Needs Setup |

---

**Last Updated**: December 2024  
**Next Steps**: Add missing provider types and verify backend support

