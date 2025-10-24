# ONODE API

## 📋 **Table of Contents**

- [Overview](#overview)
- [ONODE Management](#onode-management)
- [ONODE Operations](#onode-operations)
- [Error Responses](#error-responses)

## Overview

The ONODE API provides OASIS Node management for the OASIS ecosystem. It handles node operations, status monitoring, and network management.

## ONODE Management

### Get ONODE Status
```http
GET /api/v1/onode/status
Authorization: Bearer YOUR_TOKEN
```

### Get ONODE Info
```http
GET /api/v1/onode/info
Authorization: Bearer YOUR_TOKEN
```

### Get ONODE Stats
```http
GET /api/v1/onode/stats
Authorization: Bearer YOUR_TOKEN
```

## ONODE Operations

### Start ONODE
```http
POST /api/v1/onode/start
Authorization: Bearer YOUR_TOKEN
```

### Stop ONODE
```http
POST /api/v1/onode/stop
Authorization: Bearer YOUR_TOKEN
```

### Restart ONODE
```http
POST /api/v1/onode/restart
Authorization: Bearer YOUR_TOKEN
```

### Get ONODE Health
```http
GET /api/v1/onode/health
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### ONODE Not Running
```json
{
  "result": null,
  "isError": true,
  "message": "ONODE is not running"
}
```

---

## Navigation

**← Previous:** [HyperDrive API](HyperDrive-API.md) | **Next:** [Provider API](Provider-API.md) →