# GeoHotSpots API

## 📋 **Table of Contents**

- [Overview](#overview)
- [GeoHotSpot Management](#geohotspot-management)
- [GeoHotSpot Operations](#geohotspot-operations)
- [Error Responses](#error-responses)

## Overview

The GeoHotSpots API provides location-based hotspot management for the STAR ecosystem. It handles geo-located points of interest.

## GeoHotSpot Management

### Get All GeoHotSpots
```http
GET /api/geohotspots
Authorization: Bearer YOUR_TOKEN
```

### Get GeoHotSpot by ID
```http
GET /api/geohotspots/{geoHotSpotId}
Authorization: Bearer YOUR_TOKEN
```

### Create GeoHotSpot
```http
POST /api/geohotspots
Authorization: Bearer YOUR_TOKEN
```

### Update GeoHotSpot
```http
PUT /api/geohotspots/{geoHotSpotId}
Authorization: Bearer YOUR_TOKEN
```

### Delete GeoHotSpot
```http
DELETE /api/geohotspots/{geoHotSpotId}
Authorization: Bearer YOUR_TOKEN
```

## GeoHotSpot Operations

### Visit GeoHotSpot
```http
POST /api/geohotspots/{geoHotSpotId}/visit
Authorization: Bearer YOUR_TOKEN
```

### Get Nearby GeoHotSpots
```http
GET /api/geohotspots/nearby
Authorization: Bearer YOUR_TOKEN
```

### Get GeoHotSpot Activity
```http
GET /api/geohotspots/{geoHotSpotId}/activity
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### GeoHotSpot Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "GeoHotSpot not found"
}
```

---

## Navigation

**← Previous:** [GeoNFTs API](GeoNFTs-API.md) | **Next:** [Holons API](Holons-API.md) →
