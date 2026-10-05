# STAR API

## 📋 **Table of Contents**

- [Overview](#overview)
- [STAR Management](#star-management)
- [STAR Operations](#star-operations)
- [Error Responses](#error-responses)

## Overview

The STAR API provides core STAR ODK functionality for the STAR ecosystem. It handles STAR creation, compilation, and deployment.

## STAR Management

### Get All STARs
```http
GET /api/star
Authorization: Bearer YOUR_TOKEN
```

### Get STAR by ID
```http
GET /api/star/{starId}
Authorization: Bearer YOUR_TOKEN
```

### Create STAR
```http
POST /api/star
Authorization: Bearer YOUR_TOKEN
```

### Update STAR
```http
PUT /api/star/{starId}
Authorization: Bearer YOUR_TOKEN
```

### Delete STAR
```http
DELETE /api/star/{starId}
Authorization: Bearer YOUR_TOKEN
```

## STAR Operations

### Compile STAR
```http
POST /api/star/{starId}/compile
Authorization: Bearer YOUR_TOKEN
```

### Deploy STAR
```http
POST /api/star/{starId}/deploy
Authorization: Bearer YOUR_TOKEN
```

### Get STAR Status
```http
GET /api/star/{starId}/status
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### STAR Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "STAR not found"
}
```

---

## Navigation

**← Previous:** [InventoryItems API](InventoryItems-API.md) | **Next:** [Avatar API](Avatar-API.md) →
