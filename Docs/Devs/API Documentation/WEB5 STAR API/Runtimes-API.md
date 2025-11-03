# Runtimes API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Runtime Management](#runtime-management)
- [Runtime Operations](#runtime-operations)
- [Error Responses](#error-responses)

## Overview

The Runtimes API provides runtime environment management for the STAR ecosystem. It handles execution environments for OAPPs.

## Runtime Management

### Get All Runtimes
```http
GET /api/runtimes
Authorization: Bearer YOUR_TOKEN
```

### Get Runtime by ID
```http
GET /api/runtimes/{runtimeId}
Authorization: Bearer YOUR_TOKEN
```

### Create Runtime
```http
POST /api/runtimes
Authorization: Bearer YOUR_TOKEN
```

### Update Runtime
```http
PUT /api/runtimes/{runtimeId}
Authorization: Bearer YOUR_TOKEN
```

### Delete Runtime
```http
DELETE /api/runtimes/{runtimeId}
Authorization: Bearer YOUR_TOKEN
```

## Runtime Operations

### Start Runtime
```http
POST /api/runtimes/{runtimeId}/start
Authorization: Bearer YOUR_TOKEN
```

### Stop Runtime
```http
POST /api/runtimes/{runtimeId}/stop
Authorization: Bearer YOUR_TOKEN
```

### Get Runtime Status
```http
GET /api/runtimes/{runtimeId}/status
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Runtime Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Runtime not found"
}
```

---

## Navigation

**← Previous:** [Templates API](Templates-API.md) | **Next:** [Parks API](Parks-API.md) →
