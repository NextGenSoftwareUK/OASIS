# Data API – Holon operations

## Overview

The **Data API** exposes holon CRUD and query operations. All holon endpoints live under **`/api/data`**. Use them to load, save, delete, and list holons and their children across OASIS storage providers.

**Base URL:** `http://api.oasisweb4.com/api/data`

**Authentication:** Required (Bearer token). Obtain via [Avatar authenticate](../../getting-started/authentication.md).

**Rate limits:** Same as [WEB4 API](../../reference/rate-limits.md) (e.g. Free: 100 req/min).

<Info>
  **Concept first.** If you're new to holons, read [What are Holons?](../../concepts/holons.md) and [How to use holons](../../guides/using-holons.md) before this reference.
</Info>

---

## Quick reference

| Operation | Method | Endpoint (pattern) |
|-----------|--------|---------------------|
| Load holon by Id | GET / POST | `load-holon/{id}` or POST with body |
| Load all holons (by type) | GET / POST | `load-all-holons/{holonType}` or POST with body |
| Load children of parent | GET / POST | `load-holons-for-parent` (POST with body) |
| Save holon | POST | `save-holon` |
| Delete holon | DELETE | `delete-holon/{id}` |

All responses use the standard OASIS wrapper: `{ "result": { ... }, "isError": false, "message": null }`. Always check **isError**.

---

## Load holon

Load a single holon by Id, with optional child loading and provider options.

### Endpoints

| Method | Endpoint | Use when |
|--------|----------|----------|
| GET | `/api/data/load-holon/{id}` | Simple load with defaults (loadChildren=true, recursive=true). |
| GET | `/api/data/load-holon/{id}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}` | Load with basic options. |
| GET | `.../{id}/.../{version}/{providerType}/{setGlobally}` | Same + provider selection. |
| POST | `/api/data/load-holon` | Full control via request body. |

### POST request body (LoadHolonRequest)

| Field | Type | Default | Description |
|-------|------|---------|--------------|
| id | GUID | required | Holon Id to load. |
| loadChildren | bool | true | Load child holons. |
| recursive | bool | true | Load descendants recursively. |
| maxChildDepth | int | 0 | Max depth (0 = unlimited). |
| continueOnError | bool | true | Keep loading if a child fails. |
| loadChildrenFromProvider | bool | false | Load children from provider (vs cache). |
| childHolonType | string | "All" | Filter children by type (e.g. "Mission"). |
| version | int | 0 | Version to load (0 = latest). |
| providerType | string | - | e.g. MongoDBOASIS, SolanaOASIS. |
| setGlobally | bool | false | Use this provider for future requests. |
| autoReplicationMode, autoFailOverMode, autoLoadBalanceMode | string | "DEFAULT" | HyperDrive behaviour. |
| showDetailedSettings | bool | false | Include provider lists in response. |

### Response

```json
{
  "result": {
    "result": {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "name": "My holon",
      "description": "Description",
      "holonType": 40,
      "parentHolonId": "00000000-0000-0000-0000-000000000000",
      "metadata": {},
      "providerUniqueStorageKey": {},
      "providerMetaData": {},
      "version": 1,
      "isActive": true,
      "createdDate": "2024-01-15T10:30:00Z",
      "modifiedDate": "2024-01-15T10:30:00Z"
    }
  },
  "isError": false,
  "message": null
}
```

### Code example

```http
POST http://api.oasisweb4.com/api/data/load-holon
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "loadChildren": true,
  "recursive": true,
  "maxChildDepth": 0,
  "continueOnError": true,
  "version": 0
}
```

---

## Load all holons

Load all holons of a given type (or all types).

### Endpoints

| Method | Endpoint | Notes |
|--------|----------|--------|
| GET | `/api/data/load-all-holons/{holonType}` | Simple: all of one type. |
| GET | `/api/data/load-all-holons/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}` | With options. |
| GET | `.../{version}/{providerType}/{setGlobally}` | + provider. |
| POST | `/api/data/load-all-holons` | Full control via body (LoadAllHolonsRequest). |

**holonType** examples: `All`, `Mission`, `Quest`, `Avatar`, `Holon`, `Web3NFT`, etc. (see [HolonType](https://github.com/NextGenSoftwareUK/OASIS/blob/master/OASIS%20Architecture/NextGenSoftware.OASIS.API.Core/Enums/HolonType.cs)).

### POST request body (LoadAllHolonsRequest)

Same shape as base load options: **holonType**, **childHolonType**, **loadChildren**, **recursive**, **maxChildDepth**, **continueOnError**, **version**, **providerType**, **setGlobally**, HyperDrive options, **showDetailedSettings**.

### Response

```json
{
  "result": {
    "result": [
      {
        "id": "...",
        "name": "...",
        "holonType": 12
      }
    ]
  },
  "isError": false,
  "message": null
}
```

---

## Load holons for parent

Load all child holons of a parent, optionally filtered by type.

### Endpoints

| Method | Endpoint | Notes |
|--------|----------|--------|
| POST | `/api/data/load-holons-for-parent` | Recommended: full body. |
| GET | `/api/data/load-holons-for-parent/{id}/{holonType}` | Simple. |
| GET | `.../{id}/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}` | With options. |
| GET | `.../.../{version}/{providerType}/{setGlobally}` | + provider. |

### POST request body (LoadHolonsForParentRequest)

| Field | Type | Default | Description |
|-------|------|---------|--------------|
| id | GUID | required | Parent holon Id. |
| holonType | string | "All" | Filter children by type. |
| loadChildren | bool | true | Load each child’s children. |
| recursive | bool | true | Recursive. |
| maxChildDepth | int | 0 | Max depth. |
| continueOnError | bool | true | Continue on child error. |
| version | int | 0 | Version. |
| providerType, setGlobally, HyperDrive options | - | - | Same as load-holon. |

### Response

Same as load-all-holons: array of holons.

### Code example

```http
POST http://api.oasisweb4.com/api/data/load-holons-for-parent
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "id": "PARENT_HOLON_ID",
  "holonType": "Mission",
  "loadChildren": true,
  "recursive": false,
  "maxChildDepth": 0,
  "continueOnError": true,
  "version": 0
}
```

---

## Save holon

Create or update a holon. Use Id `00000000-0000-0000-0000-000000000000` (or omit) for create; existing Id for update.

### Endpoints

| Method | Endpoint | Notes |
|--------|----------|--------|
| POST | `/api/data/save-holon` | Recommended: body with holon + options. |
| POST | `/api/data/save-holon/{saveChildren}/{recursive}/{maxChildDepth}/{continueOnError}` | With flags in path. |
| POST | `.../.../{providerType}/{setGlobally}` | + provider. |

### POST request body (SaveHolonRequest)

| Field | Type | Default | Description |
|-------|------|---------|--------------|
| holon | object | required | Holon to save. |
| saveChildren | bool | true | Save child holons. |
| recursive | bool | true | Save descendants. |
| maxChildDepth | int | 0 | Max depth. |
| continueOnError | bool | true | Continue if a child fails. |
| providerType | string | - | e.g. MongoDBOASIS. |
| setGlobally | bool | false | Use provider for future requests. |
| onChainProvider, offChainProvider | string | - | For hybrid on/off-chain. |
| HyperDrive options, showDetailedSettings | - | - | Same as load. |

**Holon object (minimal for create):**

| Field | Type | Description |
|-------|------|--------------|
| id | GUID | `00000000-0000-0000-0000-000000000000` for new. |
| name | string | Name. |
| description | string | Description. |
| holonType | int | e.g. 40 = Holon. |
| metadata | object | Key-value metadata. |
| isActive | bool | Active flag. |
| parentHolonId | GUID | Optional parent. |

### Response

```json
{
  "result": {
    "result": {
      "id": "NEW_OR_EXISTING_ID",
      "name": "My holon",
      "providerUniqueStorageKey": { "MongoDBOASIS": "..." }
    }
  },
  "isError": false,
  "message": null
}
```

### Code example

```http
POST http://api.oasisweb4.com/api/data/save-holon
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "holon": {
    "id": "00000000-0000-0000-0000-000000000000",
    "name": "New holon",
    "description": "Created via API",
    "holonType": 40,
    "metadata": {},
    "isActive": true
  },
  "saveChildren": false
}
```

---

## Delete holon

Delete a holon by Id. Soft delete is supported.

### Endpoints

| Method | Endpoint | Notes |
|--------|----------|--------|
| DELETE | `/api/data/delete-holon/{id}` | Default soft delete. |
| DELETE | `/api/data/delete-holon/{id}/{softDelete}` | softDelete true/false. |
| DELETE | `/api/data/delete-holon/{id}/{softDelete}/{providerType}/{setGlobally}` | + provider. |
| POST | `/api/data/delete-holon` | Body: DeleteHolonRequest (id, softDelete, provider, etc.). |

### Response

```json
{
  "result": { "result": true },
  "isError": false,
  "message": null
}
```

---

## Files and raw data

The Data controller also exposes:

- **save-file** / **load-file** — Store and retrieve file data (by Id).
- **save-data** / **load-data** — Key/value-style storage.

These operate on the same provider layer as holons but are separate from the holon model. See Swagger for request/response shapes: [Swagger UI](http://api.oasisweb4.com/swagger/index.html) → `api/data`.

---

## Error handling

Responses may return HTTP 200 with `isError: true`. Always check the body:

```json
{
  "result": null,
  "isError": true,
  "message": "Error loading holon: ...",
  "errorCode": "NOT_FOUND"
}
```

Common cases: **UNAUTHORIZED** (missing/invalid token), **NOT_FOUND** (invalid Id), **VALIDATION_ERROR** (bad request). See [Error codes](../../reference/error-codes.md).

---

## Related documentation

- [What are Holons?](../../concepts/holons.md)
- [How to use holons](../../guides/using-holons.md)
- [Data & Storage overview](../overview.md#2-data--storage)
- [Getting started / Authentication](../../getting-started/authentication.md)

---

*Last updated: January 2026*
