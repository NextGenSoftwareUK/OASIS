# Wallet Creation Guide for OASIS Avatars

**Date:** 2026-01-07  
**Status:** ✅ Documented

## Overview

This guide documents the process for creating Solana wallets (and other blockchain wallets) for OASIS avatars using MCP endpoints.

## MCP Endpoints Available

### 1. `oasis_create_wallet_full` (Recommended)
**Full-featured wallet creation with all options**

**Required Parameters:**
- `avatarId` (string) - Avatar UUID
- `WalletProviderType` (string) - Blockchain type: "SolanaOASIS", "EthereumOASIS", etc.

**Optional Parameters:**
- `GenerateKeyPair` (boolean) - Generate keypair automatically (default: true)
- `IsDefaultWallet` (boolean) - Set as default wallet (default: false)
- `Name` (string) - Wallet name
- `Description` (string) - Wallet description
- `providerType` (string) - **IMPORTANT:** Storage provider type (use "LocalFileOASIS" or "MongoDBOASIS")

### 2. `oasis_create_wallet` (Basic)
**Simpler wallet creation (may have limitations)**

**Parameters:**
- `avatarId` (string) - Avatar UUID
- `walletType` (string, optional) - Wallet type

## Step-by-Step Process for Creating Solana Wallet

### Step 1: Authenticate Avatar
```typescript
await oasis_authenticate_avatar({
  username: "OASIS_ADMIN",
  password: "your_password"
});
```
**Result:** Returns JWT token and avatar information including existing wallets.

### Step 2: Create Wallet
```typescript
await oasis_create_wallet_full({
  avatarId: "f489231f-73c8-4642-9fc9-11efeb9698fa",
  WalletProviderType: "SolanaOASIS",
  GenerateKeyPair: true,
  IsDefaultWallet: true,  // Set as default if desired
  Name: "My Solana Wallet",
  Description: "Primary Solana wallet",
  providerType: "LocalFileOASIS"  // ⚠️ CRITICAL: Must specify storage provider
});
```

**Important Notes:**
- **`providerType` parameter is critical** - Must be set to a storage provider like "LocalFileOASIS" or "MongoDBOASIS"
- Without `providerType`, the API defaults to MongoDBOASIS which may cause errors
- `WalletProviderType` is automatically converted from "SolanaOASIS" to numeric enum value (3)

### Step 3: Verify Wallet
After creation, the response includes:
- `walletId` - Unique wallet identifier
- `publicKey` / `walletAddress` - Valid Solana address (base58 encoded, 32-44 chars)
- `privateKey` - Private key (keep secure!)
- `secretRecoveryPhrase` - Recovery phrase (if generated)

### Step 4: Set as Default (Optional)
```typescript
await oasis_set_default_wallet({
  avatarId: "f489231f-73c8-4642-9fc9-11efeb9698fa",
  walletId: "6b044457-0ba9-47f9-9afe-297918e9cac0",
  providerType: "SolanaOASIS"
});
```

## Common Issues & Solutions

### Issue 1: "ProviderCategory must be StorageLocal or StorageLocalAndNetwork"
**Solution:** Add `providerType: "LocalFileOASIS"` parameter

### Issue 2: "WalletProviderType could not be converted"
**Solution:** Use string values like "SolanaOASIS" - the MCP client automatically converts to numeric enum

### Issue 3: "Unauthorized" error
**Solution:** Authenticate first using `oasis_authenticate_avatar`

### Issue 4: Invalid wallet address format
**Solution:** 
- Valid Solana addresses are base58 encoded, 32-44 characters
- If you see an invalid address, delete it and create a new wallet
- New wallets created with `GenerateKeyPair: true` will have valid addresses

## Valid Solana Address Format

- **Length:** 32-44 characters
- **Encoding:** Base58 (uses: 1-9, A-H, J-N, P-Z, a-k, m-z)
- **Excludes:** 0, O, I, l (to avoid confusion)
- **Example Valid:** `Cwy7Xxg4HbwNrhPJoTwwyY9S52BkDev49XGPfoz4SD6h`
- **Example Invalid:** `25KwaX3H7UZjpmBWj8qt7VfGvL1QxHSthmC5Y39WcE9HjNbJgPm` (contains invalid characters)

## Provider Type Enum Values

The API uses numeric enum values internally:
- `SolanaOASIS` = 3
- `EthereumOASIS` = 12
- `ArbitrumOASIS` = 9
- `PolygonOASIS` = 14

**Note:** MCP client automatically converts string names to numeric values.

## Storage Provider Types

For the `providerType` parameter (where wallet data is stored):
- `LocalFileOASIS` - Local file system (ProviderCategory.StorageLocal)
- `MongoDBOASIS` - MongoDB database (ProviderCategory.StorageLocalAndNetwork)
- `SQLLiteDBOASIS` - SQLite database (ProviderCategory.StorageLocal)

## Complete Example

```typescript
// 1. Authenticate
const auth = await oasis_authenticate_avatar({
  username: "OASIS_ADMIN",
  password: "Uppermall1!"
});

// 2. Create Solana wallet
const wallet = await oasis_create_wallet_full({
  avatarId: auth.result.avatarId,
  WalletProviderType: "SolanaOASIS",
  GenerateKeyPair: true,
  IsDefaultWallet: true,
  Name: "Primary Solana Wallet",
  Description: "Main wallet for smart contract deployment",
  providerType: "LocalFileOASIS"  // ⚠️ Required!
});

// 3. Verify wallet address is valid
console.log("Wallet Address:", wallet.result.walletAddress);
console.log("Wallet ID:", wallet.result.walletId);

// 4. Use wallet for smart contract deployment
await scgen_deploy_contract({
  compiledContractPath: "...",
  blockchain: "Solana",
  oasisJwtToken: auth.result.jwtToken  // Uses wallet automatically!
});
```

## Integration with Smart Contract Deployment

Once you have a valid Solana wallet, you can use it for smart contract deployment:

```typescript
// Deploy using OASIS avatar wallet (automatic)
await scgen_deploy_contract({
  compiledContractPath: "/path/to/compiled.zip",
  blockchain: "Solana",
  oasisJwtToken: "your_jwt_token"  // Automatically uses avatar's Solana wallet
});
```

The OASIS integration will:
1. Validate JWT token
2. Extract avatar ID
3. Fetch Solana private key from OASIS
4. Create keypair file automatically
5. Deploy contract using the wallet

## Troubleshooting

### Wallet not appearing in authentication response
- Wallets may be stored in different storage providers
- Try querying with specific provider: `oasis_get_provider_wallets(avatarId, "SolanaOASIS")`

### Cannot delete invalid wallet
- Wallets are stored as holons
- Try: `oasis_delete_holon(holonId)` 
- If that doesn't work, may require direct database cleanup

### Multiple wallets created
- Each call to `oasis_create_wallet_full` creates a new wallet
- Use `IsDefaultWallet: true` to set one as default
- Or use `oasis_set_default_wallet` after creation

## Best Practices

1. **Always authenticate first** - Required for wallet operations
2. **Specify storage provider** - Use `providerType: "LocalFileOASIS"` to avoid errors
3. **Verify address format** - Check that generated addresses are valid base58
4. **Set default wallet** - Mark primary wallet as default for easier access
5. **Keep private keys secure** - Never expose private keys or recovery phrases
6. **Use OASIS integration** - For smart contract deployment, use JWT token instead of manual keypair upload

## Related Documentation

- [OASIS Avatar/Wallet Integration](./OASIS_AVATAR_WALLET_INTEGRATION.md) - Smart contract deployment integration
- [ENDPOINT_INVENTORY.md](./ENDPOINT_INVENTORY.md) - Complete list of available endpoints
- [README.md](./README.md) - General MCP server documentation







