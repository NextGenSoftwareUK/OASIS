# Base Wallet Creation Guide

**Last Updated:** 2026-01-12  
**Status:** ✅ **WORKING SOLUTION**

---

## ⚠️ CRITICAL: Correct Order for Base Wallet Creation

For **BaseOASIS** and other EVM-compatible providers, you **MUST** follow this order:

1. **Register Base Provider** (instantiates provider from DNA config)
2. **Activate Base Provider** (activates the registered provider)
3. **Generate Key Pair** (creates Ethereum/Base format keypair)
4. **Link Public Key FIRST** (creates wallet with correct address)
5. **Link Private Key SECOND** (completes wallet using wallet ID from step 4)

### Why This Order?

The `LinkProviderPrivateKeyToAvatar` method calls `WalletAddressHelper.PrivateKeyToAddress()` which:
- Only works for Bitcoin format (base58 WIF)
- Fails with "Invalid base58 data" for EVM chains if private keys are not properly formatted
- Cannot derive Ethereum/Base addresses from private keys directly

By linking the public key first:
- The wallet is created with the correct address from keypair generation
- The private key is then linked to that existing wallet
- This avoids the address derivation issue entirely

---

## Base Configuration Details

### From OASIS_DNA.json

```json
{
  "BaseOASIS": {
    "RpcEndpoint": "https://mainnet.base.org",
    "NetworkId": "mainnet",
    "ChainId": "0x2105"
  }
}
```

### Network Details

**Mainnet:**
- **RPC URL**: `https://mainnet.base.org`
- **Chain ID**: `8453` (decimal) or `0x2105` (hex)
- **Network ID**: `8453`
- **Explorer**: https://basescan.org
- **Currency**: ETH
- **Bridge**: https://bridge.base.org

**Sepolia Testnet:**
- **RPC URL**: `https://sepolia.base.org`
- **Chain ID**: `84532` (decimal) or `0x14a34` (hex)
- **Network ID**: `84532`
- **Explorer**: https://sepolia.basescan.org
- **Faucet**: https://www.coinbase.com/faucets/base-ethereum-goerli-faucet

### Provider Type Enum Value

- **Numeric**: `7`
- **String**: `"BaseOASIS"`

---

## Complete Workflow

### Prerequisites

1. **Register Base Provider:**
   ```bash
   POST /api/provider/register-provider-type/7
   # or
   POST /api/provider/register-provider-type/BaseOASIS
   ```

2. **Activate Base Provider:**
   ```bash
   POST /api/provider/activate-provider/7
   # or
   POST /api/provider/activate-provider/BaseOASIS
   ```

### Step 1: Generate Base Keypair

**Endpoint:** `POST /api/keys/generate_keypair_with_wallet_address_for_provider/BaseOASIS`

**Response:**
```json
{
  "isError": false,
  "result": {
    "privateKey": "0x1e760ddb4655f02a641788eb3e2d7b30032b926830554bc9ed0d7fa77ccc2c27",
    "publicKey": "0x0252bac628463b5279a0ba0e883e44260c31d9c3ec532af952690921d5ab61b9a6",
    "walletAddress": "0x6861bdf70f829096033f6b296df39d0e319de599"
  }
}
```

**Note:** 
- `privateKey` is hex format (0x + 64 hex chars)
- `publicKey` is hex format (0x + 66 hex chars, compressed)
- `walletAddress` is Ethereum/Base format (0x + 40 hex chars)

**⚠️ Important:** If BaseOASIS provider is not registered/activated, the key generation may fall back to Bitcoin-style addresses. Always register and activate the provider first!

### Step 2: Link Public Key FIRST (Creates Wallet)

**Endpoint:** `POST /api/keys/link_provider_public_key_to_avatar_by_id`

**Request:**
```json
{
  "AvatarID": "0df19747-fa32-4c2f-a6b8-b55ed76d04af",
  "ProviderType": "BaseOASIS",
  "ProviderKey": "0x0252bac628463b5279a0ba0e883e44260c31d9c3ec532af952690921d5ab61b9a6",
  "WalletAddress": "0x6861bdf70f829096033f6b296df39d0e319de599"
}
```

**Note:** Omit `WalletId` to create a new wallet.

**Response:**
```json
{
  "isError": false,
  "result": {
    "id": "17f56cd5-5474-4aa0-aba3-271642d5b496",
    "walletId": "17f56cd5-5474-4aa0-aba3-271642d5b496",
    "walletAddress": "0x6861bdf70f829096033f6b296df39d0e319de599",
    "providerType": 7
  }
}
```

**Extract:** `walletId` or `id` for next step.

### Step 3: Link Private Key SECOND (Completes Wallet)

**Endpoint:** `POST /api/keys/link_provider_private_key_to_avatar_by_id`

**Request:**
```json
{
  "WalletId": "17f56cd5-5474-4aa0-aba3-271642d5b496",
  "AvatarID": "0df19747-fa32-4c2f-a6b8-b55ed76d04af",
  "ProviderType": "BaseOASIS",
  "ProviderKey": "0x1e760ddb4655f02a641788eb3e2d7b30032b926830554bc9ed0d7fa77ccc2c27"
}
```

**Note:** `WalletId` is **REQUIRED** - use the wallet ID from step 2.

**Response:**
```json
{
  "isError": false,
  "result": {
    "id": "17f56cd5-5474-4aa0-aba3-271642d5b496",
    "walletId": "17f56cd5-5474-4aa0-aba3-271642d5b496",
    "providerType": 7
  }
}
```

---

## Alternative: Using Create Wallet Endpoint

You can also use the wallet creation endpoint directly:

**Endpoint:** `POST /api/wallet/avatar/{avatarId}/create-wallet`

**Request:**
```json
{
  "name": "Base Wallet",
  "description": "Base wallet for SERV tokens",
  "walletProviderType": 7,
  "generateKeyPair": true,
  "isDefaultWallet": true
}
```

**Note:** Must use numeric value `7` for `walletProviderType` (not string "BaseOASIS").

**Response:**
```json
{
  "message": "Wallet Created Successfully",
  "result": {
    "walletId": "17f56cd5-5474-4aa0-aba3-271642d5b496",
    "providerType": 7,
    "isDefaultWallet": true,
    "name": "Base Wallet",
    "description": "Base wallet for SERV tokens"
  },
  "isError": false
}
```

**⚠️ Note:** This method may generate Bitcoin-style addresses if BaseOASIS provider is not registered/activated. For proper Ethereum/Base format addresses, use the key linking method above.

---

## Testing

### Manual Testing with curl

```bash
# Get authentication token
TOKEN=$(curl -s -k -X POST "https://localhost:5004/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username":"OASIS_ADMIN","password":"Uppermall1!"}' \
  | grep -o '"jwtToken":"[^"]*"' | cut -d'"' -f4)

# 1. Register provider
curl -k -X POST "https://localhost:5004/api/provider/register-provider-type/7" \
  -H "Authorization: Bearer $TOKEN"

# 2. Activate provider
curl -k -X POST "https://localhost:5004/api/provider/activate-provider/7" \
  -H "Authorization: Bearer $TOKEN"

# 3. Generate keypair
curl -k -X POST "https://localhost:5004/api/keys/generate_keypair_with_wallet_address_for_provider/BaseOASIS" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN"

# 4. Link public key FIRST
curl -k -X POST "https://localhost:5004/api/keys/link_provider_public_key_to_avatar_by_id" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "AvatarID": "0df19747-fa32-4c2f-a6b8-b55ed76d04af",
    "ProviderType": "BaseOASIS",
    "ProviderKey": "0x0252bac628463b5279a0ba0e883e44260c31d9c3ec532af952690921d5ab61b9a6",
    "WalletAddress": "0x6861bdf70f829096033f6b296df39d0e319de599"
  }'

# 5. Link private key SECOND (with WalletId from step 4)
curl -k -X POST "https://localhost:5004/api/keys/link_provider_private_key_to_avatar_by_id" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "WalletId": "17f56cd5-5474-4aa0-aba3-271642d5b496",
    "AvatarID": "0df19747-fa32-4c2f-a6b8-b55ed76d04af",
    "ProviderType": "BaseOASIS",
    "ProviderKey": "0x1e760ddb4655f02a641788eb3e2d7b30032b926830554bc9ed0d7fa77ccc2c27"
  }'
```

---

## Common Errors & Solutions

### Error: "Base provider is not activated"
**Solution:** Register and activate the provider first (see Prerequisites above)

### Error: "Invalid base58 data" when linking private key
**Solution:** You're linking private key first. Use the correct order: public key first, then private key.

### Error: "providerType Default"
**Solution:** Make sure you're passing `"BaseOASIS"` as a string, not a number, in the JSON body.

### Error: "WalletId is required"
**Solution:** When linking private key, you must provide the `WalletId` from the public key linking step.

### Error: Wallet address format is Bitcoin-style (not 0x format)
**Solution:** BaseOASIS provider is not registered/activated. Register and activate the provider before generating keypairs.

### Error: "The JSON value could not be converted to NextGenSoftware.OASIS.API.Core.Enums.ProviderType"
**Solution:** When using the create-wallet endpoint, use numeric value `7` for `walletProviderType`, not string "BaseOASIS".

---

## Summary

✅ **Always use this order for Base:**
1. Register provider (`/api/provider/register-provider-type/7`)
2. Activate provider (`/api/provider/activate-provider/7`)
3. Generate keypair (`/api/keys/generate_keypair_with_wallet_address_for_provider/BaseOASIS`)
4. Link **public key FIRST** (creates wallet)
5. Link **private key SECOND** (completes wallet)

✅ **Provider activation uses route parameters**, not JSON body

✅ **Use numeric value `7`** for BaseOASIS in create-wallet endpoint

✅ **Use string `"BaseOASIS"`** in key linking endpoints

---

## Configuration Reference

### OASIS_DNA.json Configuration

```json
{
  "StorageProviders": {
    "BaseOASIS": {
      "RpcEndpoint": "https://mainnet.base.org",
      "NetworkId": "mainnet",
      "ChainId": "0x2105"
    }
  }
}
```

### BaseOASISProviderSettings (C#)

```csharp
public class BaseOASISProviderSettings : ProviderSettingsBase
{
    public string RpcEndpoint { get; set; } = "https://mainnet.base.org";
    public string NetworkId { get; set; } = "8453";
    public string ChainId { get; set; } = "0x2105";
    public string ChainPrivateKey { get; set; } = "";
    public string ContractAddress { get; set; } = "";
}
```

---

**Related Documentation:**
- `SOLANA_WALLET_CREATION_GUIDE.md` - Similar guide for Solana (same pattern)
- `SERV_TOKEN_INTEGRATION_BRIEF.md` - SERV token integration details
- `BASE_WALLET_API_TEST_SUMMARY.md` - API test results
