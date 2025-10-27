# Messaging API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Message Management](#message-management)
- [Message Sending](#message-sending)
- [Message Delivery](#message-delivery)
- [Message Analytics](#message-analytics)
- [Error Responses](#error-responses)

## Overview

The Messaging API provides comprehensive messaging services for the OASIS ecosystem. It handles message creation, sending, delivery tracking, and analytics with support for multiple message types, encryption, and real-time updates.

## Message Management

### Get All Messages
```http
GET /api/messaging/messages
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `status` (string, optional): Filter by status (Sent, Delivered, Read, Failed)
- `type` (string, optional): Filter by type (Email, SMS, Push, In-App)
- `recipient` (string, optional): Filter by recipient
- `startDate` (string, optional): Start date (ISO 8601)
- `endDate` (string, optional): End date (ISO 8601)
- `sortBy` (string, optional): Sort field (createdAt, sentAt, status)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "messages": [
        {
          "id": "msg_123",
          "type": "Email",
          "status": "Delivered",
          "recipient": {
            "id": "user_123",
            "email": "john@example.com",
            "name": "John Doe"
          },
          "subject": "Welcome to OASIS",
          "content": "Welcome to the OASIS platform!",
          "sender": {
            "id": "system",
            "name": "OASIS System"
          },
          "createdAt": "2024-01-20T14:30:00Z",
          "sentAt": "2024-01-20T14:30:05Z",
          "deliveredAt": "2024-01-20T14:30:10Z",
          "readAt": null
        }
      ],
      "totalCount": 1,
      "limit": 50,
      "offset": 0
    },
    "message": "Messages retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Message by ID
```http
GET /api/messaging/messages/{messageId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `messageId` (string): Message UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "msg_123",
      "type": "Email",
      "status": "Delivered",
      "recipient": {
        "id": "user_123",
        "email": "john@example.com",
        "name": "John Doe"
      },
      "subject": "Welcome to OASIS",
      "content": "Welcome to the OASIS platform!",
      "sender": {
        "id": "system",
        "name": "OASIS System"
      },
      "metadata": {
        "template": "welcome_email",
        "variables": {
          "userName": "John Doe",
          "platformName": "OASIS"
        },
        "priority": "Normal",
        "encryption": true
      },
      "delivery": {
        "attempts": 1,
        "maxAttempts": 3,
        "nextRetry": null,
        "error": null
      },
      "tracking": {
        "opened": false,
        "clicked": false,
        "bounced": false,
        "complained": false
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "sentAt": "2024-01-20T14:30:05Z",
      "deliveredAt": "2024-01-20T14:30:10Z",
      "readAt": null
    },
    "message": "Message retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Create Message
```http
POST /api/messaging/messages
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "type": "Email",
  "recipient": {
    "id": "user_123",
    "email": "john@example.com",
    "name": "John Doe"
  },
  "subject": "Project Update",
  "content": "Here's the latest update on your project.",
  "metadata": {
    "template": "project_update",
    "variables": {
      "projectName": "OASIS Development",
      "status": "In Progress"
    },
    "priority": "High",
    "encryption": true
  },
  "schedule": {
    "sendAt": "2024-01-20T15:00:00Z",
    "timezone": "UTC"
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "msg_124",
      "type": "Email",
      "status": "Scheduled",
      "recipient": {
        "id": "user_123",
        "email": "john@example.com",
        "name": "John Doe"
      },
      "subject": "Project Update",
      "content": "Here's the latest update on your project.",
      "sender": {
        "id": "user_456",
        "name": "Jane Smith"
      },
      "metadata": {
        "template": "project_update",
        "variables": {
          "projectName": "OASIS Development",
          "status": "In Progress"
        },
        "priority": "High",
        "encryption": true
      },
      "delivery": {
        "attempts": 0,
        "maxAttempts": 3,
        "nextRetry": null,
        "error": null
      },
      "tracking": {
        "opened": false,
        "clicked": false,
        "bounced": false,
        "complained": false
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "sentAt": null,
      "deliveredAt": null,
      "readAt": null
    },
    "message": "Message created successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Message
```http
PUT /api/messaging/messages/{messageId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `messageId` (string): Message UUID

**Request Body:**
```json
{
  "subject": "Updated Project Update",
  "content": "Here's the latest update on your project (Updated).",
  "metadata": {
    "template": "project_update",
    "variables": {
      "projectName": "OASIS Development",
      "status": "Completed"
    },
    "priority": "High",
    "encryption": true
  }
}
```

### Delete Message
```http
DELETE /api/messaging/messages/{messageId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `messageId` (string): Message UUID

## Message Sending

### Send Email
```http
POST /api/messaging/send/email
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "recipient": {
    "email": "john@example.com",
    "name": "John Doe"
  },
  "subject": "Welcome to OASIS",
  "content": "Welcome to the OASIS platform!",
  "htmlContent": "<h1>Welcome to OASIS</h1><p>Welcome to the OASIS platform!</p>",
  "attachments": [
    {
      "filename": "welcome.pdf",
      "content": "base64_encoded_content",
      "type": "application/pdf"
    }
  ],
  "metadata": {
    "template": "welcome_email",
    "variables": {
      "userName": "John Doe",
      "platformName": "OASIS"
    },
    "priority": "Normal",
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
      "id": "msg_125",
      "type": "Email",
      "status": "Sent",
      "recipient": {
        "email": "john@example.com",
        "name": "John Doe"
      },
      "subject": "Welcome to OASIS",
      "content": "Welcome to the OASIS platform!",
      "htmlContent": "<h1>Welcome to OASIS</h1><p>Welcome to the OASIS platform!</p>",
      "attachments": [
        {
          "filename": "welcome.pdf",
          "type": "application/pdf",
          "size": 1024
        }
      ],
      "metadata": {
        "template": "welcome_email",
        "variables": {
          "userName": "John Doe",
          "platformName": "OASIS"
        },
        "priority": "Normal",
        "encryption": true
      },
      "delivery": {
        "attempts": 1,
        "maxAttempts": 3,
        "nextRetry": null,
        "error": null
      },
      "tracking": {
        "opened": false,
        "clicked": false,
        "bounced": false,
        "complained": false
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "sentAt": "2024-01-20T14:30:05Z",
      "deliveredAt": null,
      "readAt": null
    },
    "message": "Email sent successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Send SMS
```http
POST /api/messaging/send/sms
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "recipient": {
    "phone": "+1234567890",
    "name": "John Doe"
  },
  "content": "Welcome to OASIS! Your verification code is: 123456",
  "metadata": {
    "template": "verification_sms",
    "variables": {
      "verificationCode": "123456",
      "platformName": "OASIS"
    },
    "priority": "High",
    "encryption": true
  }
}
```

### Send Push Notification
```http
POST /api/messaging/send/push
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "recipient": {
    "id": "user_123",
    "deviceToken": "device_token_123"
  },
  "title": "New Message",
  "body": "You have a new message from Jane Smith",
  "data": {
    "conversationId": "conv_123",
    "messageId": "msg_123",
    "senderId": "user_456"
  },
  "metadata": {
    "template": "new_message_push",
    "variables": {
      "senderName": "Jane Smith",
      "messagePreview": "Hello there!"
    },
    "priority": "Normal",
    "encryption": true
  }
}
```

### Send In-App Message
```http
POST /api/messaging/send/inapp
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "recipient": {
    "id": "user_123"
  },
  "title": "System Notification",
  "content": "Your account has been updated successfully.",
  "type": "info",
  "metadata": {
    "template": "system_notification",
    "variables": {
      "action": "account_updated",
      "timestamp": "2024-01-20T14:30:00Z"
    },
    "priority": "Normal",
    "encryption": true
  }
}
```

## Message Delivery

### Get Message Status
```http
GET /api/messaging/messages/{messageId}/status
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `messageId` (string): Message UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "msg_123",
      "status": "Delivered",
      "delivery": {
        "attempts": 1,
        "maxAttempts": 3,
        "nextRetry": null,
        "error": null
      },
      "tracking": {
        "opened": true,
        "clicked": false,
        "bounced": false,
        "complained": false
      },
      "timestamps": {
        "createdAt": "2024-01-20T14:30:00Z",
        "sentAt": "2024-01-20T14:30:05Z",
        "deliveredAt": "2024-01-20T14:30:10Z",
        "readAt": "2024-01-20T14:35:00Z"
      },
      "lastUpdated": "2024-01-20T14:35:00Z"
    },
    "message": "Message status retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Message Tracking
```http
GET /api/messaging/messages/{messageId}/tracking
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `messageId` (string): Message UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "msg_123",
      "tracking": {
        "opened": true,
        "clicked": false,
        "bounced": false,
        "complained": false
      },
      "events": [
        {
          "type": "sent",
          "timestamp": "2024-01-20T14:30:05Z",
          "details": "Message sent to recipient"
        },
        {
          "type": "delivered",
          "timestamp": "2024-01-20T14:30:10Z",
          "details": "Message delivered to recipient's inbox"
        },
        {
          "type": "opened",
          "timestamp": "2024-01-20T14:35:00Z",
          "details": "Recipient opened the message"
        }
      ],
      "analytics": {
        "openRate": 1.0,
        "clickRate": 0.0,
        "bounceRate": 0.0,
        "complaintRate": 0.0
      },
      "lastUpdated": "2024-01-20T14:35:00Z"
    },
    "message": "Message tracking retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Retry Message
```http
POST /api/messaging/messages/{messageId}/retry
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `messageId` (string): Message UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "msg_123",
      "status": "Retrying",
      "delivery": {
        "attempts": 2,
        "maxAttempts": 3,
        "nextRetry": "2024-01-20T14:35:00Z",
        "error": null
      },
      "retriedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Message retry initiated successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Cancel Message
```http
POST /api/messaging/messages/{messageId}/cancel
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `messageId` (string): Message UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "msg_123",
      "status": "Cancelled",
      "cancelledAt": "2024-01-20T14:30:00Z",
      "reason": "User requested cancellation"
    },
    "message": "Message cancelled successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Message Analytics

### Get Messaging Statistics
```http
GET /api/messaging/stats
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `type` (string, optional): Filter by message type (Email, SMS, Push, In-App)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "statistics": {
        "messages": {
          "total": 10000,
          "sent": 9500,
          "delivered": 9000,
          "read": 8000,
          "failed": 500
        },
        "byType": {
          "Email": 6000,
          "SMS": 2000,
          "Push": 1500,
          "In-App": 500
        },
        "performance": {
          "deliveryRate": 0.95,
          "openRate": 0.89,
          "clickRate": 0.15,
          "bounceRate": 0.02,
          "complaintRate": 0.001
        },
        "timing": {
          "averageDeliveryTime": 5.0,
          "peakDeliveryTime": 15.0,
          "averageOpenTime": 30.0,
          "peakOpenTime": 60.0
        }
      },
      "trends": {
        "messages": "increasing",
        "deliveryRate": "stable",
        "openRate": "increasing",
        "clickRate": "stable"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Messaging statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Message Performance
```http
GET /api/messaging/performance
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "performance": {
        "averageDeliveryTime": 5.0,
        "peakDeliveryTime": 15.0,
        "averageOpenTime": 30.0,
        "peakOpenTime": 60.0,
        "deliveryRate": 0.95,
        "openRate": 0.89,
        "clickRate": 0.15
      },
      "metrics": {
        "messagesPerSecond": 100,
        "averageLatency": 5.0,
        "p95Latency": 10.0,
        "p99Latency": 15.0,
        "errorRate": 0.05,
        "successRate": 0.95
      },
      "trends": {
        "deliveryTime": "stable",
        "openTime": "stable",
        "deliveryRate": "stable",
        "openRate": "increasing"
      },
      "breakdown": {
        "Email": {
          "deliveryRate": 0.96,
          "openRate": 0.90,
          "clickRate": 0.20
        },
        "SMS": {
          "deliveryRate": 0.98,
          "openRate": 0.95,
          "clickRate": 0.10
        },
        "Push": {
          "deliveryRate": 0.92,
          "openRate": 0.85,
          "clickRate": 0.15
        },
        "In-App": {
          "deliveryRate": 0.99,
          "openRate": 0.95,
          "clickRate": 0.25
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Message performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Messaging Health
```http
GET /api/messaging/health
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "status": "Healthy",
      "overallHealth": 0.95,
      "components": {
        "email": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "sms": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "push": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "inapp": {
          "status": "Healthy",
          "health": 0.97,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Email Service Test",
          "status": "Pass",
          "responseTime": 2.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "SMS Service Test",
          "status": "Pass",
          "responseTime": 1.5,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Push Service Test",
          "status": "Pass",
          "responseTime": 1.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "In-App Service Test",
          "status": "Pass",
          "responseTime": 0.5,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Messaging health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Error Responses

### Message Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Message not found",
  "exception": "Message with ID msg_123 not found"
}
```

### Invalid Recipient
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid recipient",
  "exception": "Invalid email address format"
}
```

### Message Send Failed
```json
{
  "result": null,
  "isError": true,
  "message": "Message send failed",
  "exception": "Failed to send message: SMTP server unavailable"
}
```

### Template Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Template not found",
  "exception": "Template 'welcome_email' not found"
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

**← Previous:** [Chat API](Chat-API.md) | **Next:** [Files API](Files-API.md) →
