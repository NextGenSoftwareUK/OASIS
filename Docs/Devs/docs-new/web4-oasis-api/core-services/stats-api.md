# Stats API

## Overview

The Stats API provides analytics and statistics for the OASIS ecosystem. It returns comprehensive stats for the current avatar (karma, achievements, gifts, etc.), karma stats/history per avatar, gift stats, and other stat endpoints. All endpoints are avatar-scoped and require authentication.

**Base URL:** `/api/stats`

**Authentication:** Required (Bearer token). Unauthenticated requests often return **HTTP 200** with `isError: true` and "Avatar not found. Please ensure you are logged in."—always check the response body.

**Rate Limits:** Same as [WEB4 API](../../reference/rate-limits.md).

---

## Quick Start

### Get stats for current avatar

```http
GET http://api.oasisweb4.com/api/stats/get-stats-for-current-logged-in-avatar
Authorization: Bearer YOUR_JWT_TOKEN
```

### Get karma stats for an avatar

```http
GET http://api.oasisweb4.com/api/stats/karma-stats/{avatarId}
Authorization: Bearer YOUR_JWT_TOKEN
```

---

## Endpoints Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `get-stats-for-current-logged-in-avatar` | Comprehensive stats for current avatar (karma, achievements, gifts, etc.) |
| GET | `karma-stats/{avatarId}` | Karma statistics for an avatar |
| GET | `karma-history/{avatarId}` | Karma history for an avatar (query: limit, default 50) |
| GET | `gift-stats/{avatarId}` | Gift statistics for an avatar |

Additional stat endpoints may exist (e.g. achievement stats); see [Swagger](http://api.oasisweb4.com/swagger/index.html) under **Stats**.

---

## Response Format

**get-stats-for-current-logged-in-avatar** and **karma-stats** / **gift-stats** typically return:

```json
{
  "result": {
    "karma": 1250,
    "achievements": [ ... ],
    "gifts": { ... },
    ...
  },
  "isError": false,
  "message": "Success"
}
```

**karma-history** returns a list of karma transaction objects (e.g. type, amount, source, timestamp).

---

## Related Documentation

- [Karma API](../authentication-identity/karma-api.md) – Add/remove karma, weightings, akashic records
- [Avatar API](../authentication-identity/avatar-api.md) – Identity

---

*Last Updated: January 24, 2026*
