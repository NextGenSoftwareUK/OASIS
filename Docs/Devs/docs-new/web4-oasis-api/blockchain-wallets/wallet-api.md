# Wallet API

## Overview

The Wallet API provides multi-chain wallet operations for the OASIS ecosystem. It handles wallet lifecycle (load, save, import, default), token transfers, portfolio value, and chain-specific operations with support for 50+ blockchain providers.

**Base URL:** `/api/wallet`

**Authentication:** Required (Bearer token). Many endpoints return **HTTP 200** with `isError: true` and an "Unauthorized" message when the token is missing—always check the response body.

**Rate Limits:**
- Free tier: 100 requests/minute
- Pro tier: 1,000 requests/minute

**Key Features:**
- ✅ **Multi-chain** – Ethereum, Solana, Polygon, and 50+ providers
- ✅ **Avatar-scoped** – Wallets by avatar ID, username, or email
- ✅ **Import** – Private key or public key (watch-only) import
- ✅ **Default wallet** – Get/set default per avatar
- ✅ **Send token** – Send Web4 token via `send_token`
- ✅ **Portfolio & analytics** – Portfolio value, tokens, analytics, supported chains

---

## Quick Start

### Load wallets for your avatar

```http
GET http://api.oasisweb4.com/api/wallet/avatar/{avatarId}/wallets/{showOnlyDefault}/{decryptPrivateKeys}
Authorization: Bearer YOUR_JWT_TOKEN
```

Use `showOnlyDefault=false` and `decryptPrivateKeys=false` for a safe listing. Optional: append `/{providerType}/{setGlobally}` (e.g. `SolanaOASIS/false`) to target a provider.

### Send token

```http
POST http://api.oasisweb4.com/api/wallet/send_token
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "fromAvatarId": "avatar-guid",
  "toAvatarIdOrWalletAddress": "wallet-address-or-avatar-id",
  "amount": "0.1",
  "token": "ETH",
  "chainId": 1
}
```

---

## Endpoints Summary

| Area | Method | Endpoint pattern |
|------|--------|------------------|
| **Send** | POST | `send_token` |
| **Load wallets** | GET | `avatar/{id}/wallets/{showOnlyDefault}/{decryptPrivateKeys}` |
| | GET | `avatar/username/{username}/wallets/{showOnlyDefault}/{decryptPrivateKeys}` |
| | GET | `avatar/email/{email}/wallets` or `.../wallets/{showOnlyDefault}/{decryptPrivateKeys}/{providerType}` |
| **Save wallets** | POST | `avatar/{id}/wallets`, `avatar/username/{username}/wallets`, `avatar/email/{email}/wallets` |
| **Default wallet** | GET | `avatar/{id}/default-wallet`, `avatar/username/{username}/default-wallet/...`, `avatar/email/{email}/default-wallet` |
| | POST | `avatar/{id}/default-wallet/{walletId}`, username, email variants |
| **Import** | POST | `avatar/{avatarId}/import/private-key`, `.../import/public-key` (and username/email variants) |
| **Lookup** | GET | `find-wallet?providerKey=...&providerType=...` |
| **Portfolio** | GET | `avatar/{avatarId}/portfolio/value` |
| **By chain** | GET | `avatar/{avatarId}/wallets/chain/{chain}` |
| **Transfer** | POST | `transfer` |
| **Analytics** | GET | `avatar/{avatarId}/wallet/{walletId}/analytics` |
| **Supported chains** | GET | `supported-chains` |
| **Tokens** | GET | `avatar/{avatarId}/wallet/{walletId}/tokens` |
| **Create wallet** | POST | `avatar/{avatarId}/create`, `avatar/username/{username}/create`, `avatar/email/{email}/create` |

All `.../wallets` and default-wallet GET/POST routes support optional `ProviderType` (and sometimes `setGlobally`) for provider-specific behaviour.

---

## Send Token

**Endpoint:** `POST /api/wallet/send_token`

**Authentication:** Required

**Body:** Implements `ISendWeb4TokenRequest` (e.g. fromAvatarId, toAvatarIdOrWalletAddress, amount, token, chainId/provider).

**Response:**
```json
{
  "result": { "transactionHash": "...", "success": true },
  "isError": false,
  "message": "Success"
}
```

---

## Load / Save Wallets

### Load provider wallets by avatar ID

**Endpoint:** `GET /api/wallet/avatar/{id}/wallets/{showOnlyDefault}/{decryptPrivateKeys}`

**Parameters:**

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| id | GUID | path | Yes | Avatar ID |
| showOnlyDefault | bool | path | Yes | If true, return only the default wallet |
| decryptPrivateKeys | bool | path | Yes | If true, include decrypted private keys (use with care) |

Optional query/path: `providerType` (e.g. `SolanaOASIS`), `setGlobally` (bool) when using provider-specific route.

**Response:**
```json
{
  "result": {
    "3": [{ "id": "wallet-guid", "address": "0x...", "isDefault": true }],
    "4": []
  },
  "isError": false,
  "message": "Success"
}
```

Keys in `result` are provider type values; values are arrays of provider wallets.

### Load by username / email

- `GET /api/wallet/avatar/username/{username}/wallets/{showOnlyDefault}/{decryptPrivateKeys}`
- `GET /api/wallet/avatar/email/{email}/wallets`  
- `GET /api/wallet/avatar/email/{email}/wallets/{showOnlyDefault}/{decryptPrivateKeys}/{providerType}`

Same response shape as by ID.

### Save wallets

**Endpoints:**  
`POST /api/wallet/avatar/{id}/wallets`  
`POST /api/wallet/avatar/username/{username}/wallets`  
`POST /api/wallet/avatar/email/{email}/wallets`

**Body:** `Dictionary<ProviderType, List<IProviderWallet>>` (provider type to list of wallets).

**Response:** `result: true` on success; check `isError` and `message` on failure.

---

## Default Wallet

### Get default wallet

- `GET /api/wallet/avatar/{id}/default-wallet` (requires `providerType` query)
- `GET /api/wallet/avatar/username/{username}/default-wallet/{showOnlyDefault}/{decryptPrivateKeys}`
- `GET /api/wallet/avatar/email/{email}/default-wallet`

### Set default wallet

- `POST /api/wallet/avatar/{id}/default-wallet/{walletId}`
- `POST /api/wallet/avatar/username/{username}/default-wallet/{walletId}`
- `POST /api/wallet/avatar/email/{email}/default-wallet/{walletId}`

Query: `providerType` where applicable.

---

## Import Wallet

### Import by private key

- `POST /api/wallet/avatar/{avatarId}/import/private-key?key=...&providerToImportTo=SolanaOASIS`
- `POST /api/wallet/avatar/username/{username}/import/private-key`
- `POST /api/wallet/avatar/email/{email}/import/private-key`

**Parameters:** `key` (private key), `providerToImportTo` (ProviderType).

**Response:** Returns the created/updated wallet (e.g. `IProviderWallet`) in `result`.

### Import by public key (watch-only)

- `POST /api/wallet/avatar/{avatarId}/import/public-key` (body/query: key, walletAddress, providerToImportTo)
- Same for `username` and `email`.

---

## Find Wallet

**Endpoint:** `GET /api/wallet/find-wallet?providerKey=...&providerType=...`

**Authentication:** Required

Returns the wallet that the given public key belongs to for the given provider.

---

## Portfolio & Analytics

### Portfolio value

**Endpoint:** `GET /api/wallet/avatar/{avatarId}/portfolio/value`

**Response (example):**
```json
{
  "result": {
    "totalValue": 15420.50,
    "totalValueUSD": 15420.50,
    "currency": "USD",
    "lastUpdated": "2024-01-15T10:30:00Z",
    "breakdown": { "ethereum": { "value": 8500.25, "usdValue": 8500.25, "count": 3 }, ... }
  },
  "isError": false
}
```

### Wallets by chain

**Endpoint:** `GET /api/wallet/avatar/{avatarId}/wallets/chain/{chain}`

Returns wallets for the given chain (e.g. `ethereum`, `solana`).

### Wallet analytics

**Endpoint:** `GET /api/wallet/avatar/{avatarId}/wallet/{walletId}/analytics`

Returns analytics for the given avatar and wallet (e.g. balances, transaction history).

### Tokens in wallet

**Endpoint:** `GET /api/wallet/avatar/{avatarId}/wallet/{walletId}/tokens`

Returns token list (symbol, amount, value, chain) for the wallet.

---

## Transfer

**Endpoint:** `POST /api/wallet/transfer`

**Body:** Transfer request (from/to, amount, token, chain, etc.). Exact schema follows backend `TransferRequest` / DTO.

**Response (example):**
```json
{
  "result": { "transactionId": "...", "status": "pending", "timestamp": "..." },
  "isError": false,
  "message": "Transfer initiated successfully"
}
```

---

## Supported Chains

**Endpoint:** `GET /api/wallet/supported-chains`

Returns list of supported chains (id, name, symbol, icon, isActive). No auth required for typical implementations; confirm in Swagger.

---

## Create Wallet

**Endpoints:**  
- `POST /api/wallet/avatar/{avatarId}/create`  
- `POST /api/wallet/avatar/username/{username}/create`  
- `POST /api/wallet/avatar/email/{email}/create`

**Body/query (typical):** name, description, walletProviderType, generateKeyPair, isDefaultWallet, providerTypeToLoadSave.

Creates a new wallet for the avatar and optionally sets it as default.

---

## Provider Variants

Many endpoints have overloads with path segments `/{providerType}/{setGlobally}` (e.g. `SolanaOASIS/false`). Use these to:

- **providerType** – Force the request to a specific blockchain/storage provider.
- **setGlobally** – If `true`, use this provider for subsequent requests in the same context.

---

## Error Handling

- Always check **isError** and **message** in the response body. Unauthenticated requests often return **HTTP 200** with `isError: true` and a message like "Unauthorized. Try Logging In First With api/avatar/authenticate REST API Route."
- Use [Error Code Reference](../../reference/error-codes.md) for standard codes (VALIDATION_ERROR, UNAUTHORIZED, etc.).

---

## Related Documentation

- [NFT API](nft-api.md) – NFT ownership and minting (avatar/wallet-linked)
- [Avatar API](../authentication-identity/avatar-api.md) – Authentication and avatar management
- [Getting Started / Authentication](../../getting-started/authentication.md)

---

*Last Updated: January 24, 2026*
