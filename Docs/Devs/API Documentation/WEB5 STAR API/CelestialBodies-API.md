# CelestialBodies API

## 📋 **Table of Contents**

- [Overview](#overview)
- [CelestialBody Management](#celestialbody-management)
- [CelestialBody Operations](#celestialbody-operations)
- [CelestialBody Analytics](#celestialbody-analytics)
- [Error Responses](#error-responses)

## Overview

The CelestialBodies API provides celestial body management for the STAR ecosystem. It handles planets, stars, moons, and other celestial objects.

## CelestialBody Management

### Get All CelestialBodies
```http
GET /api/celestialbodies
Authorization: Bearer YOUR_TOKEN
```

### Get CelestialBody by ID
```http
GET /api/celestialbodies/{celestialBodyId}
Authorization: Bearer YOUR_TOKEN
```

### Create CelestialBody
```http
POST /api/celestialbodies
Authorization: Bearer YOUR_TOKEN
```

### Update CelestialBody
```http
PUT /api/celestialbodies/{celestialBodyId}
Authorization: Bearer YOUR_TOKEN
```

### Delete CelestialBody
```http
DELETE /api/celestialbodies/{celestialBodyId}
Authorization: Bearer YOUR_TOKEN
```

## CelestialBody Operations

### Explore CelestialBody
```http
POST /api/celestialbodies/{celestialBodyId}/explore
Authorization: Bearer YOUR_TOKEN
```

### Colonize CelestialBody
```http
POST /api/celestialbodies/{celestialBodyId}/colonize
Authorization: Bearer YOUR_TOKEN
```

### Get CelestialBody Resources
```http
GET /api/celestialbodies/{celestialBodyId}/resources
Authorization: Bearer YOUR_TOKEN
```

## CelestialBody Analytics

### Get CelestialBody Stats
```http
GET /api/celestialbodies/stats
Authorization: Bearer YOUR_TOKEN
```

### Get CelestialBody Activity
```http
GET /api/celestialbodies/{celestialBodyId}/activity
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### CelestialBody Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "CelestialBody not found"
}
```

### Colonization Failed
```json
{
  "result": null,
  "isError": true,
  "message": "Colonization failed"
}
```

---

## Navigation

**← Previous:** [Eggs API](Eggs-API.md) | **Next:** [CelestialSpaces API](CelestialSpaces-API.md) →
