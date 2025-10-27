# CelestialBodiesMetaData API

## 📋 **Table of Contents**

- [Overview](#overview)
- [MetaData Management](#metadata-management)
- [MetaData Operations](#metadata-operations)
- [Error Responses](#error-responses)

## Overview

The CelestialBodiesMetaData API provides metadata management for celestial bodies in the STAR ecosystem. It handles metadata creation, storage, and retrieval.

## MetaData Management

### Get All CelestialBodiesMetaData
```http
GET /api/celestialbodiesmetadata
Authorization: Bearer YOUR_TOKEN
```

### Get CelestialBodiesMetaData by ID
```http
GET /api/celestialbodiesmetadata/{metadataId}
Authorization: Bearer YOUR_TOKEN
```

### Create CelestialBodiesMetaData
```http
POST /api/celestialbodiesmetadata
Authorization: Bearer YOUR_TOKEN
```

### Update CelestialBodiesMetaData
```http
PUT /api/celestialbodiesmetadata/{metadataId}
Authorization: Bearer YOUR_TOKEN
```

### Delete CelestialBodiesMetaData
```http
DELETE /api/celestialbodiesmetadata/{metadataId}
Authorization: Bearer YOUR_TOKEN
```

## MetaData Operations

### Get MetaData by CelestialBody
```http
GET /api/celestialbodiesmetadata/celestialbody/{celestialBodyId}
Authorization: Bearer YOUR_TOKEN
```

### Update MetaData
```http
PUT /api/celestialbodiesmetadata/{metadataId}/update
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### MetaData Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "CelestialBodiesMetaData not found"
}
```

---

## Navigation

**← Previous:** [InventoryItems API](InventoryItems-API.md) | **Next:** [HolonsMetaData API](HolonsMetaData-API.md) →
