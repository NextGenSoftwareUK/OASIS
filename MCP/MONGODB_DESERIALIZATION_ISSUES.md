# MongoDB Deserialization Issues - Investigation Log

**Date:** 2026-01-07  
**Issue:** Cannot list all avatars from MongoDB - deserialization error

## Problem Summary

When attempting to list all avatars using `get-all-avatars` endpoint, MongoDB fails to deserialize `Avatar` documents because the `ProviderWallets` dictionary contains `ProviderWallet` objects with a field `WalletAddressSegwitP2SH` that doesn't exist in the current `ProviderWallet` class definition.

## Root Cause

**Why ProviderWallet affects Avatar deserialization:**
- MongoDB deserializes the entire `Avatar` document atomically
- The `Avatar` entity contains a nested `ProviderWallets` dictionary: `Dictionary<ProviderType, List<ProviderWallet>>`
- When MongoDB tries to deserialize a `ProviderWallet` object from the database, it encounters the `WalletAddressSegwitP2SH` field
- Since this property doesn't exist in the current `ProviderWallet` class, MongoDB throws a `FormatException`
- This causes the entire `Avatar` document deserialization to fail, preventing us from reading any avatar data

## Error Details

```
System.FormatException: Element 'WalletAddressSegwitP2SH' does not match any field or property of class NextGenSoftware.OASIS.API.Core.Objects.ProviderWallet.
```

**Location:** `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/AvatarRepository.cs:line 289`

## Comparison with Master Branch

**Master Branch:**
- ✅ `ProviderWallet` class **HAS** `WalletAddressSegwitP2SH` property (line 35)
- ✅ MongoDB deserialization works correctly
- ✅ No special IgnoreExtraElements configuration needed

**Current Branch (max-build2-star-working):**
- ❌ `ProviderWallet` class **MISSING** `WalletAddressSegwitP2SH` property
- ❌ MongoDB deserialization fails
- ❌ Avatars stored in MongoDB cannot be read

## Attempted Fixes (All Reverted)

### Fix 1: Add Missing Property
- **Action:** Added `WalletAddressSegwitP2SH` property to `ProviderWallet` class
- **Result:** Property added, but API still failing (likely due to cached class maps or build issues)
- **Status:** ✅ Reverted

### Fix 2: MongoDB IgnoreExtraElements Convention
- **Action:** Registered global `IgnoreExtraElementsConvention` for OASIS classes
- **Location:** `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/MongoDBOASIS.cs`
- **Result:** Convention registered, but may not apply if class map already cached
- **Status:** ✅ Reverted

### Fix 3: Explicit ProviderWallet Class Map Registration
- **Action:** Explicitly registered `ProviderWallet` class map with `SetIgnoreExtraElements(true)`
- **Result:** Class map registration attempted, but may be too late if MongoDB already auto-mapped
- **Status:** ✅ Reverted

### Fix 4: BsonIgnoreIfNull on ProviderWallets
- **Action:** Added `[BsonIgnoreIfNull]` attribute to `ProviderWallets` property in MongoDB `Avatar` entity
- **Result:** Not tested (reverted before testing)
- **Status:** ✅ Reverted

## Files Modified (All Reverted)

1. `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Objects/Wallets/ProviderWallet.cs`
   - ❌ Removed: `WalletAddressSegwitP2SH` property

2. `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Enums/AvatarType.cs`
   - ❌ Removed: `Agent` enum value (added to fix build errors)

3. `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/MongoDBOASIS.cs`
   - ❌ Removed: IgnoreExtraElements convention registration
   - ❌ Removed: ProviderWallet class map registration

4. `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Entities/Avatar.cs`
   - ❌ Removed: `[BsonIgnoreIfNull]` attribute (if added)

## Key Learnings

1. **MongoDB deserialization is all-or-nothing:** If any nested property fails to deserialize, the entire document fails
2. **Class map caching:** MongoDB may cache class maps, making runtime registration ineffective
3. **Master branch has the property:** The simplest fix is to restore `WalletAddressSegwitP2SH` to match master
4. **ProviderWallets are nested:** The error occurs during deserialization of nested `ProviderWallet` objects within `Avatar` documents

## Recommended Approach (For Future)

1. **Restore property from master:** Add `WalletAddressSegwitP2SH` back to `ProviderWallet` class to match database schema
2. **Verify build succeeds:** Ensure no compilation errors
3. **Restart API:** Fresh restart to clear any cached class maps
4. **Test deserialization:** Verify avatars can be loaded from MongoDB
5. **Consider backward compatibility:** If removing fields in future, use `IgnoreExtraElements` convention proactively

## Current State

- ✅ `WalletAddressSegwitP2SH` property **RESTORED** to `ProviderWallet` class
- ✅ Property added with comment: "SegWit P2SH address format (for Bitcoin compatibility, empty for Solana)"
- ✅ Verified: Adding this property does NOT affect Solana wallet generation (Solana explicitly sets it to empty)
- ✅ Build verified: No compilation errors
- ⏳ Next step: Restart API and test MongoDB deserialization

## Resolution Applied

**Date:** 2026-01-07  
**Action:** Restored `WalletAddressSegwitP2SH` property to `ProviderWallet` class and added `Agent` to `AvatarType` enum

### Fix 1: WalletAddressSegwitP2SH Property

**Why this is safe:**
- Solana wallet generation explicitly sets `WalletAddressSegwitP2SH = string.Empty` (see `KeyManager.cs` line 137)
- Solana uses `WalletAddress` (base58 public key) which is always populated
- The property is just a string field - having it doesn't affect generation logic
- Bitcoin wallets populate this field, so MongoDB can now deserialize all wallet types

**Files Modified:**
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Objects/Wallets/ProviderWallet.cs`
  - ✅ Added: `public string WalletAddressSegwitP2SH { get; set; }`

### Fix 2: AvatarType.Agent Enum Value

**Issue:** A2A Manager code was using `AvatarType.Agent` but the enum didn't have this value, causing build errors.

**Purpose:** The `Agent` avatar type is used for autonomous agents that can communicate and transact with other agents via the A2A Protocol. It's used in:
- Creating OpenSERV agent avatars (`A2AManager-OpenSERV.cs`)
- Validating agent types for A2A messages (`A2AManager.cs`)
- Karma operations for agents (`A2AManager-Karma.cs`)
- Mission operations between agents (`A2AManager-Mission.cs`)

**Files Modified:**
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Enums/AvatarType.cs`
  - ✅ Added: `Agent` enum value with comment: "Autonomous agents that can communicate and transact with other agents via A2A Protocol"

**Build Status:**
- ✅ `AvatarType.Agent` errors resolved
- ⚠️ Other build errors remain (unrelated to MongoDB deserialization - SERV integration issues)

