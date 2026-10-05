# CelestialSpaces API

## 📋 **Table of Contents**

- [Overview](#overview)
- [CelestialSpace Management](#celestialspace-management)
- [CelestialSpace Operations](#celestialspace-operations)
- [Error Responses](#error-responses)

## Overview

The CelestialSpaces API provides space region management for the STAR ecosystem. It handles space sectors, regions, and zones.

## CelestialSpace Management

### Get All CelestialSpaces
```http
GET /api/celestialspaces
Authorization: Bearer YOUR_TOKEN
```

### Get CelestialSpace by ID
```http
GET /api/celestialspaces/{celestialSpaceId}
Authorization: Bearer YOUR_TOKEN
```

### Create CelestialSpace
```http
POST /api/celestialspaces
Authorization: Bearer YOUR_TOKEN
```

### Update CelestialSpace
```http
PUT /api/celestialspaces/{celestialSpaceId}
Authorization: Bearer YOUR_TOKEN
```

### Delete CelestialSpace
```http
DELETE /api/celestialspaces/{celestialSpaceId}
Authorization: Bearer YOUR_TOKEN
```

## CelestialSpace Operations

### Explore CelestialSpace
```http
POST /api/celestialspaces/{celestialSpaceId}/explore
Authorization: Bearer YOUR_TOKEN
```

### Claim CelestialSpace
```http
POST /api/celestialspaces/{celestialSpaceId}/claim
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### CelestialSpace Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "CelestialSpace not found"
}
```

---

## Navigation

**← Previous:** [CelestialBodies API](CelestialBodies-API.md) | **Next:** [NFTs API](NFTs-API.md) →
