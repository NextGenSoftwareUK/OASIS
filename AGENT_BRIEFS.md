# Agent Briefs - STAR CLI Build Fixes

## Current Status
- **Starting Point**: 130 errors remaining
- **Target**: 0 errors
- **Strategy**: Comment out Bridge functionality (not required for STAR CLI)

## Pattern to Follow
All Bridge-related code should be commented out using:
```csharp
// TODO: Bridge functionality excluded - Bridge not required for STAR CLI
/*public async Task<OASISResult<BridgeTransactionResponse>> MethodName(...)
{
    // ... method body ...
}*/
```

For `using` statements:
```csharp
// TODO: Bridge functionality excluded - Bridge not required for STAR CLI
//using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
//using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
```

---

## Brief 1: ThreeFoldOASIS Bridge References
**Agent**: Fix ThreeFoldOASIS Bridge references  
**File**: `Providers/Network/NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS/ThreeFoldOASIS.cs`  
**Errors**: 14 errors (Bridge using statements and Bridge methods)

### Tasks:
1. Comment out Bridge `using` statements (lines 28-29)
2. Comment out all Bridge methods:
   - `WithdrawAsync` (around line 2321)
   - `DepositAsync` (around line 2371)
   - `GetTransactionStatusAsync` (around line 2420)
   - `WithdrawNFTAsync` (around line 2589)
   - `DepositNFTAsync` (around line 2640)

### Example Pattern:
```csharp
// TODO: Bridge functionality excluded - Bridge not required for STAR CLI
/*public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(...)
{
    // ... existing code ...
}*/
```

### Verification:
```bash
cd /Volumes/Storage/OASIS_CLEAN && dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" --configuration Release 2>&1 | grep "ThreeFoldOASIS" | grep "error CS" | wc -l
```
Should return 0.

---

## Brief 2: AvalancheOASIS Bridge References
**Agent**: Fix AvalancheOASIS Bridge references  
**Files**: 
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AvalancheOASIS/AvalancheOASIS.cs`
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AvalancheOASIS/Infrastructure/Services/Avalanche/AvalancheBridgeService.cs`
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AvalancheOASIS/Infrastructure/Services/Avalanche/IAvalancheBridgeService.cs`
**Errors**: 14 errors

### Tasks:
1. Comment out Bridge `using` statements in `AvalancheOASIS.cs`
2. Comment out all Bridge methods in `AvalancheOASIS.cs`:
   - `WithdrawAsync`
   - `DepositAsync`
   - `GetTransactionStatusAsync`
   - `WithdrawNFTAsync`
   - `DepositNFTAsync`
3. Exclude Bridge service files from compilation in `.csproj`:
   ```xml
   <ItemGroup>
     <Compile Remove="Infrastructure\Services\Avalanche\IAvalancheBridgeService.cs" />
     <Compile Remove="Infrastructure\Services\Avalanche\AvalancheBridgeService.cs" />
   </ItemGroup>
   ```

### Verification:
```bash
cd /Volumes/Storage/OASIS_CLEAN && dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" --configuration Release 2>&1 | grep "AvalancheOASIS" | grep "error CS" | wc -l
```

---

## Brief 3: ArbitrumOASIS Bridge References
**Agent**: Fix ArbitrumOASIS Bridge references  
**Files**:
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS/ArbitrumOASIS.cs`
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS/Infrastructure/Services/Arbitrum/ArbitrumBridgeService.cs`
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS/Infrastructure/Services/Arbitrum/IArbitrumBridgeService.cs`
**Errors**: 14 errors

### Tasks:
1. Comment out Bridge `using` statements in `ArbitrumOASIS.cs`
2. Comment out all Bridge methods in `ArbitrumOASIS.cs`
3. Exclude Bridge service files from compilation in `.csproj`

### Verification:
```bash
cd /Volumes/Storage/OASIS_CLEAN && dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" --configuration Release 2>&1 | grep "ArbitrumOASIS" | grep "error CS" | wc -l
```

---

## Brief 4: SOLANAOASIS Bridge References
**Agent**: Fix SOLANAOASIS Bridge references  
**File**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/SolanaOasis.cs`  
**Errors**: 10 errors (Bridge methods not commented out)

### Tasks:
1. Find and comment out all Bridge methods:
   - `WithdrawAsync` (around line 3594)
   - `DepositAsync` (around line 3663)
   - `GetTransactionStatusAsync` (around line 3729)
   - `WithdrawNFTAsync` (around line 3791)
   - `DepositNFTAsync` (if exists)

### Note:
Bridge service files are already excluded in `.csproj`. Only need to comment out methods in `SolanaOasis.cs`.

### Verification:
```bash
cd /Volumes/Storage/OASIS_CLEAN && dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" --configuration Release 2>&1 | grep "SOLANAOASIS" | grep "error CS" | wc -l
```

---

## Brief 5: EOSIOOASIS Bridge References
**Agent**: Fix EOSIOOASIS Bridge references  
**File**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EOSIOOASIS/EOSIOOASIS.cs`  
**Errors**: 10 errors

### Tasks:
1. Comment out Bridge `using` statements
2. Comment out all Bridge methods:
   - `WithdrawAsync`
   - `DepositAsync`
   - `GetTransactionStatusAsync`
   - `WithdrawNFTAsync`
   - `DepositNFTAsync`

### Verification:
```bash
cd /Volumes/Storage/OASIS_CLEAN && dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" --configuration Release 2>&1 | grep "EOSIOOASIS" | grep "error CS" | wc -l
```

---

## Brief 6: CosmosBlockChainOASIS Bridge References
**Agent**: Fix CosmosBlockChainOASIS Bridge references  
**File**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/CosmosBlockChainOASIS.cs`  
**Errors**: 6 errors

### Tasks:
1. Comment out Bridge `using` statements
2. Comment out all Bridge methods

### Note:
This file was partially fixed earlier. Check for any remaining Bridge references.

### Verification:
```bash
cd /Volumes/Storage/OASIS_CLEAN && dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" --configuration Release 2>&1 | grep "CosmosBlockChainOASIS" | grep "error CS" | wc -l
```

---

## Brief 7: BaseOASIS Bridge References
**Agent**: Fix BaseOASIS Bridge references  
**File**: `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BaseOASIS/BaseOASIS.cs`  
**Errors**: 4 errors

### Tasks:
1. Verify Bridge `using` statements are commented out (should already be done)
2. Comment out any remaining Bridge methods

### Note:
This file was partially fixed earlier. Check for any remaining Bridge references.

### Verification:
```bash
cd /Volumes/Storage/OASIS_CLEAN && dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" --configuration Release 2>&1 | grep "BaseOASIS" | grep "error CS" | wc -l
```

---

## Brief 8: Comment Block Syntax Fixes
**Agent**: Fix comment block syntax errors  
**Files**:
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TRONOASIS/TRONOASIS.cs` (4 errors)
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BitcoinOASIS/BitcoinOASIS.cs` (2 errors)
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SuiOASIS.cs` (2 errors)

### Tasks:

#### TRONOASIS:
- Line 3499: Check for unclosed comment block before this line
- Line 3854: Check for unclosed comment block - ensure `*/` is present at end of file (around line 4114)

#### BitcoinOASIS:
- Line 2865: Check for unclosed comment block - ensure `*/` closes the `WithdrawAsync` comment

#### SuiOASIS:
- Line 2558: Check for unexpected preprocessor directive - likely an unclosed comment block

### Pattern to Check:
```csharp
// Every /* must have a matching */
// Check for:
/*public async Task...  // Opening
{
    // ... code ...
}*/  // Closing
```

### Verification:
```bash
cd /Volumes/Storage/OASIS_CLEAN && dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" --configuration Release 2>&1 | grep -E "(TRONOASIS|BitcoinOASIS|SuiOASIS)" | grep "error CS" | wc -l
```

---

## Brief 9: KeyHelper and Implementation Issues
**Agent**: Fix KeyHelper and ZcashOASIS implementation issues  
**Files**:
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AztecOASIS/AztecOASIS.cs` (1 error - KeyHelper)
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/src/Web3CoreOASISBaseProvider.cs` (2 errors - KeyHelper)
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ZcashOASIS/ZcashOASIS.cs` (2 errors - WalletAddressLegacy)
- `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ZcashOASIS/Infrastructure/Repositories/ZcashRepository.cs` (6 errors - ListTransactionsAsync)

### Tasks:

#### KeyHelper Issues:
1. Find all `KeyHelper` references
2. Comment them out or replace with alternative implementation:
   ```csharp
   // TODO: KeyHelper not available - Bridge functionality excluded
   //var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
   var keyPair = null; // Or provide alternative implementation
   ```

#### ZcashOASIS Issues:
1. **ZcashRepository.cs**: Comment out or fix `ListTransactionsAsync` calls (lines 37, 184, 222)
2. **ZcashOASIS.cs**: Fix `WalletAddressLegacy` access (line 1756) - likely needs type casting

### Verification:
```bash
cd /Volumes/Storage/OASIS_CLEAN && dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" --configuration Release 2>&1 | grep -E "(KeyHelper|ZcashOASIS|ListTransactionsAsync|WalletAddressLegacy)" | grep "error CS" | wc -l
```

---

## Brief 10: Final Verification and Cleanup
**Agent**: Final verification and cleanup  
**Tasks**:
1. Run full build and count remaining errors
2. Verify all Bridge references are commented out
3. Check for any remaining syntax errors
4. Update `STAR_BUILD_FIXES.md` with final status

### Commands:
```bash
# Count total errors
cd /Volumes/Storage/OASIS_CLEAN && dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" --configuration Release 2>&1 | grep "error CS" | wc -l

# List all remaining errors by file
cd /Volumes/Storage/OASIS_CLEAN && dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" --configuration Release 2>&1 | grep "error CS" | cut -d'(' -f1 | sort | uniq -c | sort -rn
```

---

## General Guidelines for All Agents

1. **Always check for unclosed comment blocks** - Every `/*` must have a matching `*/`
2. **Preserve code structure** - Don't delete code, comment it out
3. **Use consistent comments** - Always use: `// TODO: Bridge functionality excluded - Bridge not required for STAR CLI`
4. **Test after changes** - Run the verification command after making changes
5. **Check for related files** - Some providers have Bridge service files in separate directories
6. **Update .csproj files** - Exclude Bridge service files from compilation when they exist

## Success Criteria
- All Bridge-related errors resolved
- All syntax errors fixed
- Build completes with 0 errors
- All commented code is clearly marked with TODO comments

---

## Current Error Breakdown (130 total)
- ThreeFoldOASIS: 14
- AvalancheOASIS: 14
- ArbitrumOASIS: 14
- SOLANAOASIS: 10
- EOSIOOASIS: 10
- ZcashOASIS: 8
- CosmosBlockChainOASIS: 6
- TRONOASIS: 4
- BaseOASIS: 4
- Web3CoreOASIS: 2
- AztecOASIS: 1
- SuiOASIS: 2
- BitcoinOASIS: 2
- Others: ~39

---

**Last Updated**: After fixing EthereumOASIS unclosed comment block (150 → 130 errors)

