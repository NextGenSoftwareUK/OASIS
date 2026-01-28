# Search API

## Overview

The Search API provides universal search across OASIS data. You pass search parameters (query, filters, options) and optionally a provider type; results are returned in the standard `ISearchResults` format.

**Base URL:** `/api/search`

**Authentication:** Depends on implementation; check Swagger. Unauthenticated calls may return **HTTP 200** with `isError: true`—always check the response body.

**Rate Limits:** Same as [WEB4 API](../../reference/rate-limits.md).

---

## Endpoints

### Search

**Endpoint:** `GET /api/search/{searchParams}`

Search params are typically passed as query parameters (e.g. query string, filters, sort, limit). Exact shape is defined by `SearchParams` in the API (see Swagger).

**Provider variant:** `GET /api/search/{searchParams}/{providerType}/{setGlobally}`

- **providerType** – Force a specific storage provider for the search.
- **setGlobally** – If `true`, use this provider for subsequent requests.

**Response:**
```json
{
  "result": {
    "results": [ ... ],
    "totalCount": 100,
    "pageSize": 50,
    "pageIndex": 0
  },
  "isError": false,
  "message": "Success"
}
```

`result` implements `ISearchResults`; exact fields depend on the backend (e.g. results array, totalCount, pagination).

---

## Usage

```http
GET http://api.oasisweb4.com/api/search?query=avatar&limit=20&offset=0
Authorization: Bearer YOUR_JWT_TOKEN
```

For the provider-specific route, encode or pass search params as required by the route template (see Swagger for exact parameter binding).

---

## Related Documentation

- [Data API](../data-storage/data-api.md) – Holon and file data
- [Avatar API](../authentication-identity/avatar-api.md) – Identity search
- [WEB4 Overview](../overview.md)

---

*Last Updated: January 24, 2026*
