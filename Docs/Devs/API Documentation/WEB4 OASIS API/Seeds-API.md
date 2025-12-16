# Seeds API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Seed Transaction Endpoints](#seed-transaction-endpoints)
- [Legacy SEEDS Blockchain Endpoints](#legacy-seeds-blockchain-endpoints)

## Overview

The Seeds API provides SEEDS blockchain integration for the OASIS ecosystem. It handles SEEDS transactions, karma, and community features.

The WEB4 Seeds API has **two layers**:

- A new, **holon-based** layer backed by `SeedsController` + `SeedsManager` that stores `SeedTransaction` holons in the OASIS.
- The original **SEEDSOASIS / TelosOASIS** integration (currently preserved in comments in `SeedsController` and used in other services).

All active WEB4 endpoints live under:

```http
Base: /api/seeds
```

All responses are wrapped in `OASISResult<T>`.

---

## Seed Transaction Endpoints

Backed by `SeedsManager`:

```csharp
OASISResult<SeedTransaction> SaveSeedTransaction(...)
OASISResult<IEnumerable<SeedTransaction>> LoadSeedTransactionsForAvatar(...)
```

### Save seed transaction for an avatar

```http
POST /api/seeds/save-seed-transaction
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json
```

Body (`SaveSeedTransactionRequest`):

```json
{
  "avatarId": "00000000-0000-0000-0000-000000000000", // optional; defaults to current AvatarId
  "avatarUserName": "john_doe",
  "amount": 100,
  "memo": "Earned SEEDS for contribution"
}
```

### Get seed transactions for a specific avatar

```http
GET /api/seeds/avatar/{avatarId}/transactions
Authorization: Bearer YOUR_TOKEN
```

Returns: `OASISResult<IEnumerable<SeedTransaction>>`

### Get seed transactions for the current avatar

```http
GET /api/seeds/me/transactions
Authorization: Bearer YOUR_TOKEN
```

Returns: `OASISResult<IEnumerable<SeedTransaction>>` for `AvatarId`.

> See `SeedTransaction` in `ONODE.Core/Holons/SeedTransaction.cs` for all fields.

---

## Legacy SEEDS Blockchain Endpoints

The original `SeedsController` contained a rich set of SEEDS blockchain integration endpoints:

- `GET /api/seeds/get-all-organisations`
- `POST /api/seeds/pay-with-seeds-using-telos-account`
- `POST /api/seeds/pay-with-seeds-using-avatar`
- `POST /api/seeds/reward-with-seeds-using-telos-account`
- `POST /api/seeds/reward-with-seeds-using-avatar`
- `POST /api/seeds/donate-with-seeds-using-telos-account`
- `POST /api/seeds/donate-with-seeds-using-avatar`
- `POST /api/seeds/send-invite-to-join-seeds-using-telos-account`
- `POST /api/seeds/send-invite-to-join-seeds-using-avatar`
- `POST /api/seeds/accept-invite-to-join-seeds-using-telos-account`
- `POST /api/seeds/accept-invite-to-join-seeds-using-avatar`
- Various Telos account lookup helpers.

These are **preserved in comments** in `SeedsController` as reference and are used by other integration layers (SEEDSOASIS/TelosOASIS).  
They are not all wired as active WEB4 endpoints in this WebAPI build by default.

If you need the full SEEDS blockchain surface re-exposed via WEB4, we can:

- Un-comment and modernise the original controller methods, or
- Expose a new controller (e.g. `SeedsBlockchainController`) that wraps `SEEDSOASIS` directly.


