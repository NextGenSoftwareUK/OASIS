# Settings API

## Overview

The Settings API provides configuration management for the OASIS ecosystem. You can get and update all settings for the current avatar, including HyperDrive settings, notifications, privacy, and other OASIS configuration. All endpoints are avatar-scoped and require authentication.

**Base URL:** `/api/settings`

**Authentication:** Required (Bearer token). Unauthenticated or missing avatar often returns **HTTP 200** with `isError: true` and "Avatar not found. Please ensure you are logged in."—always check the response body.

**Rate Limits:** Same as [WEB4 API](../../reference/rate-limits.md).

---

## Quick Start

### Get all settings for current avatar

```http
GET http://api.oasisweb4.com/api/settings/get-all-settings-for-current-logged-in-avatar
Authorization: Bearer YOUR_JWT_TOKEN
```

### Get HyperDrive settings

```http
GET http://api.oasisweb4.com/api/settings/hyperdrive-settings
Authorization: Bearer YOUR_JWT_TOKEN
```

### Update HyperDrive settings

```http
PUT http://api.oasisweb4.com/api/settings/hyperdrive-settings
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "autoFailoverEnabled": true,
  "autoReplicationEnabled": true,
  "autoLoadBalancingEnabled": true
}
```

---

## Endpoints Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `get-all-settings-for-current-logged-in-avatar` | Comprehensive OASIS settings (HyperDrive, notifications, privacy, etc.) |
| GET | `hyperdrive-settings` | HyperDrive configuration for current avatar |
| PUT | `hyperdrive-settings` | Update HyperDrive settings (body: Dictionary<string, object>) |

Additional settings endpoints (e.g. notifications, privacy, preferences) may exist; see [Swagger](http://api.oasisweb4.com/swagger/index.html) under **Settings**.

---

## Response Format

**get-all-settings-for-current-logged-in-avatar** and **hyperdrive-settings** typically return:

```json
{
  "result": {
    "hyperDrive": { ... },
    "notifications": { ... },
    "privacy": { ... },
    ...
  },
  "isError": false,
  "message": "Success"
}
```

**PUT hyperdrive-settings** returns `result: true` on success.

---

## Related Documentation

- [HyperDrive API](../network-operations/hyperdrive-api.md) – Auto-failover and provider configuration
- [WEB4 Overview](../overview.md) – Provider selection
- [Avatar API](../authentication-identity/avatar-api.md) – Identity

---

*Last Updated: January 24, 2026*
