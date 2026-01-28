# Missions API

## Overview

The Missions API provides mission management for the STAR ecosystem: create, assign, complete, and analyze missions with objectives, rewards, and leaderboards. It is part of the WEB5 STAR gamification layer.

**Intended base path:** `/api/missions`  
**Host:** Same as [WEB4](http://api.oasisweb4.com) or STAR-specific host when deployed. Confirm in [Swagger](http://api.oasisweb4.com/swagger/index.html).

**Authentication:** Required (Bearer token). Always check **isError** in responses.

**Rate limits:** Same as [WEB4 API](../../reference/rate-limits.md).

---

## Intended endpoints (reference)

When the Missions API is deployed, it typically exposes:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/missions` | List missions (query: limit, offset, type, status, difficulty, sortBy, sortOrder) |
| GET | `/api/missions/{id}` | Get mission by ID |
| POST | `/api/missions` | Create mission |
| PUT | `/api/missions/{id}` | Update mission |
| DELETE | `/api/missions/{id}` | Delete mission |
| GET | `/api/missions/by-type/{type}` | Filter by type (Quest, Challenge, Task, Achievement) |
| GET | `/api/missions/by-status/{status}` | Filter by status |
| GET | `/api/missions/search` | Search missions |
| POST | `/api/missions/create` | Create mission (alternate) |
| GET | `/api/missions/{id}/load` | Load mission |
| GET | `/api/missions/load-all-for-avatar` | Load all missions for avatar |
| POST | `/api/missions/{id}/publish` | Publish mission |
| POST | `/api/missions/{id}/complete` | Complete mission |
| GET | `/api/missions/{id}/leaderboard` | Mission leaderboard |
| GET | `/api/missions/{id}/rewards` | Mission rewards |
| GET | `/api/missions/stats` | Mission statistics |

---

## Request / response (typical)

**List missions (GET /api/missions):**

Query: `limit`, `offset`, `type`, `status`, `difficulty`, `sortBy`, `sortOrder`.

**Response:**
```json
{
  "result": {
    "missions": [
      {
        "id": "mission_123",
        "name": "STAR Explorer",
        "description": "Explore the STAR platform",
        "type": "Quest",
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
- [Quests API](quests-api.md)
- [WEB5 STAR Overview](../overview.md) – Live STAR endpoints (Competition at `/api/competition`, Eggs at `/api/eggs`)

---

*Last Updated: January 24, 2026*
