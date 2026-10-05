# Quests API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Quest Management](#quest-management)
- [Quest Operations](#quest-operations)
- [Quest Analytics](#quest-analytics)
- [Quest Security](#quest-security)
- [Error Responses](#error-responses)

## Overview

The Quests API provides comprehensive quest management services for the STAR ecosystem. It handles quest creation, assignment, completion, and analytics with support for multiple quest types, real-time updates, and advanced security features.

## Quest Management

### Get All Quests
```http
GET /api/quests
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by type (Main, Side, Daily, Weekly, Event)
- `status` (string, optional): Filter by status (Active, Inactive, Completed, Failed)
- `difficulty` (string, optional): Filter by difficulty (Easy, Medium, Hard, Expert)
- `sortBy` (string, optional): Sort field (name, createdAt, difficulty, rewards)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "quests": [
        {
          "id": "quest_123",
          "name": "STAR Explorer",
          "description": "Explore the STAR platform and complete your first tasks",
          "type": "Main",
          "status": "Active",
          "difficulty": "Easy",
          "creator": {
            "id": "user_123",
            "username": "john_doe",
            "avatar": "https://example.com/avatars/john.jpg"
          },
          "objectives": [
            {
              "id": "obj_123",
              "description": "Create your first avatar",
              "type": "Action",
              "status": "Completed",
              "completedAt": "2024-01-20T14:30:00Z"
            },
            {
              "id": "obj_124",
              "description": "Complete your profile",
              "type": "Action",
              "status": "Pending",
              "completedAt": null
            }
          ],
          "rewards": {
            "karma": 100,
            "experience": 50,
            "currency": {
              "amount": 10.0,
              "type": "STAR"
            },
            "items": ["Explorer Badge", "Starter Pack"]
          },
          "requirements": {
            "level": 1,
            "prerequisites": [],
            "timeLimit": 7,
            "maxParticipants": 1000
          },
          "metadata": {
            "tags": ["tutorial", "beginner", "exploration"],
            "category": "Tutorial",
            "version": "1.0"
          },
          "createdAt": "2024-01-20T14:30:00Z",
          "lastModified": "2024-01-20T14:30:00Z"
        }
      ],
      "totalCount": 1,
      "limit": 50,
      "offset": 0
    },
    "message": "Quests retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Quest by ID
```http
GET /api/quests/{questId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `questId` (string): Quest UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "quest_123",
      "name": "STAR Explorer",
      "description": "Explore the STAR platform and complete your first tasks",
      "type": "Main",
      "status": "Active",
      "difficulty": "Easy",
      "creator": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "objectives": [
        {
          "id": "obj_123",
          "description": "Create your first avatar",
          "type": "Action",
          "status": "Completed",
          "completedAt": "2024-01-20T14:30:00Z"
        },
        {
          "id": "obj_124",
          "description": "Complete your profile",
          "type": "Action",
          "status": "Pending",
          "completedAt": null
        }
      ],
      "rewards": {
        "karma": 100,
        "experience": 50,
        "currency": {
          "amount": 10.0,
          "type": "STAR"
        },
        "items": ["Explorer Badge", "Starter Pack"]
      },
      "requirements": {
        "level": 1,
        "prerequisites": [],
        "timeLimit": 7,
        "maxParticipants": 1000
      },
      "metadata": {
        "tags": ["tutorial", "beginner", "exploration"],
        "category": "Tutorial",
        "version": "1.0"
      },
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC"
      },
      "analytics": {
        "participants": 500,
        "completions": 300,
        "completionRate": 0.6,
        "averageTime": 2.5
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "Quest retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Create Quest
```http
POST /api/quests
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "name": "STAR Master",
  "description": "Master the STAR platform and become an expert user",
  "type": "Main",
  "difficulty": "Hard",
  "objectives": [
    {
      "description": "Complete 10 advanced tasks",
      "type": "Action",
      "target": 10
    },
    {
      "description": "Help 5 other users",
      "type": "Social",
      "target": 5
    }
  ],
  "rewards": {
    "karma": 500,
    "experience": 250,
    "currency": {
      "amount": 100.0,
      "type": "STAR"
    },
    "items": ["Master Badge", "Expert Pack"]
  },
  "requirements": {
    "level": 10,
    "prerequisites": ["quest_123"],
    "timeLimit": 30,
    "maxParticipants": 100
  },
  "metadata": {
    "tags": ["advanced", "master", "expert"],
    "category": "Advanced",
    "version": "1.0"
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "quest_124",
      "name": "STAR Master",
      "description": "Master the STAR platform and become an expert user",
      "type": "Main",
      "status": "Active",
      "difficulty": "Hard",
      "creator": {
        "id": "user_456",
        "username": "jane_smith",
        "avatar": "https://example.com/avatars/jane.jpg"
      },
      "objectives": [
        {
          "id": "obj_125",
          "description": "Complete 10 advanced tasks",
          "type": "Action",
          "target": 10,
          "status": "Pending",
          "completedAt": null
        },
        {
          "id": "obj_126",
          "description": "Help 5 other users",
          "type": "Social",
          "target": 5,
          "status": "Pending",
          "completedAt": null
        }
      ],
      "rewards": {
        "karma": 500,
        "experience": 250,
        "currency": {
          "amount": 100.0,
          "type": "STAR"
        },
        "items": ["Master Badge", "Expert Pack"]
      },
      "requirements": {
        "level": 10,
        "prerequisites": ["quest_123"],
        "timeLimit": 30,
        "maxParticipants": 100
      },
      "metadata": {
        "tags": ["advanced", "master", "expert"],
        "category": "Advanced",
        "version": "1.0"
      },
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC"
      },
      "analytics": {
        "participants": 0,
        "completions": 0,
        "completionRate": 0.0,
        "averageTime": 0.0
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "Quest created successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Quest
```http
PUT /api/quests/{questId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `questId` (string): Quest UUID

**Request Body:**
```json
{
  "name": "Updated STAR Master",
  "description": "Updated description for mastering the STAR platform",
  "objectives": [
    {
      "id": "obj_125",
      "description": "Complete 15 advanced tasks",
      "type": "Action",
      "target": 15,
      "status": "Pending"
    },
    {
      "id": "obj_126",
      "description": "Help 10 other users",
      "type": "Social",
      "target": 10,
      "status": "Pending"
    }
  ],
  "rewards": {
    "karma": 750,
    "experience": 375,
    "currency": {
      "amount": 150.0,
      "type": "STAR"
    },
    "items": ["Master Badge", "Expert Pack", "Elite Pack"]
  }
}
```

### Delete Quest
```http
DELETE /api/quests/{questId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `questId` (string): Quest UUID

## Quest Operations

### Start Quest
```http
POST /api/quests/{questId}/start
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `questId` (string): Quest UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "questId": "quest_123",
      "participant": {
        "id": "user_456",
        "username": "jane_smith",
        "avatar": "https://example.com/avatars/jane.jpg"
      },
      "status": "Active",
      "startedAt": "2024-01-20T14:30:00Z",
      "deadline": "2024-01-27T14:30:00Z",
      "progress": {
        "completed": 0,
        "total": 2,
        "percentage": 0.0
      }
    },
    "message": "Quest started successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Complete Quest
```http
POST /api/quests/{questId}/complete
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `questId` (string): Quest UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "questId": "quest_123",
      "participant": {
        "id": "user_456",
        "username": "jane_smith",
        "avatar": "https://example.com/avatars/jane.jpg"
      },
      "status": "Completed",
      "completedAt": "2024-01-20T14:30:00Z",
      "duration": 2.5,
      "rewards": {
        "karma": 100,
        "experience": 50,
        "currency": {
          "amount": 10.0,
          "type": "STAR"
        },
        "items": ["Explorer Badge", "Starter Pack"]
      },
      "progress": {
        "completed": 2,
        "total": 2,
        "percentage": 100.0
      }
    },
    "message": "Quest completed successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Quest Progress
```http
GET /api/quests/{questId}/progress
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `questId` (string): Quest UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "questId": "quest_123",
      "participant": {
        "id": "user_456",
        "username": "jane_smith",
        "avatar": "https://example.com/avatars/jane.jpg"
      },
      "status": "Active",
      "startedAt": "2024-01-20T14:30:00Z",
      "deadline": "2024-01-27T14:30:00Z",
      "progress": {
        "completed": 1,
        "total": 2,
        "percentage": 50.0
      },
      "objectives": [
        {
          "id": "obj_123",
          "description": "Create your first avatar",
          "type": "Action",
          "status": "Completed",
          "completedAt": "2024-01-20T14:30:00Z"
        },
        {
          "id": "obj_124",
          "description": "Complete your profile",
          "type": "Action",
          "status": "Pending",
          "completedAt": null
        }
      ],
      "timeRemaining": 6.5
    },
    "message": "Quest progress retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Quest Participants
```http
GET /api/quests/{questId}/participants
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `questId` (string): Quest UUID

**Query Parameters:**
- `status` (string, optional): Filter by status (Active, Completed, Failed)
- `sortBy` (string, optional): Sort field (startedAt, completedAt, progress)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "questId": "quest_123",
      "participants": [
        {
          "id": "user_456",
          "username": "jane_smith",
          "avatar": "https://example.com/avatars/jane.jpg",
          "status": "Active",
          "startedAt": "2024-01-20T14:30:00Z",
          "completedAt": null,
          "progress": {
            "completed": 1,
            "total": 2,
            "percentage": 50.0
          }
        },
        {
          "id": "user_789",
          "username": "bob_wilson",
          "avatar": "https://example.com/avatars/bob.jpg",
          "status": "Completed",
          "startedAt": "2024-01-19T10:00:00Z",
          "completedAt": "2024-01-20T14:30:00Z",
          "progress": {
            "completed": 2,
            "total": 2,
            "percentage": 100.0
          }
        }
      ],
      "totalCount": 2,
      "activeCount": 1,
      "completedCount": 1,
      "failedCount": 0
    },
    "message": "Quest participants retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Quest Leaderboard
```http
GET /api/quests/{questId}/leaderboard
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `questId` (string): Quest UUID

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 10)
- `sortBy` (string, optional): Sort field (progress, time, score)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "questId": "quest_123",
      "leaderboard": [
        {
          "rank": 1,
          "participant": {
            "id": "user_789",
            "username": "bob_wilson",
            "avatar": "https://example.com/avatars/bob.jpg"
          },
          "status": "Completed",
          "progress": {
            "completed": 2,
            "total": 2,
            "percentage": 100.0
          },
          "score": 100,
          "time": 2.5,
          "completedAt": "2024-01-20T14:30:00Z"
        },
        {
          "rank": 2,
          "participant": {
            "id": "user_456",
            "username": "jane_smith",
            "avatar": "https://example.com/avatars/jane.jpg"
          },
          "status": "Active",
          "progress": {
            "completed": 1,
            "total": 2,
            "percentage": 50.0
          },
          "score": 50,
          "time": 1.0,
          "completedAt": null
        }
      ],
      "totalCount": 2,
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Quest leaderboard retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Quest Analytics

### Get Quest Statistics
```http
GET /api/quests/stats
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `type` (string, optional): Filter by quest type

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "statistics": {
        "quests": {
          "total": 1000,
          "active": 800,
          "completed": 150,
          "failed": 50
        },
        "byType": {
          "Main": 500,
          "Side": 300,
          "Daily": 150,
          "Weekly": 50
        },
        "byDifficulty": {
          "Easy": 400,
          "Medium": 300,
          "Hard": 200,
          "Expert": 100
        },
        "participants": {
          "total": 5000,
          "active": 3000,
          "completed": 1500,
          "failed": 500
        },
        "performance": {
          "completionRate": 0.75,
          "averageTime": 3.5,
          "userSatisfaction": 0.88,
          "retentionRate": 0.85
        }
      },
      "trends": {
        "quests": "increasing",
        "participants": "increasing",
        "performance": "stable"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Quest statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Quest Performance
```http
GET /api/quests/performance
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "performance": {
        "completionRate": 0.75,
        "averageTime": 3.5,
        "userSatisfaction": 0.88,
        "retentionRate": 0.85,
        "averageScore": 85.0,
        "peakScore": 100.0
      },
      "metrics": {
        "questsPerHour": 50,
        "averageLatency": 2.0,
        "p95Latency": 5.0,
        "p99Latency": 10.0,
        "errorRate": 0.02,
        "successRate": 0.98
      },
      "trends": {
        "completionRate": "stable",
        "averageTime": "stable",
        "satisfaction": "increasing",
        "retention": "stable"
      },
      "breakdown": {
        "Main": {
          "completionRate": 0.80,
          "averageTime": 2.5,
          "satisfaction": 0.90
        },
        "Side": {
          "completionRate": 0.70,
          "averageTime": 5.0,
          "satisfaction": 0.85
        },
        "Daily": {
          "completionRate": 0.75,
          "averageTime": 1.0,
          "satisfaction": 0.88
        },
        "Weekly": {
          "completionRate": 0.60,
          "averageTime": 10.0,
          "satisfaction": 0.95
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Quest performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Quest Health
```http
GET /api/quests/health
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
        "creation": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "execution": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "tracking": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "rewards": {
          "status": "Healthy",
          "health": 0.97,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Creation Test",
          "status": "Pass",
          "responseTime": 2.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Execution Test",
          "status": "Pass",
          "responseTime": 1.5,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Tracking Test",
          "status": "Pass",
          "responseTime": 1.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Rewards Test",
          "status": "Pass",
          "responseTime": 0.8,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Quest health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Quest Security

### Get Quest Security
```http
GET /api/quests/{questId}/security
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `questId` (string): Quest UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "questId": "quest_123",
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC",
        "verification": true,
        "auditLogging": true
      },
      "access": {
        "lastAccessed": "2024-01-20T14:30:00Z",
        "accessCount": 100,
        "failedAttempts": 2,
        "locked": false
      },
      "compliance": {
        "gdpr": true,
        "ccpa": true,
        "coppa": true,
        "privacy": true
      },
      "audit": {
        "lastAudit": "2024-01-20T14:30:00Z",
        "auditCount": 5,
        "complianceScore": 0.98
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Quest security retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Quest Security
```http
PUT /api/quests/{questId}/security
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `questId` (string): Quest UUID

**Request Body:**
```json
{
  "encryption": "AES-256",
  "authentication": "JWT",
  "authorization": "RBAC",
  "verification": true,
  "auditLogging": true
}
```

## Error Responses

### Quest Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Quest not found",
  "exception": "Quest with ID quest_123 not found"
}
```

### Quest Already Started
```json
{
  "result": null,
  "isError": true,
  "message": "Quest already started",
  "exception": "Quest has already been started by this user"
}
```

### Quest Expired
```json
{
  "result": null,
  "isError": true,
  "message": "Quest expired",
  "exception": "Quest has expired and cannot be started"
}
```

### Insufficient Requirements
```json
{
  "result": null,
  "isError": true,
  "message": "Insufficient requirements",
  "exception": "User does not meet the quest requirements"
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

**← Previous:** [Missions API](Missions-API.md) | **Next:** [Competition API](Competition-API.md) →
