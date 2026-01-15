# SmartContractGenerator Integration Guide

This guide outlines how to integrate the SmartContractGenerator service (`/Users/maxgershfield/OASIS_CLEAN/SmartContractGenerator`) into the OASIS architecture.

## Overview

The SmartContractGenerator is a standalone .NET API service that provides:
- **Contract Generation**: Generate smart contracts from JSON specifications
- **Contract Compilation**: Compile contracts to bytecode
- **Contract Deployment**: Deploy contracts to blockchains

**API Endpoints:**
- `POST /api/v1/contracts/generate` - Generate contract from JSON
- `POST /api/v1/contracts/compile` - Compile contract
- `POST /api/v1/contracts/deploy` - Deploy contract

## Integration Approaches

### Approach 1: Service/Manager Pattern (Recommended)

Create a `SmartContractManager` that wraps the SmartContractGenerator API, following OASIS patterns.

#### 1.1 Create the Service Interface

**File:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/SmartContractManager/ISmartContractManager.cs`

```csharp
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public interface ISmartContractManager
    {
        Task<OASISResult<byte[]>> GenerateContractAsync(
            SmartContractLanguage language,
            string jsonSpecification,
            CancellationToken cancellationToken = default);

        Task<OASISResult<CompileContractResult>> CompileContractAsync(
            SmartContractLanguage language,
            byte[] sourceCode,
            CancellationToken cancellationToken = default);

        Task<OASISResult<DeployContractResult>> DeployContractAsync(
            SmartContractLanguage language,
            byte[] compiledContract,
            byte[] abiOrSchema,
            string walletKeypairOrPrivateKey,
            CancellationToken cancellationToken = default);
    }

    public class CompileContractResult
    {
        public byte[] CompiledBytecode { get; set; }
        public byte[] AbiOrSchema { get; set; }
        public string AbiFileName { get; set; }
        public string BytecodeFileName { get; set; }
    }

    public class DeployContractResult
    {
        public string ContractAddress { get; set; }
        public string TransactionHash { get; set; }
        public bool IsSuccessful { get; set; }
    }
}
```

#### 1.2 Create the Manager Implementation

**File:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/SmartContractManager/SmartContractManager.cs`

```csharp
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Logging;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class SmartContractManager : ISmartContractManager
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger _logger;

        public SmartContractManager(string baseUrl = null, HttpClient httpClient = null)
        {
            _baseUrl = baseUrl ?? "http://localhost:5000";
            _httpClient = httpClient ?? new HttpClient();
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.Timeout = TimeSpan.FromMinutes(30); // Long timeout for compilation
            _logger = LoggingManager.CurrentLoggingFramework.GetLogger("SmartContractManager");
        }

        public async Task<OASISResult<byte[]>> GenerateContractAsync(
            SmartContractLanguage language,
            string jsonSpecification,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<byte[]>();

            try
            {
                using var formData = new MultipartFormDataContent();
                
                // Add JSON specification
                var jsonContent = new StringContent(jsonSpecification, Encoding.UTF8, "application/json");
                formData.Add(jsonContent, "JsonFile", "spec.json");
                
                // Add language
                formData.Add(new StringContent(language.ToString()), "Language");

                var response = await _httpClient.PostAsync("/api/v1/contracts/generate", formData, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    result.Result = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    result.IsError = false;
                    result.Message = "Contract generated successfully";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    OASISErrorHandling.HandleError(ref result, 
                        $"Failed to generate contract: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating contract: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<CompileContractResult>> CompileContractAsync(
            SmartContractLanguage language,
            byte[] sourceCode,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<CompileContractResult>();

            try
            {
                using var formData = new MultipartFormDataContent();
                
                // Add source code file
                var sourceContent = new ByteArrayContent(sourceCode);
                sourceContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                
                string fileName = language switch
                {
                    SmartContractLanguage.Solidity => "contract.sol",
                    SmartContractLanguage.Rust => "project.zip",
                    SmartContractLanguage.Scrypto => "package.zip",
                    _ => "source"
                };
                
                formData.Add(sourceContent, "Source", fileName);
                formData.Add(new StringContent(language.ToString()), "Language");

                var response = await _httpClient.PostAsync("/api/v1/contracts/compile", formData, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var zipBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    
                    // Extract ZIP contents (you'll need a ZIP extraction utility)
                    var extracted = ExtractZipContents(zipBytes);
                    
                    result.Result = new CompileContractResult
                    {
                        CompiledBytecode = extracted.Bytecode,
                        AbiOrSchema = extracted.Abi,
                        AbiFileName = extracted.AbiFileName,
                        BytecodeFileName = extracted.BytecodeFileName
                    };
                    result.IsError = false;
                    result.Message = "Contract compiled successfully";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    OASISErrorHandling.HandleError(ref result, 
                        $"Failed to compile contract: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error compiling contract: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<DeployContractResult>> DeployContractAsync(
            SmartContractLanguage language,
            byte[] compiledContract,
            byte[] abiOrSchema,
            string walletKeypairOrPrivateKey,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<DeployContractResult>();

            try
            {
                using var formData = new MultipartFormDataContent();
                
                // Add compiled contract
                var compiledContent = new ByteArrayContent(compiledContract);
                compiledContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                formData.Add(compiledContent, "CompiledContractFile", "contract.bin");

                // For Solana, use keypair; for others, use schema/ABI
                if (language == SmartContractLanguage.Rust)
                {
                    var keypairContent = new ByteArrayContent(Encoding.UTF8.GetBytes(walletKeypairOrPrivateKey));
                    formData.Add(keypairContent, "WalletKeypair", "keypair.json");
                }
                else
                {
                    var schemaContent = new ByteArrayContent(abiOrSchema);
                    schemaContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    formData.Add(schemaContent, "Schema", "contract.abi");
                }

                formData.Add(new StringContent(language.ToString()), "Language");

                var response = await _httpClient.PostAsync("/api/v1/contracts/deploy", formData, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    var deployResult = JsonSerializer.Deserialize<DeployContractResult>(responseContent);
                    
                    result.Result = deployResult;
                    result.IsError = false;
                    result.Message = "Contract deployed successfully";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    OASISErrorHandling.HandleError(ref result, 
                        $"Failed to deploy contract: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deploying contract: {ex.Message}", ex);
            }

            return result;
        }

        private (byte[] Bytecode, byte[] Abi, string AbiFileName, string BytecodeFileName) ExtractZipContents(byte[] zipBytes)
        {
            // Implement ZIP extraction logic
            // Use System.IO.Compression.ZipArchive
            throw new NotImplementedException("Implement ZIP extraction");
        }
    }
}
```

#### 1.3 Register in OASISBootLoader

**File:** `OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.cs`

Add to the boot process:

```csharp
public static ISmartContractManager SmartContractManager { get; private set; }

public static void BootOASIS(string OASISDNAPath = null)
{
    // ... existing boot code ...
    
    // Initialize SmartContractManager
    string scGenUrl = OASISDNA?.OASIS?.Services?.SmartContractGenerator?.BaseUrl 
        ?? "http://localhost:5000";
    SmartContractManager = new SmartContractManager(scGenUrl);
}
```

#### 1.4 Add to OASIS_DNA.json

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

### Approach 2: Provider Extension Pattern

Extend existing blockchain providers (e.g., `SolanaOASIS`) to use SmartContractGenerator for compilation.

#### 2.1 Extend SolanaOASIS

**File:** `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/SolanaOasis.cs`

Add a method to compile generated Rust code:

```csharp
public async Task<OASISResult<byte[]>> CompileNativeCodeAsync(
    string rustSourcePath,
    CancellationToken cancellationToken = default)
{
    var result = new OASISResult<byte[]>();

    try
    {
        // Use SmartContractManager to compile
        if (SmartContractManager == null)
        {
            string scGenUrl = OASISDNA?.OASIS?.Services?.SmartContractGenerator?.BaseUrl 
                ?? "http://localhost:5000";
            SmartContractManager = new SmartContractManager(scGenUrl);
        }

        // Read Rust source and create ZIP
        var rustZip = CreateRustProjectZip(rustSourcePath);
        
        var compileResult = await SmartContractManager.CompileContractAsync(
            SmartContractLanguage.Rust,
            rustZip,
            cancellationToken);

        if (compileResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, compileResult.Message);
            return result;
        }

        result.Result = compileResult.Result.CompiledBytecode;
        result.IsError = false;
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError(ref result, $"Error compiling Rust code: {ex.Message}", ex);
    }

    return result;
}

private byte[] CreateRustProjectZip(string rustSourcePath)
{
    // Create a proper Anchor project structure
    // Include Cargo.toml, Anchor.toml, lib.rs, etc.
    // Return as ZIP bytes
    throw new NotImplementedException("Implement Rust project ZIP creation");
}
```

### Approach 3: ONODE WebAPI Integration

Add SmartContractGenerator endpoints to ONODE WebAPI.

#### 3.1 Create Controller

**File:** `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/SmartContractController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SmartContractController : ControllerBase
    {
        private readonly ISmartContractManager _smartContractManager;

        public SmartContractController()
        {
            _smartContractManager = SmartContractManager ?? 
                new SmartContractManager();
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateContract(
            [FromBody] GenerateContractRequest request)
        {
            var result = await _smartContractManager.GenerateContractAsync(
                request.Language,
                request.JsonSpecification);

            if (result.IsError)
                return BadRequest(result.Message);

            return File(result.Result, "application/octet-stream", "contract.sol");
        }

        [HttpPost("compile")]
        public async Task<IActionResult> CompileContract(
            [FromForm] CompileContractRequest request)
        {
            byte[] sourceBytes;
            using (var ms = new MemoryStream())
            {
                await request.SourceFile.CopyToAsync(ms);
                sourceBytes = ms.ToArray();
            }

            var result = await _smartContractManager.CompileContractAsync(
                request.Language,
                sourceBytes);

            if (result.IsError)
                return BadRequest(result.Message);

            // Return ZIP file
            return File(CreateZip(result.Result), "application/zip", "compiled.zip");
        }

        [HttpPost("deploy")]
        public async Task<IActionResult> DeployContract(
            [FromForm] DeployContractRequest request)
        {
            byte[] compiledBytes;
            using (var ms = new MemoryStream())
            {
                await request.CompiledFile.CopyToAsync(ms);
                compiledBytes = ms.ToArray();
            }

            byte[] abiBytes = null;
            if (request.AbiFile != null)
            {
                using (var ms = new MemoryStream())
                {
                    await request.AbiFile.CopyToAsync(ms);
                    abiBytes = ms.ToArray();
                }
            }

            var result = await _smartContractManager.DeployContractAsync(
                request.Language,
                compiledBytes,
                abiBytes,
                request.WalletKeypairOrPrivateKey);

            if (result.IsError)
                return BadRequest(result.Message);

            return Ok(result.Result);
        }

        private byte[] CreateZip(CompileContractResult result)
        {
            // Create ZIP from compiled artifacts
            throw new NotImplementedException();
        }
    }

    public class GenerateContractRequest
    {
        public SmartContractLanguage Language { get; set; }
        public string JsonSpecification { get; set; }
    }

    public class CompileContractRequest
    {
        public SmartContractLanguage Language { get; set; }
        public IFormFile SourceFile { get; set; }
    }

    public class DeployContractRequest
    {
        public SmartContractLanguage Language { get; set; }
        public IFormFile CompiledFile { get; set; }
        public IFormFile AbiFile { get; set; }
        public string WalletKeypairOrPrivateKey { get; set; }
    }
}
```

#### 3.2 Register in Startup.cs

```csharp
// In ConfigureServices
services.AddSingleton<ISmartContractManager>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["OASIS:Services:SmartContractGenerator:BaseUrl"] 
        ?? "http://localhost:5000";
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return new SmartContractManager(baseUrl, httpClientFactory.CreateClient());
});
```

### Approach 4: Complete Integration with NativeCodeGenesis

Integrate compilation into the `NativeCodeGenesis` workflow.

#### 4.1 Enhanced SolanaOASIS.NativeCodeGenesis

```csharp
public async Task<OASISResult<DeployContractResult>> NativeCodeGenesisAndDeployAsync(
    ICelestialBody celestialBody,
    string outputFolder,
    string nativeSource = null,
    bool compileAndDeploy = false,
    CancellationToken cancellationToken = default)
{
    var result = new OASISResult<DeployContractResult>();

    try
    {
        // Step 1: Generate Rust code (existing NativeCodeGenesis)
        bool generated = NativeCodeGenesis(celestialBody, outputFolder, nativeSource);
        if (!generated)
        {
            OASISErrorHandling.HandleError(ref result, "Failed to generate Rust code");
            return result;
        }

        if (!compileAndDeploy)
        {
            result.Result = new DeployContractResult { IsSuccessful = true };
            result.Message = "Code generated successfully (not compiled/deployed)";
            return result;
        }

        // Step 2: Compile using SmartContractGenerator
        string rustPath = Path.Combine(outputFolder, "Solana", "lib.rs");
        var rustZip = CreateAnchorProjectFromRust(rustPath, outputFolder);
        
        var compileResult = await SmartContractManager.CompileContractAsync(
            SmartContractLanguage.Rust,
            rustZip,
            cancellationToken);

        if (compileResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, compileResult.Message);
            return result;
        }

        // Step 3: Deploy (optional)
        // Get wallet from OASIS
        var walletResult = await WalletManager.GetWalletAsync(/* avatar ID */);
        if (walletResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, walletResult.Message);
            return result;
        }

        var deployResult = await SmartContractManager.DeployContractAsync(
            SmartContractLanguage.Rust,
            compileResult.Result.CompiledBytecode,
            compileResult.Result.AbiOrSchema,
            walletResult.Result.PrivateKey,
            cancellationToken);

        result.Result = deployResult.Result;
        result.IsError = false;
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError(ref result, $"Error in NativeCodeGenesisAndDeploy: {ex.Message}", ex);
    }

    return result;
}

private byte[] CreateAnchorProjectFromRust(string rustPath, string outputFolder)
{
    // Create full Anchor project structure:
    // - Anchor.toml
    // - Cargo.toml (workspace)
    // - programs/{program_name}/Cargo.toml
    // - programs/{program_name}/src/lib.rs (the generated Rust code)
    // Return as ZIP
    throw new NotImplementedException();
}
```

## Configuration

### appsettings.json (ONODE)

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

### OASIS_DNA.json

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

## Running SmartContractGenerator

### Option 1: Standalone Service

```bash
cd SmartContractGenerator/src/SmartContractGen/ScGen.API
dotnet run
```

### Option 2: Docker

```bash
cd SmartContractGenerator
docker build -t smartcontract-generator .
docker run -p 5000:5000 smartcontract-generator
```

### Option 3: As Part of ONODE

You can embed SmartContractGenerator as a project reference in ONODE, but it's recommended to run it as a separate service for:
- **Isolation**: Compilation is resource-intensive
- **Scalability**: Can scale independently
- **Maintenance**: Easier to update without affecting ONODE

## Testing

### Test SmartContractManager

```csharp
var manager = new SmartContractManager("http://localhost:5000");

// Test generation
var jsonSpec = @"{""contract_type"": ""token"", ""name"": ""TestToken""}";
var generateResult = await manager.GenerateContractAsync(
    SmartContractLanguage.Solidity,
    jsonSpec);

// Test compilation
var compileResult = await manager.CompileContractAsync(
    SmartContractLanguage.Solidity,
    generateResult.Result);
```

## Next Steps

1. **Implement ZIP extraction/creation utilities**
2. **Add error handling and retry logic**
3. **Add caching for compiled contracts**
4. **Integrate with OASIS wallet management**
5. **Add logging and monitoring**
6. **Create unit tests**

## Notes

- SmartContractGenerator must be running before using these integrations
- For production, use HTTPS and add authentication
- Consider rate limiting for compilation endpoints
- Compilation can take 5-30 minutes for first builds (subsequent builds use cache)
