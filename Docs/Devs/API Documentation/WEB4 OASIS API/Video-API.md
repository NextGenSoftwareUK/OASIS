# Video API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Video Management](#video-management)
- [Video Operations](#video-operations)
- [Error Responses](#error-responses)

## Overview

The Video API provides video processing and management for the OASIS ecosystem. It handles video upload, processing, and streaming.

## Video Management

### Get All Videos
```http
GET /api/video
Authorization: Bearer YOUR_TOKEN
```

### Get Video by ID
```http
GET /api/video/{videoId}
Authorization: Bearer YOUR_TOKEN
```

### Upload Video
```http
POST /api/video/upload
Authorization: Bearer YOUR_TOKEN
```

## Video Operations

### Process Video
```http
POST /api/video/{videoId}/process
Authorization: Bearer YOUR_TOKEN
```

### Stream Video
```http
GET /api/video/{videoId}/stream
Authorization: Bearer YOUR_TOKEN
```

### Delete Video
```http
DELETE /api/video/{videoId}
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Video Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Video not found"
}
```

---

## Navigation

**← Previous:** [Social API](Social-API.md) | **Next:** [Share API](Share-API.md) →
