# Libraries API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Library Management](#library-management)
- [Library Operations](#library-operations)
- [Error Responses](#error-responses)

## Overview

The Libraries API provides library management for the STAR ecosystem. It handles code libraries and dependencies.

## Library Management

### Get All Libraries
```http
GET /api/libraries
Authorization: Bearer YOUR_TOKEN
```

### Get Library by ID
```http
GET /api/libraries/{libraryId}
Authorization: Bearer YOUR_TOKEN
```

### Create Library
```http
POST /api/libraries
Authorization: Bearer YOUR_TOKEN
```

### Update Library
```http
PUT /api/libraries/{libraryId}
Authorization: Bearer YOUR_TOKEN
```

### Delete Library
```http
DELETE /api/libraries/{libraryId}
Authorization: Bearer YOUR_TOKEN
```

## Library Operations

### Import Library
```http
POST /api/libraries/{libraryId}/import
Authorization: Bearer YOUR_TOKEN
```

### Export Library
```http
GET /api/libraries/{libraryId}/export
Authorization: Bearer YOUR_TOKEN
```

### Get Library Dependencies
```http
GET /api/libraries/{libraryId}/dependencies
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Library Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Library not found"
}
```

---

## Navigation

**← Previous:** [Plugins API](Plugins-API.md) | **Next:** [InventoryItems API](InventoryItems-API.md) →
