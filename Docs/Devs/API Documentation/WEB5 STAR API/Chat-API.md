# Chat API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Chat Management](#chat-management)
- [Message Operations](#message-operations)
- [Chat Analytics](#chat-analytics)
- [Error Responses](#error-responses)

## Overview

The Chat API provides real-time chat functionality for the STAR ecosystem. It handles chat rooms, messages, and user interactions.

## Chat Management

### Get All Chats
```http
GET /api/chat
Authorization: Bearer YOUR_TOKEN
```

### Get Chat by ID
```http
GET /api/chat/{chatId}
Authorization: Bearer YOUR_TOKEN
```

### Create Chat
```http
POST /api/chat
Authorization: Bearer YOUR_TOKEN
```

### Update Chat
```http
PUT /api/chat/{chatId}
Authorization: Bearer YOUR_TOKEN
```

### Delete Chat
```http
DELETE /api/chat/{chatId}
Authorization: Bearer YOUR_TOKEN
```

## Message Operations

### Send Message
```http
POST /api/chat/{chatId}/message
Authorization: Bearer YOUR_TOKEN
```

### Get Messages
```http
GET /api/chat/{chatId}/messages
Authorization: Bearer YOUR_TOKEN
```

### Delete Message
```http
DELETE /api/chat/{chatId}/message/{messageId}
Authorization: Bearer YOUR_TOKEN
```

### Edit Message
```http
PUT /api/chat/{chatId}/message/{messageId}
Authorization: Bearer YOUR_TOKEN
```

## Chat Analytics

### Get Chat Stats
```http
GET /api/chat/stats
Authorization: Bearer YOUR_TOKEN
```

### Get Chat Activity
```http
GET /api/chat/{chatId}/activity
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Chat Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Chat not found"
}
```

### Message Send Failed
```json
{
  "result": null,
  "isError": true,
  "message": "Message send failed"
}
```

---

## Navigation

**← Previous:** [Competition API](Competition-API.md) | **Next:** [Messaging API](Messaging-API.md) →
