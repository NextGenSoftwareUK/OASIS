# Share API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Share Management](#share-management)
- [Share Operations](#share-operations)
- [Error Responses](#error-responses)

## Overview

The Share API provides content sharing functionality for the OASIS ecosystem. It handles sharing of content, links, and resources.

## Share Management

### Get Shared Content
```http
GET /api/share
Authorization: Bearer YOUR_TOKEN
```

### Get Share by ID
```http
GET /api/share/{shareId}
Authorization: Bearer YOUR_TOKEN
```

### Create Share
```http
POST /api/share
Authorization: Bearer YOUR_TOKEN
```

## Share Operations

### Share Content
```http
POST /api/share/content
Authorization: Bearer YOUR_TOKEN
```

### Get Share Stats
```http
GET /api/share/{shareId}/stats
Authorization: Bearer YOUR_TOKEN
```

### Delete Share
```http
DELETE /api/share/{shareId}
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Share Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Share not found"
}
```

---

## Navigation

**← Previous:** [Video API](Video-API.md) | **Next:** [Seeds API](Seeds-API.md) →

## Holon permissions

Owners can grant each avatar any of `read`, `write`, `delete`, `addChildren` and `manage` on a single holon. Needs
`manage` on the holon (the creator always has it).

```http
POST /api/share/permissions/{holonId}
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{ "grants": { "<avatarId>": ["read", "write"], "<otherAvatarId>": [] } }
```

An avatar listed with names gets exactly those permissions; an empty list removes its access; avatars not listed are
unchanged.

```http
GET /api/share/permissions/{holonId}
Authorization: Bearer YOUR_TOKEN
```

Returns the stored grants as `{ "<avatarId>": ["read", ...] }`. Full model, enforcement and error codes:
[Holon Permissions](../../HOLON_PERMISSIONS.md).
