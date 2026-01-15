# SmartContractGenerator Integration - Quick Start

## Overview

The SmartContractGenerator service at `/Users/maxgershfield/OASIS_CLEAN/SmartContractGenerator` provides smart contract generation, compilation, and deployment capabilities. This guide shows the fastest way to integrate it into OASIS.

## Prerequisites

1. **SmartContractGenerator must be running:**
   ```bash
   cd SmartContractGenerator/src/SmartContractGen/ScGen.API
   dotnet run
   ```
   Default URL: `http://localhost:5000`

2. **Required compilers installed:**
   - For Solana: `rustup`, `cargo`, `anchor`, `solana-cli`
   - For Ethereum: `solc`
   - For Radix: `scrypto-cli`

## Quick Integration (3 Steps)

### Step 1: Add SmartContractLanguage Enum

**File:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Enums/SmartContractLanguage.cs`

```csharp
namespace NextGenSoftware.OASIS.API.Core.Enums
{
    public enum SmartContractLanguage
    {
        Solidity,  // Ethereum
        Rust,      // Solana
        Scrypto    // Radix
    }
}
```

### Step 2: Create SmartContractManager

Copy the `SmartContractManager` implementation from `Docs/SMARTCONTRACT_GENERATOR_INTEGRATION_GUIDE.md` (Approach 1, Section 1.2) to:

**File:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/SmartContractManager/SmartContractManager.cs`

### Step 3: Initialize in OASISBootLoader

**File:** `OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.cs`

Add to `BootOASIS()` method:

```csharp
// Initialize SmartContractManager
string scGenUrl = OASISDNA?.OASIS?.Services?.SmartContractGenerator?.BaseUrl 
    ?? "http://localhost:5000";
SmartContractManager = new SmartContractManager(scGenUrl);
```

## Usage Examples

### Example 1: Compile Generated Rust Code

```csharp
// After calling SolanaOASIS.NativeCodeGenesis()
string rustPath = Path.Combine(outputFolder, "Solana", "lib.rs");

// Create Anchor project ZIP
byte[] projectZip = CreateAnchorProjectZip(rustPath);

// Compile
var result = await SmartContractManager.CompileContractAsync(
    SmartContractLanguage.Rust,
    projectZip);

if (!result.IsError)
{
    byte[] compiledBytecode = result.Result.CompiledBytecode;
    // Use compiled bytecode for deployment
}
```

### Example 2: Generate and Deploy Token Contract

```csharp
// 1. Generate
var jsonSpec = @"{
    ""contract_type"": ""token"",
    ""blockchain"": ""solana"",
    ""language"": ""Rust"",
    ""spec"": {
        ""name"": ""MyToken"",
        ""symbol"": ""MTK"",
        ""total_supply"": 1000000
    }
}";

var generateResult = await SmartContractManager.GenerateContractAsync(
    SmartContractLanguage.Rust,
    jsonSpec);

// 2. Compile (create ZIP from generated code first)
byte[] projectZip = CreateProjectZipFromGeneratedCode(generateResult.Result);
var compileResult = await SmartContractManager.CompileContractAsync(
    SmartContractLanguage.Rust,
    projectZip);

// 3. Deploy
var deployResult = await SmartContractManager.DeployContractAsync(
    SmartContractLanguage.Rust,
    compileResult.Result.CompiledBytecode,
    compileResult.Result.AbiOrSchema,
    walletPrivateKey);
```

## Integration with SolanaOASIS.NativeCodeGenesis

To automatically compile after code generation:

```csharp
// Enhanced NativeCodeGenesis that compiles
public async Task<OASISResult<byte[]>> NativeCodeGenesisAndCompileAsync(
    ICelestialBody celestialBody,
    string outputFolder)
{
    // 1. Generate (existing method)
    bool generated = NativeCodeGenesis(celestialBody, outputFolder);
    if (!generated) return OASISResult<byte[]>.Error("Generation failed");

    // 2. Create Anchor project
    string rustPath = Path.Combine(outputFolder, "Solana", "lib.rs");
    byte[] projectZip = CreateAnchorProjectZip(rustPath, outputFolder);

    // 3. Compile
    var compileResult = await SmartContractManager.CompileContractAsync(
        SmartContractLanguage.Rust,
        projectZip);

    return compileResult.IsError 
        ? OASISResult<byte[]>.Error(compileResult.Message)
        : new OASISResult<byte[]> { Result = compileResult.Result.CompiledBytecode };
}
```

## Configuration

Add to `OASIS_DNA.json`:

```json
{
  "OASIS": {
    "Services": {
      "SmartContractGenerator": {
        "BaseUrl": "http://localhost:5000",
        "TimeoutMinutes": 30,
        "Enabled": true
      }
    }
  }
}
```

## Testing

1. **Start SmartContractGenerator:**
   ```bash
   cd SmartContractGenerator/src/SmartContractGen/ScGen.API
   dotnet run
   ```

2. **Test the API directly:**
   ```bash
   curl -X POST "http://localhost:5000/api/v1/contracts/generate" \
     -F 'Language=Rust' \
     -F 'JsonFile=@spec.json'
   ```

3. **Test from OASIS:**
   ```csharp
   var manager = new SmartContractManager();
   var result = await manager.GenerateContractAsync(
       SmartContractLanguage.Rust,
       jsonSpec);
   ```

## Common Issues

### Issue: "Connection refused" or timeout
**Solution:** Ensure SmartContractGenerator is running on the configured port.

### Issue: Compilation fails with dependency errors
**Solution:** Ensure all required compilers (`anchor`, `cargo`, `rustc`) are installed and in PATH.

### Issue: ZIP extraction fails
**Solution:** Implement proper ZIP handling using `System.IO.Compression.ZipArchive`.

## Next Steps

1. See `SMARTCONTRACT_GENERATOR_INTEGRATION_GUIDE.md` for detailed implementation
2. See `SMARTCONTRACT_GENERATOR_INTEGRATION_EXAMPLE.cs` for code examples
3. Implement ZIP utilities for project creation/extraction
4. Add error handling and retry logic
5. Integrate with OASIS wallet management

## Architecture

```
┌─────────────────┐
│   OASIS Core    │
│  (ONODE/STAR)   │
└────────┬────────┘
         │
         │ HTTP/REST
         │
┌────────▼──────────────────┐
│ SmartContractManager       │
│ (OASIS Integration Layer) │
└────────┬──────────────────┘
         │
         │ HTTP/REST
         │
┌────────▼──────────────────┐
│ SmartContractGenerator API │
│ (Standalone Service)        │
│ - Generate                  │
│ - Compile                   │
│ - Deploy                    │
└────────────────────────────┘
```

## Benefits

- ✅ **Separation of Concerns**: Compilation logic isolated
- ✅ **Scalability**: Can scale SmartContractGenerator independently
- ✅ **Reusability**: Same service used by multiple OASIS components
- ✅ **Maintainability**: Updates to compiler don't affect OASIS core
