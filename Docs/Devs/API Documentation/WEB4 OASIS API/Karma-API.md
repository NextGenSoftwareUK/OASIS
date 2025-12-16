# Karma API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Weightings & Voting](#weightings--voting)
- [Avatar Karma & Akashic Records](#avatar-karma--akashic-records)
- [Add / Remove Karma](#add--remove-karma)
- [Stats & History](#stats--history)

## Overview

The Karma API provides comprehensive karma management services for the OASIS ecosystem.

The WEB4 Karma API is backed by `KarmaController` and `KarmaManager`.  
It provides read/write karma operations, karma Akashic records, and analytics.

All endpoints live under:

```http
Base: /api/karma
```

All responses are wrapped in `OASISResult<T>`.

---

## Weightings & Voting

- **Get positive karma weighting**
  - `GET /api/karma/get-positive-karma-weighting/{karmaType}`
  - `GET /api/karma/get-positive-karma-weighting/{karmaType}/{providerType}/{setGlobally}`

- **Get negative karma weighting**
  - `GET /api/karma/get-negative-karma-weighting/{karmaType}`
  - `GET /api/karma/get-negative-karma-weighting/{karmaType}/{providerType}/{setGlobally}`

- **Vote for positive karma weighting**
  - `POST /api/karma/vote-for-positive-karma-weighting/{karmaType}/{weighting}`
  - `POST /api/karma/vote-for-positive-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}`

- **Vote for negative karma weighting**
  - `POST /api/karma/vote-for-negative-karma-weighting/{karmaType}/{weighting}`
  - `POST /api/karma/vote-for-negative-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}`

- **Set positive karma weighting**
  - `POST /api/karma/set-positive-karma-weighting/{karmaType}/{weighting}`
  - `POST /api/karma/set-positive-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}`

- **Set negative karma weighting**
  - `POST /api/karma/set-negative-karma-weighting/{karmaType}/{weighting}`
  - `POST /api/karma/set-negative-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}`

> Weighting methods currently use `KarmaTypePositive` / `KarmaTypeNegative` enums and `ProviderType`.

---

## Avatar Karma & Akashic Records

- **Get total karma for avatar**
  - `GET /api/karma/get-karma-for-avatar/{avatarId}`
  - `GET /api/karma/get-karma-for-avatar/{avatarId}/{providerType}/{setGlobally}`
  - Uses `KarmaManager.GetKarmaAsync(avatarId)` via controller.

- **Get karma Akashic records for avatar**
  - `GET /api/karma/get-karma-akashic-records-for-avatar/{avatarId}`
  - Returns `IEnumerable<IKarmaAkashicRecord>` from `AvatarDetail.KarmaAkashicRecords`.

---

## Add / Remove Karma

These endpoints wrap the rich `AddKarmaToAvatarAsync` and `RemoveKarmaFromAvatarAsync` methods in `KarmaManager`.

- **Add karma to avatar**
  - `POST /api/karma/add-karma-to-avatar/{avatarId}`
  - Body: `AddRemoveKarmaToAvatarRequest`
  - Overload with provider activation:
    - `POST /api/karma/add-karma-to-avatar/{avatarId}/{providerType}/{setGlobally}`

- **Remove karma from avatar**
  - `POST /api/karma/remove-karma-from-avatar/{avatarId}`
  - Body: `AddRemoveKarmaToAvatarRequest`
  - Overload with provider activation:
    - `POST /api/karma/remove-karma-from-avatar/{avatarId}/{providerType}/{setGlobally}`

Each returns:

```csharp
OASISResult<KarmaAkashicRecord>
```

---

## Stats & History

- **Get karma stats for avatar**
  - `GET /api/karma/get-karma-stats/{avatarId}`
  - `GET /api/karma/get-karma-stats/{avatarId}/{providerType}/{setGlobally}`
  - Calls `KarmaManager.GetKarmaStatsAsync`
  - Returns `Dictionary<string, object>` with totals, distributions, and recent activity.

- **Get karma history for avatar**
  - `GET /api/karma/get-karma-history/{avatarId}`
  - Optional query: `limit`, `offset` (if exposed)
  - `GET /api/karma/get-karma-history/{avatarId}/{providerType}/{setGlobally}`
  - Calls `KarmaManager.GetKarmaHistoryAsync`
  - Returns `List<KarmaTransaction>`

> The original generic `/api/karma` CRUD endpoints described in older docs are **not** part of `KarmaController`.  
> This file now reflects the actual controller methods implemented in the WEB4 OASIS WebAPI.


