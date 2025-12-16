# Keys API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Cache & Provider Key Linking](#cache--provider-key-linking)
- [Keypair & Wallet Address Generation](#keypair--wallet-address-generation)
- [Lookup by Keys](#lookup-by-keys)
- [WIF & Encoding Utilities](#wif--encoding-utilities)

## Overview

The Keys API provides comprehensive cryptographic key management services for the OASIS ecosystem. It handles key generation, storage, rotation, and security with support for multiple key types, encryption algorithms, and advanced security features.
The WEB4 Keys API is backed by `KeysController` and `KeyManager`.  
It manages linking provider public/private keys and wallet addresses to avatars and generating provider-specific keypairs.

All endpoints live under:

```http
Base: /api/keys
```

All responses are wrapped in `OASISResult<T>`.

---

## Cache & Provider Key Linking

- **Clear key cache**
  - `POST /api/keys/clear_cache`
  - Calls `KeyManager.ClearCache`

- **Link provider public key to avatar (by ID)**
  - `POST /api/keys/link_provider_public_key_to_avatar_by_id`
  - Body: `LinkProviderKeyToAvatarParams`
  - Calls `KeyManager.LinkProviderPublicKeyToAvatarById`

- **Link provider public key to avatar (by username)**
  - `POST /api/keys/link_provider_public_key_to_avatar_by_username`

- **Link provider public key to avatar (by email)**
  - `POST /api/keys/link_provider_public_key_to_avatar_by_email`

- **Link provider private key to avatar (by ID)**
  - `POST /api/keys/link_provider_private_key_to_avatar_by_id`

- **Link provider private key to avatar (by username)**
  - `POST /api/keys/link_provider_private_key_to_avatar_by_username`

> The email-based private key endpoint is intentionally commented out for security reasons in the controller.

---

## Keypair & Wallet Address Generation

- **Generate keypair and link provider keys to avatar (by ID)**
  - `POST /api/keys/generate_keypair_and_link_provider_keys_to_avatar_by_id`
  - Body: `LinkProviderKeyToAvatarParams`
  - Calls `KeyManager.GenerateKeyPairAndLinkProviderKeysToAvatarById`

- **Generate keypair and link provider keys to avatar (by username)**
  - `POST /api/keys/generate_keypair_and_link_provider_keys_to_avatar_by_username`

- **Generate keypair and link provider keys to avatar (by email)**
  - `POST /api/keys/generate_keypair_and_link_provider_keys_to_avatar_by_email`

- **Generate keypair with wallet address & link to avatar (by ID)**
  - `POST /api/keys/generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_id`

- **Generate keypair with wallet address & link to avatar (by username)**
  - `POST /api/keys/generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_username`

- **Generate keypair with wallet address & link to avatar (by email)**
  - `POST /api/keys/generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_email`

- **Generate keypair for provider (no avatar linkage)**
  - `POST /api/keys/generate_keypair_for_provider/{providerType}`
  - Returns `KeyPair`

- **Generate raw keypair**
  - `POST /api/keys/generate_keypair/{keyPrefix}`
  - Returns `KeyPair`

- **Generate keypair with wallet address (no avatar linkage)**
  - `POST /api/keys/generate_keypair_with_wallet_address_for_provider/{providerType}`
  - Returns `IKeyPairAndWallet`

- **Link provider wallet address to avatar (by ID)**
  - `POST /api/keys/link_provider_wallet_address_to_avatar_by_id`
  - Calls `KeyManager.LinkProviderWalletAddressToAvatarById`

- **Link provider wallet address to avatar (by username)**
  - `POST /api/keys/link_provider_wallet_address_to_avatar_by_username`

- **Link provider wallet address to avatar (by email)**
  - `POST /api/keys/link_provider_wallet_address_to_avatar_by_email`

---

## Lookup by Keys

These endpoints resolve avatars or keys from provider-specific storage/public/private keys.

- **Get provider unique storage key for avatar**
  - By ID:
    - `GET /api/keys/get_provider_unique_storage_key_for_avatar_by_id`
  - By username:
    - `GET /api/keys/get_provider_unique_storage_key_for_avatar_by_username`
  - By email:
    - `GET /api/keys/get_provider_unique_storage_key_for_avatar_by_email`

- **Get provider private key(s) for avatar**
  - By ID:
    - `GET /api/keys/get_provider_private_key_for_avatar_by_id`
  - By username:
    - `GET /api/keys/get_provider_private_key_for_avatar_by_username`
  - (Email variant is intentionally commented out)

- **Get provider public keys for avatar**
  - By ID:
    - `GET /api/keys/get_provider_public_keys_for_avatar_by_id`
  - By username:
    - `GET /api/keys/get_provider_public_keys_for_avatar_by_username`
  - By email:
    - `GET /api/keys/get_provider_public_keys_for_avatar_by_email`

- **Get all provider public keys for avatar**
  - By ID:
    - `GET /api/keys/get_all_provider_public_keys_for_avatar_by_id/{id}`
  - By username:
    - `GET /api/keys/get_all_provider_public_keys_for_avatar_by_username/{username}`
  - By email:
    - `GET /api/keys/get_all_provider_public_keys_for_avatar_by_email/{email}`

- **Get all provider private keys for avatar**
  - By ID:
    - `GET /api/keys/get_all_provider_private_keys_for_avatar_by_id/{id}`
  - By username:
    - `GET /api/keys/get_all_provider_private_keys_for_avatar_by_username/{username}`

- **Get all provider unique storage keys for avatar**
  - By ID:
    - `GET /api/keys/get_all_provider_unique_storage_keys_for_avatar_by_id/{id}`
  - By username:
    - `GET /api/keys/get_all_provider_unique_storage_keys_for_avatar_by_username/{username}`
  - By email:
    - `GET /api/keys/get_all_provider_unique_storage_keys_for_avatar_by_email/{email}`

- **Reverse lookup from unique storage key**
  - `GET /api/keys/get_avatar_id_for_provider_unique_storage_key/{providerKey}`
  - `GET /api/keys/get_avatar_username_for_provider_unique_storage_key/{providerKey}`
  - `GET /api/keys/get_avatar_email_for_provider_unique_storage_key/{providerKey}`
  - `GET /api/keys/get_avatar_for_provider_unique_storage_key/{providerKey}`

- **Reverse lookup from public key**
  - `GET /api/keys/get_avatar_id_for_provider_public_key/{providerKey}`
  - `GET /api/keys/get_avatar_username_for_provider_public_key/{providerKey}`
  - `GET /api/keys/get_avatar_email_for_provider_public_key/{providerKey}`
  - `GET /api/keys/get_avatar_for_provider_public_key/{providerKey}`

---

## WIF & Encoding Utilities

- **Get private WIF**
  - `POST /api/keys/get_private_wifi/{source}`
  - Body: `byte[] source` (raw private key bytes)

- **Get public WIF**
  - `POST /api/keys/get_public_wifi`
  - Body: `WifParams` (`publicKey`, `prefix`)

- **Decode private WIF**
  - `POST /api/keys/decode_private_wif/{data}`
  - Returns `byte[]`

- **Base58 check decode**
  - `POST /api/keys/base58_check_decode/{data}`
  - Returns `byte[]`

- **Encode signature**
  - `POST /api/keys/encode_signature/{source}`
  - Returns `string`

> For DTOs see `NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Keys.*` and  
> `NextGenSoftware.OASIS.API.Core.Objects.KeyPair`.


