# Templates API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Template Management](#template-management)
- [Template Operations](#template-operations)
- [Error Responses](#error-responses)

## Overview

The Templates API provides template management for the STAR ecosystem. It handles reusable templates for OAPPs, Zomes, and other components.

## Template Management

### Get All Templates
```http
GET /api/templates
Authorization: Bearer YOUR_TOKEN
```

### Get Template by ID
```http
GET /api/templates/{templateId}
Authorization: Bearer YOUR_TOKEN
```

### Create Template
```http
POST /api/templates
Authorization: Bearer YOUR_TOKEN
```

### Update Template
```http
PUT /api/templates/{templateId}
Authorization: Bearer YOUR_TOKEN
```

### Delete Template
```http
DELETE /api/templates/{templateId}
Authorization: Bearer YOUR_TOKEN
```

## Template Operations

### Apply Template
```http
POST /api/templates/{templateId}/apply
Authorization: Bearer YOUR_TOKEN
```

### Clone Template
```http
POST /api/templates/{templateId}/clone
Authorization: Bearer YOUR_TOKEN
```

### Export Template
```http
GET /api/templates/{templateId}/export
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Template Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Template not found"
}
```

---

## Navigation

**← Previous:** [OAPPs API](OAPPs-API.md) | **Next:** [Runtimes API](Runtimes-API.md) →
