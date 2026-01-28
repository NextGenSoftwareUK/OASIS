# How to Use Holons

> Quick start and common patterns for loading, saving, and managing holons via the OASIS Data API.

**Prerequisites:** You understand [what holons are](../concepts/holons.md). You have an [avatar account](../getting-started/overview.md) and a JWT token. For full endpoint details, see the [Data API (Holons)](../web4-oasis-api/data-storage/holons-api.md) reference.

**Base URL:** `http://api.oasisweb4.com/api/data`  
**Authentication:** Bearer token required for holon endpoints.

---

## Quick start (3 steps)

### Step 1: Authenticate

Get a JWT so you can call the Data API:

```http
POST http://api.oasisweb4.com/api/avatar/authenticate
Content-Type: application/json

{
  "username": "your_username",
  "password": "your_password"
}
```

Use the `token` from the response in the `Authorization` header for all Data API calls.

### Step 2: Load a holon by ID

If you already have a holon ID (e.g. from a previous save or from another API):

```http
GET http://api.oasisweb4.com/api/data/load-holon/{holonId}
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
{
  "result": {
    "result": {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "name": "My first holon",
      "description": "A sample holon",
      "holonType": 40,
      "parentHolonId": "00000000-0000-0000-0000-000000000000",
      "metadata": {},
      "version": 1,
      "isActive": true
    }
  },
  "isError": false,
  "message": null
}
```

<Info>
  **Always check `isError`** in the response. The API may return HTTP 200 even when `isError` is true; use the message and result for error handling.
</Info>

### Step 3: Save a new holon

Create and persist a holon (e.g. a custom “Note” or “Project”):

```http
POST http://api.oasisweb4.com/api/data/save-holon
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "holon": {
    "id": "00000000-0000-0000-0000-000000000000",
    "name": "My first holon",
    "description": "A sample holon",
    "holonType": 40,
    "metadata": { "app": "my-app" },
    "isActive": true
  },
  "saveChildren": false
}
```

- Use `id: "00000000-0000-0000-0000-000000000000"` (or omit) for a **new** holon; the API will assign an Id.
- **holonType** `40` = generic `Holon`. Use other [HolonType](https://github.com/NextGenSoftwareUK/OASIS/blob/master/OASIS%20Architecture/NextGenSoftware.OASIS.API.Core/Enums/HolonType.cs) values for Avatar, Mission, NFT, etc.
- Response `result.result` will contain the saved holon (with Id and provider keys).

---

## Code examples

### TypeScript / JavaScript

```typescript
const BASE = 'http://api.oasisweb4.com/api';
const token = 'YOUR_JWT_TOKEN';

// Load a holon
async function loadHolon(id: string) {
  const res = await fetch(`${BASE}/data/load-holon/${id}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const data = await res.json();
  if (data.isError) throw new Error(data.message);
  return data.result?.result;
}

// Save a holon
async function saveHolon(holon: { name: string; description?: string; holonType?: number; metadata?: object }) {
  const res = await fetch(`${BASE}/data/save-holon`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      holon: {
        id: '00000000-0000-0000-0000-000000000000',
        name: holon.name,
        description: holon.description ?? '',
        holonType: holon.holonType ?? 40,
        metadata: holon.metadata ?? {},
        isActive: true
      },
      saveChildren: false
    })
  });
  const data = await res.json();
  if (data.isError) throw new Error(data.message);
  return data.result?.result;
}

// Usage
const saved = await saveHolon({ name: 'My note', description: 'Hello holon' });
console.log('Saved holon ID:', saved.id);
const loaded = await loadHolon(saved.id);
console.log('Loaded:', loaded.name);
```

### cURL

```bash
# Load holon
curl -X GET "http://api.oasisweb4.com/api/data/load-holon/YOUR_HOLON_ID" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Save holon (minimal body)
curl -X POST "http://api.oasisweb4.com/api/data/save-holon" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "holon": {
      "id": "00000000-0000-0000-0000-000000000000",
      "name": "My holon",
      "description": "",
      "holonType": 40,
      "metadata": {},
      "isActive": true
    },
    "saveChildren": false
  }'
```

---

## Common patterns

### Load with children

To load a holon and all its child holons (e.g. a Mission and its Quests):

**POST** with a request body gives you full control:

```http
POST http://api.oasisweb4.com/api/data/load-holon
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "id": "YOUR_HOLON_ID",
  "loadChildren": true,
  "recursive": true,
  "maxChildDepth": 0,
  "continueOnError": true,
  "version": 0
}
```

- **recursive** `true` = load children of children (default).
- **maxChildDepth** `0` = no limit; use e.g. `1` for one level only.

### Load children of a parent

To get all holons that belong to a parent (e.g. all Missions for an Avatar):

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

Use **holonType** `"All"` to get every child regardless of type.

### Load all holons of a type

To list all holons of a given type (e.g. all Missions):

```http
GET http://api.oasisweb4.com/api/data/load-all-holons/Mission
Authorization: Bearer YOUR_JWT_TOKEN
```

Optional query-style control via the full path (see [Holons API reference](../web4-oasis-api/data-storage/holons-api.md)): loadChildren, recursive, maxChildDepth, continueOnError, version, providerType, setGlobally.

### Delete a holon

```http
DELETE http://api.oasisweb4.com/api/data/delete-holon/{holonId}
Authorization: Bearer YOUR_JWT_TOKEN
```

Add `/{softDelete}` (e.g. `/true`) to soft-delete: `DELETE .../delete-holon/{id}/true`.

### Provider selection

To force a specific storage provider (e.g. MongoDB) for one request:

```http
GET http://api.oasisweb4.com/api/data/load-holon/{id}/true/true/0/true/0/MongoDBOASIS/false
Authorization: Bearer YOUR_JWT_TOKEN
```

Format: `.../load-holon/{id}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}`. Set **setGlobally** to `true` to use that provider for subsequent requests.

---

## Use cases

### Use case 1: App-specific data (notes, projects)

1. Save a holon with a custom **holonType** or use generic `Holon` (40) and put app context in **metadata**.
2. Load by Id when the user opens an item.
3. Load children of a “folder” holon by parent Id + optional type.

### Use case 2: Missions and quests

1. Create a Mission holon; save it.
2. Create Quest holons with **parentHolonId** = Mission Id; save them.
3. Load the Mission with `loadChildren: true` to get the Mission and all Quests in one call.

### Use case 3: Replication and providers

1. Use default behavior (HyperDrive) for automatic failover and replication.
2. Or pass **providerType** (and optional **setGlobally**) to read/write from a specific provider (e.g. Solana, IPFS).

---

## Best practices

1. **Always check `isError`** in the response body; do not rely only on HTTP status.
2. **Store the holon `id`** after save; use it for load, update, and delete.
3. **Use `metadata`** for app-specific fields so you don’t need schema changes for every feature.
4. **Prefer POST** for load when you need explicit options (loadChildren, recursive, providerType); use GET for simple “load by id” or “load all by type.”
5. **Respect rate limits** — see [Rate limits](../reference/rate-limits.md).

---

## Related documentation

- **[What are Holons?](../concepts/holons.md)** — Core concept and properties.
- **[Data API (Holons)](../web4-oasis-api/data-storage/holons-api.md)** — Full endpoint reference for `/api/data` holon operations.
- **[Getting started](../getting-started/overview.md)** — Register and authenticate.
- **[Error codes](../reference/error-codes.md)** — Handling errors.

---

*Last updated: January 2026*
