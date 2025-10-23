# Cargo API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Cargo Management](#cargo-management)
- [Cargo Operations](#cargo-operations)
- [Cargo Tracking](#cargo-tracking)
- [Error Responses](#error-responses)

## Overview

The Cargo API provides cargo and logistics management for the OASIS ecosystem. It handles cargo creation, shipping, tracking, and delivery.

## Cargo Management

### Get All Cargo
```http
GET /api/cargo
Authorization: Bearer YOUR_TOKEN
```

### Get Cargo by ID
```http
GET /api/cargo/{cargoId}
Authorization: Bearer YOUR_TOKEN
```

### Create Cargo
```http
POST /api/cargo
Authorization: Bearer YOUR_TOKEN
```

### Update Cargo
```http
PUT /api/cargo/{cargoId}
Authorization: Bearer YOUR_TOKEN
```

### Delete Cargo
```http
DELETE /api/cargo/{cargoId}
Authorization: Bearer YOUR_TOKEN
```

## Cargo Operations

### Ship Cargo
```http
POST /api/cargo/{cargoId}/ship
Authorization: Bearer YOUR_TOKEN
```

### Deliver Cargo
```http
POST /api/cargo/{cargoId}/deliver
Authorization: Bearer YOUR_TOKEN
```

### Cancel Shipment
```http
POST /api/cargo/{cargoId}/cancel
Authorization: Bearer YOUR_TOKEN
```

## Cargo Tracking

### Track Cargo
```http
GET /api/cargo/{cargoId}/track
Authorization: Bearer YOUR_TOKEN
```

### Get Cargo Status
```http
GET /api/cargo/{cargoId}/status
Authorization: Bearer YOUR_TOKEN
```

### Get Cargo History
```http
GET /api/cargo/{cargoId}/history
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Cargo Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Cargo not found"
}
```

### Shipment Failed
```json
{
  "result": null,
  "isError": true,
  "message": "Shipment failed"
}
```

---

## Navigation

**← Previous:** [OLand API](OLand-API.md) | **Next:** [Avatar API](Avatar-API.md) →
