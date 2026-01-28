# OAPPs API

## Overview

The OAPPs API provides OASIS Application (OAPP) management for the STAR ecosystem: list installed OAPPs, install, update, uninstall, and stats. OAPP endpoints may be under a different route or disabled on the current deployment—confirm in [Swagger](http://api.oasisweb4.com/swagger/index.html).

**Intended base path:** `/api/oapps` or `/api/oapp`  
**Host:** Same as [WEB4](http://api.oasisweb4.com) or STAR host when deployed.

**Authentication:** Required (Bearer token). Always check **isError** in responses.

**Rate limits:** Same as [WEB4 API](../../reference/rate-limits.md).

---

## Intended endpoints (reference)

When the OAPPs API is enabled, it may expose:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/oapps` or `installed` | List installed OAPPs |
| GET | `/api/oapps/{oappId}` | Get OAPP by ID |
| POST | `/api/oapps` or `install` | Create / install OAPP |
| PUT | `/api/oapps/{oappId}` | Update OAPP |
| DELETE | `/api/oapps/{oappId}` | Uninstall / delete OAPP |
| POST | `/api/oapps/{oappId}/deploy` | Deploy OAPP |
| POST | `/api/oapps/{oappId}/launch` | Launch OAPP |
| POST | `/api/oapps/{oappId}/stop` | Stop OAPP |
| GET | `/api/oapps/stats` | OAPP statistics |

---

## Request / response (typical)

**List OAPPs:**  
`GET /api/oapps` (or `GET /api/oapp/installed` depending on deployment).

**Response:**
```json
{
  "result": {
    "oapps": [
      {
        "id": "oapp_123",
        "name": "My OAPP",
        "version": "1.0",
        "status": "installed",
        "createdAt": "2024-01-20T14:30:00Z"
      }
    ]
  },
  "isError": false,
  "message": "Success"
}
```

---

## Implementation note

The ONODE WebAPI includes an **OAPPController** with routes that may be commented out or under a different prefix. Use Swagger to see which OAPP endpoints are currently available on your API instance.

---

## Related documentation

- [WEB5 STAR API Overview](../overview.md)
- [WEB4 OASIS API Overview](../../web4-oasis-api/overview.md)

---

*Last Updated: January 24, 2026*
