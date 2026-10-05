# OAPPs API

## 📋 **Table of Contents**

- [Overview](#overview)
- [OAPP Management](#oapp-management)
- [OAPP Operations](#oapp-operations)
- [Error Responses](#error-responses)

## Overview

The OAPPs API provides OASIS Application management for the STAR ecosystem. It handles OAPP creation, deployment, and execution.

## OAPP Management

### Get All OAPPs
```http
GET /api/oapps
Authorization: Bearer YOUR_TOKEN
```

### Get OAPP by ID
```http
GET /api/oapps/{oappId}
Authorization: Bearer YOUR_TOKEN
```

### Create OAPP
```http
POST /api/oapps
Authorization: Bearer YOUR_TOKEN
```

### Update OAPP
```http
PUT /api/oapps/{oappId}
Authorization: Bearer YOUR_TOKEN
```

### Delete OAPP
```http
DELETE /api/oapps/{oappId}
Authorization: Bearer YOUR_TOKEN
```

## OAPP Operations

### Deploy OAPP
```http
POST /api/oapps/{oappId}/deploy
Authorization: Bearer YOUR_TOKEN
```

### Launch OAPP
```http
POST /api/oapps/{oappId}/launch
Authorization: Bearer YOUR_TOKEN
```

### Stop OAPP
```http
POST /api/oapps/{oappId}/stop
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### OAPP Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "OAPP not found"
}
```

---

## Navigation

**← Previous:** [Chapters API](Chapters-API.md) | **Next:** [Templates API](Templates-API.md) →
