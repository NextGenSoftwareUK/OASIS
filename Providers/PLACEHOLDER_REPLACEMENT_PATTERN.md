# Placeholder Replacement Pattern for Blockchain Providers

## Overview
Blockchain providers (RadixOASIS, MidenOASIS, StarknetOASIS, etc.) focus on bridge operations and blockchain-specific functionality. Storage operations should delegate to ProviderManager/HolonManager/AvatarManager.

## Replacement Pattern

### Storage Operations → Delegate to Managers

#### Holon Operations
```csharp
// BEFORE (Placeholder):
public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, ...)
{
    return Task.FromResult(new OASISResult<IHolon>
    {
        IsError = true,
        Message = "SaveHolon not implemented for RadixOASIS - use for bridge operations"
    });
}

// AFTER (Real Implementation):
public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, ...)
{
    // RadixOASIS focuses on bridge operations - delegate storage to ProviderManager
    return HolonManager.Instance.SaveHolonAsync(holon, Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
}
```

#### Avatar Operations
```csharp
// BEFORE (Placeholder):
public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
{
    return Task.FromResult(new OASISResult<IAvatar>
    {
        IsError = true,
        Message = "LoadAvatarByProviderKey not implemented for RadixOASIS - use for bridge operations"
    });
}

// AFTER (Real Implementation):
public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
{
    // RadixOASIS focuses on bridge operations - delegate storage to ProviderManager
    return AvatarManager.Instance.LoadAvatarByProviderKeyAsync(providerKey, version);
}
```

#### Export/Import Operations
```csharp
// BEFORE (Placeholder):
public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
{
    return Task.FromResult(new OASISResult<bool>
    {
        IsError = true,
        Message = "Import not implemented for RadixOASIS - use for bridge operations"
    });
}

// AFTER (Real Implementation):
public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
{
    // RadixOASIS focuses on bridge operations - delegate storage to ProviderManager
    return HolonManager.Instance.ImportAsync(holons);
}
```

## Required Using Statements

Add to provider file:
```csharp
using NextGenSoftware.OASIS.API.Core.Managers;
```

## Providers to Update

1. **RadixOASIS** - 59 placeholder methods remaining
2. **MidenOASIS** - Multiple placeholder methods
3. **StarknetOASIS** - Multiple placeholder methods  
4. **AztecOASIS** - Multiple placeholder methods
5. **HashgraphOASIS** - Multiple placeholder methods
6. **PolkadotOASIS** - Multiple placeholder methods
7. **CardanoOASIS** - Multiple placeholder methods
8. **BlockStackOASIS** - Multiple placeholder methods

## Bridge Operations (Keep Real Implementations)

Bridge operations should remain blockchain-specific:
- `WithdrawAsync` - Use blockchain SDK
- `DepositAsync` - Use blockchain SDK
- `GetAccountBalanceAsync` - Use blockchain SDK
- `CreateAccountAsync` - Use blockchain SDK
- `SendTokenAsync` - Use blockchain SDK
- NFT operations - Use blockchain SDK

## Status

- ✅ Pattern established
- ✅ RadixOASIS: Started replacement (3/59 complete)
- ⏳ Remaining: 56 methods in RadixOASIS + all other providers




