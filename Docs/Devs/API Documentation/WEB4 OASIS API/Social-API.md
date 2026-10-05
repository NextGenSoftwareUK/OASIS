# Social API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Social Management](#social-management)
- [Social Operations](#social-operations)
- [Error Responses](#error-responses)

## Overview

The Social API provides social features for the OASIS ecosystem. It handles social connections, interactions, and community features.

## Social Management

### Get Social Connections
```http
GET /api/social/connections
Authorization: Bearer YOUR_TOKEN
```

### Get Social Feed
```http
GET /api/social/feed
Authorization: Bearer YOUR_TOKEN
```

### Get Social Profile
```http
GET /api/social/profile/{userId}
Authorization: Bearer YOUR_TOKEN
```

## Social Operations

### Add Connection
```http
POST /api/social/connect
Authorization: Bearer YOUR_TOKEN
```

### Remove Connection
```http
POST /api/social/disconnect
Authorization: Bearer YOUR_TOKEN
```

### Share Content
```http
POST /api/social/share
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### User Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "User not found"
}
```

---

## Navigation

**← Previous:** [Gifts API](Gifts-API.md) | **Next:** [Video API](Video-API.md) →
