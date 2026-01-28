# Inventory API

## Overview

The Inventory API provides virtual item management for the STAR ecosystem: create, store, use, and transfer inventory items. It is part of the WEB5 STAR data structures layer.

**Intended base path:** `/api/inventoryitems` or `/api/inventory`  
**Host:** Same as [WEB4](http://api.oasisweb4.com) or STAR-specific host when deployed. Confirm in [Swagger](http://api.oasisweb4.com/swagger/index.html).

**Authentication:** Required (Bearer token). Always check **isError** in responses.

**Rate limits:** Same as [WEB4 API](../../reference/rate-limits.md).

---

## Intended endpoints (reference)

When the Inventory API is deployed, it typically exposes:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/inventoryitems` | List inventory items |
| GET | `/api/inventoryitems/{itemId}` | Get item by ID |
| POST | `/api/inventoryitems` | Create item |
| PUT | `/api/inventoryitems/{itemId}` | Update item |
| DELETE | `/api/inventoryitems/{itemId}` | Delete item |
| POST | `/api/inventoryitems/{itemId}/use` | Use item |
| POST | `/api/inventoryitems/{itemId}/transfer` | Transfer item |
| GET | `/api/inventoryitems/user/{userId}` | User inventory |

---

## Request / response (typical)

**List items (GET /api/inventoryitems):**

**Response:**
```json
{
  "result": {
    "items": [
      {
        "id": "item_123",
        "name": "Starter Sword",
        "type": "Weapon",
        "quantity": 1,
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

**Use item:**  
`POST /api/inventoryitems/{itemId}/use` — body and response depend on item type; see Swagger.

---

## Related documentation

- [WEB5 STAR API Overview](../overview.md)
- [WEB4 Data / Holons](../../web4-oasis-api/data-storage/holons-api.md) – Core data model

---

*Last Updated: January 24, 2026*
