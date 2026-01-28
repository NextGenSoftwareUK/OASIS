# Data API

## Overview

The **Data API** is the universal data layer for the OASIS ecosystem. All data operations live under **`/api/data`** and are built on the **holon** model. This page is an overview; detailed holon CRUD and options are in the [Holons API](holons-api.md). File upload and management are covered in the [Files API](files-api.md).

**Base URL:** `/api/data`

**Authentication:** Required (Bearer token) for holon load/save/delete and for file operations.

**Rate Limits:** Same as [WEB4 API](../../reference/rate-limits.md) (e.g. Free: 100 req/min).

**Key Concepts:**
- **Holons** – The core data entity; hierarchical, versioned, with optional child loading and provider selection.
- **Load / Save / Delete** – Holon CRUD plus “load all by type” and “load children for parent.”
- **Provider & HyperDrive** – Optional provider type, auto-failover, auto-replication, auto-load-balance (see [Holons API](holons-api.md)).

---

## What Lives Under `/api/data`

| Area | Description | Doc |
|------|-------------|-----|
| **Holons** | Load holon by ID, load all by type, load children for parent, save holon, delete holon. | [Holons API](holons-api.md) |
| **Files** | File upload and management. | [Files API](files-api.md) |

There is no separate “key/value” API surface; key/value-style data is typically stored in holon metadata or custom holon types.

---

## Quick Reference (Holons)

| Operation | Method | Endpoint (pattern) |
|-----------|--------|---------------------|
| Load holon | GET / POST | `load-holon/{id}` or POST `load-holon` with body |
| Load all holons (by type) | GET / POST | `load-all-holons/{holonType}` or POST `load-all-holons` |
| Load children for parent | GET / POST | `load-holons-for-parent` (POST with body) or GET `load-holons-for-parent/{id}/{holonType}/...` |
| Save holon | POST | `save-holon` |
| Delete holon | DELETE | `delete-holon/{id}` |

All responses use the standard OASIS wrapper: `{ "result": { ... }, "isError": false, "message": null }`. Always check **isError**.

---

## Response Format

Success:
```json
{
  "result": { ... },
  "isError": false,
  "message": "Success"
}
```

Error (often HTTP 200 with body indicating failure):
```json
{
  "result": null,
  "isError": true,
  "message": "Error message",
  "errorCode": "ERROR_CODE"
}
```

---

## Provider Selection & HyperDrive

For holon operations you can:
- Pass **providerType** and **setGlobally** to target a specific storage provider.
- Use **autoFailOverMode**, **autoReplicationMode**, **autoLoadBalanceMode** (and related provider lists) when using the full POST request bodies.

See [Holons API](holons-api.md) for request bodies and [WEB4 Overview](../overview.md) for HyperDrive behaviour.

---

## Related Documentation

- [Holons API](holons-api.md) – Full holon CRUD, options, and request/response details
- [Files API](files-api.md) – File upload and management
- [WEB4 OASIS API Overview](../overview.md) – Providers and HyperDrive

---

*Last Updated: January 24, 2026*
