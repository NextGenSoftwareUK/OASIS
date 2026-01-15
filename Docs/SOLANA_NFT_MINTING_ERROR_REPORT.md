# Solana NFT Minting Error Report

**Date:** January 2025  
**Issue:** NFT minting fails with "Object reference not set to an instance of an object" error  
**Provider:** SolanaOASIS  
**Status:** Under Investigation

---

## Problem Summary

NFT minting via the REST API (`/api/nft/mint-nft`) fails with a null reference exception when using SolanaOASIS as the on-chain provider. The error occurs during the minting process, specifically in the `MintNftAsync` method.

**Error Message:**
```
Error occured in MintNftAsync in NFTManager. Reason: Unknown error occured: Object reference not set to an instance of an object.
```

**Additional Error (from logs):**
```
Value cannot be null. (Parameter 'key')
```

This secondary error occurs when attempting to send the NFT after minting fails, indicating the `TokenAddress` (mint address) is null because minting didn't complete.

---

## Configuration Details

### Current Solana Configuration (`OASIS_DNA.json`)

**Location:** `OASIS Architecture/NextGenSoftware.OASIS.API.DNA/OASIS_DNA.json`

```json
"SolanaOASIS": {
    "WalletMnemonicWords": "",
    "PrivateKey": "5HDREtkQoMUfe9WAhBpM3rnFFbt3UDBvq4uzVNW2fW4eHGgo58WrHEiQQX5o5jwfhTJfB3buohPjNZ1UVebN1a13",
    "PublicKey": "8E1nWXsi55QQphJHvdAPSb1irioub6H9ouQZSTMNnNMb",
    "ConnectionString": "https://api.devnet.solana.com"
}
```

**Key Format:** Both keys are Base58 encoded (as required by Solnet.Wallet)

**Wallet Status:** 
- Wallet has been funded with devnet SOL
- Wallet address: `8E1nWXsi55QQphJHvdAPSb1irioub6H9ouQZSTMNnNMb`
- Balance: Confirmed sufficient SOL for transaction fees

---

## Code Analysis

### 1. Account Constructor (`SolanaOasis.cs`)

**File:** `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/SolanaOasis.cs`  
**Line:** 82

```csharp
public SolanaOASIS(string hostUri, string privateKey, string publicKey)
{
    // ... other initialization ...
    this._oasisSolanaAccount = new(privateKey, publicKey);
    // ...
}
```

**Issue:** The `Solnet.Wallet.Account` constructor is being called with two string parameters (`privateKey`, `publicKey`). However, based on Solnet.Wallet 6.1.0 documentation and build errors encountered, the `Account` constructor may not accept this signature.

**Attempted Fixes:**
1. Changed to `new Account(privateKey)` - Build failed: `'Account' does not contain a constructor that takes 1 arguments`
2. Changed to `new Account(privateKeyObj.KeyBytes)` - Build failed: Constructor signature mismatch
3. **Reverted** to original `new(privateKey, publicKey)` - Current state

**Note:** The master branch contains the same constructor call, suggesting this may have worked in a previous Solnet version or there's a different Account class being used.

### 2. SolanaService Initialization (`SolanaService.cs`)

**File:** `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/Infrastructure/Services/Solana/SolanaService.cs`  
**Line:** 18-21

```csharp
private readonly List<Creator> _creators =
[
    new(oasisAccount.PublicKey, share: CreatorShare, verified: true)
];
```

**Issue:** The `_creators` list is initialized at class construction time using `oasisAccount.PublicKey`. If `oasisAccount` is null or `PublicKey` is null when `SolanaService` is instantiated, this will throw a null reference exception.

**Constructor:**
```csharp
public sealed class SolanaService(Account oasisAccount, IRpcClient rpcClient) : ISolanaService
```

The `oasisAccount` parameter comes from `_oasisSolanaAccount` in `SolanaOASIS`, which is set in the constructor. If the `Account` constructor fails silently or returns null, `oasisAccount.PublicKey` will be null.

### 3. Provider Instantiation (`OASISBootLoader.cs`)

**File:** `OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.cs`  
**Line:** 813-816

```csharp
SolanaOASIS solanaOasis = new(
    OASISDNA.OASIS.StorageProviders.SolanaOASIS.ConnectionString,
    OASISDNA.OASIS.StorageProviders.SolanaOASIS.PrivateKey,
    OASISDNA.OASIS.StorageProviders.SolanaOASIS.PublicKey);
```

The provider is correctly instantiated from the DNA configuration.

---

## Testing Performed

### 1. Configuration Updates
- ✅ Copied Solana configuration from STAR ODK to API's `OASIS_DNA.json`
- ✅ Generated new Solana wallet via API
- ✅ Converted private key from Base64 to Base58 format
- ✅ Updated `OASIS_DNA.json` with Base58 keys
- ✅ Funded wallet with devnet SOL

### 2. API Testing
- ✅ Authentication successful (`OASIS_ADMIN` / `Uppermall1!`)
- ✅ Provider registration successful (`SolanaOASIS`)
- ✅ Provider activation successful (`SolanaOASIS`)
- ❌ NFT minting fails with null reference exception

### 3. Request Payload
```json
{
    "title": "Test NFT",
    "description": "Test",
    "onChainProvider": "SolanaOASIS",
    "offChainProvider": "MongoDBOASIS",
    "nftOffChainMetaType": "OASIS",
    "nftStandardType": "SPL",
    "sendToAddressAfterMinting": "8E1nWXsi55QQphJHvdAPSb1irioub6H9ouQZSTMNnNMb",
    "waitTillNFTSent": false
}
```

**Note:** `Symbol` and `JSONMetaDataURL` were not included in initial tests, but `NFTManager` sets default `Symbol` ("OASISNFT") if empty. However, `JSONMetaDataURL` may be required for Solana metadata.

---

## Dependencies

**Solnet.Wallet Version:** 6.1.0  
**Package:** `Solnet.Wallet` v6.1.0

**Other Solnet Packages:**
- `Solnet.Extensions` v6.1.0
- `Solnet.KeyStore` v6.1.0
- `Solnet.Programs` v6.1.0
- `Solnet.Rpc` v6.1.0
- `Solana.Metaplex` v8.0.0

---

## Root Cause Analysis

### Primary Suspect: Account Constructor Mismatch

The most likely root cause is that the `Solnet.Wallet.Account` constructor in version 6.1.0 does not accept two string parameters (`privateKey`, `publicKey`). 

**Expected Constructor Signature (based on Solnet documentation):**
- `Account(byte[] privateKey)` - Takes byte array of private key, derives public key automatically
- Or requires conversion from Base58 string to byte array first

**Current Code:**
```csharp
this._oasisSolanaAccount = new(privateKey, publicKey); // Both are Base58 strings
```

If this constructor doesn't exist or fails silently, `_oasisSolanaAccount` may be null or have a null `PublicKey`, causing:
1. `SolanaService` constructor to fail when accessing `oasisAccount.PublicKey`
2. Null reference exceptions throughout the minting process

### Secondary Issues

1. **Missing Required Fields:** `JSONMetaDataURL` may be required for Solana NFT metadata, but wasn't included in test requests
2. **Error Handling:** The null reference may be occurring silently during provider activation, not being caught until minting is attempted

---

## Recommendations

### 1. Verify Account Constructor Signature

**Action:** Check Solnet.Wallet 6.1.0 documentation or source code for the correct `Account` constructor signature.

**Expected Fix:**
```csharp
// Convert Base58 private key to byte array
var privateKeyBytes = Base58.Decode(privateKey);
this._oasisSolanaAccount = new Account(privateKeyBytes);
```

Or if a different approach is needed:
```csharp
var privateKeyObj = new PrivateKey(privateKey); // Assuming PrivateKey accepts Base58 string
this._oasisSolanaAccount = new Account(privateKeyObj.KeyBytes);
```

### 2. Add Null Checks and Defensive Coding

**Location:** `SolanaService.cs` constructor

```csharp
public sealed class SolanaService(Account oasisAccount, IRpcClient rpcClient) : ISolanaService
{
    private readonly List<Creator> _creators;
    
    public SolanaService(Account oasisAccount, IRpcClient rpcClient)
    {
        if (oasisAccount == null)
            throw new ArgumentNullException(nameof(oasisAccount));
        if (oasisAccount.PublicKey == null)
            throw new InvalidOperationException("oasisAccount.PublicKey cannot be null");
            
        this.oasisAccount = oasisAccount;
        this.rpcClient = rpcClient;
        
        _creators = new List<Creator>
        {
            new(oasisAccount.PublicKey, share: CreatorShare, verified: true)
        };
    }
}
```

### 3. Include Required Fields in Test Requests

Ensure test requests include:
- `symbol`: "TESTNFT" (or let default "OASISNFT" apply)
- `jsonMetaDataURL`: A valid metadata URL (even if placeholder)

### 4. Add Better Error Logging

Add try-catch blocks around Account instantiation to capture specific errors:

```csharp
try
{
    this._oasisSolanaAccount = new(privateKey, publicKey);
    if (this._oasisSolanaAccount == null || this._oasisSolanaAccount.PublicKey == null)
    {
        throw new InvalidOperationException("Failed to create Account or PublicKey is null");
    }
}
catch (Exception ex)
{
    throw new InvalidOperationException($"Failed to initialize Solana Account: {ex.Message}", ex);
}
```

### 5. Check Test Harness for Reference

**File:** `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.TestHarness/Program.cs`

This file shows how Account is created in tests:
```csharp
static readonly byte[] _privateKeyBytes = Convert.FromBase64String(PrivateKeyBase64);
static readonly string _privateKeyBase58 = Base58.Encode(_privateKeyBytes);
public static readonly PrivateKey PrivateKey = new(_privateKeyBase58);
public static readonly PublicKey PublicKey = new("AfpSpMjNyoHTZWMWkog6Znf57KV82MGzkpDUUjLtmHwG");

// Then used as:
SolanaOASIS solanaOasis = new(TestData.HostUri, TestData.PrivateKey.Key, TestData.PublicKey.Key);
```

This suggests the constructor should accept two string parameters, but the test harness uses `PrivateKey.Key` and `PublicKey.Key` properties, not raw Base58 strings.

---

## Next Steps

1. **Verify Solnet.Wallet 6.1.0 Account constructor** - Check official documentation or source code
2. **Test Account instantiation** - Create a minimal test to verify Account can be created with Base58 strings
3. **Add null checks** - Implement defensive coding around Account initialization
4. **Test with required fields** - Include `Symbol` and `JSONMetaDataURL` in mint requests
5. **Review test harness** - Compare working test harness code with production code
6. **Check for version mismatch** - Verify if master branch uses a different Solnet version

---

## Files Modified

1. `OASIS Architecture/NextGenSoftware.OASIS.API.DNA/OASIS_DNA.json`
   - Updated SolanaOASIS PrivateKey and PublicKey to Base58 format
   - Generated new wallet via API

2. `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/SolanaOasis.cs`
   - Attempted Account constructor fixes (reverted)

---

## Additional Context

- **STAR CLI:** NFT minting works in STAR CLI, which uses native C# managers (not REST API)
- **Master Branch:** Contains same Account constructor code, suggesting this may have worked previously or requires specific setup
- **Wallet Creation:** API wallet creation endpoint successfully generates Base58 keys
- **Provider Activation:** Provider activation completes without errors, but Account may be invalid

---

## Questions for David

1. Does the `Account` constructor in Solnet.Wallet 6.1.0 accept two string parameters (`privateKey`, `publicKey`)?
2. Has this code worked previously, or is this a known issue?
3. Should we be using `PrivateKey` and `PublicKey` objects instead of raw strings?
4. Is there a different way to instantiate the Account that we should be using?
5. Are there any additional configuration steps required for SolanaOASIS that we might be missing?

---

**Report Generated By:** AI Assistant  
**For:** Max Gershfield  
**To Share With:** David Ellams
