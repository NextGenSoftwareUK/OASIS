# Plugins API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Plugin Management](#plugin-management)
- [Plugin Operations](#plugin-operations)
- [Error Responses](#error-responses)

## Overview

The Plugins API provides plugin management for the STAR ecosystem. It handles plugin installation, configuration, and execution.

## Plugin Management

### Get All Plugins
```http
GET /api/plugins
Authorization: Bearer YOUR_TOKEN
```

### Get Plugin by ID
```http
GET /api/plugins/{pluginId}
Authorization: Bearer YOUR_TOKEN
```

### Install Plugin
```http
POST /api/plugins
Authorization: Bearer YOUR_TOKEN
```

### Update Plugin
```http
PUT /api/plugins/{pluginId}
Authorization: Bearer YOUR_TOKEN
```

### Uninstall Plugin
```http
DELETE /api/plugins/{pluginId}
Authorization: Bearer YOUR_TOKEN
```

## Plugin Operations

### Enable Plugin
```http
POST /api/plugins/{pluginId}/enable
Authorization: Bearer YOUR_TOKEN
```

### Disable Plugin
```http
POST /api/plugins/{pluginId}/disable
Authorization: Bearer YOUR_TOKEN
```

### Configure Plugin
```http
PUT /api/plugins/{pluginId}/configure
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Plugin Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Plugin not found"
}
```

---

## Navigation

**← Previous:** [Parks API](Parks-API.md) | **Next:** [Libraries API](Libraries-API.md) →
