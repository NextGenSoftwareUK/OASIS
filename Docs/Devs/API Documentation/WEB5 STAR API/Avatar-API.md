# Avatar API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Avatar Management](#avatar-management)
- [Avatar Operations](#avatar-operations)
- [Error Responses](#error-responses)

## Overview

The Avatar API provides avatar management for the STAR ecosystem. It handles avatar creation, customization, and profile management.

## Avatar Management

### Get All Avatars
```http
GET /api/avatar
Authorization: Bearer YOUR_TOKEN
```

### Get Avatar by ID
```http
GET /api/avatar/{avatarId}
Authorization: Bearer YOUR_TOKEN
```

### Create Avatar
```http
POST /api/avatar
Authorization: Bearer YOUR_TOKEN
```

### Update Avatar
```http
PUT /api/avatar/{avatarId}
Authorization: Bearer YOUR_TOKEN
```

### Delete Avatar
```http
DELETE /api/avatar/{avatarId}
Authorization: Bearer YOUR_TOKEN
```

## Avatar Operations

### Customize Avatar
```http
PUT /api/avatar/{avatarId}/customize
Authorization: Bearer YOUR_TOKEN
```

### Get Avatar Stats
```http
GET /api/avatar/{avatarId}/stats
Authorization: Bearer YOUR_TOKEN
```

### Get Avatar Inventory
```http
GET /api/avatar/{avatarId}/inventory
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Avatar Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Avatar not found"
}
```

---

## Navigation

**← Previous:** [STAR API](STAR-API.md) | **Next:** [Missions API](Missions-API.md) →
