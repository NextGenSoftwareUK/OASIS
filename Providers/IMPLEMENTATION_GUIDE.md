# OASIS Provider Implementation Guide

## Overview
This document tracks the implementation status of all OASIS providers and provides guidance for completing real SDK/API integrations.

## Implementation Strategy

### 1. Bridge Operations (Priority: HIGH)
Bridge operations require real blockchain SDK integrations:
- **Radix**: Use RadixEngineToolkit for transaction manifests
- **Miden**: Use Miden API client for private notes and STARK proofs
- **Starknet**: Use Starknet RPC client for transactions
- **Aztec**: Use Aztec CLI/API for private transactions

### 2. Storage Operations (Priority: MEDIUM)
For blockchain providers, storage operations should delegate to ProviderManager:
- Use `ProviderManager.Instance` to save/load to persistent storage (MongoDB, etc.)
- Blockchain providers focus on bridge/wallet operations
- Storage operations are handled by dedicated storage providers

### 3. NFT Operations (Priority: HIGH)
NFT operations require real blockchain integrations:
- Minting: Use blockchain-specific NFT standards (ERC721, etc.)
- Metadata: Store on-chain or IPFS
- Transfer: Use blockchain transaction APIs

## Provider Status

### ✅ Fully Implemented
- **EthereumOASIS**: Full Web3 integration
- **SOLANAOASIS**: Full Solnet integration
- **AptosOASIS**: Full Move SDK integration

### 🔄 Partially Implemented (Needs Completion)

#### RadixOASIS
- ✅ Account creation/restoration
- ✅ Balance queries
- ❌ Transaction building (ManifestBuilder needs completion)
- ❌ PrivateKey creation from bytes/hex

**Action Items:**
1. Complete `RadixBridgeHelper.GetPrivateKey()` using RadixEngineToolkit API
2. Implement transaction manifest building
3. Complete `ExecuteTransactionAsync()` method

#### MidenOASIS
- ✅ API client structure
- ✅ Private note creation
- ❌ STARK proof generation (needs API endpoint)
- ❌ Bridge operations completion

**Action Items:**
1. Complete Miden API client methods
2. Implement STARK proof workflows
3. Complete bridge deposit/withdraw operations

#### StarknetOASIS
- ✅ RPC client structure
- ✅ Balance queries
- ❌ Transaction submission (needs proper SDK)
- ❌ Account creation from seed phrase

**Action Items:**
1. Integrate proper Starknet SDK (Nethermind.Starknet or similar)
2. Complete transaction building
3. Implement account derivation

#### BlockStackOASIS
- ✅ BlockStackClient structure
- ❌ Gaia storage operations
- ❌ File upload/download

**Action Items:**
1. Complete Gaia storage integration
2. Implement file operations
3. Complete authentication flow

### ❌ Placeholder Implementations (Need Real SDKs)

#### HashgraphOASIS
- Needs Hedera SDK integration
- Current: Basic HTTP client only

#### PolkadotOASIS
- Needs Polkadot.NET SDK
- Current: Placeholder methods

#### CardanoOASIS
- Needs CardanoSharp SDK
- Current: Placeholder methods

## Implementation Patterns

### Pattern 1: Bridge Operations
```csharp
public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(...)
{
    // 1. Validate inputs
    // 2. Check balance
    // 3. Build transaction using blockchain SDK
    // 4. Sign transaction
    // 5. Submit to blockchain
    // 6. Return transaction hash/status
}
```

### Pattern 2: Storage Operations (Delegation)
```csharp
public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
{
    // For blockchain providers, delegate to ProviderManager
    return await ProviderManager.Instance.SaveAvatarAsync(avatar);
}
```

### Pattern 3: NFT Operations
```csharp
public async Task<OASISResult<INFTTransactionResponse>> MintNFTAsync(...)
{
    // 1. Validate request
    // 2. Build NFT mint transaction
    // 3. Submit to blockchain
    // 4. Store metadata (on-chain or IPFS)
    // 5. Return NFT details
}
```

## Next Steps

1. **Complete Radix transaction building** - Use RadixEngineToolkit API
2. **Complete Miden bridge operations** - Use Miden API client
3. **Complete Starknet transactions** - Integrate proper SDK
4. **Replace storage placeholders** - Use ProviderManager delegation
5. **Complete NFT operations** - Use blockchain-specific standards

## Testing Requirements

Each provider should have:
- Unit tests for SDK integration
- Integration tests for blockchain operations
- Bridge operation tests
- Error handling tests



