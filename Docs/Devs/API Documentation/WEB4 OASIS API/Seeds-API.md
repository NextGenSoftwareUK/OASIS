# Seeds API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Seeds Management](#seeds-management)
- [Seeds Operations](#seeds-operations)
- [Error Responses](#error-responses)

## Overview

The Seeds API provides SEEDS blockchain integration for the OASIS ecosystem. It handles SEEDS transactions, karma, and community features.

## Seeds Management

### Get Seeds Balance
```http
GET /api/seeds/balance/{accountName}
Authorization: Bearer YOUR_TOKEN
```

### Get Seeds Organizations
```http
GET /api/seeds/organizations
Authorization: Bearer YOUR_TOKEN
```

### Get Seeds Organization
```http
GET /api/seeds/organization/{organizationName}
Authorization: Bearer YOUR_TOKEN
```

## Seeds Operations

### Pay with Seeds
```http
POST /api/seeds/pay
Authorization: Bearer YOUR_TOKEN
```

### Donate with Seeds
```http
POST /api/seeds/donate
Authorization: Bearer YOUR_TOKEN
```

### Reward with Seeds
```http
POST /api/seeds/reward
Authorization: Bearer YOUR_TOKEN
```

### Send Seeds Invite
```http
POST /api/seeds/invite
Authorization: Bearer YOUR_TOKEN
```

### Accept Seeds Invite
```http
POST /api/seeds/accept
Authorization: Bearer YOUR_TOKEN
```

### Generate QR Code
```http
GET /api/seeds/qrcode/{accountName}
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Account Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "SEEDS account not found"
}
```

---

## Navigation

**← Previous:** [Share API](Share-API.md) | **Next:** [Telos API](Telos-API.md) →
