# Messaging API

## Overview

The Messaging API provides messaging services for the OASIS ecosystem. You can send messages to avatars, get messages for the current avatar, get a conversation between two avatars, and mark messages as read. All endpoints are avatar-scoped and require authentication.

**Base URL:** `/api/messaging`

**Authentication:** Required (Bearer token). Unauthenticated requests often return **HTTP 200** with `isError: true`—always check the response body.

**Rate Limits:** Same as [WEB4 API](../../reference/rate-limits.md).

---

## Quick Start

### Send a message to an avatar

```http
POST http://api.oasisweb4.com/api/messaging/send-message-to-avatar/{toAvatarId}
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

"Hello, this is the message content."
```

Optional query: `messageType` (e.g. Direct).

### Get messages for current avatar

```http
GET http://api.oasisweb4.com/api/messaging/messages?limit=50&offset=0
Authorization: Bearer YOUR_JWT_TOKEN
```

### Get conversation with another avatar

```http
GET http://api.oasisweb4.com/api/messaging/conversation/{otherAvatarId}?limit=50&offset=0
Authorization: Bearer YOUR_JWT_TOKEN
```

---

## Endpoints Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `send-message-to-avatar/{toAvatarId}` | Send message to avatar (body: content string; query: messageType) |
| GET | `messages` | Get messages for current avatar (query: limit, offset) |
| GET | `conversation/{otherAvatarId}` | Get conversation between current avatar and other (query: limit, offset) |
| POST | `mark-messages-read` | Mark messages as read (body: list of message IDs) |

---

## Request / Response

**send-message-to-avatar:**  
- **Body:** Raw string (message content) or JSON string.  
- **Query:** `messageType` – e.g. `Direct` (MessagingType enum).  
- **Response:** `result: true` on success.

**messages:**  
- **Query:** `limit` (default 50), `offset` (default 0).  
- **Response:** `result` is a list of `Message` objects (id, fromAvatarId, toAvatarId, content, messageType, createdDate, isRead, etc.).

**conversation/{otherAvatarId}:**  
- **Query:** `limit`, `offset`.  
- **Response:** List of messages between current avatar and `otherAvatarId`.

**mark-messages-read:**  
- **Body:** `List<Guid>` – message IDs to mark as read.  
- **Response:** `result: true` on success.

---

## Error Handling

- Always check **isError** and **message** in the response body.
- See [Error Code Reference](../../reference/error-codes.md).

---

## Related Documentation

- [Avatar API](../authentication-identity/avatar-api.md) – Identity

---

*Last Updated: January 24, 2026*
