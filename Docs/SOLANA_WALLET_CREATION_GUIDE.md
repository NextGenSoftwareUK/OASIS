# Wallet Creation Guide for EVM and Solana Chains

**Last Updated:** 2026-01-12  
**Status:** ✅ **WORKING SOLUTION**

---

## Supported Chains

This guide covers wallet creation for the following blockchain providers:

### EVM-Compatible Chains (Ethereum-style addresses: `0x...`)
- **BaseOASIS** - Base blockchain (Coinbase L2)
- **PolygonOASIS** - Polygon network
- **RootstockOASIS** - Rootstock (Bitcoin sidechain)
- **MonadOASIS** - Monad blockchain
- **ArbitrumOASIS** - Arbitrum L2 (requires separate activation logic)

### Non-EVM Chains
- **SolanaOASIS** - Solana blockchain (base58 addresses)

---

## ⚠️ CRITICAL: Correct Order for Wallet Creation

For **all non-Bitcoin providers** (Solana, Base, Polygon, Rootstock, Monad, Arbitrum), you **MUST** follow this order:

1. **Register Provider** (instantiates provider from DNA config)
2. **Activate Provider** (activates the registered provider)
3. **Generate Key Pair** (creates chain-specific format keypair)
4. **Link Public Key FIRST** (creates wallet with correct address)
5. **Link Private Key SECOND** (completes wallet using wallet ID from step 4)

### Why This Order?

The `LinkProviderPrivateKeyToAvatar` method calls `WalletAddressHelper.PrivateKeyToAddress()` which:
- Only works for Bitcoin format (base58 WIF)
- Fails with "Invalid base58 data" for Solana/EVM chains if private keys are not properly formatted
- Cannot derive Solana or Ethereum addresses from private keys directly

By linking the public key first:
- The wallet is created with the correct address from keypair generation
- The private key is then linked to that existing wallet
- This avoids the address derivation issue entirely

---

## Provider Type Reference

### Numeric Values (for API route parameters)

| Provider | Numeric Value | String Value |
|----------|---------------|--------------|
| SolanaOASIS | 3 | `"SolanaOASIS"` |
| ArbitrumOASIS | 4 | `"ArbitrumOASIS"` |
| BaseOASIS | 6 | `"BaseOASIS"` |
| PolygonOASIS | 8 | `"PolygonOASIS"` |
| RootstockOASIS | 25 | `"RootstockOASIS"` |
| MonadOASIS | 42 | `"MonadOASIS"` |

### Provider Architecture

**EVM-Compatible Providers** (Base, Polygon, Rootstock, Monad):
- Extend `Web3CoreOASISBaseProvider`
- Generate Ethereum-style addresses (`0x...` format)
- Can activate with just `RpcEndpoint` (private key optional for key generation)
- Use Nethereum library for blockchain interactions

**Solana Provider**:
- Custom implementation
- Generates base58 addresses (32-44 characters)
- Uses Solana.NET library

**Arbitrum Provider**:
- Custom implementation (extends `OASISStorageProviderBase` directly)
- May require `ChainPrivateKey` for activation (check configuration)

---

## Complete Workflow

### Prerequisites

1. **Register Provider:**
   ```bash
   # For Solana
   POST /api/provider/register-provider-type/3
   # or
   POST /api/provider/register-provider-type/SolanaOASIS
   
   # For Base
   POST /api/provider/register-provider-type/6
   # or
   POST /api/provider/register-provider-type/BaseOASIS
   
   # For Polygon
   POST /api/provider/register-provider-type/8
   # or
   POST /api/provider/register-provider-type/PolygonOASIS
   
   # For Rootstock
   POST /api/provider/register-provider-type/22
   # or
   POST /api/provider/register-provider-type/RootstockOASIS
   
   # For Monad
   POST /api/provider/register-provider-type/42
   # or
   POST /api/provider/register-provider-type/MonadOASIS
   
   # For Arbitrum
   POST /api/provider/register-provider-type/4
   # or
   POST /api/provider/register-provider-type/ArbitrumOASIS
   ```

2. **Activate Provider:**
   ```bash
   # Use the same numeric value or string name as registration
   POST /api/provider/activate-provider/{providerType}
   ```

---

## Solana Wallet Creation

### Step 1: Generate Solana Keypair

**Endpoint:** `POST /api/keys/generate_keypair_with_wallet_address_for_provider/SolanaOASIS`

**Response:**
```json
{
  "isError": false,
  "result": {
    "privateKey": "wq+ufyyXgTXZxc6f7MONxQgrpJ10AawLEO6IHwmoZtlhqObAQOhnBCdkc5saz2oMQ6EyIQW2C/pPCMdxdXtE4w==",
    "publicKey": "7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q",
    "walletAddressLegacy": "7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q"
  }
}
```

**Note:** 
- `privateKey` is base64 encoded (Solana format)
- `publicKey` and `walletAddressLegacy` are base58 (valid Solana addresses, 32-44 chars)

### Step 2: Link Public Key FIRST (Creates Wallet)

**Endpoint:** `POST /api/keys/link_provider_public_key_to_avatar_by_id`

**Request:**
```json
{
  "AvatarID": "d42b8448-52a9-4579-a6b1-b7c624616459",
  "ProviderType": "SolanaOASIS",
  "ProviderKey": "7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q",
  "WalletAddress": "7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q"
}
```

**Note:** Omit `WalletId` to create a new wallet.

**Response:**
```json
{
  "isError": false,
  "result": {
    "id": "e743527b-4011-406c-8d7d-af38fd1fcad8",
    "walletId": "e743527b-4011-406c-8d7d-af38fd1fcad8",
    "walletAddress": "7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q",
    "providerType": 3
  }
}
```

**Extract:** `walletId` or `id` for next step.

### Step 3: Link Private Key SECOND (Completes Wallet)

**Endpoint:** `POST /api/keys/link_provider_private_key_to_avatar_by_id`

**Request:**
```json
{
  "WalletId": "e743527b-4011-406c-8d7d-af38fd1fcad8",
  "AvatarID": "d42b8448-52a9-4579-a6b1-b7c624616459",
  "ProviderType": "SolanaOASIS",
  "ProviderKey": "wq+ufyyXgTXZxc6f7MONxQgrpJ10AawLEO6IHwmoZtlhqObAQOhnBCdkc5saz2oMQ6EyIQW2C/pPCMdxdXtE4w=="
}
```

**Note:** `WalletId` is **REQUIRED** - use the wallet ID from step 2.

**Response:**
```json
{
  "isError": false,
  "result": {
    "id": "e743527b-4011-406c-8d7d-af38fd1fcad8",
    "walletId": "e743527b-4011-406c-8d7d-af38fd1fcad8",
    "providerType": 3
  }
}
```

---

## EVM-Compatible Chain Wallet Creation (Base, Polygon, Rootstock, Monad)

### Step 1: Generate EVM Keypair

**Endpoint:** `POST /api/keys/generate_keypair_with_wallet_address_for_provider/{ProviderName}`

**Examples:**
- `POST /api/keys/generate_keypair_with_wallet_address_for_provider/BaseOASIS`
- `POST /api/keys/generate_keypair_with_wallet_address_for_provider/PolygonOASIS`
- `POST /api/keys/generate_keypair_with_wallet_address_for_provider/RootstockOASIS`
- `POST /api/keys/generate_keypair_with_wallet_address_for_provider/MonadOASIS`

**Response:**
```json
{
  "isError": false,
  "result": {
    "privateKey": "0x1e760ddb4655f02a641788eb3e2d7b30032b926830554bc9ed0d7fa77ccc2c27",
    "publicKey": "0x2E8c1F06BE56309E9C24aad461813bcb26922651",
    "walletAddress": "0x2E8c1F06BE56309E9C24aad461813bcb26922651",
    "walletAddressLegacy": "0x2E8c1F06BE56309E9C24aad461813bcb26922651"
  }
}
```

**Note:** 
- `privateKey` is hex-encoded (0x prefix)
- `publicKey`, `walletAddress`, and `walletAddressLegacy` are Ethereum-style addresses (0x prefix, 42 characters)
- All three address fields contain the same value for EVM chains

### Step 2: Link Public Key FIRST (Creates Wallet)

**Endpoint:** `POST /api/keys/link_provider_public_key_to_avatar_by_id`

**Request:**
```json
{
  "AvatarID": "d42b8448-52a9-4579-a6b1-b7c624616459",
  "ProviderType": "BaseOASIS",
  "ProviderKey": "0x2E8c1F06BE56309E9C24aad461813bcb26922651",
  "WalletAddress": "0x2E8c1F06BE56309E9C24aad461813bcb26922651"
}
```

**Note:** 
- Replace `"BaseOASIS"` with `"PolygonOASIS"`, `"RootstockOASIS"`, or `"MonadOASIS"` as needed
- Omit `WalletId` to create a new wallet

**Response:**
```json
{
  "isError": false,
  "result": {
    "id": "e743527b-4011-406c-8d7d-af38fd1fcad8",
    "walletId": "e743527b-4011-406c-8d7d-af38fd1fcad8",
    "walletAddress": "0x2E8c1F06BE56309E9C24aad461813bcb26922651",
    "providerType": 6
  }
}
```

**Extract:** `walletId` or `id` for next step.

### Step 3: Link Private Key SECOND (Completes Wallet)

**Endpoint:** `POST /api/keys/link_provider_private_key_to_avatar_by_id`

**Request:**
```json
{
  "WalletId": "e743527b-4011-406c-8d7d-af38fd1fcad8",
  "AvatarID": "d42b8448-52a9-4579-a6b1-b7c624616459",
  "ProviderType": "BaseOASIS",
  "ProviderKey": "0x1e760ddb4655f02a641788eb3e2d7b30032b926830554bc9ed0d7fa77ccc2c27"
}
```

**Note:** 
- `WalletId` is **REQUIRED** - use the wallet ID from step 2
- Replace `"BaseOASIS"` with the appropriate provider name

**Response:**
```json
{
  "isError": false,
  "result": {
    "id": "e743527b-4011-406c-8d7d-af38fd1fcad8",
    "walletId": "e743527b-4011-406c-8d7d-af38fd1fcad8",
    "providerType": 6
  }
}
```

---

## Using the Helper Method

The `linkKeys()` method in `keysApi.ts` now automatically handles the correct order:

```typescript
import { keysAPI } from './lib/keysApi';

// For Solana
const solanaResult = await keysAPI.linkKeys({
  avatarId: "d42b8448-52a9-4579-a6b1-b7c624616459",
  providerType: "SolanaOASIS",
  privateKey: "...",
  publicKey: "...",
  walletAddress: "..."
});

// For Base (or other EVM chains)
const baseResult = await keysAPI.linkKeys({
  avatarId: "d42b8448-52a9-4579-a6b1-b7c624616459",
  providerType: "BaseOASIS",
  privateKey: "0x...",
  publicKey: "0x...",
  walletAddress: "0x..."
});

if (result.isError) {
  console.error("Failed:", result.message);
} else {
  console.log("Wallet created:", result.result.walletId);
}
```

The helper method:
- Detects provider type automatically
- Links public key first
- Links private key second
- Returns the wallet ID

---

## Provider Registration & Activation

### Registration

**Endpoint:** `POST /api/provider/register-provider-type/{providerType}`

- Route parameter: `3` or `SolanaOASIS`
- This instantiates the provider from DNA config and registers it

### Activation

**Endpoint:** `POST /api/provider/activate-provider/{providerType}`

- Route parameter: `3` or `SolanaOASIS`
- This activates the registered provider

**Note:** Both endpoints use route parameters, NOT JSON body.

---

## Configuration Requirements

### EVM-Compatible Providers (Base, Polygon, Rootstock, Monad)

**Minimum Configuration** (for key generation only):
```json
{
  "OASIS": {
    "StorageProviders": {
      "BaseOASIS": {
        "RpcEndpoint": "https://mainnet.base.org"
      },
      "PolygonOASIS": {
        "ConnectionString": "https://polygon-rpc.com"
      },
      "RootstockOASIS": {
        "ConnectionString": "https://public-node.rsk.co"
      },
      "MonadOASIS": {
        "ConnectionString": "https://rpc.monad.xyz"
      }
    }
  }
}
```

**Full Configuration** (for transactions):
```json
{
  "OASIS": {
    "StorageProviders": {
      "BaseOASIS": {
        "RpcEndpoint": "https://mainnet.base.org",
        "ChainPrivateKey": "your-private-key-here",
        "ContractAddress": "your-contract-address-here"
      }
    }
  }
}
```

### Solana Provider

```json
{
  "OASIS": {
    "StorageProviders": {
      "SolanaOASIS": {
        "ConnectionString": "https://api.mainnet-beta.solana.com"
      }
    }
  }
}
```

### Arbitrum Provider

**Note:** Arbitrum uses a different activation mechanism and may require `ChainPrivateKey`:
```json
{
  "OASIS": {
    "StorageProviders": {
      "ArbitrumOASIS": {
        "ConnectionString": "https://arb1.arbitrum.io/rpc",
        "ChainPrivateKey": "your-private-key-here",
        "ChainId": "42161"
      }
    }
  }
}
```

---

## Common Errors & Solutions

### Error: "{Provider} provider is not activated"
**Solution:** Register and activate the provider first (see Prerequisites above)

### Error: "Invalid base58 data" when linking private key
**Solution:** You're linking private key first. Use the correct order: public key first, then private key.

### Error: "providerType Default"
**Solution:** Make sure you're passing the provider name as a string (e.g., `"BaseOASIS"`), not a number, in the JSON body.

### Error: "WalletId is required"
**Solution:** When linking private key, you must provide the `WalletId` from the public key linking step.

### Error: "Web3Core provider is not activated" (EVM chains)
**Solution:** 
- Ensure `RpcEndpoint` or `ConnectionString` is configured in `OASIS_DNA.json`
- For EVM providers (Base, Polygon, Rootstock, Monad), activation only requires `RpcEndpoint` - `ChainPrivateKey` is optional for key generation

### Error: "NullReferenceException" during provider registration
**Solution:** Ensure the provider configuration section exists in `OASIS_DNA.json`, even if only `RpcEndpoint` is set

---

## Testing Examples

### Test Solana Wallet Creation

```bash
# 1. Authenticate
TOKEN=$(curl -s -k -X POST "https://localhost:5004/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username":"OASIS_ADMIN","password":"Uppermall1!"}' \
  | grep -o '"jwtToken":"[^"]*"' | cut -d'"' -f4)

# 2. Register provider
curl -k -X POST "https://localhost:5004/api/provider/register-provider-type/3" \
  -H "Authorization: Bearer $TOKEN"

# 3. Activate provider
curl -k -X POST "https://localhost:5004/api/provider/activate-provider/3" \
  -H "Authorization: Bearer $TOKEN"

# 4. Generate keypair
curl -k -X POST "https://localhost:5004/api/keys/generate_keypair_with_wallet_address_for_provider/SolanaOASIS" \
  -H "Authorization: Bearer $TOKEN"

# 5. Link public key FIRST
curl -k -X POST "https://localhost:5004/api/keys/link_provider_public_key_to_avatar_by_id" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"AvatarID": "...", "ProviderType": "SolanaOASIS", "ProviderKey": "...", "WalletAddress": "..."}'

# 6. Link private key SECOND
curl -k -X POST "https://localhost:5004/api/keys/link_provider_private_key_to_avatar_by_id" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"WalletId": "...", "AvatarID": "...", "ProviderType": "SolanaOASIS", "ProviderKey": "..."}'
```

### Test Base Wallet Creation

```bash
# 1. Register Base provider
curl -k -X POST "https://localhost:5004/api/provider/register-provider-type/6" \
  -H "Authorization: Bearer $TOKEN"

# 2. Activate Base provider
curl -k -X POST "https://localhost:5004/api/provider/activate-provider/6" \
  -H "Authorization: Bearer $TOKEN"

# 3. Generate keypair
curl -k -X POST "https://localhost:5004/api/keys/generate_keypair_with_wallet_address_for_provider/BaseOASIS" \
  -H "Authorization: Bearer $TOKEN"

# 4. Link public key FIRST
curl -k -X POST "https://localhost:5004/api/keys/link_provider_public_key_to_avatar_by_id" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"AvatarID": "...", "ProviderType": "BaseOASIS", "ProviderKey": "0x...", "WalletAddress": "0x..."}'

# 5. Link private key SECOND
curl -k -X POST "https://localhost:5004/api/keys/link_provider_private_key_to_avatar_by_id" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"WalletId": "...", "AvatarID": "...", "ProviderType": "BaseOASIS", "ProviderKey": "0x..."}'
```

**Note:** Replace `BaseOASIS` with `PolygonOASIS`, `RootstockOASIS`, or `MonadOASIS` and use the corresponding numeric value (8, 25, or 42) for registration/activation.

---

## Summary

✅ **Always use this order for ALL non-Bitcoin providers:**
1. Register provider
2. Activate provider  
3. Generate keypair
4. Link **public key FIRST** (creates wallet)
5. Link **private key SECOND** (completes wallet)

✅ **Use the `linkKeys()` helper method** - it handles the correct order automatically

✅ **Provider activation uses route parameters**, not JSON body

✅ **EVM providers (Base, Polygon, Rootstock, Monad)** can activate with just `RpcEndpoint` - no private key required for key generation

✅ **Address formats:**
- **EVM chains**: `0x` prefix, 42 characters (e.g., `0x2E8c1F06BE56309E9C24aad461813bcb26922651`)
- **Solana**: Base58, 32-44 characters (e.g., `7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q`)

---

## Related Documentation

- `BASE_WALLET_CREATION_GUIDE.md` - Detailed Base-specific guide
- `BASEOASIS_ACTIVATION_FIX.md` - Technical details on activation fixes
- `SERV_TOKEN_INTEGRATION_BRIEF.md` - $SERV token integration on Base
- `WALLET_CREATION_VIA_KEYS_API_SOLUTION.md` - General Keys API guide
