# Holons API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Holon Management](#holon-management)
- [Holon Operations](#holon-operations)
- [Error Responses](#error-responses)

## Overview

The Holons API provides holon (data object) management for the STAR ecosystem. It handles holons as universal data containers.

## Holon Management

### Get All Holons
```http
GET /api/holons
Authorization: Bearer YOUR_TOKEN
```

### Get Holon by ID
```http
GET /api/holons/{holonId}
Authorization: Bearer YOUR_TOKEN
```

### Create Holon
```http
POST /api/holons
Authorization: Bearer YOUR_TOKEN
```

### Update Holon
```http
PUT /api/holons/{holonId}
Authorization: Bearer YOUR_TOKEN
```

### Delete Holon
```http
DELETE /api/holons/{holonId}
Authorization: Bearer YOUR_TOKEN
```

## Holon Operations

### Link Holons
```http
POST /api/holons/{holonId}/link
Authorization: Bearer YOUR_TOKEN
```

### Get Holon Children
```http
GET /api/holons/{holonId}/children
Authorization: Bearer YOUR_TOKEN
```

### Get Holon Parent
```http
GET /api/holons/{holonId}/parent
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Holon Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Holon not found"
}
```

---

## Navigation

**← Previous:** [GeoHotSpots API](GeoHotSpots-API.md) | **Next:** [Zomes API](Zomes-API.md) →
