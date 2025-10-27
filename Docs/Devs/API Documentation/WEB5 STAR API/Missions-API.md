# Missions API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Mission Management](#mission-management)
- [Mission Operations](#mission-operations)
- [Mission Analytics](#mission-analytics)
- [Mission Security](#mission-security)
- [Error Responses](#error-responses)

## Overview

The Missions API provides comprehensive mission management services for the STAR ecosystem. It handles mission creation, assignment, completion, and analytics with support for multiple mission types, real-time updates, and advanced security features.

## Mission Management

### Get All Missions
```http
GET /api/missions
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by type (Quest, Challenge, Task, Achievement)
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
      "missions": [
        {
          "id": "mission_123",
          "name": "STAR Explorer",
          "description": "Explore the STAR platform and complete your first tasks",
          "type": "Quest",
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
    "message": "Missions retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Mission by ID
```http
GET /api/missions/{missionId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `missionId` (string): Mission UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "mission_123",
      "name": "STAR Explorer",
      "description": "Explore the STAR platform and complete your first tasks",
      "type": "Quest",
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
    "message": "Mission retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Create Mission
```http
POST /api/missions
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "name": "STAR Master",
  "description": "Master the STAR platform and become an expert user",
  "type": "Challenge",
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
    "prerequisites": ["mission_123"],
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
      "id": "mission_124",
      "name": "STAR Master",
      "description": "Master the STAR platform and become an expert user",
      "type": "Challenge",
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
        "prerequisites": ["mission_123"],
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
    "message": "Mission created successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Mission
```http
PUT /api/missions/{missionId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `missionId` (string): Mission UUID

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

### Delete Mission
```http
DELETE /api/missions/{missionId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `missionId` (string): Mission UUID

## Mission Operations

### Start Mission
```http
POST /api/missions/{missionId}/start
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `missionId` (string): Mission UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "missionId": "mission_123",
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
    "message": "Mission started successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Complete Mission
```http
POST /api/missions/{missionId}/complete
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `missionId` (string): Mission UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "missionId": "mission_123",
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
    "message": "Mission completed successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Mission Progress
```http
GET /api/missions/{missionId}/progress
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `missionId` (string): Mission UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "missionId": "mission_123",
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
    "message": "Mission progress retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Mission Participants
```http
GET /api/missions/{missionId}/participants
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `missionId` (string): Mission UUID

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
      "missionId": "mission_123",
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
    "message": "Mission participants retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Mission Leaderboard
```http
GET /api/missions/{missionId}/leaderboard
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `missionId` (string): Mission UUID

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
      "missionId": "mission_123",
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
    "message": "Mission leaderboard retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Mission Analytics

### Get Mission Statistics
```http
GET /api/missions/stats
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `type` (string, optional): Filter by mission type

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "statistics": {
        "missions": {
          "total": 1000,
          "active": 800,
          "completed": 150,
          "failed": 50
        },
        "byType": {
          "Quest": 500,
          "Challenge": 300,
          "Task": 150,
          "Achievement": 50
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
        "missions": "increasing",
        "participants": "increasing",
        "performance": "stable"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Mission statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Mission Performance
```http
GET /api/missions/performance
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
        "missionsPerHour": 50,
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
        "Quest": {
          "completionRate": 0.80,
          "averageTime": 2.5,
          "satisfaction": 0.90
        },
        "Challenge": {
          "completionRate": 0.70,
          "averageTime": 5.0,
          "satisfaction": 0.85
        },
        "Task": {
          "completionRate": 0.75,
          "averageTime": 1.0,
          "satisfaction": 0.88
        },
        "Achievement": {
          "completionRate": 0.60,
          "averageTime": 10.0,
          "satisfaction": 0.95
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Mission performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Mission Health
```http
GET /api/missions/health
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
    "message": "Mission health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Mission Security

### Get Mission Security
```http
GET /api/missions/{missionId}/security
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `missionId` (string): Mission UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "missionId": "mission_123",
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
    "message": "Mission security retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Mission Security
```http
PUT /api/missions/{missionId}/security
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `missionId` (string): Mission UUID

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

### Mission Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Mission not found",
  "exception": "Mission with ID mission_123 not found"
}
```

### Mission Already Started
```json
{
  "result": null,
  "isError": true,
  "message": "Mission already started",
  "exception": "Mission has already been started by this user"
}
```

### Mission Expired
```json
{
  "result": null,
  "isError": true,
  "message": "Mission expired",
  "exception": "Mission has expired and cannot be started"
}
```

### Insufficient Requirements
```json
{
  "result": null,
  "isError": true,
  "message": "Insufficient requirements",
  "exception": "User does not meet the mission requirements"
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

**← Previous:** [Avatar API](Avatar-API.md) | **Next:** [Quests API](Quests-API.md) →