# Keys API

## Overview

The Keys API provides cryptographic key and provider-key management for the OASIS ecosystem. It links provider public/private keys and wallet addresses to avatars (by ID, username, or email), generates keypairs, retrieves keys and unique storage keys, and resolves avatars from provider keys. All key operations are avatar-scoped.

**Base URL:** `/api/keys`

**Authentication:** Required (Bearer token) for all endpoints. Unauthenticated requests often return **HTTP 200** with `isError: true` and an "Unauthorized" message—always check the response body.

**Rate Limits:**
- Free tier: 100 requests/minute
- Pro tier: 1,000 requests/minute

**Key Features:**
- ✅ **Link keys to avatar** – Public/private key and wallet address by ID, username, email
- ✅ **Generate keypairs** – For avatar or for provider type; with optional wallet address
- ✅ **Get keys** – Provider public/private keys and unique storage keys per avatar
- ✅ **Resolve avatar from key** – Avatar ID, username, email, or full avatar from provider key
- ✅ **Utility** – Clear cache; WIF encode/decode; base58 decode; encode signature; Telos/EOSIO/Holochain link

---

## Quick Start

### Link a provider public key to your avatar

```http
POST http://api.oasisweb4.com/api/keys/link_provider_public_key_to_avatar_by_id
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "avatarId": "avatar-guid",
  "walletId": "wallet-guid",
  "providerTypeToLinkTo": "SolanaOASIS",
  "providerKey": "public-key-string",
  "walletAddress": "0x...",
  "showSecretRecoveryWords": false
}
```

### Get all provider public keys for an avatar

```http
GET http://api.oasisweb4.com/api/keys/get_all_provider_public_keys_for_avatar_by_id/{avatarId}
Authorization: Bearer YOUR_JWT_TOKEN
```

---

## Endpoints Summary

### Link keys to avatar (POST)

| Endpoint | Description |
|----------|-------------|
| `link_provider_public_key_to_avatar_by_id` | Link public key by avatar ID (body: LinkProviderKeyToAvatarParams) |
| `link_provider_public_key_to_avatar_by_username` | Link public key by username |
| `link_provider_public_key_to_avatar_by_email` | Link public key by email |
| `link_provider_private_key_to_avatar_by_id` | Link private key by avatar ID |
| `link_provider_private_key_to_avatar_by_username` | Link private key by username |
| `link_provider_private_key_to_avatar_by_email` | Link private key by email |
| `link_provider_wallet_address_to_avatar_by_id` | Link wallet address by avatar ID |
| `link_provider_wallet_address_to_avatar_by_username` | Link wallet address by username |
| `link_provider_wallet_address_to_avatar_by_email` | Link wallet address by email |

### Generate keypairs (POST)

| Endpoint | Description |
|----------|-------------|
| `generate_keypair_and_link_provider_keys_to_avatar_by_id` | Generate and link by avatar ID (body params) |
| `generate_keypair_and_link_provider_keys_to_avatar_by_username` | Generate and link by username |
| `generate_keypair_and_link_provider_keys_to_avatar_by_email` | Generate and link by email |
| `generate_keypair_for_provider/{providerType}` | Generate keypair for a provider type |
| `generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_id` | Generate with wallet address and link by ID |
| `generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_username` | Same by username |
| `generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_email` | Same by email |
| `generate_keypair_with_wallet_address_for_provider/{providerType}` | Generate with wallet for provider |

### Get provider keys (GET)

| Endpoint | Description |
|----------|-------------|
| `get_provider_unique_storage_key_for_avatar_by_id` | Unique storage key by avatar ID (query: avatarId, providerType) |
| `get_provider_unique_storage_key_for_avatar_by_username` | By username |
| `get_provider_unique_storage_key_for_avatar_by_email` | By email |
| `get_provider_private_key_for_avatar_by_id` | Private key by avatar ID |
| `get_provider_private_key_for_avatar_by_username` | By username |
| `get_all_provider_public_keys_for_avatar_by_id/{id}` | All public keys by avatar ID |
| `get_all_provider_public_keys_for_avatar_by_username/{username}` | By username |
| `get_all_provider_public_keys_for_avatar_by_email/{email}` | By email |
| `get_all_provider_private_keys_for_avatar_by_id/{id}` | All private keys by ID |
| `get_all_provider_private_keys_for_avatar_by_username/{username}` | By username |
| `get_all_provider_unique_storage_keys_for_avatar_by_id/{id}` | All unique storage keys by ID |
| `get_all_provider_unique_storage_keys_for_avatar_by_username/{username}` | By username |
| `get_all_provider_unique_storage_keys_for_avatar_by_email/{email}` | By email |

### Resolve avatar from key (GET)

| Endpoint | Description |
|----------|-------------|
| `get_avatar_id_for_provider_unique_storage_key/{providerKey}` | Avatar ID from unique storage key |
| `get_avatar_username_for_provider_unique_storage_key/{providerKey}` | Username from unique storage key |
| `get_avatar_email_for_provider_unique_storage_key/{providerKey}` | Email from unique storage key |
| `get_avatar_for_provider_unique_storage_key/{providerKey}` | Full avatar from unique storage key |
| `get_avatar_id_for_provider_public_key/{providerKey}` | Avatar ID from public key |
| `get_avatar_username_for_provider_public_key/{providerKey}` | Username from public key |
| `get_avatar_email_for_provider_public_key/{providerKey}` | Email from public key |
| `get_avatar_for_provider_public_key/{providerKey}` | Full avatar from public key |
| `get_avatar_id_for_provider_private_key/{providerKey}` | Avatar ID from private key |
| `get_avatar_username_for_provider_private_key/{providerKey}` | Username from private key |
| `get_avatar_for_provider_private_key/{providerKey}` | Full avatar from private key |

### Utility (POST / GET / PUT / DELETE)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `clear_cache` | Clear KeyManager internal cache |
| POST | `get_private_wifi/{source}` | Get private WIF from source |
| POST | `get_public_wifi` | Get public WIF |
| POST | `decode_private_wif/{data}` | Decode private WIF |
| POST | `base58_check_decode/{data}` | Base58 check decode |
| POST | `encode_signature/{source}` | Encode signature |
| POST | `{id}/{telosAccountName}` | Link Telos account to avatar |
| POST | `{avatarId}/{eosioAccountName}` | Link EOSIO account |
| POST | `{avatarId}/{holochainAgentID}` | Link Holochain agent ID |
| GET | `all` | List keys (query params per implementation) |
| POST | `create` | Create key (body per implementation) |
| PUT | `{keyId}` | Update key |
| DELETE | `{keyId}` | Delete key |
| GET | `stats` | Key statistics |

---

## Link / Generate request params

**LinkProviderKeyToAvatarParams** (typical body for link endpoints):

| Field | Type | Description |
|-------|------|-------------|
| avatarId | GUID | Avatar ID (for by-id endpoints) |
| avatarUsername | string | Username (for by-username) |
| avatarEmail | string | Email (for by-email) |
| walletId | GUID | Wallet to attach key to |
| providerTypeToLinkTo | ProviderType | e.g. SolanaOASIS, EthereumOASIS |
| providerKey | string | Public or private key value |
| walletAddress | string | Wallet address (optional) |
| walletAddressSegwitP2SH | string | Segwit address (optional) |
| showPrivateKey | bool | Include private key in response (private-key endpoints) |
| showSecretRecoveryWords | bool | Include recovery words (public-key endpoints) |

---

## Response Format

Success:
```json
{
  "result": { ... },
  "isError": false,
  "message": "Success"
}
```

Error (often HTTP 200 with body):
```json
{
  "result": null,
  "isError": true,
  "message": "Error message"
}
```

---

## Error Handling

- Always check **isError** and **message** in the response body. Unauthorized or invalid params often return **HTTP 200** with `isError: true`.
- See [Error Code Reference](../../reference/error-codes.md) for standard codes.

---

## Related Documentation

- [Avatar API](avatar-api.md) – Identity and authentication
- [Wallet API](../blockchain-wallets/wallet-api.md) – Wallet lifecycle and import
- [Getting Started / Authentication](../../getting-started/authentication.md)

---

*Last Updated: January 24, 2026*
