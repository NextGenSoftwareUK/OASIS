# Competition API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Competition Management](#competition-management)
- [Competition Operations](#competition-operations)
- [Competition Analytics](#competition-analytics)
- [Error Responses](#error-responses)

## Overview

The Competition API provides competition management for the STAR ecosystem. It handles competition creation, participation, scoring, and leaderboards.

## Competition Management

### Get All Competitions
```http
GET /api/competition
Authorization: Bearer YOUR_TOKEN
```

### Get Competition by ID
```http
GET /api/competition/{competitionId}
Authorization: Bearer YOUR_TOKEN
```

### Create Competition
```http
POST /api/competition
Authorization: Bearer YOUR_TOKEN
```

### Update Competition
```http
PUT /api/competition/{competitionId}
Authorization: Bearer YOUR_TOKEN
```

### Delete Competition
```http
DELETE /api/competition/{competitionId}
Authorization: Bearer YOUR_TOKEN
```

## Competition Operations

### Join Competition
```http
POST /api/competition/{competitionId}/join
Authorization: Bearer YOUR_TOKEN
```

### Submit Score
```http
POST /api/competition/{competitionId}/score
Authorization: Bearer YOUR_TOKEN
```

### Get Leaderboard
```http
GET /api/competition/{competitionId}/leaderboard
Authorization: Bearer YOUR_TOKEN
```

### Get Participants
```http
GET /api/competition/{competitionId}/participants
Authorization: Bearer YOUR_TOKEN
```

## Competition Analytics

### Get Competition Stats
```http
GET /api/competition/stats
Authorization: Bearer YOUR_TOKEN
```

### Get Competition Performance
```http
GET /api/competition/performance
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Competition Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Competition not found"
}
```

### Already Joined
```json
{
  "result": null,
  "isError": true,
  "message": "Already joined competition"
}
```

---

## Navigation

**← Previous:** [Quests API](Quests-API.md) | **Next:** [Chat API](Chat-API.md) →
