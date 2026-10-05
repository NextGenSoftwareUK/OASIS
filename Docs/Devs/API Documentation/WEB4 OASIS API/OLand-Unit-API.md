# OLand Unit API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Authentication](#authentication)
- [Endpoints](#endpoints)
- [Error Responses](#error-responses)

## Overview

The OLand Unit API provides administrative functionality for managing OLand units within the OASIS ecosystem.

## Authentication

All endpoints require authentication using Bearer tokens:

```http
Authorization: Bearer YOUR_TOKEN
```

## Endpoints

### OLand Unit Management

#### Get All OLand Units
```http
GET /api/admin/olandunit
Authorization: Bearer YOUR_TOKEN
```

#### Get OLand Unit by ID
```http
GET /api/admin/olandunit/{id}
Authorization: Bearer YOUR_TOKEN
```

#### Create OLand Unit
```http
POST /api/admin/olandunit
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "name": "Unit Name",
  "description": "Unit Description",
  "coordinates": {
    "x": 100.0,
    "y": 200.0,
    "z": 0.0
  }
}
```

#### Update OLand Unit
```http
PUT /api/admin/olandunit/{id}
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "name": "Updated Unit Name",
  "description": "Updated Description"
}
```

#### Delete OLand Unit
```http
DELETE /api/admin/olandunit/{id}
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Unit Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "OLand unit not found"
}
```

### Unauthorized Access
```json
{
  "result": null,
  "isError": true,
  "message": "Insufficient permissions for OLand unit management"
}
```

---

## Navigation

**← Previous:** [Telos API](Telos-API.md) | **Next:** [Disabled APIs](../README.md#disabled-apis) →
