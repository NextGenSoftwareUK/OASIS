# NFT Minting Workflow - Step-by-Step Guide

**Date:** January 2026  
**Status:** ✅ Complete Workflow Documentation

---

## Overview

This document outlines the **correct order of steps** required to mint an NFT using SolanaOASIS. The steps must be followed **in this exact order** or the minting will fail.

---

## Required Steps (In Order)

### Step 1: Login/Authenticate

**Purpose:** Get JWT token for authenticated requests

**Endpoint:** `POST /api/avatar/authenticate`

**Request:**
```json
{
  "username": "your_username",
  "password": "your_password"
}
```

**Response:**
```json
{
  "result": {
    "id": "avatar-id",
    "jwtToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ...
  }
}
```

**Important:**
- Save the JWT token from the response
- Use this token in the `Authorization: Bearer {token}` header for all subsequent requests
- Token is required for provider registration, activation, and NFT minting

---

### Step 2: Register Provider Type

**Purpose:** Register SolanaOASIS provider with the system

**Endpoint:** `POST /api/provider/register-provider-type/SolanaOASIS`

**Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request:** No body required (provider type is in URL)

**Response:**
```json
{
  "result": true,
  "isError": false,
  "message": "Provider registered successfully"
}
```

**Important:**
- Must be authenticated (JWT token required)
- Provider must be registered before it can be activated
- If already registered, may return error (can be ignored)

---

### Step 3: Activate Provider

**Purpose:** Activate the SolanaOASIS provider for use

**Endpoint:** `POST /api/provider/activate-provider/SolanaOASIS`

**Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request:** No body required (provider type is in URL)

**Response:**
```json
{
  "result": true,
  "isError": false,
  "message": "Provider activated successfully"
}
```

**Important:**
- Must be authenticated (JWT token required)
- Provider must be **registered first** (Step 2)
- Provider must be activated before NFT minting will work
- If already activated, may return error (can be ignored)

---

### Step 4: Mint NFT

**Purpose:** Create and mint the NFT on Solana blockchain

**Endpoint:** `POST /api/nft/mint-nft`

**Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request:**
```json
{
  "Title": "My Test NFT",
  "Description": "A test NFT minted via API",
  "ImageUrl": "https://example.com/image.png",
  "Price": 0,
  "Symbol": "TESTNFT",
  "OnChainProvider": "SolanaOASIS",
  "OffChainProvider": "MongoDBOASIS",
  "NFTOffChainMetaType": "OASIS",
  "NFTStandardType": "SPL",
  "StoreNFTMetaDataOnChain": false,
  "NumberToMint": 1,
  "WaitTillNFTMinted": true,
  "WaitForNFTToMintInSeconds": 60,
  "AttemptToMintEveryXSeconds": 1,
  "SendToAvatarAfterMintingId": "your-avatar-id"
}
```

**Response (Success):**
```json
{
  "result": {
    "id": "nft-id",
    "title": "My Test NFT",
    "description": "A test NFT minted via API",
    "web3NFTs": [
      {
        "mintTransactionHash": "transaction-hash",
        "nftMintedUsingWalletAddress": "solana-wallet-address"
      }
    ]
  },
  "isError": false
}
```

**Important:**
- Must be authenticated (JWT token required)
- Provider must be **activated** (Step 3)
- All required fields must be provided
- `OnChainProvider` must match the activated provider

---

## Complete Workflow Example

### Using cURL

```bash
# Step 1: Authenticate
TOKEN=$(curl -X POST "http://localhost:5003/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username": "testuser", "password": "password"}' \
  | jq -r '.result.jwtToken')

# Step 2: Register Provider
curl -X POST "http://localhost:5003/api/provider/register-provider-type/SolanaOASIS" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"

# Step 3: Activate Provider
curl -X POST "http://localhost:5003/api/provider/activate-provider/SolanaOASIS" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"

# Step 4: Mint NFT
curl -X POST "http://localhost:5003/api/nft/mint-nft" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "Title": "My Test NFT",
    "Description": "Test NFT",
    "ImageUrl": "https://via.placeholder.com/512",
    "OnChainProvider": "SolanaOASIS",
    "OffChainProvider": "MongoDBOASIS",
    "NFTOffChainMetaType": "OASIS",
    "NFTStandardType": "SPL",
    "Symbol": "TESTNFT"
  }'
```

### Using Python

```python
import requests

BASE_URL = "http://localhost:5003/api"

# Step 1: Authenticate
auth_response = requests.post(
    f"{BASE_URL}/avatar/authenticate",
    json={"username": "testuser", "password": "password"}
)
token = auth_response.json()["result"]["jwtToken"]
headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}

# Step 2: Register Provider
requests.post(
    f"{BASE_URL}/provider/register-provider-type/SolanaOASIS",
    headers=headers
)

# Step 3: Activate Provider
requests.post(
    f"{BASE_URL}/provider/activate-provider/SolanaOASIS",
    headers=headers
)

# Step 4: Mint NFT
nft_response = requests.post(
    f"{BASE_URL}/nft/mint-nft",
    headers=headers,
    json={
        "Title": "My Test NFT",
        "Description": "Test NFT",
        "ImageUrl": "https://via.placeholder.com/512",
        "OnChainProvider": "SolanaOASIS",
        "OffChainProvider": "MongoDBOASIS",
        "NFTOffChainMetaType": "OASIS",
        "NFTStandardType": "SPL",
        "Symbol": "TESTNFT"
    }
)
```

---

## Common Errors & Solutions

### Error: "Unauthorized. Try Logging In First"

**Cause:** Missing or invalid JWT token

**Solution:**
- Ensure Step 1 (authentication) completed successfully
- Verify token is included in `Authorization: Bearer {token}` header
- Check token hasn't expired

### Error: "Provider not registered"

**Cause:** Skipped Step 2 (register provider)

**Solution:**
- Call `POST /api/provider/register-provider-type/SolanaOASIS` first
- Ensure authentication token is included

### Error: "Provider not activated"

**Cause:** Skipped Step 3 (activate provider) or provider not registered

**Solution:**
- Ensure Step 2 (register) completed first
- Then call `POST /api/provider/activate-provider/SolanaOASIS`
- Verify activation was successful

### Error: "Provider activation failed"

**Cause:** Provider may not be properly configured or dependencies missing

**Solution:**
- Check Solana provider is properly installed
- Verify Solana RPC endpoint is configured
- Check wallet has sufficient balance for gas fees

---

## Why This Order Matters

1. **Authentication First:** All provider and NFT operations require authentication. Without a valid JWT token, requests will fail with "Unauthorized".

2. **Register Before Activate:** The system needs to know about the provider before it can be activated. Registration adds the provider to the system's registry.

3. **Activate Before Use:** Activation prepares the provider for use (connects to blockchain, initializes wallets, etc.). NFT minting requires an active provider.

4. **Mint Last:** Only after all setup is complete can NFT minting succeed. The minting process uses the activated provider to interact with the blockchain.

---

## Additional Provider Setup

For SolanaOASIS, you may also need:

- **MongoDBOASIS** (for off-chain metadata):
  - Register: `POST /api/provider/register-provider-type/MongoDBOASIS`
  - Activate: `POST /api/provider/activate-provider/MongoDBOASIS`

- **IPFSOASIS** (optional, for metadata storage):
  - Register: `POST /api/provider/register-provider-type/IPFSOASIS`
  - Activate: `POST /api/provider/activate-provider/IPFSOASIS`

---

## Verification

After completing all steps, verify the setup:

```bash
# Check registered providers
curl -X GET "http://localhost:5003/api/provider/get-all-registered-providers" \
  -H "Authorization: Bearer $TOKEN"

# Check if Solana is activated
curl -X GET "http://localhost:5003/api/provider/get-registered-provider/SolanaOASIS" \
  -H "Authorization: Bearer $TOKEN"
```

---

## Summary

**Required Steps (In Order):**
1. ✅ **Login** → Get JWT token
2. ✅ **Register Provider** → `POST /api/provider/register-provider-type/SolanaOASIS`
3. ✅ **Activate Provider** → `POST /api/provider/activate-provider/SolanaOASIS`
4. ✅ **Mint NFT** → `POST /api/nft/mint-nft`

**All steps require:**
- Valid JWT token in `Authorization: Bearer {token}` header
- Steps must be completed in order
- Each step must succeed before proceeding to the next

---

**Status:** ✅ Complete  
**Last Updated:** January 2026
