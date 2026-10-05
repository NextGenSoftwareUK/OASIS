# Eggs API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Egg Management](#egg-management)
- [Egg Operations](#egg-operations)
- [Egg Analytics](#egg-analytics)
- [Error Responses](#error-responses)

## Overview

The Eggs API provides egg collection and hatching functionality for the STAR ecosystem. It handles egg creation, collection, hatching, and rewards.

## Egg Management

### Get All Eggs
```http
GET /api/eggs
Authorization: Bearer YOUR_TOKEN
```

### Get Egg by ID
```http
GET /api/eggs/{eggId}
Authorization: Bearer YOUR_TOKEN
```

### Create Egg
```http
POST /api/eggs
Authorization: Bearer YOUR_TOKEN
```

### Update Egg
```http
PUT /api/eggs/{eggId}
Authorization: Bearer YOUR_TOKEN
```

### Delete Egg
```http
DELETE /api/eggs/{eggId}
Authorization: Bearer YOUR_TOKEN
```

## Egg Operations

### Collect Egg
```http
POST /api/eggs/{eggId}/collect
Authorization: Bearer YOUR_TOKEN
```

### Hatch Egg
```http
POST /api/eggs/{eggId}/hatch
Authorization: Bearer YOUR_TOKEN
```

### Get User Eggs
```http
GET /api/eggs/user/{userId}
Authorization: Bearer YOUR_TOKEN
```

## Egg Analytics

### Get Egg Stats
```http
GET /api/eggs/stats
Authorization: Bearer YOUR_TOKEN
```

### Get Egg Leaderboard
```http
GET /api/eggs/leaderboard
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Egg Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Egg not found"
}
```

### Already Collected
```json
{
  "result": null,
  "isError": true,
  "message": "Egg already collected"
}
```

---

## Navigation

**← Previous:** [Messaging API](Messaging-API.md) | **Next:** [CelestialBodies API](CelestialBodies-API.md) →
