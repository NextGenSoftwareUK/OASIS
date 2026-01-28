# Karma API

## Overview

The Karma API provides reputation and reward tracking for the OASIS ecosystem. It supports positive and negative karma types, weightings, karma per avatar, akashic records, voting on weightings, and karma statistics/history. Karma is tied to avatars and is not transferable.

**Base URL:** `/api/karma`

**Authentication:** Most read endpoints do not require auth; add/remove karma and voting/setting weightings require a Bearer token. Wizard-level auth is required to set weightings.

**Rate Limits:**
- Free tier: 100 requests/minute
- Pro tier: 1,000 requests/minute

**Key Features:**
- ✅ **Positive & negative karma** – Separate types and weightings
- ✅ **Per-avatar karma** – Get total karma and akashic records
- ✅ **Add/remove karma** – With source (App, dApp, hApp, Website, Game)
- ✅ **Weighting** – Get/vote/set weightings (Wizard for set)
- ✅ **Stats & history** – Karma stats and paged history

---

## Quick Start

### Get karma for an avatar

```http
GET http://api.oasisweb4.com/api/karma/get-karma-for-avatar/{avatarId}
```

### Get karma history (paged)

```http
GET http://api.oasisweb4.com/api/karma/get-karma-history/{avatarId}?limit=50&offset=0
Authorization: Bearer YOUR_JWT_TOKEN
```

### Add positive karma (authenticated)

```http
POST http://api.oasisweb4.com/api/karma/add-karma-to-avatar/{avatarId}
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "karmaType": "HelpAnother",
  "karmaSourceType": "App",
  "karamSourceTitle": "My App",
  "karmaSourceDesc": "Description of source"
}
```

---

## Endpoints Summary

| Area | Method | Endpoint pattern |
|------|--------|------------------|
| **Positive weighting** | GET | `get-positive-karma-weighting/{karmaType}` |
| | GET | `get-positive-karma-weighting/{karmaType}/{providerType}/{setGlobally}` |
| **Negative weighting** | GET | `get-negative-karma-weighting/{karmaType}` |
| | GET | `get-negative-karma-weighting/{karmaType}/{providerType}/{setGlobally}` |
| **Karma for avatar** | GET | `get-karma-for-avatar/{avatarId}` |
| | GET | `get-karma-for-avatar/{avatarId}/{providerType}/{setGlobally}` |
| **Akashic records** | GET | `get-karma-akashic-records-for-avatar/{avatarId}` |
| | GET | `get-karma-akashic-records-for-avatar/{avatarId}/{providerType}/{setGlobally}` |
| **Vote weighting** | POST | `vote-for-positive-karma-weighting/{karmaType}/{weighting}` |
| | POST | `vote-for-positive-karma-weighting/{karmaType}/{weighting}/{providerType}/{setGlobally}` |
| | POST | `vote-for-negative-karma-weighting/{karmaType}/{weighting}` (and provider variant) |
| **Set weighting (Wizard)** | POST | `set-positive-karma-weighting/{karmaType}/{weighting}` |
| | POST | `set-negative-karma-weighting/{karmaType}/{weighting}` (and provider variants) |
| **Add karma** | POST | `add-karma-to-avatar/{avatarId}` (and `/{providerType}/{setGlobally}`) |
| **Remove karma** | POST | `remove-karma-from-avatar/{avatarId}` (and provider variant) |
| **Stats** | GET | `get-karma-stats/{avatarId}` (and provider variant) |
| **History** | GET | `get-karma-history/{avatarId}?limit=50&offset=0` (and provider variant) |

---

## Karma for Avatar

### Get karma total

**Endpoint:** `GET /api/karma/get-karma-for-avatar/{avatarId}`

**Parameters:**

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| avatarId | GUID | path | Yes | Avatar ID |

Optional: `/{providerType}/{setGlobally}` for provider-specific call.

**Response:**
```json
{
  "result": 1250,
  "isError": false,
  "message": "Success"
}
```

`result` is the avatar’s total karma (long).

---

## Karma Akashic Records

**Endpoint:** `GET /api/karma/get-karma-akashic-records-for-avatar/{avatarId}`

Returns the list of karma akashic records (history of karma events) for the avatar. Optional: `/{providerType}/{setGlobally}`.

---

## Add / Remove Karma

### Add positive karma

**Endpoint:** `POST /api/karma/add-karma-to-avatar/{avatarId}`

**Authentication:** Required

**Body (AddRemoveKarmaToAvatarRequest):**

| Field | Type | Description |
|-------|------|-------------|
| karmaType | string | Must be a value from **KarmaTypePositive** enum (e.g. HelpAnother) |
| karmaSourceType | string | **KarmaSourceType**: App, dApp, hApp, Website, Game |
| karamSourceTitle | string | Name of the app/website/game |
| karmaSourceDesc | string | Description of the source |

**Response:** Returns the created `KarmaAkashicRecord` in `result`.

Provider variant: `POST /api/karma/add-karma-to-avatar/{avatarId}/{providerType}/{setGlobally}` (body same).

### Remove karma (negative)

**Endpoint:** `POST /api/karma/remove-karma-from-avatar/{avatarId}`

**Body:** Same shape; **karmaType** must be a value from **KarmaTypeNegative** enum.

Provider variant: `POST /api/karma/remove-karma-from-avatar/{avatarId}/{providerType}/{setGlobally}`.

---

## Weightings

### Get positive/negative karma weighting

- `GET /api/karma/get-positive-karma-weighting/{karmaType}`
- `GET /api/karma/get-positive-karma-weighting/{karmaType}/{providerType}/{setGlobally}`
- `GET /api/karma/get-negative-karma-weighting/{karmaType}` (and provider variant)

**karmaType:** A value from `KarmaTypePositive` or `KarmaTypeNegative` enum.

**Response:** `result` is the weighting (e.g. boolean or numeric per implementation).

### Vote for weighting (authenticated)

- `POST /api/karma/vote-for-positive-karma-weighting/{karmaType}/{weighting}`
- `POST /api/karma/vote-for-negative-karma-weighting/{karmaType}/{weighting}`

Optional: `/{providerType}/{setGlobally}`. **weighting** is an integer.

### Set weighting (Wizard only)

- `POST /api/karma/set-positive-karma-weighting/{karmaType}/{weighting}`
- `POST /api/karma/set-negative-karma-weighting/{karmaType}/{weighting}`

Requires **Wizard** avatar type. Optional provider variant.

---

## Karma Stats

**Endpoint:** `GET /api/karma/get-karma-stats/{avatarId}`

Returns karma statistics (totals, distributions, recent activity) for the avatar. Optional: `/{providerType}/{setGlobally}`.

**Response (example):**
```json
{
  "result": {
    "totalKarma": 1250,
    "positiveTotal": 1300,
    "negativeTotal": 50,
    "distribution": { ... },
    "recentActivity": [ ... ]
  },
  "isError": false
}
```

---

## Karma History

**Endpoint:** `GET /api/karma/get-karma-history/{avatarId}`

**Query parameters:**

| Parameter | Type | Default | Description |
|-----------|------|--------|-------------|
| limit | int | 50 | Page size |
| offset | int | 0 | Offset for paging |

Optional path: `/{providerType}/{setGlobally}`.

**Response:** `result` is a list of `KarmaTransaction` (e.g. type, amount, source, timestamp).

---

## Enums (reference)

- **KarmaTypePositive** – e.g. HelpAnother, ShareKnowledge, etc. (see API/Swagger for full list).
- **KarmaTypeNegative** – Negative karma categories.
- **KarmaSourceType** – App, dApp, hApp, Website, Game.

Use `GET` error messages or Swagger for exact enum values when sending `karmaType` and `karmaSourceType`.

---

## Error Handling

- Check **isError** and **message** in the response. Invalid `karmaType` or `karmaSourceType` returns an error message listing allowed enum values.
- Unauthorized calls to protected endpoints may return **HTTP 200** with `isError: true` and an "Unauthorized" message—see [Error Code Reference](../../reference/error-codes.md).

---

## Related Documentation

- [Avatar API](avatar-api.md) – Identity and authentication
- [Getting Started / Authentication](../../getting-started/authentication.md)

---

*Last Updated: January 24, 2026*
