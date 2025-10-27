# InventoryItems API

## 📋 **Table of Contents**

- [Overview](#overview)
- [InventoryItem Management](#inventoryitem-management)
- [InventoryItem Operations](#inventoryitem-operations)
- [Error Responses](#error-responses)

## Overview

The InventoryItems API provides inventory item management for the STAR ecosystem. It handles item creation, storage, and trading.

## InventoryItem Management

### Get All InventoryItems
```http
GET /api/inventoryitems
Authorization: Bearer YOUR_TOKEN
```

### Get InventoryItem by ID
```http
GET /api/inventoryitems/{itemId}
Authorization: Bearer YOUR_TOKEN
```

### Create InventoryItem
```http
POST /api/inventoryitems
Authorization: Bearer YOUR_TOKEN
```

### Update InventoryItem
```http
PUT /api/inventoryitems/{itemId}
Authorization: Bearer YOUR_TOKEN
```

### Delete InventoryItem
```http
DELETE /api/inventoryitems/{itemId}
Authorization: Bearer YOUR_TOKEN
```

## InventoryItem Operations

### Use Item
```http
POST /api/inventoryitems/{itemId}/use
Authorization: Bearer YOUR_TOKEN
```

### Transfer Item
```http
POST /api/inventoryitems/{itemId}/transfer
Authorization: Bearer YOUR_TOKEN
```

### Get User Inventory
```http
GET /api/inventoryitems/user/{userId}
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### InventoryItem Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "InventoryItem not found"
}
```

---

## Navigation

**← Previous:** [Libraries API](Libraries-API.md) | **Next:** [STAR API](STAR-API.md) →
