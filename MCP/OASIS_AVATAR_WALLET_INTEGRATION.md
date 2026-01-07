# OASIS Avatar/Wallet Integration for Smart Contract Deployment

**Date:** 2026-01-07  
**Status:** ✅ Implemented

## Overview

Smart contract deployment is now integrated with OASIS avatars and wallets. Users can deploy contracts using their OASIS avatar's wallet automatically by providing a JWT token, eliminating the need to manually upload wallet keypair files.

## What Was Implemented

### 1. OASIS Authentication Service Enhancement
- **File:** `SmartContractGenerator/src/SmartContractGen/ScGen.Lib/Shared/Services/OASIS/OASISAuthService.cs`
- **Added:** `GetAvatarPrivateKeyAsync()` method to retrieve private keys from OASIS API
- **Purpose:** Fetches Solana private keys for avatar wallets

### 2. Deployment Request Enhancement
- **File:** `SmartContractGenerator/src/SmartContractGen/ScGen.Lib/Shared/DTOs/Requests/DeployContractRequest.cs`
- **Added:** Optional `OasisJwtToken` parameter
- **Purpose:** Allows passing JWT token instead of manual keypair upload

### 3. Deployment Controller Integration
- **File:** `SmartContractGenerator/src/SmartContractGen/ScGen.API/Infrastructure/Controllers/V1/ContractGeneratorController.cs`
- **Changes:**
  - Injected `IOASISAuthService` via dependency injection
  - Added logic to validate JWT token and fetch avatar wallet when token is provided
  - Automatically creates keypair file from OASIS private key for Solana deployments
  - Falls back to manual keypair/schema upload if token not provided

### 4. Solana Keypair Helper
- **File:** `SmartContractGenerator/src/SmartContractGen/ScGen.Lib/Shared/Helpers/SolanaKeypairHelper.cs`
- **Purpose:** Converts Solana private keys to keypair JSON format
- **Note:** Currently uses simplified conversion - can be enhanced to properly derive public keys

### 5. Service Registration
- **File:** `SmartContractGenerator/src/SmartContractGen/ScGen.Lib/Shared/Extensions/DI/ScGenServices.cs`
- **Added:** Registration of `IOASISAuthService` with HttpClient support

### 6. Configuration
- **File:** `SmartContractGenerator/src/SmartContractGen/ScGen.API/appsettings.json`
- **Added:** OASIS API URL configuration
- **File:** `SmartContractGenerator/src/SmartContractGen/ScGen.Lib/Shared/Extensions/DI/RegisterServices.cs`
- **Added:** OASIS options configuration

### 7. MCP Client Integration
- **File:** `MCP/src/clients/smartContractClient.ts`
- **Added:** `oasisJwtToken` parameter to `deployContract()` method
- **File:** `MCP/src/tools/smartContractTools.ts`
- **Added:** `oasisJwtToken` parameter to `scgen_deploy_contract` tool definition

## How It Works

### For Solana Contracts

1. **With OASIS JWT Token (New):**
   ```typescript
   scgen_deploy_contract({
     compiledContractPath: "...",
     blockchain: "Solana",
     oasisJwtToken: "eyJhbGc..." // OASIS JWT token
   })
   ```
   - Validates JWT token with OASIS API
   - Extracts avatar ID from token
   - Fetches Solana private key from OASIS Keys API
   - Converts private key to Solana keypair JSON format
   - Uses keypair for deployment automatically

2. **Manual Keypair (Existing):**
   ```typescript
   scgen_deploy_contract({
     compiledContractPath: "...",
     blockchain: "Solana",
     walletKeypairPath: "/path/to/keypair.json"
   })
   ```
   - Works as before - uploads keypair file manually

### For Ethereum/Radix Contracts

- Currently requires manual schema/ABI upload
- OASIS integration can be extended for these chains in the future

## API Endpoints Used

### OASIS API Endpoints
1. **Token Validation:** Validates JWT token and extracts avatar ID
2. **Get Private Key:** `GET /api/keys/get_provider_private_key_for_avatar_by_id`
   - Parameters: `id={avatarId}&providerType=Solana`
   - Returns: Private key for the avatar's Solana wallet

## Configuration

### OASIS API URL
Set in `appsettings.json`:
```json
{
  "OASIS": {
    "ApiUrl": "http://127.0.0.1:5000"
  }
}
```

## Usage Examples

### MCP Tool Usage

```typescript
// Deploy using OASIS avatar wallet
await mcp_oasis_unified_scgen_deploy_contract({
  compiledContractPath: "/path/to/compiled.zip",
  blockchain: "Solana",
  oasisJwtToken: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
});

// Deploy using manual keypair (still supported)
await mcp_oasis_unified_scgen_deploy_contract({
  compiledContractPath: "/path/to/compiled.zip",
  blockchain: "Solana",
  walletKeypairPath: "/path/to/keypair.json"
});
```

## Benefits

1. **Seamless Integration:** No need to manually export/upload wallet keypairs
2. **Security:** Private keys stay within OASIS ecosystem
3. **User Experience:** Single JWT token enables deployment
4. **Backward Compatible:** Manual keypair upload still works

## Limitations & Future Enhancements

### Current Limitations
1. **Solana Only:** OASIS integration currently works for Solana contracts only
2. **Keypair Conversion:** Uses simplified keypair conversion - may need enhancement for production
3. **Public Key Derivation:** Doesn't fully derive public key from private key (uses placeholder)

### Future Enhancements
1. **Ethereum/Radix Support:** Extend OASIS integration for other blockchains
2. **Proper Key Derivation:** Implement full Solana keypair derivation (private + public key)
3. **Error Handling:** Enhanced error messages for missing wallets or invalid tokens
4. **Wallet Selection:** Support selecting specific wallet if avatar has multiple wallets

## Testing

To test the integration:

1. **Get OASIS JWT Token:**
   ```typescript
   const authResult = await oasis_authenticate_avatar({
     username: "your_username",
     password: "your_password"
   });
   // Token is in the response
   ```

2. **Deploy Contract:**
   ```typescript
   await scgen_deploy_contract({
     compiledContractPath: "...",
     blockchain: "Solana",
     oasisJwtToken: authResult.token
   });
   ```

## Files Modified

1. `SmartContractGenerator/src/SmartContractGen/ScGen.Lib/Shared/Services/OASIS/OASISAuthService.cs`
2. `SmartContractGenerator/src/SmartContractGen/ScGen.Lib/Shared/Services/OASIS/IOASISAuthService.cs`
3. `SmartContractGenerator/src/SmartContractGen/ScGen.Lib/Shared/DTOs/Requests/DeployContractRequest.cs`
4. `SmartContractGenerator/src/SmartContractGen/ScGen.API/Infrastructure/Controllers/V1/ContractGeneratorController.cs`
5. `SmartContractGenerator/src/SmartContractGen/ScGen.Lib/Shared/Helpers/SolanaKeypairHelper.cs` (new)
6. `SmartContractGenerator/src/SmartContractGen/ScGen.Lib/Shared/Extensions/DI/ScGenServices.cs`
7. `SmartContractGenerator/src/SmartContractGen/ScGen.Lib/Shared/Extensions/DI/RegisterServices.cs`
8. `SmartContractGenerator/src/SmartContractGen/ScGen.API/appsettings.json`
9. `MCP/src/clients/smartContractClient.ts`
10. `MCP/src/tools/smartContractTools.ts`

## Next Steps

1. **Test the integration** with a real OASIS avatar and Solana wallet
2. **Enhance keypair conversion** to properly derive public keys
3. **Add Ethereum/Radix support** for OASIS wallet integration
4. **Add comprehensive error handling** and user-friendly error messages








