# ZomesMetaData API

## 📋 **Table of Contents**

- [Overview](#overview)
- [MetaData Management](#metadata-management)
- [MetaData Operations](#metadata-operations)
- [Error Responses](#error-responses)

## Overview

The ZomesMetaData API provides metadata management for zomes in the STAR ecosystem. It handles metadata creation, storage, and retrieval for application modules.

## MetaData Management

### Get All ZomesMetaData
```http
GET /api/zomesmetadata
Authorization: Bearer YOUR_TOKEN
```

### Get ZomesMetaData by ID
```http
GET /api/zomesmetadata/{metadataId}
Authorization: Bearer YOUR_TOKEN
```

### Create ZomesMetaData
```http
POST /api/zomesmetadata
Authorization: Bearer YOUR_TOKEN
```

### Update ZomesMetaData
```http
PUT /api/zomesmetadata/{metadataId}
Authorization: Bearer YOUR_TOKEN
```

### Delete ZomesMetaData
```http
DELETE /api/zomesmetadata/{metadataId}
Authorization: Bearer YOUR_TOKEN
```

## MetaData Operations

### Get MetaData by Zome
```http
GET /api/zomesmetadata/zome/{zomeId}
Authorization: Bearer YOUR_TOKEN
```

### Update MetaData
```http
PUT /api/zomesmetadata/{metadataId}/update
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### MetaData Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "ZomesMetaData not found"
}
```

---

## Navigation

**← Previous:** [HolonsMetaData API](HolonsMetaData-API.md) | **Next:** [Back to WEB5 STAR API](../README.md) →
