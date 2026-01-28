# Quests API

## Overview

The Quests API provides quest management for the STAR ecosystem: create, assign, complete, and analyze quests with objectives, rewards, and multiple quest types (Main, Side, Daily, Weekly, Event). It is part of the WEB5 STAR gamification layer.

**Intended base path:** `/api/quests`  
**Host:** Same as [WEB4](http://api.oasisweb4.com) or STAR-specific host when deployed. Confirm in [Swagger](http://api.oasisweb4.com/swagger/index.html).

**Authentication:** Required (Bearer token). Always check **isError** in responses.

**Rate limits:** Same as [WEB4 API](../../reference/rate-limits.md).

---

## Intended endpoints (reference)

When the Quests API is deployed, it typically exposes:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/quests` | List quests (query: limit, offset, type, status, difficulty, sortBy, sortOrder) |
| GET | `/api/quests/{id}` | Get quest by ID |
| POST | `/api/quests` | Create quest |
| PUT | `/api/quests/{id}` | Update quest |
| DELETE | `/api/quests/{id}` | Delete quest |
| POST | `/api/quests/start` | Start quest |
| POST | `/api/quests/complete` | Complete quest |
| GET | `/api/quests/{id}/objectives` | Quest objectives |
| GET | `/api/quests/{id}/rewards` | Quest rewards |
| GET | `/api/quests/stats` | Quest statistics |

---

## Request / response (typical)

**List quests (GET /api/quests):**

Query: `limit`, `offset`, `type` (Main, Side, Daily, Weekly, Event), `status`, `difficulty`, `sortBy`, `sortOrder`.

**Response:**
```json
{
  "result": {
    "quests": [
      {
        "id": "quest_123",
        "name": "STAR Explorer",
        "description": "Complete your first tasks",
        "type": "Main",
        "status": "Active",
        "difficulty": "Easy",
        "objectives": [ ... ],
        "rewards": { "karma": 100, "experience": 50 },
        "createdAt": "2024-01-20T14:30:00Z"
      }
    ],
    "totalCount": 1,
    "limit": 50,
    "offset": 0
  },
  "isError": false,
  "message": "Success"
}
```

---

## Related documentation

- [WEB5 STAR API Overview](../overview.md)
- [Missions API](missions-api.md)
- [WEB5 STAR Overview](../overview.md) – Egg quests at `/api/eggs/get-current-egg-quests`; Map at `/api/map` (live)

---

*Last Updated: January 24, 2026*
