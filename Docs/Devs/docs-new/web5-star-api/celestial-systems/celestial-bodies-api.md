# Celestial Bodies API

## Overview

The Celestial Bodies API provides celestial body management for the STAR ecosystem: planets, stars, moons, and other virtual world objects. Create, explore, colonize, and analyze celestial bodies and their resources. It is part of the WEB5 STAR metaverse layer.

**Intended base path:** `/api/celestialbodies`  
**Host:** Same as [WEB4](http://api.oasisweb4.com) or STAR-specific host when deployed. Confirm in [Swagger](http://api.oasisweb4.com/swagger/index.html).

**Authentication:** Required (Bearer token). Always check **isError** in responses.

**Rate limits:** Same as [WEB4 API](../../reference/rate-limits.md).

---

## Intended endpoints (reference)

When the Celestial Bodies API is deployed, it typically exposes:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/celestialbodies` | List celestial bodies |
| GET | `/api/celestialbodies/{celestialBodyId}` | Get by ID |
| POST | `/api/celestialbodies` | Create celestial body |
| PUT | `/api/celestialbodies/{celestialBodyId}` | Update |
| DELETE | `/api/celestialbodies/{celestialBodyId}` | Delete |
| GET | `/api/celestialbodies/by-type/{type}` | Filter by type |
| GET | `/api/celestialbodies/in-space/{spaceId}` | In a given space |
| GET | `/api/celestialbodies/search` | Search |
| POST | `/api/celestialbodies/{celestialBodyId}/explore` | Explore |
| POST | `/api/celestialbodies/{celestialBodyId}/colonize` | Colonize |
| GET | `/api/celestialbodies/{celestialBodyId}/resources` | Get resources |
| GET | `/api/celestialbodies/stats` | Celestial body stats |
| GET | `/api/celestialbodies/{celestialBodyId}/activity` | Activity |

---

## Request / response (typical)

**List celestial bodies (GET /api/celestialbodies):**

**Response:**
```json
{
  "result": {
    "celestialBodies": [
      {
        "id": "body_123",
        "name": "New Horizon",
        "type": "Planet",
        "spaceId": "space_456",
        "metadata": { ... },
        "createdAt": "2024-01-20T14:30:00Z"
      }
    ],
    "totalCount": 1
  },
  "isError": false,
  "message": "Success"
}
```

---

## Related documentation

- [WEB5 STAR API Overview](../overview.md)
- [WEB4 Data / Holons](../../web4-oasis-api/data-storage/holons-api.md) – Core data model used by STAR

---

*Last Updated: January 24, 2026*
