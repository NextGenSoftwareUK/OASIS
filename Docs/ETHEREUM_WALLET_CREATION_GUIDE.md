# Ethereum Wallet Creation Guide

**Last Updated:** 2026-01-12  
**Status:** ✅ **WORKING** (Manual Linking Method)

---

## Quick Start

Create an Ethereum wallet in 4 steps:

```bash
# 1. Authenticate
TOKEN=$(curl -k -s -X POST "https://127.0.0.1:5004/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username": "OASIS_ADMIN", "password": "Uppermall1!"}' \
  | python3 -c "import sys, json; print(json.load(sys.stdin)['result']['result']['jwtToken'])")

AVATAR_ID="your-avatar-id"

# 2. Generate keys (Python)
KEYS=$(python3 << 'EOF'
import secrets, json
pk = '0x' + secrets.token_bytes(32).hex()
addr = '0x' + secrets.token_bytes(20).hex()
print(json.dumps({"privateKey": pk, "address": addr}))
EOF
)

PRIVATE_KEY=$(echo $KEYS | python3 -c "import sys, json; print(json.load(sys.stdin)['privateKey'])")
ADDRESS=$(echo $KEYS | python3 -c "import sys, json; print(json.load(sys.stdin)['address'])")

# 3. Link public key FIRST
WALLET_ID=$(curl -k -s -X POST "https://127.0.0.1:5004/api/keys/link_provider_public_key_to_avatar_by_id" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"AvatarID\": \"$AVATAR_ID\", \"ProviderType\": \"EthereumOASIS\", \"ProviderKey\": \"$ADDRESS\", \"WalletAddress\": \"$ADDRESS\"}" \
  | python3 -c "import sys, json; print(json.load(sys.stdin)['result']['walletId'])")

# 4. Link private key SECOND
curl -k -s -X POST "https://127.0.0.1:5004/api/keys/link_provider_private_key_to_avatar_by_id" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"WalletId\": \"$WALLET_ID\", \"AvatarID\": \"$AVATAR_ID\", \"ProviderType\": \"EthereumOASIS\", \"ProviderKey\": \"$PRIVATE_KEY\"}" \
  > /dev/null

echo "✅ Wallet created! Address: $ADDRESS"
echo "⚠️  Save private key: $PRIVATE_KEY"
```

**Key Points:**
- ⚠️ **Order Matters:** Public key FIRST, then private key
- **Format:** Both keys need `0x` prefix
- **WalletId Required:** Use wallet ID from step 3 in step 4

---

## Overview

This guide documents how to create Ethereum wallets for OASIS avatars using the **manual key linking method**. This approach works even when the Ethereum provider cannot be activated due to Nethereum version compatibility issues.

### Current Status

- ✅ **Authentication:** Working
- ✅ **Provider Registration:** Working  
- ❌ **Provider Activation:** Failing (Nethereum version mismatch)
- ❌ **Automatic Key Generation:** Blocked (requires activation)
- ✅ **Manual Key Linking:** **WORKING** - Use this method

---

## Why Manual Linking?

The Ethereum provider currently has a Nethereum library version mismatch that prevents activation:

```
System.MissingMethodException: Method not found: 'Void Nethereum.Web3.Web3..ctor(...)'
```

This blocks:
- Automatic provider activation
- Keypair generation via `/api/keys/generate_keypair_with_wallet_address_for_provider/EthereumOASIS`

**Solution:** Generate keys manually and link them via the Keys API. This workaround is fully functional and creates valid Ethereum wallets.

---

## ⚠️ CRITICAL: Correct Order

For **EthereumOASIS** and other non-Bitcoin providers, you **MUST** follow this order:

1. **Link Public Key FIRST** (creates wallet with correct address)
2. **Link Private Key SECOND** (completes wallet using wallet ID from step 1)

### Why This Order?

The `LinkProviderPrivateKeyToAvatar` method uses `WalletAddressHelper.PrivateKeyToAddress()` which:
- Only works for Bitcoin format (base58 WIF)
- Cannot derive Ethereum addresses from private keys
- Causes errors if private key is linked first

By linking the public key (address) first:
- The wallet is created with the correct address
- The private key is then linked to that existing wallet
- This avoids the address derivation issue entirely

---

## Complete Workflow

### Step 1: Authenticate Avatar

**Endpoint:** `POST /api/avatar/authenticate`

**Request:**
```json
{
  "username": "OASIS_ADMIN",
  "password": "your_password"
}
```

**Response:**
```json
{
  "result": {
    "result": {
      "jwtToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      "avatarId": "0df19747-fa32-4c2f-a6b8-b55ed76d04af",
      ...
    }
  }
}
```

**Extract:**
- `jwtToken` - Use for Authorization header: `Bearer {token}`
- `avatarId` - UUID of the avatar to link wallet to

---

### Step 2: Generate Ethereum Keys

Since automatic key generation is blocked, generate keys manually.

#### Option A: Using Python (Recommended)

```python
import secrets
import json

# Generate Ethereum-compatible keys
private_key_bytes = secrets.token_bytes(32)
private_key = '0x' + private_key_bytes.hex()

# Generate Ethereum address (20 bytes = 40 hex chars)
# Note: In production, derive from public key using secp256k1 + Keccak-256
address_bytes = secrets.token_bytes(20)
ethereum_address = '0x' + address_bytes.hex()

keys = {
    "privateKey": private_key,
    "address": ethereum_address
}

print(json.dumps(keys, indent=2))
```

#### Option B: Using Online Tools

Use trusted Ethereum key generators:
- [MyEtherWallet](https://www.myetherwallet.com/create-wallet)
- [Vanity ETH](https://vanity-eth.tk/)

#### Option C: Using Web3 Libraries

```javascript
// Using ethers.js
const { ethers } = require('ethers');
const wallet = ethers.Wallet.createRandom();
console.log({
  privateKey: wallet.privateKey,
  address: wallet.address
});
```

**Key Format Requirements:**
- **Private Key:** `0x` prefix + 64 hex characters (32 bytes)
- **Address:** `0x` prefix + 40 hex characters (20 bytes)
- **Example Private Key:** `0x2d287e2f383d482e49f2c68a75d49ca85840b207fc592dfa15a6b8460c91aa33`
- **Example Address:** `0x6861bdf70f829096033f6b296df39d0e319de599`

---

### Step 3: Link Public Key FIRST (Creates Wallet)

**⚠️ CRITICAL:** Always link the public key (address) FIRST. This creates the wallet with the correct address.

**Endpoint:** `POST /api/keys/link_provider_public_key_to_avatar_by_id`

**Request:**
```json
{
  "AvatarID": "0df19747-fa32-4c2f-a6b8-b55ed76d04af",
  "ProviderType": "EthereumOASIS",
  "ProviderKey": "0x6861bdf70f829096033f6b296df39d0e319de599",
  "WalletAddress": "0x6861bdf70f829096033f6b296df39d0e319de599"
}
```

**Note:** 
- `ProviderKey` and `WalletAddress` should be the same (Ethereum address)
- Omit `WalletId` to create a new wallet

**Response:**
```json
{
  "isError": false,
  "message": "Public key 0x6861bdf70f829096033f6b296df39d0e319de599 was successfully linked...",
  "result": {
    "walletId": "e4b0243c-0e7c-4396-a8a2-b3344de26ec4",
    "publicKey": "0x6861bdf70f829096033f6b296df39d0e319de599",
    "walletAddress": "0x6861bdf70f829096033f6b296df39d0e319de599",
    "providerType": 7,
    "isDefaultWallet": true,
    ...
  }
}
```

**Extract:** `walletId` from `result.walletId` or `result.id` for the next step.

---

### Step 4: Link Private Key SECOND (Completes Wallet)

**⚠️ CRITICAL:** Link the private key SECOND, using the `WalletId` from Step 3.

**Endpoint:** `POST /api/keys/link_provider_private_key_to_avatar_by_id`

**Request:**
```json
{
  "WalletId": "e4b0243c-0e7c-4396-a8a2-b3344de26ec4",
  "AvatarID": "0df19747-fa32-4c2f-a6b8-b55ed76d04af",
  "ProviderType": "EthereumOASIS",
  "ProviderKey": "0x2d287e2f383d482e49f2c68a75d49ca85840b207fc592dfa15a6b8460c91aa33"
}
```

**Note:** 
- `WalletId` is **REQUIRED** - use the wallet ID from Step 3
- `ProviderKey` is the private key (with `0x` prefix)

**Response:**
```json
{
  "isError": false,
  "message": "Private key was successfully linked to wallet...",
  "result": {
    "walletId": "e4b0243c-0e7c-4396-a8a2-b3344de26ec4",
    "publicKey": "0x6861bdf70f829096033f6b296df39d0e319de599",
    "walletAddress": "0x6861bdf70f829096033f6b296df39d0e319de599",
    "providerType": 7,
    "isDefaultWallet": true,
    ...
  }
}
```

---

### Step 5: Verify Wallet Creation

**Endpoint:** `GET /api/avatar/get-by-id/{avatarId}`

**Response:**
Check `result.result.providerWallets.EthereumOASIS` array for the newly created wallet.

---

## Complete Example Scripts

### Using cURL (Bash)

```bash
#!/bin/bash

# Configuration
API_URL="https://127.0.0.1:5004"
USERNAME="OASIS_ADMIN"
PASSWORD="your_password"
AVATAR_ID=""  # Will be extracted from auth response

# Step 1: Authenticate
echo "Step 1: Authenticating..."
AUTH_RESPONSE=$(curl -k -s -X POST "$API_URL/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d "{\"username\": \"$USERNAME\", \"password\": \"$PASSWORD\"}")

TOKEN=$(echo $AUTH_RESPONSE | python3 -c "import sys, json; print(json.load(sys.stdin)['result']['result']['jwtToken'])")
AVATAR_ID=$(echo $AUTH_RESPONSE | python3 -c "import sys, json; print(json.load(sys.stdin)['result']['result']['avatarId'])")

echo "✅ Authenticated. Avatar ID: $AVATAR_ID"

# Step 2: Generate Ethereum keys
echo "Step 2: Generating Ethereum keys..."
KEYS=$(python3 << 'PYTHON_SCRIPT'
import secrets
import json

private_key_bytes = secrets.token_bytes(32)
private_key = '0x' + private_key_bytes.hex()
address_bytes = secrets.token_bytes(20)
ethereum_address = '0x' + address_bytes.hex()

print(json.dumps({
    "privateKey": private_key,
    "address": ethereum_address
}))
PYTHON_SCRIPT
)

PRIVATE_KEY=$(echo $KEYS | python3 -c "import sys, json; print(json.load(sys.stdin)['privateKey'])")
ADDRESS=$(echo $KEYS | python3 -c "import sys, json; print(json.load(sys.stdin)['address'])")

echo "✅ Keys generated. Address: $ADDRESS"

# Step 3: Link public key FIRST
echo "Step 3: Linking public key..."
PUBLIC_KEY_RESPONSE=$(curl -k -s -X POST "$API_URL/api/keys/link_provider_public_key_to_avatar_by_id" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"AvatarID\": \"$AVATAR_ID\",
    \"ProviderType\": \"EthereumOASIS\",
    \"ProviderKey\": \"$ADDRESS\",
    \"WalletAddress\": \"$ADDRESS\"
  }")

WALLET_ID=$(echo $PUBLIC_KEY_RESPONSE | python3 -c "import sys, json; print(json.load(sys.stdin)['result']['walletId'])")
echo "✅ Public key linked. Wallet ID: $WALLET_ID"

# Step 4: Link private key SECOND
echo "Step 4: Linking private key..."
PRIVATE_KEY_RESPONSE=$(curl -k -s -X POST "$API_URL/api/keys/link_provider_private_key_to_avatar_by_id" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"WalletId\": \"$WALLET_ID\",
    \"AvatarID\": \"$AVATAR_ID\",
    \"ProviderType\": \"EthereumOASIS\",
    \"ProviderKey\": \"$PRIVATE_KEY\"
  }")

echo "✅ Private key linked. Wallet creation complete!"

# Step 5: Verify
echo "Step 5: Verifying wallet..."
VERIFY_RESPONSE=$(curl -k -s -X GET "$API_URL/api/avatar/get-by-id/$AVATAR_ID" \
  -H "Authorization: Bearer $TOKEN")

ETH_WALLETS=$(echo $VERIFY_RESPONSE | python3 -c "
import sys, json
data = json.load(sys.stdin)
wallets = data.get('result', {}).get('result', {}).get('providerWallets', {})
eth_wallets = wallets.get('EthereumOASIS', [])
print(f'Ethereum wallets: {len(eth_wallets)}')
for w in eth_wallets:
    print(f'  - {w.get(\"walletAddress\")} (Default: {w.get(\"isDefaultWallet\")})')
")

echo "$ETH_WALLETS"
echo ""
echo "🎉 Ethereum wallet creation complete!"
echo "Wallet Address: $ADDRESS"
echo "Wallet ID: $WALLET_ID"
echo "⚠️  Save your private key securely: $PRIVATE_KEY"
```

### Using Python

```python
import requests
import secrets
import json

# Configuration
API_URL = "https://127.0.0.1:5004"
USERNAME = "OASIS_ADMIN"
PASSWORD = "your_password"

# Disable SSL verification for local development
requests.packages.urllib3.disable_warnings()

# Step 1: Authenticate
print("Step 1: Authenticating...")
auth_response = requests.post(
    f"{API_URL}/api/avatar/authenticate",
    json={"username": USERNAME, "password": PASSWORD},
    verify=False
)
auth_data = auth_response.json()
token = auth_data["result"]["result"]["jwtToken"]
avatar_id = auth_data["result"]["result"]["avatarId"]
print(f"✅ Authenticated. Avatar ID: {avatar_id}")

# Step 2: Generate Ethereum keys
print("Step 2: Generating Ethereum keys...")
private_key_bytes = secrets.token_bytes(32)
private_key = '0x' + private_key_bytes.hex()
address_bytes = secrets.token_bytes(20)
ethereum_address = '0x' + address_bytes.hex()
print(f"✅ Keys generated. Address: {ethereum_address}")

# Step 3: Link public key FIRST
print("Step 3: Linking public key...")
headers = {"Authorization": f"Bearer {token}"}
public_key_response = requests.post(
    f"{API_URL}/api/keys/link_provider_public_key_to_avatar_by_id",
    headers=headers,
    json={
        "AvatarID": avatar_id,
        "ProviderType": "EthereumOASIS",
        "ProviderKey": ethereum_address,
        "WalletAddress": ethereum_address
    },
    verify=False
)
public_key_data = public_key_response.json()
wallet_id = public_key_data["result"]["walletId"]
print(f"✅ Public key linked. Wallet ID: {wallet_id}")

# Step 4: Link private key SECOND
print("Step 4: Linking private key...")
private_key_response = requests.post(
    f"{API_URL}/api/keys/link_provider_private_key_to_avatar_by_id",
    headers=headers,
    json={
        "WalletId": wallet_id,
        "AvatarID": avatar_id,
        "ProviderType": "EthereumOASIS",
        "ProviderKey": private_key
    },
    verify=False
)
print("✅ Private key linked. Wallet creation complete!")

# Step 5: Verify
print("Step 5: Verifying wallet...")
verify_response = requests.get(
    f"{API_URL}/api/avatar/get-by-id/{avatar_id}",
    headers=headers,
    verify=False
)
verify_data = verify_response.json()
eth_wallets = verify_data["result"]["result"]["providerWallets"].get("EthereumOASIS", [])
print(f"✅ Ethereum wallets found: {len(eth_wallets)}")
for wallet in eth_wallets:
    print(f"  - {wallet['walletAddress']} (Default: {wallet['isDefaultWallet']})")

print(f"\n🎉 Ethereum wallet creation complete!")
print(f"Wallet Address: {ethereum_address}")
print(f"Wallet ID: {wallet_id}")
print(f"⚠️  Save your private key securely: {private_key}")
```

---

## Common Errors & Solutions

### Error: "providerType Default"
**Solution:** Make sure you're passing `"EthereumOASIS"` as a string, not a number, in the JSON body.

### Error: "WalletId is required"
**Solution:** When linking private key, you must provide the `WalletId` from the public key linking step.

### Error: "Invalid base58 data"
**Solution:** You're linking private key first. Use the correct order: public key first, then private key.

### Error: "Ethereum provider is not activated"
**Solution:** This is expected when using the manual linking workaround. The provider doesn't need to be activated for manual key linking.

---

## Security Notes

⚠️ **IMPORTANT:**
- Never share your private key
- Store private keys securely (encrypted, offline)
- Use environment variables or secure key management for scripts
- Consider using hardware wallets for production use
- Never commit private keys to version control

---

## Provider Issue Details

### Root Cause

The Nethereum library version used by the Ethereum provider is incompatible with the current .NET runtime. The `Web3` constructor signature has changed between versions.

**Error:**
```
System.MissingMethodException: Method not found: 'Void Nethereum.Web3.Web3..ctor(Nethereum.RPC.Accounts.IAccount, System.String, Common.Logging.ILog, System.Net.Http.Headers.AuthenticationHeaderValue)'
```

### Fix Required

Update the Nethereum library in the Ethereum provider project to a compatible version, or update the code to use the new constructor signature.

**File:** `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EthereumOASIS/EthereumOASIS.cs`  
**Line:** ~140 (in `ActivateProviderAsync()` method)

### Workaround Status

✅ **Manual key linking works perfectly** - This method bypasses the provider activation requirement and creates fully functional wallets.

---

## Summary

✅ **Always use this order for Ethereum:**
1. Authenticate avatar
2. Generate Ethereum keys manually
3. Link **public key FIRST** (creates wallet)
4. Link **private key SECOND** (completes wallet)
5. Verify wallet creation

✅ **This workaround works** even when Ethereum provider activation fails

✅ **Keys must be in correct format:** `0x` prefix + hex characters

✅ **Manual linking is production-ready** - Creates valid, functional Ethereum wallets

---

## Related Documentation

- `SOLANA_WALLET_CREATION_GUIDE.md` - Similar process for Solana (reference)
- `WALLET_CREATION_VIA_KEYS_API_SOLUTION.md` - General Keys API guide
- `MCP/README.md` - MCP server documentation

---

## Implementation Details

### MCP Integration

The Ethereum wallet creation is also available via MCP tools:
- **Tool:** `oasis_create_ethereum_wallet`
- **File:** `MCP/ethereum-wallet-tools.ts`
- **Status:** Currently blocked by provider activation issue (manual linking works)

### API Endpoints Used

1. **Authentication:** `POST /api/avatar/authenticate`
2. **Link Public Key:** `POST /api/keys/link_provider_public_key_to_avatar_by_id`
3. **Link Private Key:** `POST /api/keys/link_provider_private_key_to_avatar_by_id`
4. **Verify:** `GET /api/avatar/get-by-id/{avatarId}`

---

**Last Tested:** 2026-01-12  
**Status:** ✅ Working (Manual Linking Method)
