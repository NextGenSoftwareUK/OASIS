# Storage Implementation Pattern for Blockchain Providers

## Overview
Each blockchain provider should use its native SDK/API to store data on-chain:
- **EVM chains** (Ethereum, Avalanche, Base, Arbitrum, etc.) → Smart contracts via Web3/Nethereum
- **Solana** → Solana programs
- **NEAR** → NEAR smart contracts
- **Radix** → Radix transaction manifests calling blueprints/components
- **Miden** → Miden programs/private notes
- **Starknet** → Starknet contracts
- **Aztec** → Aztec contracts

## Pattern: EVM Chains (Ethereum, Avalanche, Base, Arbitrum, etc.)

### SaveHolonAsync Example
```csharp
public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, ...)
{
    var result = new OASISResult<IHolon>();
    
    try
    {
        // 1. Serialize holon to JSON
        string holonInfo = JsonSerializer.Serialize(holon);
        int holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
        string holonId = holon.Id.ToString();
        
        // 2. Get wallet for transaction
        OASISResult<IProviderWallet> wallet = await WalletManager.Instance
            .GetAvatarDefaultWalletByIdAsync(holon.Id, this.ProviderType.Value);
        if (wallet.IsError)
        {
            OASISErrorHandling.HandleError(ref result, wallet.Message);
            return result;
        }
        
        // 3. Call smart contract function
        Function createHolonFunc = _contract.GetFunction("CreateHolon");
        TransactionReceipt txReceipt = await createHolonFunc
            .SendTransactionAndWaitForReceiptAsync(
                wallet.Result.WalletAddress,
                receiptRequestCancellationToken: null,
                holonEntityId,
                holonId,
                holonInfo);
        
        // 4. Check transaction result
        if (txReceipt.HasErrors())
        {
            OASISErrorHandling.HandleError(ref result, "Transaction failed");
            return result;
        }
        
        result.Result = holon;
        result.IsError = false;
        result.IsSaved = true;
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError(ref result, ex.Message, ex);
    }
    
    return result;
}
```

## Pattern: Radix (Transaction Manifests)

Radix uses Scrypto blueprints/components instead of Solidity contracts. Storage operations need:
1. OASIS blueprint/component deployed on Radix
2. Transaction manifest calling blueprint function
3. Submit transaction via RadixEngineToolkit

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
        OASISResult<IProviderWallet> wallet = await WalletManager.Instance
            .GetAvatarDefaultWalletByIdAsync(holon.Id, this.ProviderType.Value);
        
        // 3. Build transaction manifest calling OASIS blueprint
        // Note: Requires OASIS blueprint deployed on Radix
        var manifest = new ManifestBuilder()
            .CallMethod(
                componentAddress: _oasisBlueprintAddress,
                functionName: "create_holon",
                args: new object[] { holonEntityId, holonInfo })
            .Build(_config.NetworkId);
        
        // 4. Sign and submit transaction
        // (Implementation depends on RadixEngineToolkit API)
        
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

## Pattern: Solana (Programs)

```csharp
public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, ...)
{
    // Use Solana program to store holon
    // Similar pattern: serialize, get wallet, call program instruction, submit transaction
}
```

## Pattern: NEAR (Smart Contracts)

```csharp
public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, ...)
{
    // Use NEAR SDK to call smart contract function
    // Similar pattern: serialize, get wallet, call contract method, submit transaction
}
```

## Required Smart Contracts/Blueprints

Each blockchain provider needs:
1. **OASIS Storage Contract/Blueprint** deployed
2. Functions: `CreateAvatar`, `CreateHolon`, `CreateAvatarDetail`, `UpdateAvatar`, `DeleteAvatar`, etc.
3. Contract address stored in provider config

## Implementation Status

### ✅ Has Smart Contracts
- EthereumOASIS - Uses NextGenSoftwareOASIS contract
- AvalancheOASIS - Uses AvalancheContractHelper
- BaseOASIS - Uses BaseContractHelper
- ArbitrumOASIS - Has OASISStorage.sol
- SOLANAOASIS - Has OASISStorage.sol program
- EOSIOOASIS - Has C++ contract

### ❌ Needs Smart Contracts/Blueprints
- RadixOASIS - Needs Radix blueprint/component
- MidenOASIS - Needs Miden program
- StarknetOASIS - Needs Starknet contract
- NEAROASIS - Needs NEAR contract
- PolkadotOASIS - Needs Polkadot contract
- CardanoOASIS - Needs Cardano contract

## Next Steps

1. **For providers with contracts**: Implement real smart contract calls
2. **For providers without contracts**: 
   - Document contract deployment requirements
   - Implement placeholder that returns clear error message
   - Or use ProviderManager delegation as fallback until contract deployed

