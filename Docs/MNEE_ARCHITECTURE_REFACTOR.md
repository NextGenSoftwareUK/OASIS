# MNEE Architecture Refactor - Generic Token Operations

**Date:** 2026-01-12  
**Status:** ✅ **REFACTORED TO FOLLOW OASIS GENERIC PATTERNS**

---

## 🎯 Problem Identified

The original `MNEEController` was **too specific** and didn't conform to OASIS architecture principles:
- ❌ Token-specific (MNEE only)
- ❌ Hardcoded to Ethereum provider
- ❌ Not reusable for other ERC-20 tokens
- ❌ Violated OASIS "everything should be generic" principle

---

## ✅ Solution: Generic Architecture

### 1. **Generic Token Service** (`MNEEService.cs`)
- **Renamed conceptually** to be a generic ERC-20 token service
- Accepts `contractAddress` parameter (defaults to MNEE for backward compatibility)
- Works with **any ERC-20 token**, not just MNEE
- All methods now accept optional `contractAddress` parameter

**Before:**
```csharp
public class MNEEService
{
    private const string MNEE_CONTRACT_ADDRESS = "0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF";
    // Hardcoded to MNEE
}
```

**After:**
```csharp
public class MNEEService
{
    public const string MNEE_CONTRACT_ADDRESS = "0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF";
    private readonly string _defaultContractAddress;
    
    public MNEEService(string rpcUrl, string contractAddress = null)
    {
        _defaultContractAddress = contractAddress ?? MNEE_CONTRACT_ADDRESS;
    }
    
    // All methods accept optional contractAddress parameter
    public async Task<OASISResult<decimal>> GetBalanceAsync(string address, string contractAddress = null)
}
```

### 2. **Generic WalletController Endpoints**

Added generic token operation endpoints to `WalletController`:

```
GET  /api/wallet/token/balance?tokenContractAddress={address}&providerType={type}
POST /api/wallet/token/approve
GET  /api/wallet/token/allowance?tokenContractAddress={address}&spenderAddress={spender}
GET  /api/wallet/token/info?tokenContractAddress={address}
```

**Features:**
- ✅ Works with **any ERC-20 token** (not just MNEE)
- ✅ Works with **any provider** (Ethereum, Base, Arbitrum, etc.)
- ✅ Accepts contract address as parameter
- ✅ Provider-agnostic design

### 3. **MNEEController as Convenience Wrapper**

`MNEEController` is now a **thin convenience wrapper** that:
- ✅ Defaults to MNEE contract address for convenience
- ✅ Documents that generic endpoints exist in WalletController
- ✅ Maintains backward compatibility
- ✅ Clearly marked as "convenience" endpoints

**Example:**
```csharp
/// <summary>
/// Get MNEE balance for an avatar (convenience wrapper)
/// Uses MNEE contract address: 0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF
/// For generic token balance, use: GET /api/wallet/token/balance?tokenContractAddress={address}
/// </summary>
```

---

## 📊 Architecture Comparison

### Before (Non-Generic)
```
MNEEController (MNEE-specific)
  └── Hardcoded to MNEE contract
  └── Hardcoded to Ethereum provider
  └── Not reusable
```

### After (Generic)
```
WalletController (Generic)
  └── /api/wallet/token/balance (any ERC-20, any provider)
  └── /api/wallet/token/approve (any ERC-20, any provider)
  └── /api/wallet/token/allowance (any ERC-20, any provider)
  └── /api/wallet/token/info (any ERC-20, any provider)

MNEEController (Convenience Wrapper)
  └── /api/mnee/balance (defaults to MNEE)
  └── /api/mnee/transfer (defaults to MNEE)
  └── /api/mnee/approve (defaults to MNEE)
  └── /api/mnee/allowance (defaults to MNEE)
  └── /api/mnee/token-info (defaults to MNEE)
```

---

## 🔄 Usage Examples

### Generic Token Operations (Recommended)

**Get any ERC-20 token balance:**
```bash
GET /api/wallet/token/balance?tokenContractAddress=0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF&providerType=EthereumOASIS
```

**Get USDC balance:**
```bash
GET /api/wallet/token/balance?tokenContractAddress=0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48&providerType=EthereumOASIS
```

**Approve any token:**
```bash
POST /api/wallet/token/approve
{
  "tokenContractAddress": "0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF",
  "spenderAddress": "0x...",
  "amount": 100.0,
  "providerType": "EthereumOASIS"
}
```

### MNEE Convenience Endpoints (Backward Compatible)

**Get MNEE balance (convenience):**
```bash
GET /api/mnee/balance/{avatarId}
```

**Transfer MNEE (convenience):**
```bash
POST /api/mnee/transfer
{
  "FromAvatarId": "...",
  "ToAvatarId": "...",
  "Amount": 10.5
}
```

---

## ✅ Benefits

1. **Generic & Reusable**: Works with any ERC-20 token
2. **Provider-Agnostic**: Works with Ethereum, Base, Arbitrum, etc.
3. **OASIS Compliant**: Follows "everything should be generic" principle
4. **Backward Compatible**: MNEE convenience endpoints still work
5. **Extensible**: Easy to add support for other token standards (ERC-721, etc.)

---

## 📝 Migration Guide

### For New Integrations
- ✅ **Use generic endpoints**: `/api/wallet/token/*`
- ✅ **Pass contract address as parameter**
- ✅ **Specify provider type**

### For Existing MNEE Integrations
- ✅ **Continue using**: `/api/mnee/*` endpoints
- ✅ **No breaking changes**
- ✅ **Consider migrating to generic endpoints** for flexibility

---

## 🎯 OASIS Architecture Compliance

| Principle | Before | After |
|-----------|--------|-------|
| **Generic** | ❌ MNEE-specific | ✅ Any ERC-20 token |
| **Provider-Agnostic** | ❌ Ethereum-only | ✅ Any provider |
| **Reusable** | ❌ Hardcoded | ✅ Parameterized |
| **Extensible** | ❌ Limited | ✅ Easy to extend |

---

**Status:** ✅ **FULLY COMPLIANT WITH OASIS ARCHITECTURE**
