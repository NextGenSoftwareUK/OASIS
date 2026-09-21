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
  "continuousDurationSeconds": 10,
  "gameSource": "Our World"
}
```

WEB5 validates the authored trigger type, observation time, radius and required continuous duration. It applies the shared GeoNFT/GeoHotSpot quantity and cooldown policy, records matching `LinkedGeoHotSpotId` / `NeedToGoToGeoHotSpots` progress, grants configured inventory and GeoNFT rewards, and returns the resulting quest transitions and presentation events. Repeating an accepted idempotency key returns the original result without incrementing counts or replaying grants.

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
    "crossGameEvents": [],
    "refreshedQuests": []
  },
  "isError": false,
  "message": "GeoHotSpot trigger accepted."
}
```

`gameSource` selects the corresponding quest requirement dictionary row. A linked objective records the accepted hotspot ID in `GeoHotSpotsArrived`. Explicit ID lists require every listed hotspot; a numeric requirement such as `["3"]` requires three distinct accepted hotspot IDs. `ObjectiveCompletionOrder.InOrder` limits progress to the current objective, while `AnyOrder` permits every matching incomplete objective.

The endpoint can return these validation errors: missing authentication, missing hotspot/idempotency key, inactive or missing hotspot, trigger-type mismatch, stale/future evidence, missing/out-of-radius coordinates, insufficient dwell or gaze duration, exhausted global/player allocation, another-player exclusivity, active cooldown, invalid persisted trigger state, quest update failure, or reward grant failure.

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
