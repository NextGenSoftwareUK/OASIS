# SDK Integration Requirements for OASIS Providers

## Overview
Each blockchain provider needs real SDK integrations for storage operations. This document outlines what's needed for each provider.

## Implementation Status by Provider

### ✅ Fully Implemented (Using Smart Contracts)

#### EVM Chains (Ethereum, Avalanche, Base, Arbitrum, Optimism, Fantom, BNB Chain, Polygon, Rootstock, TRON)
- **Status**: ✅ Smart contracts deployed and integrated
- **SDK**: Nethereum (Web3)
- **Storage Method**: Smart contract calls (`CreateAvatar`, `CreateHolon`, `CreateAvatarDetail`)
- **Example**: `BaseOASIS.cs`, `AvalancheOASIS.cs`, `EthereumOASIS.cs`

#### Solana
- **Status**: ✅ Solana program deployed
- **SDK**: Solnet
- **Storage Method**: Solana program instructions
- **Example**: `SOLANAOASIS.cs`

#### EOSIO
- **Status**: ✅ C++ smart contract deployed
- **SDK**: EOSIO SDK
- **Storage Method**: EOSIO contract actions
- **Example**: `EOSIOOASIS.cs`

### 🔄 Partially Implemented (Needs SDK Integration)

#### HashgraphOASIS (Hedera)
- **Current**: HTTP calls to Mirror Node (read-only)
- **Needs**: Hedera SDK for File Service or Smart Contract Service
- **Required SDK**: `Hedera.SDK` NuGet package
- **Storage Options**:
  1. **Hedera File Service** - Store JSON data in files
  2. **Hedera Smart Contract Service** - Deploy/call Solidity contracts
- **Action**: Add Hedera SDK and implement File Service or Smart Contract calls

#### RadixOASIS
- **Current**: Bridge operations implemented, storage placeholders
- **Needs**: Radix blueprint/component for storage
- **Required**: 
  1. OASIS blueprint deployed on Radix
  2. Transaction manifest builder for blueprint calls
- **SDK**: RadixEngineToolkit (already referenced)
- **Action**: 
  1. Deploy OASIS blueprint/component
  2. Implement transaction manifests calling blueprint functions
  3. Complete `SaveHolonAsync`, `SaveAvatarAsync` using manifests

#### MidenOASIS
- **Current**: API client structure, private notes for bridge
- **Needs**: Miden program for storage
- **Required**: 
  1. Miden program deployed
  2. Program calls for storage operations
- **SDK**: Miden API client (already implemented)
- **Action**: Implement program calls for `SaveHolonAsync`, `SaveAvatarAsync`

#### StarknetOASIS
- **Current**: RPC client for bridge operations
- **Needs**: Starknet contract for storage
- **Required**: 
  1. Starknet contract deployed
  2. Contract calls via RPC
- **SDK**: Starknet RPC client (already implemented)
- **Action**: Implement contract calls for storage operations

#### NEAROASIS
- **Current**: Basic structure
- **Needs**: NEAR contract for storage
- **Required SDK**: `NEAR.SDK` or `near-api-dotnet`
- **Action**: Implement NEAR contract calls

#### PolkadotOASIS
- **Current**: Placeholder methods
- **Needs**: Polkadot pallet/contract
- **Required SDK**: `Polkadot.NET` or Substrate.NET
- **Action**: Implement Substrate/Polkadot storage calls

#### CardanoOASIS
- **Current**: Placeholder methods
- **Needs**: Cardano smart contract
- **Required SDK**: `CardanoSharp`
- **Action**: Implement Cardano contract calls

#### BlockStackOASIS
- **Current**: BlockStackClient structure
- **Needs**: Gaia storage integration
- **Required**: BlockStack SDK for Gaia storage
- **Action**: Implement Gaia file operations

#### AztecOASIS
- **Current**: Bridge operations, CLI service
- **Needs**: Aztec contract for storage
- **Required**: Aztec contract deployed
- **Action**: Implement contract calls for storage

## Implementation Pattern

### For Providers WITH Smart Contracts/Programs

```csharp
public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, ...)
{
    var result = new OASISResult<IHolon>();
    
    try
    {
        // 1. Serialize holon
        string holonInfo = JsonSerializer.Serialize(holon);
        int holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
        
        // 2. Get wallet
        var wallet = await WalletManager.Instance
            .GetAvatarDefaultWalletByIdAsync(holon.Id, this.ProviderType.Value);
        
        // 3. Call smart contract/program
        // Provider-specific SDK call here
        
        // 4. Return result
        result.Result = holon;
        result.IsError = false;
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError(ref result, ex.Message, ex);
    }
    
    return result;
}
```

### For Providers WITHOUT Smart Contracts Yet

```csharp
public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, ...)
{
    var result = new OASISResult<IHolon>();
    
    if (!string.IsNullOrEmpty(_contractAddress))
    {
        // Contract is configured - use it
        // Implement SDK calls here
        OASISErrorHandling.HandleError(ref result, 
            "Storage requires [Provider] SDK integration. Please install [SDK] NuGet package.");
        return result;
    }
    else
    {
        // No contract yet - delegate to ProviderManager as fallback
        return await HolonManager.Instance.SaveHolonAsync(holon, Guid.Empty, ...);
    }
}
```

## Required NuGet Packages

### To Add:
- **HashgraphOASIS**: `Hedera.SDK` or `Hedera.NET`
- **PolkadotOASIS**: `Polkadot.NET` or `Substrate.NET`
- **CardanoOASIS**: `CardanoSharp`
- **NEAROASIS**: `NEAR.SDK` or `near-api-dotnet`
- **BlockStackOASIS**: BlockStack SDK for Gaia

### Already Referenced:
- **RadixOASIS**: `RadixDlt.RadixEngineToolkit` ✅
- **EVM Chains**: `Nethereum.Web3` ✅
- **Solana**: `Solnet` ✅

## Next Steps Priority

1. **High Priority** (Most Used):
   - HashgraphOASIS - Add Hedera SDK
   - RadixOASIS - Complete transaction manifest building
   - BlockStackOASIS - Complete Gaia integration

2. **Medium Priority**:
   - MidenOASIS - Complete program calls
   - StarknetOASIS - Complete contract calls
   - NEAROASIS - Add NEAR SDK

3. **Lower Priority**:
   - PolkadotOASIS - Add Polkadot SDK
   - CardanoOASIS - Add CardanoSharp

## Notes

- Providers should check for contract/blueprint address in config
- If contract exists, use SDK to call it
- If contract doesn't exist, delegate to ProviderManager (fallback)
- Bridge operations should always use blockchain SDKs (already implemented)








