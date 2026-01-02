# Solana Wallet Address Format Error Report

**Date:** January 2, 2026  
**Status:** Open - Critical  
**Component:** Wallet Creation / Key Generation  
**Provider:** SolanaOASIS (ProviderType: 3)

---

## Executive Summary

Solana wallets are being created successfully, but the generated wallet addresses are in Bitcoin script format (hex) instead of native Solana base58 format. This prevents users from receiving SOL tokens to the generated addresses.

---

## Problem Description

When creating a Solana wallet through the API (`/api/Wallet/avatar/{avatarId}/create-wallet` with `walletProviderType: 3`), the wallet is created successfully, but the `walletAddress` field contains a Bitcoin P2PKH script (hexadecimal) instead of a valid Solana base58 address.

### Current Behavior

**Generated Wallet Address:**
```
76a914075e11acdb931e47156248e0bfd8f095b5a00fa488ac
```

**Characteristics:**
- Length: 50 characters
- Format: Hexadecimal (Bitcoin P2PKH script)
- Starts with: `76a914`
- Ends with: `88ac`
- **Result:** Invalid for Solana - cannot receive SOL tokens

### Expected Behavior

**Expected Wallet Address Format:**
- Encoding: Base58
- Length: 32-44 characters
- Format: Native Solana public key address
- Example: `7xKXtg2CW87d97TXJSDpbD5jBkheTqA83TZRuJosgAsU`
- **Result:** Valid Solana address - can receive SOL tokens

---

## Root Cause Analysis

### Issue Location

The wallet creation flow uses `KeyHelper.GenerateKeyValuePairAndWalletAddress()`, which generates Bitcoin-style keys and addresses. This method does not have provider-specific logic for Solana.

**Code Path:**
1. `WalletController.CreateWalletForAvatarByIdAsync()` 
2. → `WalletManager.CreateWalletForAvatarByIdAsync()`
3. → `WalletManager.CreateWalletWithoutSaving()`
4. → `KeyManager.GenerateKeyPairWithWalletAddress(ProviderType.SolanaOASIS)`
5. → `KeyHelper.GenerateKeyValuePairAndWalletAddress()` ❌ **Returns Bitcoin format**

### Current Implementation

**File:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/KeyManager.cs`

```csharp
public OASISResult<NextGenSoftware.Utilities.IKeyPairAndWallet> GenerateKeyPairWithWalletAddress(ProviderType providerType)
{
    // ... provider activation code for SolanaOASIS ...
    
    // Falls through to Bitcoin-style key generation
    result.Result = NextGenSoftware.Utilities.KeyHelper.GenerateKeyValuePairAndWalletAddress();
    return result;
}
```

**Problem:** The method activates the Solana provider but still uses the default Bitcoin key generation logic.

---

## Steps to Reproduce

1. **Authenticate:**
   ```bash
   POST http://localhost:5003/api/Avatar/authenticate
   {
     "username": "OASIS_ADMIN",
     "password": "Uppermall1!"
   }
   ```

2. **Create Solana Wallet:**
   ```bash
   POST http://localhost:5003/api/Wallet/avatar/{avatarId}/create-wallet
   Authorization: Bearer {jwtToken}
   {
     "name": "Test Solana Wallet",
     "description": "Testing",
     "walletProviderType": 3,
     "generateKeyPair": true,
     "isDefaultWallet": false
   }
   ```

3. **Observe Response:**
   ```json
   {
     "result": {
       "walletAddress": "76a914075e11acdb931e47156248e0bfd8f095b5a00fa488ac",
       "providerType": 3,
       "privateKey": "f057f6ce760c439b4cc7a0bbe067610d7d75d15dc73870b7b6d6b53c4d1c0ee0",
       "publicKey": "03964bace26d20a629216a8935efce3ca5f379ac95412fc29d3fa547ebf510bd29"
     }
   }
   ```

4. **Verify Address Format:**
   - Address starts with `76a914` (Bitcoin P2PKH script prefix)
   - Address is 50 characters (hexadecimal)
   - Address format is **NOT** valid Solana base58

---

## Impact

### Severity: **CRITICAL**

- **User Impact:** Users cannot receive SOL tokens to generated wallet addresses
- **Functionality:** Core wallet creation feature is non-functional for Solana
- **Business Impact:** Solana integration is broken for wallet operations

### Affected Operations

- ✅ Wallet creation (succeeds but with wrong format)
- ❌ Receiving SOL tokens (addresses are invalid)
- ❌ Sending SOL tokens (cannot use invalid addresses)
- ❌ Wallet balance queries (addresses don't exist on Solana network)

---

## Configuration Status

### Current Configuration

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json`

```json
{
  "OASIS": {
    "StorageProviders": {
      "AutoFailOverProviders": "LocalFileOASIS, MongoDBOASIS, ArbitrumOASIS, EthereumOASIS, SolanaOASIS",
      "SolanaOASIS": {
        "ConnectionString": "https://api.devnet.solana.com",
        "PrivateKey": "45BiYK1XjngQTu6asQorrpXsk5EUyhkrKWzdc66pMShnRFeTUqLEbUUirfC2ixfrjBtnufJrZ8qX7KtyaMhiEmDa",
        "PublicKey": "6rF4zzvuBgM5RgftahPQHuPfp9WmVLYkGn44CkbRijfv",
        "WalletMnemonicWords": "drift blue option right reduce eager extra federal become badge reason call"
      }
    }
  }
}
```

✅ **SolanaOASIS is configured and in AutoFailOverProviders**  
✅ **Provider configuration includes valid base58 keys**

---

## Solution Requirements

### Required Changes

1. **Integrate Solnet Library**
   - Use Solnet.Wallet for Solana key generation
   - Generate Ed25519 keypairs (Solana standard)
   - Encode addresses in base58 format

2. **Update KeyManager.GenerateKeyPairWithWalletAddress()**
   - Add Solana-specific key generation logic
   - Use Solnet when `providerType == ProviderType.SolanaOASIS`
   - Maintain backward compatibility for other providers

3. **Alternative: Provider-Specific Key Generation**
   - Add key generation method to `IOASISBlockchainStorageProvider` interface
   - Implement in `SolanaOASIS` provider class
   - Call provider-specific method from `KeyManager`

### Implementation Approach

**Recommended:** Update `KeyManager.GenerateKeyPairWithWalletAddress()` to use Solnet for Solana:

```csharp
if (providerType == ProviderType.SolanaOASIS)
{
    // Use Solnet to generate Solana keypair
    var wallet = new Wallet();
    var account = wallet.Account;
    
    return new OASISResult<IKeyPairAndWallet>
    {
        Result = new KeyPairAndWallet
        {
            PrivateKey = Convert.ToBase64String(account.PrivateKey.Key),
            PublicKey = account.PublicKey.Key.ToString(),
            WalletAddress = account.PublicKey.Key.ToString() // Base58 encoded
        }
    };
}
```

---

## Related Documentation

- **SOLANA_WALLET_CREATION_FIX.md** - Previous fix documentation (STAR API)
- **Solana Documentation:** https://docs.solana.com/developing/programming-model/accounts
- **Solnet.Wallet:** https://github.com/bmresearch/Solnet

---

## Test Cases

### Test Case 1: Wallet Creation
- **Action:** Create Solana wallet via API
- **Expected:** Wallet address is base58, 32-44 characters
- **Actual:** Wallet address is hex, 50 characters (Bitcoin format)
- **Status:** ❌ FAIL

### Test Case 2: Address Validation
- **Action:** Validate generated address format
- **Expected:** Address matches Solana base58 pattern
- **Actual:** Address matches Bitcoin script pattern
- **Status:** ❌ FAIL

### Test Case 3: Provider Registration
- **Action:** Check if SolanaOASIS is registered at boot
- **Expected:** Provider registered and available
- **Actual:** Provider configuration present, registration status unclear from logs
- **Status:** ⚠️ PARTIAL

---

## Current Status

- ✅ Wallet creation endpoint is functional
- ✅ Provider type is correctly set (SolanaOASIS = 3)
- ✅ Provider configuration is correct (base58 keys in OASIS_DNA.json)
- ✅ Provider activation code is in place
- ❌ Key generation produces Bitcoin format instead of Solana format
- ❌ Generated addresses cannot receive SOL tokens

---

## Next Steps

1. **Immediate:** Implement Solana-specific key generation using Solnet
2. **Testing:** Verify generated addresses are valid Solana base58 format
3. **Validation:** Test receiving SOL tokens to generated addresses
4. **Documentation:** Update SOLANA_WALLET_CREATION_FIX.md with solution

---

## Notes

- The STAR API implementation has a working Solana wallet creation (see SOLANA_WALLET_CREATION_FIX.md)
- This codebase may need similar Solnet integration
- The architecture prefers provider activation over architectural changes (per user request)
- Provider activation is working, but key generation still uses default Bitcoin logic

---

**Report Generated:** January 2, 2026  
**Last Updated:** January 2, 2026

