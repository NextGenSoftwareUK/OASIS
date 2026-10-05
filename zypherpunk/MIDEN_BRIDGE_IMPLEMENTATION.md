# Miden ↔ Zcash Bridge Implementation

## Overview

This document describes the implementation of the private bridge between Zcash testnet and Miden testnet for **Zypherpunk Hackathon Track 4** ($5,000 prize).

## Architecture

### Components

1. **MidenOASIS Provider** - Provider for Miden blockchain interactions
2. **MidenBridgeService** - Implements `IOASISBridge` for bridge operations
3. **CrossChainBridgeManager** - Updated to recognize Zcash ↔ Miden as private bridge pair
4. **STARK Proof System** - Miden's zero-knowledge proof system for privacy

### Bridge Flow

#### Zcash → Miden (Lock & Mint)

```
1. User initiates bridge: ZEC → Miden
   ↓
2. Lock ZEC on Zcash (shielded transaction)
   - Create shielded transaction
   - Generate viewing key for auditability
   - Store transaction hash
   ↓
3. Generate STARK proof on Miden
   - Verify Zcash lock via viewing key
   - Generate STARK proof of lock
   - Include amount, addresses, transaction hash
   ↓
4. Mint private note on Miden
   - Create private note with bridged amount
   - Link to Zcash transaction via proof
   - Update bridge holon
   ↓
5. Complete bridge order
```

#### Miden → Zcash (Lock & Mint)

```
1. User initiates bridge: Miden → ZEC
   ↓
2. Lock on Miden (private note)
   - Create lock transaction
   - Generate STARK proof
   - Store lock ID
   ↓
3. Verify STARK proof
   - Verify proof validity
   - Extract lock details
   ↓
4. Mint on Zcash (shielded)
   - Create shielded transaction
   - Use lock details from proof
   - Generate viewing key
   ↓
5. Complete bridge order
```

## Implementation Details

### 1. MidenBridgeService

**Location**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.MidenOASIS/Infrastructure/Services/Miden/MidenBridgeService.cs`

**Key Methods**:
- `WithdrawAsync` - Locks funds on Miden (for Miden → Zcash)
- `DepositAsync` - Creates private note on Miden (for Zcash → Miden)
- `GetAccountBalanceAsync` - Query Miden account balance
- `GetTransactionStatusAsync` - Check transaction status

### 2. CrossChainBridgeManager Updates

**Changes**:
- Added `Miden` constant
- Updated `IsPrivateBridgePair` to include Zcash ↔ Miden
- `HandlePrivateBridgeOrderAsync` now supports Miden bridges

### 3. STARK Proof Integration

**MidenService Methods**:
- `GenerateSTARKProofAsync` - Generate STARK proof for bridge operations
- `VerifySTARKProofAsync` - Verify proof validity
- `MintOnMidenAsync` - Mint after Zcash lock (includes proof generation)
- `LockOnMidenAsync` - Lock for Zcash withdrawal (includes proof generation)

## Privacy Features

### 1. Shielded Transactions (Zcash)
- All Zcash operations use shielded transactions
- Viewing keys enable auditability without revealing amounts
- Partial notes support enhanced privacy

### 2. Private Notes (Miden)
- All Miden operations use private notes
- STARK proofs verify operations without revealing details
- Zero-knowledge verification

### 3. Viewing Keys
- Generated for all Zcash operations
- Stored in audit service for compliance
- Enable verification without revealing amounts

## Testing

### Test Scenarios

1. **Zcash → Miden Bridge**
   ```csharp
   var request = new CreateBridgeOrderRequest
   {
       FromToken = "ZEC",
       ToToken = "MIDEN",
       Amount = 1.0m,
       FromAddress = "zs1...",
       DestinationAddress = "miden1...",
       EnableViewingKeyAudit = true,
       RequireProofVerification = true
   };
   ```

2. **Miden → Zcash Bridge**
   ```csharp
   var request = new CreateBridgeOrderRequest
   {
       FromToken = "MIDEN",
       ToToken = "ZEC",
       Amount = 1.0m,
       FromAddress = "miden1...",
       DestinationAddress = "zs1...",
       RequireProofVerification = true
   };
   ```

3. **STARK Proof Verification**
   - Test proof generation
   - Test proof verification
   - Test invalid proof rejection

### Test Environment Setup

1. **Miden Testnet**
   - API URL: `https://testnet.miden.xyz`
   - Set `MIDEN_API_URL` environment variable
   - Set `MIDEN_API_KEY` if required

2. **Zcash Testnet**
   - RPC URL: Configured in ZcashOASIS provider
   - Set `ZCASH_RPC_URL` environment variable

3. **Bridge Configuration**
   ```json
   {
     "Bridges": {
       "ZEC": "ZcashBridgeService",
       "MIDEN": "MidenBridgeService"
     }
   }
   ```

## Status

✅ **Provider Structure**: Complete
✅ **Bridge Service**: Complete
✅ **Bridge Manager Integration**: Complete
✅ **STARK Proof Support**: Complete
⏳ **Testing**: Pending Miden testnet access
⏳ **UI Integration**: Pending

## Next Steps

1. **Testnet Access**
   - Obtain Miden testnet API access
   - Configure testnet endpoints
   - Test basic operations

2. **End-to-End Testing**
   - Test Zcash → Miden bridge
   - Test Miden → Zcash bridge
   - Test STARK proof generation/verification
   - Test error handling and rollback

3. **UI Integration**
   - Add Miden to wallet UI
   - Create bridge UI component
   - Add Miden logo/assets

4. **Documentation**
   - API documentation
   - User guide
   - Developer guide

## Related Files

- `MidenOASIS.cs` - Main provider
- `MidenService.cs` - Service implementation
- `MidenBridgeService.cs` - Bridge service
- `CrossChainBridgeManager.cs` - Bridge manager
- `ZYPherPUNK_TRACK_SPECIFIC_BRIEFS.md` - Track requirements

