# HolonsMetaData API

## 📋 **Table of Contents**

- [Overview](#overview)
- [MetaData Management](#metadata-management)
- [MetaData Operations](#metadata-operations)
- [Error Responses](#error-responses)

## Overview

The HolonsMetaData API provides metadata management for holons in the STAR ecosystem. It handles metadata creation, storage, and retrieval.

## MetaData Management

### Get All HolonsMetaData
```http
GET /api/holonsmetadata
Authorization: Bearer YOUR_TOKEN
```

### Get HolonsMetaData by ID
```http
GET /api/holonsmetadata/{metadataId}
Authorization: Bearer YOUR_TOKEN
```

### Create HolonsMetaData
```http
POST /api/holonsmetadata
Authorization: Bearer YOUR_TOKEN
```

### Update HolonsMetaData
```http
PUT /api/holonsmetadata/{metadataId}
Authorization: Bearer YOUR_TOKEN
```

### Delete HolonsMetaData
```http
DELETE /api/holonsmetadata/{metadataId}
Authorization: Bearer YOUR_TOKEN
```

## MetaData Operations

### Get MetaData by Holon
```http
GET /api/holonsmetadata/holon/{holonId}
Authorization: Bearer YOUR_TOKEN
```

### Update MetaData
```http
PUT /api/holonsmetadata/{metadataId}/update
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### MetaData Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "HolonsMetaData not found"
}
```

---

## Navigation

**← Previous:** [CelestialBodiesMetaData API](CelestialBodiesMetaData-API.md) | **Next:** [ZomesMetaData API](ZomesMetaData-API.md) →
