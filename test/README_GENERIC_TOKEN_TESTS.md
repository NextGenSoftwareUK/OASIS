# Generic Token Operations Test Suite

**Date:** 2026-01-12  
**Status:** ✅ **READY FOR TESTING**

---

## Overview

This test suite validates the new **generic token operations** endpoints in `WalletController`. These endpoints work with **any ERC-20 compatible token** on any supported blockchain, not just MNEE.

---

## Prerequisites

1. **API Running:** The OASIS API must be running (default: `http://localhost:5003`)
2. **JWT Token:** You need a valid JWT token from an authenticated avatar
3. **Ethereum Wallet:** The authenticated avatar must have an Ethereum wallet linked
4. **Ethereum Provider:** EthereumOASIS provider must be activated and configured

---

## Quick Start

### 1. Get JWT Token

First, authenticate an avatar:

```bash
curl -X POST http://localhost:5003/api/avatar/authenticate \
  -H "Content-Type: application/json" \
  -d '{
    "username": "your_username",
    "password": "your_password"
  }'
```

Save the `token` from the response.

### 2. Set Environment Variables

```bash
export JWT_TOKEN="your_jwt_token_here"
export AVATAR_ID="your_avatar_id"  # Optional, defaults to authenticated avatar
export API_BASE_URL="http://localhost:5003"  # Optional, defaults to localhost:5003
export SPENDER_ADDRESS="0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb"  # Optional, for allowance/approve tests
```

### 3. Run Tests

#### Option A: Bash Script

```bash
cd test
./test_generic_token_endpoints.sh
```

#### Option B: Python Script

```bash
cd test
python3 test_generic_token_endpoints.py
```

---

## Test Coverage

### ✅ Test 1: Get MNEE Token Balance
- **Endpoint:** `GET /api/wallet/token/balance`
- **Purpose:** Test generic token balance retrieval for MNEE
- **Parameters:**
  - `tokenContractAddress`: MNEE contract address
  - `providerType`: EthereumOASIS
  - `avatarId`: (optional) Avatar ID

### ✅ Test 2: Get USDC Token Balance (Generic)
- **Endpoint:** `GET /api/wallet/token/balance`
- **Purpose:** Test generic token balance retrieval for a different token (USDC)
- **Parameters:**
  - `tokenContractAddress`: USDC contract address
  - `providerType`: EthereumOASIS
  - `avatarId`: (optional) Avatar ID

### ✅ Test 3: Get MNEE Token Info
- **Endpoint:** `GET /api/wallet/token/info`
- **Purpose:** Test generic token information retrieval
- **Parameters:**
  - `tokenContractAddress`: MNEE contract address
  - `providerType`: EthereumOASIS

### ✅ Test 4: Get USDC Token Info (Generic)
- **Endpoint:** `GET /api/wallet/token/info`
- **Purpose:** Test generic token information retrieval for a different token
- **Parameters:**
  - `tokenContractAddress`: USDC contract address
  - `providerType`: EthereumOASIS

### ✅ Test 5: Get Token Allowance
- **Endpoint:** `GET /api/wallet/token/allowance`
- **Purpose:** Test generic token allowance retrieval
- **Parameters:**
  - `tokenContractAddress`: MNEE contract address
  - `spenderAddress`: Spender address
  - `providerType`: EthereumOASIS
  - `avatarId`: (optional) Avatar ID

### ✅ Test 6: Approve Token
- **Endpoint:** `POST /api/wallet/token/approve`
- **Purpose:** Test generic token approval
- **Body:**
  ```json
  {
    "avatarId": "avatar-id",
    "tokenContractAddress": "0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF",
    "spenderAddress": "0x...",
    "amount": 100.0,
    "providerType": "EthereumOASIS"
  }
  ```

---

## Manual Testing Examples

### Get Token Balance

```bash
curl -X GET "http://localhost:5003/api/wallet/token/balance?tokenContractAddress=0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF&providerType=EthereumOASIS" \
  -H "Authorization: Bearer ${JWT_TOKEN}"
```

### Get Token Info

```bash
curl -X GET "http://localhost:5003/api/wallet/token/info?tokenContractAddress=0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF&providerType=EthereumOASIS" \
  -H "Authorization: Bearer ${JWT_TOKEN}"
```

### Get Token Allowance

```bash
curl -X GET "http://localhost:5003/api/wallet/token/allowance?tokenContractAddress=0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF&spenderAddress=0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb&providerType=EthereumOASIS" \
  -H "Authorization: Bearer ${JWT_TOKEN}"
```

### Approve Token

```bash
curl -X POST "http://localhost:5003/api/wallet/token/approve" \
  -H "Authorization: Bearer ${JWT_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "tokenContractAddress": "0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF",
    "spenderAddress": "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb",
    "amount": 100.0,
    "providerType": "EthereumOASIS"
  }'
```

---

## Expected Results

### Success Response Format

```json
{
  "result": 1234.56,
  "isError": false,
  "message": "Token balance retrieved successfully"
}
```

### Error Response Format

```json
{
  "result": null,
  "isError": true,
  "message": "Error message here"
}
```

---

## Troubleshooting

### Common Issues

1. **"Ethereum provider is not available"**
   - **Solution:** Ensure EthereumOASIS provider is activated in OASIS_DNA.json

2. **"No Ethereum wallet found for avatar"**
   - **Solution:** Create an Ethereum wallet for the avatar first

3. **"Token contract address is required"**
   - **Solution:** Ensure you're passing the `tokenContractAddress` parameter

4. **"Provider {providerType} is not available"**
   - **Solution:** Check that the provider is properly configured and activated

5. **Authentication errors**
   - **Solution:** Ensure JWT token is valid and not expired

---

## Test Results Interpretation

- ✅ **Success** - Endpoint called successfully, no errors
- ⚠️ **API Error** - Endpoint works but API returned error (e.g., "not found", "unauthorized")
- ❌ **Failed** - Endpoint call failed (network error, missing params, etc.)

---

## Next Steps

After running these tests, you can:

1. Test with different tokens (USDT, DAI, etc.)
2. Test with different providers (BaseOASIS, ArbitrumOASIS, etc.)
3. Test the MNEE convenience endpoints (`/api/mnee/*`)
4. Test the programmable finance features (Invoice, Escrow, Treasury)

---

**Note:** These are generic endpoints that work with **any ERC-20 token**. The MNEE-specific endpoints (`/api/mnee/*`) are convenience wrappers that default to the MNEE contract address.
