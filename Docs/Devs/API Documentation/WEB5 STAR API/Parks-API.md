# Parks API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Park Management](#park-management)
- [Park Operations](#park-operations)
- [Error Responses](#error-responses)

## Overview

The Parks API provides virtual park management for the STAR ecosystem. It handles park creation, maintenance, and visitor management.

## Park Management

### Get All Parks
```http
GET /api/parks
Authorization: Bearer YOUR_TOKEN
```

### Get Park by ID
```http
GET /api/parks/{parkId}
Authorization: Bearer YOUR_TOKEN
```

### Create Park
```http
POST /api/parks
Authorization: Bearer YOUR_TOKEN
```

### Update Park
```http
PUT /api/parks/{parkId}
Authorization: Bearer YOUR_TOKEN
```

### Delete Park
```http
DELETE /api/parks/{parkId}
Authorization: Bearer YOUR_TOKEN
```

## Park Operations

### Visit Park
```http
POST /api/parks/{parkId}/visit
Authorization: Bearer YOUR_TOKEN
```

### Get Park Visitors
```http
GET /api/parks/{parkId}/visitors
Authorization: Bearer YOUR_TOKEN
```

### Get Park Activity
```http
GET /api/parks/{parkId}/activity
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Park Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Park not found"
}
```

---

## Navigation

**← Previous:** [Runtimes API](Runtimes-API.md) | **Next:** [Plugins API](Plugins-API.md) →
