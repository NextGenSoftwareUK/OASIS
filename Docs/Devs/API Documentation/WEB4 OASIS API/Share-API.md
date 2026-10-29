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

Owners decide, per holon and per field, who may `read`, `write`, `delete`, create children under (`addChildren`) and
change access to (`manage`) it. A grantee is an avatar id, a group id, or `public`. Needs `manage` on the holon (the
creator always has it).

```http
POST /api/share/permissions/{holonId}
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "grants":  { "<grantee>": ["read", "write"], "<other>": [] },
  "inherit": { "<grantee>": ["read"] },
  "fields":  { "public": { "metadata.username": ["read"] } }
}
```

`grants` apply to this holon, `inherit` to it and everything below it, `fields` to single fields (`name`, `description`,
a property, `metadata.<key>`, `metadata.*`, `*`; read, write, delete only). A list of names sets exactly those; an empty
list removes; anything not listed is unchanged.

```http
GET /api/share/permissions/{holonId}
```

Returns the stored `grants`, `inherit` and `fields`.

### Groups

```http
POST /api/share/groups
{ "name": "Speech therapists", "members": ["<avatarId>"] }

POST /api/share/groups/{groupId}/members
{ "add": ["<avatarId>"], "remove": ["<avatarId>"] }
```

Grant to the group's id like an avatar's; changing the members changes everyone's access at once.

Full model, enforcement and error codes: [Holon Permissions](../../HOLON_PERMISSIONS.md).
