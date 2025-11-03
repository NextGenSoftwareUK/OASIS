# Chapters API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Chapter Management](#chapter-management)
- [Chapter Operations](#chapter-operations)
- [Error Responses](#error-responses)

## Overview

The Chapters API provides chapter management for the STAR ecosystem. It handles story chapters and narrative progression.

## Chapter Management

### Get All Chapters
```http
GET /api/chapters
Authorization: Bearer YOUR_TOKEN
```

### Get Chapter by ID
```http
GET /api/chapters/{chapterId}
Authorization: Bearer YOUR_TOKEN
```

### Create Chapter
```http
POST /api/chapters
Authorization: Bearer YOUR_TOKEN
```

### Update Chapter
```http
PUT /api/chapters/{chapterId}
Authorization: Bearer YOUR_TOKEN
```

### Delete Chapter
```http
DELETE /api/chapters/{chapterId}
Authorization: Bearer YOUR_TOKEN
```

## Chapter Operations

### Complete Chapter
```http
POST /api/chapters/{chapterId}/complete
Authorization: Bearer YOUR_TOKEN
```

### Get Chapter Progress
```http
GET /api/chapters/{chapterId}/progress
Authorization: Bearer YOUR_TOKEN
```

### Unlock Chapter
```http
POST /api/chapters/{chapterId}/unlock
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Chapter Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Chapter not found"
}
```

---

## Navigation

**← Previous:** [Zomes API](Zomes-API.md) | **Next:** [OAPPs API](OAPPs-API.md) →
