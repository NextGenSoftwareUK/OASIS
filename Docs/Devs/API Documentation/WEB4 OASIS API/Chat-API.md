# Chat API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Conversation Management](#conversation-management)
- [Message Operations](#message-operations)
- [Real-time Communication](#real-time-communication)
- [Chat Analytics](#chat-analytics)
- [Error Responses](#error-responses)

## Overview

The Chat API provides comprehensive real-time communication services for the OASIS ecosystem. It handles conversation management, message sending, real-time updates, and advanced chat features with support for multiple conversation types and media sharing.

## Conversation Management

### Get All Conversations
```http
GET /api/chat/conversations
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `status` (string, optional): Filter by status (Active, Archived, Deleted)
- `type` (string, optional): Filter by type (Direct, Group, Channel)
- `sortBy` (string, optional): Sort field (lastMessage, createdAt, name)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "conversations": [
        {
          "id": "conv_123",
          "name": "OASIS Development Team",
          "type": "Group",
          "status": "Active",
          "participants": [
            {
              "id": "user_123",
              "username": "john_doe",
              "avatar": "https://example.com/avatars/john.jpg",
              "role": "Admin",
              "joinedAt": "2024-01-15T10:30:00Z"
            },
            {
              "id": "user_456",
              "username": "jane_smith",
              "avatar": "https://example.com/avatars/jane.jpg",
              "role": "Member",
              "joinedAt": "2024-01-16T14:20:00Z"
            }
          ],
          "lastMessage": {
            "id": "msg_123",
            "content": "Hello everyone!",
            "sender": {
              "id": "user_123",
              "username": "john_doe"
            },
            "timestamp": "2024-01-20T14:30:00Z"
          },
          "unreadCount": 5,
          "createdAt": "2024-01-15T10:30:00Z",
          "lastActivity": "2024-01-20T14:30:00Z"
        }
      ],
      "totalCount": 1,
      "limit": 50,
      "offset": 0
    },
    "message": "Conversations retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Conversation by ID
```http
GET /api/chat/conversations/{conversationId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `conversationId` (string): Conversation UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "conv_123",
      "name": "OASIS Development Team",
      "type": "Group",
      "status": "Active",
      "description": "Main development team chat",
      "participants": [
        {
          "id": "user_123",
          "username": "john_doe",
          "avatar": "https://example.com/avatars/john.jpg",
          "role": "Admin",
          "permissions": ["send_messages", "manage_participants", "delete_messages"],
          "joinedAt": "2024-01-15T10:30:00Z",
          "lastSeen": "2024-01-20T14:30:00Z"
        },
        {
          "id": "user_456",
          "username": "jane_smith",
          "avatar": "https://example.com/avatars/jane.jpg",
          "role": "Member",
          "permissions": ["send_messages"],
          "joinedAt": "2024-01-16T14:20:00Z",
          "lastSeen": "2024-01-20T14:25:00Z"
        }
      ],
      "settings": {
        "allowInvites": true,
        "allowFileSharing": true,
        "allowVoiceCalls": true,
        "allowVideoCalls": true,
        "messageRetention": 30,
        "encryption": true
      },
      "lastMessage": {
        "id": "msg_123",
        "content": "Hello everyone!",
        "sender": {
          "id": "user_123",
          "username": "john_doe"
        },
        "timestamp": "2024-01-20T14:30:00Z"
      },
      "unreadCount": 5,
      "createdAt": "2024-01-15T10:30:00Z",
      "lastActivity": "2024-01-20T14:30:00Z"
    },
    "message": "Conversation retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Create Conversation
```http
POST /api/chat/conversations
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "name": "New Team Chat",
  "type": "Group",
  "description": "A new team chat for project collaboration",
  "participants": [
    "user_123",
    "user_456",
    "user_789"
  ],
  "settings": {
    "allowInvites": true,
    "allowFileSharing": true,
    "allowVoiceCalls": true,
    "allowVideoCalls": true,
    "messageRetention": 30,
    "encryption": true
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "conv_124",
      "name": "New Team Chat",
      "type": "Group",
      "status": "Active",
      "description": "A new team chat for project collaboration",
      "participants": [
        {
          "id": "user_123",
          "username": "john_doe",
          "avatar": "https://example.com/avatars/john.jpg",
          "role": "Admin",
          "permissions": ["send_messages", "manage_participants", "delete_messages"],
          "joinedAt": "2024-01-20T14:30:00Z"
        },
        {
          "id": "user_456",
          "username": "jane_smith",
          "avatar": "https://example.com/avatars/jane.jpg",
          "role": "Member",
          "permissions": ["send_messages"],
          "joinedAt": "2024-01-20T14:30:00Z"
        },
        {
          "id": "user_789",
          "username": "bob_wilson",
          "avatar": "https://example.com/avatars/bob.jpg",
          "role": "Member",
          "permissions": ["send_messages"],
          "joinedAt": "2024-01-20T14:30:00Z"
        }
      ],
      "settings": {
        "allowInvites": true,
        "allowFileSharing": true,
        "allowVoiceCalls": true,
        "allowVideoCalls": true,
        "messageRetention": 30,
        "encryption": true
      },
      "lastMessage": null,
      "unreadCount": 0,
      "createdAt": "2024-01-20T14:30:00Z",
      "lastActivity": "2024-01-20T14:30:00Z"
    },
    "message": "Conversation created successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Conversation
```http
PUT /api/chat/conversations/{conversationId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `conversationId` (string): Conversation UUID

**Request Body:**
```json
{
  "name": "Updated Team Chat",
  "description": "Updated description for the team chat",
  "settings": {
    "allowInvites": false,
    "allowFileSharing": true,
    "allowVoiceCalls": false,
    "allowVideoCalls": false,
    "messageRetention": 60,
    "encryption": true
  }
}
```

### Delete Conversation
```http
DELETE /api/chat/conversations/{conversationId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `conversationId` (string): Conversation UUID

## Message Operations

### Send Message
```http
POST /api/chat/conversations/{conversationId}/messages
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `conversationId` (string): Conversation UUID

**Request Body:**
```json
{
  "content": "Hello everyone! How's the project going?",
  "type": "text",
  "metadata": {
    "mentions": ["user_456"],
    "hashtags": ["project", "update"],
    "attachments": []
  },
  "replyTo": null,
  "encrypt": true
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "msg_124",
      "conversationId": "conv_123",
      "content": "Hello everyone! How's the project going?",
      "type": "text",
      "sender": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "metadata": {
        "mentions": ["user_456"],
        "hashtags": ["project", "update"],
        "attachments": []
      },
      "replyTo": null,
      "encrypted": true,
      "status": "sent",
      "timestamp": "2024-01-20T14:30:00Z",
      "editedAt": null,
      "deletedAt": null
    },
    "message": "Message sent successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Messages
```http
GET /api/chat/conversations/{conversationId}/messages
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `conversationId` (string): Conversation UUID

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by message type (text, image, file, voice, video)
- `startDate` (string, optional): Start date (ISO 8601)
- `endDate` (string, optional): End date (ISO 8601)
- `sortBy` (string, optional): Sort field (timestamp, content)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "conversationId": "conv_123",
      "messages": [
        {
          "id": "msg_123",
          "content": "Hello everyone!",
          "type": "text",
          "sender": {
            "id": "user_123",
            "username": "john_doe",
            "avatar": "https://example.com/avatars/john.jpg"
          },
          "metadata": {
            "mentions": [],
            "hashtags": [],
            "attachments": []
          },
          "replyTo": null,
          "encrypted": true,
          "status": "delivered",
          "timestamp": "2024-01-20T14:30:00Z",
          "editedAt": null,
          "deletedAt": null
        },
        {
          "id": "msg_122",
          "content": "Hi John! The project is going well.",
          "type": "text",
          "sender": {
            "id": "user_456",
            "username": "jane_smith",
            "avatar": "https://example.com/avatars/jane.jpg"
          },
          "metadata": {
            "mentions": ["user_123"],
            "hashtags": [],
            "attachments": []
          },
          "replyTo": "msg_123",
          "encrypted": true,
          "status": "delivered",
          "timestamp": "2024-01-20T14:25:00Z",
          "editedAt": null,
          "deletedAt": null
        }
      ],
      "totalCount": 2,
      "limit": 50,
      "offset": 0
    },
    "message": "Messages retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Edit Message
```http
PUT /api/chat/conversations/{conversationId}/messages/{messageId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `conversationId` (string): Conversation UUID
- `messageId` (string): Message UUID

**Request Body:**
```json
{
  "content": "Hello everyone! How's the project going? (Updated)",
  "metadata": {
    "mentions": ["user_456"],
    "hashtags": ["project", "update"],
    "attachments": []
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "msg_123",
      "conversationId": "conv_123",
      "content": "Hello everyone! How's the project going? (Updated)",
      "type": "text",
      "sender": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "metadata": {
        "mentions": ["user_456"],
        "hashtags": ["project", "update"],
        "attachments": []
      },
      "replyTo": null,
      "encrypted": true,
      "status": "delivered",
      "timestamp": "2024-01-20T14:30:00Z",
      "editedAt": "2024-01-20T14:35:00Z",
      "deletedAt": null
    },
    "message": "Message edited successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Delete Message
```http
DELETE /api/chat/conversations/{conversationId}/messages/{messageId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `conversationId` (string): Conversation UUID
- `messageId` (string): Message UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "msg_123",
      "conversationId": "conv_123",
      "content": "Message deleted",
      "type": "text",
      "sender": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "metadata": {
        "mentions": [],
        "hashtags": [],
        "attachments": []
      },
      "replyTo": null,
      "encrypted": true,
      "status": "deleted",
      "timestamp": "2024-01-20T14:30:00Z",
      "editedAt": null,
      "deletedAt": "2024-01-20T14:40:00Z"
    },
    "message": "Message deleted successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Real-time Communication

### Get Conversation Participants
```http
GET /api/chat/conversations/{conversationId}/participants
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `conversationId` (string): Conversation UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "conversationId": "conv_123",
      "participants": [
        {
          "id": "user_123",
          "username": "john_doe",
          "avatar": "https://example.com/avatars/john.jpg",
          "role": "Admin",
          "permissions": ["send_messages", "manage_participants", "delete_messages"],
          "joinedAt": "2024-01-15T10:30:00Z",
          "lastSeen": "2024-01-20T14:30:00Z",
          "status": "online"
        },
        {
          "id": "user_456",
          "username": "jane_smith",
          "avatar": "https://example.com/avatars/jane.jpg",
          "role": "Member",
          "permissions": ["send_messages"],
          "joinedAt": "2024-01-16T14:20:00Z",
          "lastSeen": "2024-01-20T14:25:00Z",
          "status": "away"
        }
      ],
      "totalCount": 2,
      "onlineCount": 1,
      "awayCount": 1,
      "offlineCount": 0
    },
    "message": "Participants retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Add Participant
```http
POST /api/chat/conversations/{conversationId}/participants
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `conversationId` (string): Conversation UUID

**Request Body:**
```json
{
  "userId": "user_789",
  "role": "Member",
  "permissions": ["send_messages"]
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "conversationId": "conv_123",
      "participant": {
        "id": "user_789",
        "username": "bob_wilson",
        "avatar": "https://example.com/avatars/bob.jpg",
        "role": "Member",
        "permissions": ["send_messages"],
        "joinedAt": "2024-01-20T14:30:00Z",
        "lastSeen": "2024-01-20T14:30:00Z",
        "status": "online"
      },
      "addedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Participant added successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Remove Participant
```http
DELETE /api/chat/conversations/{conversationId}/participants/{userId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `conversationId` (string): Conversation UUID
- `userId` (string): User UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "conversationId": "conv_123",
      "userId": "user_789",
      "removedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Participant removed successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Participant Role
```http
PUT /api/chat/conversations/{conversationId}/participants/{userId}/role
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `conversationId` (string): Conversation UUID
- `userId` (string): User UUID

**Request Body:**
```json
{
  "role": "Moderator",
  "permissions": ["send_messages", "delete_messages"]
}
```

## Chat Analytics

### Get Chat Statistics
```http
GET /api/chat/stats
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `conversationId` (string, optional): Filter by conversation ID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "statistics": {
        "conversations": {
          "total": 150,
          "active": 120,
          "archived": 25,
          "deleted": 5
        },
        "messages": {
          "total": 50000,
          "sent": 50000,
          "received": 45000,
          "edited": 1000,
          "deleted": 500
        },
        "users": {
          "total": 1000,
          "active": 800,
          "online": 200,
          "away": 300,
          "offline": 300
        },
        "performance": {
          "averageResponseTime": 0.5,
          "peakResponseTime": 2.0,
          "errorRate": 0.001,
          "availability": 99.9
        }
      },
      "trends": {
        "conversations": "increasing",
        "messages": "increasing",
        "users": "stable",
        "performance": "stable"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Chat statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Conversation Analytics
```http
GET /api/chat/conversations/{conversationId}/analytics
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `conversationId` (string): Conversation UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "conversationId": "conv_123",
      "analytics": {
        "messages": {
          "total": 1000,
          "sent": 1000,
          "received": 950,
          "edited": 50,
          "deleted": 25
        },
        "participants": {
          "total": 10,
          "active": 8,
          "online": 3,
          "away": 2,
          "offline": 3
        },
        "activity": {
          "averageMessagesPerDay": 50,
          "peakActivity": "14:00-15:00",
          "mostActiveDay": "Tuesday",
          "averageSessionTime": 1800
        },
        "performance": {
          "averageResponseTime": 0.5,
          "peakResponseTime": 2.0,
          "errorRate": 0.001,
          "availability": 99.9
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Conversation analytics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Chat Health
```http
GET /api/chat/health
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "status": "Healthy",
      "overallHealth": 0.98,
      "components": {
        "messaging": {
          "status": "Healthy",
          "health": 0.99,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "realTime": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "encryption": {
          "status": "Healthy",
          "health": 0.97,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "performance": {
          "status": "Healthy",
          "health": 0.96,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Messaging Test",
          "status": "Pass",
          "responseTime": 0.5,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Real-time Test",
          "status": "Pass",
          "responseTime": 0.2,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Encryption Test",
          "status": "Pass",
          "responseTime": 0.1,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Performance Test",
          "status": "Pass",
          "responseTime": 1.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Chat health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Error Responses

### Conversation Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Conversation not found",
  "exception": "Conversation with ID conv_123 not found"
}
```

### Message Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Message not found",
  "exception": "Message with ID msg_123 not found"
}
```

### Permission Denied
```json
{
  "result": null,
  "isError": true,
  "message": "Permission denied",
  "exception": "Insufficient permissions to perform this action"
}
```

### Participant Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Participant not found",
  "exception": "Participant with ID user_123 not found in conversation"
}
```

### Encryption Error
```json
{
  "result": null,
  "isError": true,
  "message": "Encryption error",
  "exception": "Failed to encrypt message content"
}
```

---

## Navigation

**← Previous:** [Map API](Map-API.md) | **Next:** [Messaging API](Messaging-API.md) →
