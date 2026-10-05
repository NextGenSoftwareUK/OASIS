# Agent Briefs - Quick Reference

## Overview
**Current Errors**: 130  
**Target**: 0  
**Strategy**: Comment out Bridge functionality (not required for STAR CLI)

## Brief Assignments

| Brief | Agent Task | Files | Errors | Priority |
|-------|-----------|-------|--------|----------|
| **Brief 1** | ThreeFoldOASIS Bridge | `ThreeFoldOASIS.cs` | 14 | High |
| **Brief 2** | AvalancheOASIS Bridge | `AvalancheOASIS.cs` + Bridge services | 14 | High |
| **Brief 3** | ArbitrumOASIS Bridge | `ArbitrumOASIS.cs` + Bridge services | 14 | High |
| **Brief 4** | SOLANAOASIS Bridge | `SolanaOasis.cs` | 10 | High |
| **Brief 5** | EOSIOOASIS Bridge | `EOSIOOASIS.cs` | 10 | High |
| **Brief 6** | CosmosBlockChainOASIS Bridge | `CosmosBlockChainOASIS.cs` | 6 | Medium |
| **Brief 7** | BaseOASIS Bridge | `BaseOASIS.cs` | 4 | Medium |
| **Brief 8** | Comment Block Syntax | `TRONOASIS.cs`, `BitcoinOASIS.cs`, `SuiOASIS.cs` | 8 | High |
| **Brief 9** | KeyHelper & Zcash Issues | `AztecOASIS.cs`, `Web3CoreOASISBaseProvider.cs`, `ZcashOASIS.cs`, `ZcashRepository.cs` | 11 | Medium |
| **Brief 10** | Final Verification | All files | - | Final |

## Quick Start Commands

### Check Current Error Count
```bash
cd /Volumes/Storage/OASIS_CLEAN && dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" --configuration Release 2>&1 | grep "error CS" | wc -l
```

### Check Errors for Specific Provider
```bash
cd /Volumes/Storage/OASIS_CLEAN && dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" --configuration Release 2>&1 | grep "ProviderName" | grep "error CS" | wc -l
```

### List All Errors by File
```bash
cd /Volumes/Storage/OASIS_CLEAN && dotnet build "STAR ODK/NextGenSoftware.OASIS.STAR.CLI/NextGenSoftware.OASIS.STAR.CLI.csproj" --configuration Release 2>&1 | grep "error CS" | cut -d'(' -f1 | sort | uniq -c | sort -rn
```

## Standard Pattern

### For Bridge Methods:
```csharp
// TODO: Bridge functionality excluded - Bridge not required for STAR CLI
/*public async Task<OASISResult<BridgeTransactionResponse>> MethodName(...)
{
    // ... existing code ...
}*/
```

### For Bridge Using Statements:
```csharp
// TODO: Bridge functionality excluded - Bridge not required for STAR CLI
//using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
//using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
```

### For Bridge Service Files in .csproj:
```xml
<!-- TODO: Bridge functionality excluded - Bridge not required for STAR CLI -->
<ItemGroup>
  <Compile Remove="Infrastructure\Services\ProviderName\IProviderBridgeService.cs" />
  <Compile Remove="Infrastructure\Services\ProviderName\ProviderBridgeService.cs" />
</ItemGroup>
```

## Full Details
See `AGENT_BRIEFS.md` for complete instructions for each brief.

