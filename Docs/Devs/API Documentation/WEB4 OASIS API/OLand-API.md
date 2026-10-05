# OLand API

## 📋 **Table of Contents**

- [Overview](#overview)
- [OLand Management](#oland-management)
- [OLand Operations](#oland-operations)
- [OLand Marketplace](#oland-marketplace)
- [Error Responses](#error-responses)

## Overview

The OLand API provides virtual land management for the OASIS ecosystem. It handles land creation, ownership, trading, and development.

## OLand Management

### Get All OLand
```http
GET /api/oland
Authorization: Bearer YOUR_TOKEN
```

### Get OLand by ID
```http
GET /api/oland/{olandId}
Authorization: Bearer YOUR_TOKEN
```

### Create OLand
```http
POST /api/oland
Authorization: Bearer YOUR_TOKEN
```

### Update OLand
```http
PUT /api/oland/{olandId}
Authorization: Bearer YOUR_TOKEN
```

### Delete OLand
```http
DELETE /api/oland/{olandId}
Authorization: Bearer YOUR_TOKEN
```

## OLand Operations

### Purchase OLand
```http
POST /api/oland/{olandId}/purchase
Authorization: Bearer YOUR_TOKEN
```

### Transfer OLand
```http
POST /api/oland/{olandId}/transfer
Authorization: Bearer YOUR_TOKEN
```

### Develop OLand
```http
POST /api/oland/{olandId}/develop
Authorization: Bearer YOUR_TOKEN
```

### Get OLand Ownership
```http
GET /api/oland/{olandId}/ownership
Authorization: Bearer YOUR_TOKEN
```

## OLand Marketplace

### List OLand for Sale
```http
POST /api/oland/{olandId}/list
Authorization: Bearer YOUR_TOKEN
```

### Get OLand Listings
```http
GET /api/oland/marketplace
Authorization: Bearer YOUR_TOKEN
```

### Get OLand Price
```http
GET /api/oland/{olandId}/price
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### OLand Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "OLand not found"
}
```

### Insufficient Funds
```json
{
  "result": null,
  "isError": true,
  "message": "Insufficient funds"
}
```

---

## Navigation

**← Previous:** [Solana API](Solana-API.md) | **Next:** [Cargo API](Cargo-API.md) →
