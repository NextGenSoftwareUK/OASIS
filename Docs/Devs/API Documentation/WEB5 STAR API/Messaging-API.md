# Messaging API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Message Management](#message-management)
- [Message Operations](#message-operations)
- [Message Analytics](#message-analytics)
- [Error Responses](#error-responses)

## Overview

The Messaging API provides messaging functionality for the STAR ecosystem. It handles direct messages, group messages, and notifications.

## Message Management

### Get All Messages
```http
GET /api/messaging
Authorization: Bearer YOUR_TOKEN
```

### Get Message by ID
```http
GET /api/messaging/{messageId}
Authorization: Bearer YOUR_TOKEN
```

### Send Message
```http
POST /api/messaging
Authorization: Bearer YOUR_TOKEN
```

### Delete Message
```http
DELETE /api/messaging/{messageId}
Authorization: Bearer YOUR_TOKEN
```

## Message Operations

### Mark as Read
```http
POST /api/messaging/{messageId}/read
Authorization: Bearer YOUR_TOKEN
```

### Get Unread Messages
```http
GET /api/messaging/unread
Authorization: Bearer YOUR_TOKEN
```

### Get Conversation
```http
GET /api/messaging/conversation/{userId}
Authorization: Bearer YOUR_TOKEN
```

## Message Analytics

### Get Message Stats
```http
GET /api/messaging/stats
Authorization: Bearer YOUR_TOKEN
```

### Get Message Activity
```http
GET /api/messaging/activity
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Message Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Message not found"
}
```

### Send Failed
```json
{
  "result": null,
  "isError": true,
  "message": "Message send failed"
}
```

---

## Navigation

**← Previous:** [Chat API](Chat-API.md) | **Next:** [Eggs API](Eggs-API.md) →
