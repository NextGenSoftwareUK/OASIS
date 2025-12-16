# Wallet API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Send & Transfer](#send--transfer)
- [Avatar Wallet Management](#avatar-wallet-management)
- [Default Wallets](#default-wallets)
- [Import & Lookup](#import--lookup)
- [Analytics & Chains](#analytics--chains)

## Overview

The Wallet API provides comprehensive wallet management services for the OASIS ecosystem. It handles wallet creation, management, transactions, and analytics with support for multiple cryptocurrencies, real-time updates, and advanced security features.
The WEB4 Wallet API is backed by `WalletController` and `WalletManager`.  
It manages **provider wallets** per avatar across multiple chains, and all endpoints use `OASISResult<T>`.

All endpoints live under:

```http
Base: /api/wallet
```

---

## Send & Transfer

- **Send Web4 Token**
  - `POST /api/wallet/send_token`
  - Body: `ISendWeb4TokenRequest`
  - Calls `WalletManager.SendTokenAsync(avatarId, request)`

- **Transfer Between Wallets (placeholder)**
  - `POST /api/wallet/transfer`
  - Body: generic transfer object (currently demo implementation only)

---

## Avatar Wallet Management

All provider wallets are returned as:

```csharp
Dictionary<ProviderType, List<IProviderWallet>>
```

- **Load provider wallets for avatar by ID**
  - `GET /api/wallet/avatar/{id}/wallets/{showOnlyDefault}/{decryptPrivateKeys}`
  - Optional query/route: `providerType`
  - Calls `WalletManager.LoadProviderWalletsForAvatarByIdAsync`

- **Load provider wallets for avatar by username**
  - `GET /api/wallet/avatar/username/{username}/wallets/{showOnlyDefault}/{decryptPrivateKeys}`
  - Calls `WalletManager.LoadProviderWalletsForAvatarByUsernameAsync`

- **Load provider wallets for avatar by email**
  - `GET /api/wallet/avatar/email/{email}/wallets`
  - `GET /api/wallet/avatar/email/{email}/wallets/{showOnlyDefault}/{decryptPrivateKeys}/{providerType}`
  - Calls `WalletManager.LoadProviderWalletsForAvatarByEmailAsync`

- **Save provider wallets for avatar by ID**
  - `POST /api/wallet/avatar/{id}/wallets`
  - Body: `Dictionary<ProviderType, List<IProviderWallet>>`

- **Save provider wallets for avatar by username**
  - `POST /api/wallet/avatar/username/{username}/wallets`

- **Save provider wallets for avatar by email**
  - `POST /api/wallet/avatar/email/{email}/wallets`

---

## Default Wallets

- **Get default wallet (by ID)**
  - `GET /api/wallet/avatar/{id}/default-wallet`
  - Calls `WalletManager.GetAvatarDefaultWalletByIdAsync`

- **Get default wallet (by username)**
  - `GET /api/wallet/avatar/username/{username}/default-wallet/{showOnlyDefault}/{decryptPrivateKeys}`
  - Calls `WalletManager.GetAvatarDefaultWalletByUsernameAsync`

- **Get default wallet (by email)**
  - `GET /api/wallet/avatar/email/{email}/default-wallet`
  - Calls `WalletManager.GetAvatarDefaultWalletByEmailAsync`

- **Set default wallet (by ID)**
  - `POST /api/wallet/avatar/{id}/default-wallet/{walletId}`
  - Calls `WalletManager.SetAvatarDefaultWalletByIdAsync`

- **Set default wallet (by username)**
  - `POST /api/wallet/avatar/username/{username}/default-wallet/{walletId}`

- **Set default wallet (by email)**
  - `POST /api/wallet/avatar/email/{email}/default-wallet/{walletId}`

---

## Import & Lookup

- **Import wallet using private key**
  - By avatar ID:
    - `POST /api/wallet/avatar/{avatarId}/import/private-key`
  - By username:
    - `POST /api/wallet/avatar/username/{username}/import/private-key`
  - By email:
    - `POST /api/wallet/avatar/email/{email}/import/private-key`
  - Parameters: `key`, `providerToImportTo`

- **Import wallet using public key**
  - By avatar ID:
    - `POST /api/wallet/avatar/{avatarId}/import/public-key`
  - By username:
    - `POST /api/wallet/avatar/username/{username}/import/public-key`
  - By email:
    - `POST /api/wallet/avatar/email/{email}/import/public-key`
  - Parameters: `key`, `walletAddress`, `providerToImportTo`

- **Lookup wallet by public key**
  - `GET /api/wallet/find-wallet?providerKey={key}&providerType={providerType}`
  - Calls `WalletManager.GetWalletThatPublicKeyBelongsTo`

---

## Analytics & Chains

These endpoints currently return demo data but are wired for future `WalletManager` extensions:

- **Portfolio value for avatar**
  - `GET /api/wallet/avatar/{avatarId}/portfolio/value`

- **Wallets by chain**
  - `GET /api/wallet/avatar/{avatarId}/wallets/chain/{chain}`

- **Wallet analytics**
  - `GET /api/wallet/avatar/{avatarId}/wallet/{walletId}/analytics`

- **Supported chains**
  - `GET /api/wallet/supported-chains`

- **Tokens in a wallet**
  - `GET /api/wallet/avatar/{avatarId}/wallet/{walletId}/tokens`

---

## Wallet Creation & Update

These map directly to the `CreateWalletForAvatar*` and `UpdateWalletForAvatar*` methods in `WalletManager`:

- **Create wallet for avatar by ID**
  - `POST /api/wallet/avatar/{avatarId}/create-wallet`
  - Body: `CreateWalletRequest`

- **Create wallet for avatar by username**
  - `POST /api/wallet/avatar/username/{username}/create-wallet`

- **Create wallet for avatar by email**
  - `POST /api/wallet/avatar/email/{email}/create-wallet`

- **Update wallet for avatar by ID**
  - `PUT /api/wallet/avatar/{avatarId}/wallet/{walletId}`
  - Body: `UpdateWalletRequest`

- **Update wallet for avatar by username**
  - `PUT /api/wallet/avatar/username/{username}/wallet/{walletId}`

- **Update wallet for avatar by email**
  - `PUT /api/wallet/avatar/email/{email}/wallet/{walletId}`

> For detailed DTOs see `NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.*` and  
> `NextGenSoftware.OASIS.API.Core.Objects.Wallet.*`.


