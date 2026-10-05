# Zomes API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Zome Management](#zome-management)
- [Zome Operations](#zome-operations)
- [Error Responses](#error-responses)

## Overview

The Zomes API provides zome (application module) management for the STAR ecosystem. It handles zomes as reusable application components.

## Zome Management

### Get All Zomes
```http
GET /api/zomes
Authorization: Bearer YOUR_TOKEN
```

### Get Zome by ID
```http
GET /api/zomes/{zomeId}
Authorization: Bearer YOUR_TOKEN
```

### Create Zome
```http
POST /api/zomes
Authorization: Bearer YOUR_TOKEN
```

### Update Zome
```http
PUT /api/zomes/{zomeId}
Authorization: Bearer YOUR_TOKEN
```

### Delete Zome
```http
DELETE /api/zomes/{zomeId}
Authorization: Bearer YOUR_TOKEN
```

## Zome Operations

### Deploy Zome
```http
POST /api/zomes/{zomeId}/deploy
Authorization: Bearer YOUR_TOKEN
```

### Execute Zome Function
```http
POST /api/zomes/{zomeId}/execute
Authorization: Bearer YOUR_TOKEN
```

### Get Zome Functions
```http
GET /api/zomes/{zomeId}/functions
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Zome Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Zome not found"
}
```

---

## Navigation

**← Previous:** [Holons API](Holons-API.md) | **Next:** [Chapters API](Chapters-API.md) →
