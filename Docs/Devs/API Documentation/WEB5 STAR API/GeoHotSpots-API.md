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

### Trigger GeoHotSpot
```http
POST /api/geohotspots/{geoHotSpotId}/trigger
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "idempotencyKey": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "triggerType": "WhenAtGeoLocationForXSeconds",
  "observedAtUtc": "2026-09-19T18:30:00Z",
  "latitude": 31.5499,
  "longitude": 74.2778,
  "accuracyMetres": 4.5,
  "continuousDurationSeconds": 10
}
```

WEB5 validates the authored trigger type, observation time, radius and required continuous duration. It then applies the shared GeoNFT/GeoHotSpot quantity and cooldown policy and persists the accepted idempotency key and activity counts before returning success. Repeating an accepted idempotency key returns the original result without incrementing counts.

```json
{
  "result": {
    "geoHotSpotId": "...",
    "idempotencyKey": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "acceptedAtUtc": "2026-09-19T18:30:01Z",
    "nextEligibleAtUtc": "2026-09-19T18:31:01Z",
    "playerTriggerCount": 1,
    "globalTriggerCount": 1,
    "inventoryRewardIds": [],
    "geoNFTRewardIds": [],
    "questTransitions": [],
    "crossGameEvents": []
  },
  "isError": false,
  "message": "GeoHotSpot trigger accepted."
}
```

### Get Nearby GeoHotSpots
```http
GET /api/geohotspots/nearby
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
