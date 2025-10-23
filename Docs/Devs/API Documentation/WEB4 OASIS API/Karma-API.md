# Karma API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Karma Management](#karma-management)
- [Karma Operations](#karma-operations)
- [Karma Analytics](#karma-analytics)
- [Karma Security](#karma-security)
- [Error Responses](#error-responses)

## Overview

The Karma API provides comprehensive karma management services for the OASIS ecosystem. It handles karma earning, spending, tracking, and analytics with support for multiple karma types, real-time updates, and advanced security features.

## Karma Management

### Get All Karma
```http
GET /api/karma
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by type (Earned, Spent, Transferred, Bonus)
- `status` (string, optional): Filter by status (Active, Pending, Expired, Cancelled)
- `category` (string, optional): Filter by category (Gaming, Social, Learning, Contribution)
- `sortBy` (string, optional): Sort field (amount, createdAt, expiresAt)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "karma": [
        {
          "id": "karma_123",
          "type": "Earned",
          "amount": 100,
          "category": "Gaming",
          "status": "Active",
          "user": {
            "id": "user_123",
            "username": "john_doe",
            "avatar": "https://example.com/avatars/john.jpg"
          },
          "source": {
            "type": "Achievement",
            "name": "First Victory",
            "description": "Won your first game"
          },
          "metadata": {
            "gameId": "game_123",
            "achievementId": "ach_123",
            "difficulty": "Easy",
            "multiplier": 1.0
          },
          "expiresAt": "2025-01-20T14:30:00Z",
          "createdAt": "2024-01-20T14:30:00Z",
          "lastModified": "2024-01-20T14:30:00Z"
        }
      ],
      "totalCount": 1,
      "limit": 50,
      "offset": 0
    },
    "message": "Karma retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Karma by ID
```http
GET /api/karma/{karmaId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `karmaId` (string): Karma UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "karma_123",
      "type": "Earned",
      "amount": 100,
      "category": "Gaming",
      "status": "Active",
      "user": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "source": {
        "type": "Achievement",
        "name": "First Victory",
        "description": "Won your first game"
      },
      "metadata": {
        "gameId": "game_123",
        "achievementId": "ach_123",
        "difficulty": "Easy",
        "multiplier": 1.0
      },
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC"
      },
      "analytics": {
        "views": 5,
        "shares": 0,
        "lastAccessed": "2024-01-20T14:30:00Z"
      },
      "expiresAt": "2025-01-20T14:30:00Z",
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "Karma retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Create Karma
```http
POST /api/karma
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "type": "Earned",
  "amount": 50,
  "category": "Social",
  "user": {
    "id": "user_456",
    "username": "jane_smith"
  },
  "source": {
    "type": "Contribution",
    "name": "Helpful Comment",
    "description": "Provided helpful feedback to another user"
  },
  "metadata": {
    "postId": "post_123",
    "commentId": "comment_123",
    "helpfulness": "High",
    "multiplier": 1.5
  },
  "expiresAt": "2025-01-20T14:30:00Z"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "karma_124",
      "type": "Earned",
      "amount": 50,
      "category": "Social",
      "status": "Active",
      "user": {
        "id": "user_456",
        "username": "jane_smith",
        "avatar": "https://example.com/avatars/jane.jpg"
      },
      "source": {
        "type": "Contribution",
        "name": "Helpful Comment",
        "description": "Provided helpful feedback to another user"
      },
      "metadata": {
        "postId": "post_123",
        "commentId": "comment_123",
        "helpfulness": "High",
        "multiplier": 1.5
      },
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC"
      },
      "analytics": {
        "views": 0,
        "shares": 0,
        "lastAccessed": null
      },
      "expiresAt": "2025-01-20T14:30:00Z",
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "Karma created successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Karma
```http
PUT /api/karma/{karmaId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `karmaId` (string): Karma UUID

**Request Body:**
```json
{
  "amount": 75,
  "metadata": {
    "postId": "post_123",
    "commentId": "comment_123",
    "helpfulness": "Very High",
    "multiplier": 2.0
  }
}
```

### Delete Karma
```http
DELETE /api/karma/{karmaId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `karmaId` (string): Karma UUID

## Karma Operations

### Earn Karma
```http
POST /api/karma/earn
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "amount": 25,
  "category": "Learning",
  "source": {
    "type": "Course Completion",
    "name": "OASIS Basics",
    "description": "Completed the OASIS basics course"
  },
  "metadata": {
    "courseId": "course_123",
    "completionTime": 120,
    "difficulty": "Beginner",
    "multiplier": 1.0
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "karmaId": "karma_125",
      "type": "Earned",
      "amount": 25,
      "category": "Learning",
      "status": "Active",
      "user": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "source": {
        "type": "Course Completion",
        "name": "OASIS Basics",
        "description": "Completed the OASIS basics course"
      },
      "metadata": {
        "courseId": "course_123",
        "completionTime": 120,
        "difficulty": "Beginner",
        "multiplier": 1.0
      },
      "totalKarma": 125,
      "earnedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Karma earned successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Spend Karma
```http
POST /api/karma/spend
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "amount": 50,
  "category": "Reward",
  "source": {
    "type": "Purchase",
    "name": "Premium Feature",
    "description": "Unlocked premium feature"
  },
  "metadata": {
    "featureId": "feature_123",
    "featureName": "Advanced Analytics",
    "duration": 30
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "karmaId": "karma_126",
      "type": "Spent",
      "amount": 50,
      "category": "Reward",
      "status": "Active",
      "user": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "source": {
        "type": "Purchase",
        "name": "Premium Feature",
        "description": "Unlocked premium feature"
      },
      "metadata": {
        "featureId": "feature_123",
        "featureName": "Advanced Analytics",
        "duration": 30
      },
      "totalKarma": 75,
      "spentAt": "2024-01-20T14:30:00Z"
    },
    "message": "Karma spent successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Transfer Karma
```http
POST /api/karma/transfer
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "amount": 25,
  "recipient": {
    "id": "user_456",
    "username": "jane_smith"
  },
  "source": {
    "type": "Gift",
    "name": "Karma Gift",
    "description": "Gifted karma to another user"
  },
  "metadata": {
    "reason": "Thank you for your help",
    "message": "Thanks for the great advice!"
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "karmaId": "karma_127",
      "type": "Transferred",
      "amount": 25,
      "category": "Gift",
      "status": "Active",
      "user": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "recipient": {
        "id": "user_456",
        "username": "jane_smith",
        "avatar": "https://example.com/avatars/jane.jpg"
      },
      "source": {
        "type": "Gift",
        "name": "Karma Gift",
        "description": "Gifted karma to another user"
      },
      "metadata": {
        "reason": "Thank you for your help",
        "message": "Thanks for the great advice!"
      },
      "totalKarma": 50,
      "transferredAt": "2024-01-20T14:30:00Z"
    },
    "message": "Karma transferred successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Karma Balance
```http
GET /api/karma/balance
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "user": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "balance": {
        "total": 1000,
        "available": 950,
        "locked": 50,
        "pending": 0
      },
      "breakdown": {
        "Gaming": 400,
        "Social": 300,
        "Learning": 200,
        "Contribution": 100
      },
      "recent": {
        "earned": 100,
        "spent": 50,
        "transferred": 25,
        "received": 75
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Karma balance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Karma History
```http
GET /api/karma/history
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by type (Earned, Spent, Transferred, Bonus)
- `category` (string, optional): Filter by category
- `startDate` (string, optional): Start date (ISO 8601)
- `endDate` (string, optional): End date (ISO 8601)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "history": [
        {
          "id": "karma_123",
          "type": "Earned",
          "amount": 100,
          "category": "Gaming",
          "source": {
            "type": "Achievement",
            "name": "First Victory",
            "description": "Won your first game"
          },
          "metadata": {
            "gameId": "game_123",
            "achievementId": "ach_123",
            "difficulty": "Easy",
            "multiplier": 1.0
          },
          "balance": 1000,
          "createdAt": "2024-01-20T14:30:00Z"
        },
        {
          "id": "karma_124",
          "type": "Spent",
          "amount": 50,
          "category": "Reward",
          "source": {
            "type": "Purchase",
            "name": "Premium Feature",
            "description": "Unlocked premium feature"
          },
          "metadata": {
            "featureId": "feature_123",
            "featureName": "Advanced Analytics",
            "duration": 30
          },
          "balance": 950,
          "createdAt": "2024-01-20T14:25:00Z"
        }
      ],
      "totalCount": 2,
      "limit": 50,
      "offset": 0
    },
    "message": "Karma history retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Karma Analytics

### Get Karma Statistics
```http
GET /api/karma/stats
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `category` (string, optional): Filter by category

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "statistics": {
        "karma": {
          "total": 10000,
          "earned": 8000,
          "spent": 5000,
          "transferred": 1000,
          "received": 2000
        },
        "byCategory": {
          "Gaming": 4000,
          "Social": 3000,
          "Learning": 2000,
          "Contribution": 1000
        },
        "byType": {
          "Earned": 8000,
          "Spent": 5000,
          "Transferred": 1000,
          "Bonus": 500
        },
        "performance": {
          "averageEarned": 50,
          "averageSpent": 25,
          "averageTransferred": 10,
          "retentionRate": 0.85
        }
      },
      "trends": {
        "karma": "increasing",
        "earned": "increasing",
        "spent": "stable",
        "transferred": "increasing"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Karma statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Karma Performance
```http
GET /api/karma/performance
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "performance": {
        "averageEarned": 50,
        "peakEarned": 200,
        "averageSpent": 25,
        "peakSpent": 100,
        "averageTransferred": 10,
        "peakTransferred": 50,
        "retentionRate": 0.85,
        "engagementRate": 0.75
      },
      "metrics": {
        "transactionsPerHour": 100,
        "averageLatency": 1.0,
        "p95Latency": 2.0,
        "p99Latency": 5.0,
        "errorRate": 0.001,
        "successRate": 0.999
      },
      "trends": {
        "earned": "increasing",
        "spent": "stable",
        "transferred": "increasing",
        "retention": "stable"
      },
      "breakdown": {
        "Gaming": {
          "averageEarned": 60,
          "averageSpent": 30,
          "retentionRate": 0.90
        },
        "Social": {
          "averageEarned": 40,
          "averageSpent": 20,
          "retentionRate": 0.80
        },
        "Learning": {
          "averageEarned": 50,
          "averageSpent": 25,
          "retentionRate": 0.85
        },
        "Contribution": {
          "averageEarned": 30,
          "averageSpent": 15,
          "retentionRate": 0.75
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Karma performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Karma Health
```http
GET /api/karma/health
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
        "earning": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "spending": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "transfer": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "analytics": {
          "status": "Healthy",
          "health": 0.97,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Earning Test",
          "status": "Pass",
          "responseTime": 0.5,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Spending Test",
          "status": "Pass",
          "responseTime": 0.8,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Transfer Test",
          "status": "Pass",
          "responseTime": 1.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Analytics Test",
          "status": "Pass",
          "responseTime": 1.2,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Karma health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Karma Security

### Get Karma Security
```http
GET /api/karma/{karmaId}/security
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `karmaId` (string): Karma UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "karmaId": "karma_123",
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC",
        "auditLogging": true,
        "antiFraud": true
      },
      "access": {
        "lastAccessed": "2024-01-20T14:30:00Z",
        "accessCount": 10,
        "failedAttempts": 0,
        "locked": false
      },
      "compliance": {
        "gdpr": true,
        "ccpa": true,
        "sox": true,
        "pci": true
      },
      "audit": {
        "lastAudit": "2024-01-20T14:30:00Z",
        "auditCount": 5,
        "complianceScore": 0.98
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Karma security retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Karma Security
```http
PUT /api/karma/{karmaId}/security
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `karmaId` (string): Karma UUID

**Request Body:**
```json
{
  "encryption": "AES-256",
  "authentication": "JWT",
  "authorization": "RBAC",
  "auditLogging": true,
  "antiFraud": true
}
```

## Error Responses

### Karma Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Karma not found",
  "exception": "Karma with ID karma_123 not found"
}
```

### Insufficient Karma
```json
{
  "result": null,
  "isError": true,
  "message": "Insufficient karma",
  "exception": "Not enough karma to complete this action"
}
```

### Karma Expired
```json
{
  "result": null,
  "isError": true,
  "message": "Karma expired",
  "exception": "Karma has expired and cannot be used"
}
```

### Invalid Amount
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid amount",
  "exception": "Karma amount must be positive"
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

---

## Navigation

**← Previous:** [Gifts API](Gifts-API.md) | **Next:** [Data API](Data-API.md) →