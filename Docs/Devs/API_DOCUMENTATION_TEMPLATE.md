# API Documentation Template

This template should be used for all API endpoint documentation to ensure consistency.

---

# [API Name] API

## Overview

[Brief description of what this API does and when to use it]

**Base URL:** `/api/[endpoint]`

**Authentication:** Required (Bearer token)

**Rate Limits:**
- Free tier: [X] requests/minute
- Pro tier: [X] requests/minute

---

## Quick Start

### Your First API Call

```http
GET /api/[endpoint]/example
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
{
  "result": {
    "id": "example-id",
    "name": "Example"
  },
  "isError": false,
  "message": "Success"
}
```

---

## Endpoints

### [Endpoint Name]

**Description:** [What this endpoint does]

**Endpoint:** `[METHOD] /api/[endpoint]/[path]`

**Authentication:** Required

**Parameters:**

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| param1 | string | path | Yes | Description |
| param2 | int | query | No | Description |

**Request Example:**
```http
GET /api/[endpoint]/example?param2=123
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response Example:**
```json
{
  "result": {
    "id": "example-id",
    "name": "Example",
    "createdDate": "2024-01-15T10:30:00Z"
  },
  "isError": false,
  "message": "Success"
}
```

**Error Responses:**

| Status Code | Error Code | Description |
|-------------|-----------|-------------|
| 400 | INVALID_PARAMETER | Parameter validation failed |
| 401 | UNAUTHORIZED | Missing or invalid JWT token |
| 404 | NOT_FOUND | Resource not found |
| 429 | RATE_LIMIT_EXCEEDED | Too many requests |

**Code Examples:**

```typescript
// TypeScript/JavaScript
import { OASISClient } from '@oasis/sdk';

const client = new OASISClient({
  apiKey: 'your-api-key',
  baseUrl: 'https://api.oasisplatform.world'
});

const result = await client.[endpoint].getExample({
  param2: 123
});
```

```python
# Python
from oasis_sdk import OASISClient

client = OASISClient(api_key='your-api-key')

result = client.[endpoint].get_example(param2=123)
```

```csharp
// C#/.NET
using NextGenSoftware.OASIS.API.Core;

var client = new OASISClient("your-api-key");
var result = await client.[Endpoint].GetExampleAsync(new ExampleRequest 
{ 
    Param2 = 123 
});
```

```bash
# cURL
curl -X GET "https://api.oasisplatform.world/api/[endpoint]/example?param2=123" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## Request/Response Schemas

### Request Schema

```json
{
  "param1": "string",
  "param2": 123,
  "optionalParam": "string (optional)"
}
```

### Response Schema

```json
{
  "result": {
    "id": "string (UUID)",
    "name": "string",
    "createdDate": "datetime (ISO 8601)",
    "metadata": {
      "key": "value"
    }
  },
  "isError": false,
  "message": "string"
}
```

---

## Pagination

For list endpoints, pagination is supported via query parameters:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| page | int | 1 | Page number |
| limit | int | 50 | Items per page (max: 100) |

**Response includes pagination metadata:**
```json
{
  "result": {
    "items": [...],
    "pagination": {
      "page": 1,
      "limit": 50,
      "total": 1234,
      "totalPages": 25,
      "hasNext": true,
      "hasPrevious": false
    }
  }
}
```

---

## Filtering & Sorting

### Filtering

Use query parameters to filter results:

```http
GET /api/[endpoint]?filter=name:example&filter=status:active
```

### Sorting

Use `sort` and `order` parameters:

```http
GET /api/[endpoint]?sort=createdDate&order=desc
```

**Available sort fields:**
- `createdDate` - Creation timestamp
- `updatedDate` - Last update timestamp
- `name` - Name field

**Order options:**
- `asc` - Ascending
- `desc` - Descending

---

## Batch Operations

For operations on multiple items, use batch endpoints:

**Endpoint:** `POST /api/[endpoint]/batch`

**Request:**
```json
{
  "ids": ["id1", "id2", "id3"],
  "operation": "update",
  "data": {
    "status": "active"
  }
}
```

**Response:**
```json
{
  "result": {
    "successful": ["id1", "id2"],
    "failed": ["id3"],
    "errors": {
      "id3": "Resource not found"
    }
  },
  "isError": false
}
```

---

## Webhooks

Subscribe to events for this API:

**Webhook Events:**
- `[endpoint].created` - Fired when a new item is created
- `[endpoint].updated` - Fired when an item is updated
- `[endpoint].deleted` - Fired when an item is deleted

**Webhook Payload:**
```json
{
  "event": "[endpoint].created",
  "timestamp": "2024-01-15T10:30:00Z",
  "data": {
    "id": "example-id",
    "name": "Example"
  }
}
```

[Learn more about webhooks →](../reference/webhooks.md)

---

## Error Handling

All errors follow a standard format:

```json
{
  "result": null,
  "isError": true,
  "message": "Human-readable error message",
  "errorCode": "ERROR_CODE",
  "errors": [
    {
      "field": "param1",
      "message": "Field-specific error message"
    }
  ],
  "metadata": {
    "requestId": "req-123",
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

**Common Error Codes:**

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| INVALID_PARAMETER | 400 | Request parameter validation failed |
| UNAUTHORIZED | 401 | Missing or invalid authentication |
| FORBIDDEN | 403 | Insufficient permissions |
| NOT_FOUND | 404 | Resource not found |
| RATE_LIMIT_EXCEEDED | 429 | Too many requests |
| INTERNAL_ERROR | 500 | Server error |

[View complete error code reference →](../reference/error-codes.md)

---

## Rate Limits

Rate limits are applied per API key:

| Tier | Requests per Minute | Burst Limit |
|------|---------------------|-------------|
| Free | 100 | 200 |
| Pro | 1,000 | 2,000 |
| Enterprise | Custom | Custom |

Rate limit headers are included in responses:

```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1642248000
```

[Learn more about rate limits →](../reference/rate-limits.md)

---

## Use Cases

### Use Case 1: [Description]

**Scenario:** [When you would use this]

**Example:**
```typescript
// Code example
```

### Use Case 2: [Description]

**Scenario:** [When you would use this]

**Example:**
```typescript
// Code example
```

---

## Best Practices

1. **Always handle errors** - Check `isError` flag in responses
2. **Use pagination** - Don't fetch all data at once
3. **Cache responses** - Cache data when appropriate
4. **Respect rate limits** - Implement exponential backoff
5. **Use batch operations** - When working with multiple items

---

## Migration Guide

### From Old Endpoint

**Old:**
```http
GET /api/old-endpoint/{id}
```

**New:**
```http
GET /api/[endpoint]/{id}
```

**Changes:**
- [List of changes

**Example Migration:**
```typescript
// Old way
const response = await fetch(`/api/old-endpoint/${id}`);

// New way
const response = await fetch(`/api/[endpoint]/${id}`);
```

---

## Related Documentation

- [Related API 1](related-api-1.md) - Description
- [Related API 2](related-api-2.md) - Description
- [Architecture Guide](../guides/architecture/system-overview.md) - System design

---

## Support

- **Questions?** → [Join Discord](https://discord.gg/oasis)
- **Issues?** → [Report on GitHub](https://github.com/NextGenSoftwareUK/OASIS/issues)
- **Feature Requests?** → [Submit Request](https://github.com/NextGenSoftwareUK/OASIS/discussions)

---

*Last Updated: [Date]*
