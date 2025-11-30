# Provider Integration Guide

## Current Provider Status

### Zcash Provider ✅
**Location**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ZcashOASIS/`

**Available Methods**:
- ✅ `LockZECForBridgeAsync()` - Lock ZEC for bridge operations
- ✅ `GenerateViewingKeyAsync()` - Generate viewing keys for auditability
- ✅ `CreateShieldedTransactionAsync()` - Create shielded transactions
- ⏳ `ReleaseZECAsync()` - **NEEDS TO BE ADDED** - Release locked ZEC

### Aztec Provider ✅
**Location**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AztecOASIS/`

**Available Methods**:
- ✅ `CreatePrivateNoteAsync()` - Create private notes
- ✅ `DepositFromZcashAsync()` - Deposit from Zcash bridge
- ✅ `WithdrawToZcashAsync()` - Withdraw to Zcash bridge
- ✅ `GenerateProofAsync()` - Generate ZK proofs
- ✅ `SubmitProofAsync()` - Submit proofs
- ⏳ `MintStablecoinAsync()` - **NEEDS TO BE ADDED** - Mint stablecoin
- ⏳ `BurnStablecoinAsync()` - **NEEDS TO BE ADDED** - Burn stablecoin
- ⏳ `DeployToYieldStrategyAsync()` - **NEEDS TO BE ADDED** - Deploy to yield
- ⏳ `SeizeCollateralAsync()` - **NEEDS TO BE ADDED** - Seize collateral for liquidation

## Integration Plan

### Phase 1: Add Missing Methods to Providers

#### Zcash Bridge Service
Add `ReleaseZECAsync()` method:
```csharp
Task<OASISResult<string>> ReleaseZECAsync(
    string lockTxHash,
    decimal amount,
    string destinationAddress
);
```

#### Aztec Service
Add stablecoin-specific methods:
```csharp
Task<OASISResult<string>> MintStablecoinAsync(
    string aztecAddress,
    decimal amount,
    string zcashTxHash,
    string viewingKey
);

Task<OASISResult<string>> BurnStablecoinAsync(
    string aztecAddress,
    decimal amount,
    string positionId
);

Task<OASISResult<string>> DeployToYieldStrategyAsync(
    string aztecAddress,
    decimal amount,
    string strategy
);

Task<OASISResult<string>> SeizeCollateralAsync(
    string aztecAddress,
    decimal amount
);
```

### Phase 2: Update StablecoinManager

Replace simulated operations with real provider calls:
1. Replace `LockZECForBridgeAsync()` simulation
2. Replace `ReleaseZECAsync()` simulation
3. Replace `MintStablecoinAsync()` simulation
4. Replace `BurnStablecoinAsync()` simulation
5. Replace `DeployToYieldStrategyAsync()` simulation
6. Replace `SeizeCollateralAsync()` simulation

### Phase 3: Testing

1. Test Zcash lock/release operations
2. Test Aztec mint/burn operations
3. Test yield deployment
4. Test liquidation flow
5. End-to-end mint/redeem flow

## Implementation Notes

### Using Providers in StablecoinManager

```csharp
// Get providers via ProviderManager
var zcashProvider = ProviderManager.GetProvider<ZcashOASIS>();
var aztecProvider = ProviderManager.GetProvider<AztecOASIS>();

// Use provider methods
var lockResult = await zcashProvider.LockZECForBridgeAsync(
    amount,
    "Aztec",
    aztecAddress,
    viewingKey
);
```

### Error Handling

All provider methods return `OASISResult<T>`, so error handling is consistent:
```csharp
if (result.IsError)
{
    // Handle error
    return result;
}
```

### Transaction Confirmation

For Zcash and Aztec transactions, we need to:
1. Wait for transaction confirmation
2. Verify transaction status
3. Handle failures with rollback

---

**Status**: Ready for Integration ⏳  
**Next**: Add missing methods to providers

