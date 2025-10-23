# Missions API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Mission Management](#mission-management)
- [Mission Creation](#mission-creation)
- [Mission Updates](#mission-updates)
- [Mission Completion](#mission-completion)
- [Mission Search](#mission-search)
- [Mission Statistics](#mission-statistics)
- [Error Responses](#error-responses)

## Overview

The Missions API provides comprehensive mission and quest management for the STAR gamification system. It handles mission creation, assignment, tracking, completion, and reward distribution across all supported providers.

## Mission Management

### Get All Missions
```http
GET /api/missions
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `status` (string, optional): Mission status (active, completed, expired)
- `difficulty` (string, optional): Difficulty level (easy, medium, hard, expert)
- `category` (string, optional): Mission category
- `sortBy` (string, optional): Sort field (createdAt, difficulty, rewards)
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
          "title": "First Steps",
          "description": "Complete your first mission in the metaverse",
          "type": "Tutorial",
          "difficulty": "Easy",
          "status": "Active",
          "category": "Getting Started",
          "rewards": {
            "karma": 100,
            "experience": 50,
            "items": ["starter_pack"]
          },
          "requirements": {
            "level": 1,
            "prerequisites": []
          },
          "createdAt": "2024-01-15T10:30:00Z",
          "expiresAt": "2024-02-15T10:30:00Z",
          "completionCount": 1250,
          "successRate": 0.95
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
      "title": "First Steps",
      "description": "Complete your first mission in the metaverse",
      "type": "Tutorial",
      "difficulty": "Easy",
      "status": "Active",
      "category": "Getting Started",
      "rewards": {
        "karma": 100,
        "experience": 50,
        "items": ["starter_pack"]
      },
      "requirements": {
        "level": 1,
        "prerequisites": []
      },
      "objectives": [
        {
          "id": "obj_1",
          "description": "Create your avatar",
          "type": "action",
          "completed": false
        },
        {
          "id": "obj_2",
          "description": "Explore the tutorial area",
          "type": "exploration",
          "completed": false
        }
      ],
      "createdAt": "2024-01-15T10:30:00Z",
      "expiresAt": "2024-02-15T10:30:00Z",
      "completionCount": 1250,
      "successRate": 0.95
    },
    "message": "Mission retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Missions by Avatar
```http
GET /api/missions/avatar/{avatarId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `avatarId` (string): Avatar UUID

**Query Parameters:**
- `status` (string, optional): Mission status filter
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "missions": [
        {
          "id": "mission_123",
          "title": "First Steps",
          "status": "In Progress",
          "progress": 0.5,
          "startedAt": "2024-01-20T10:30:00Z",
          "estimatedCompletion": "2024-01-22T10:30:00Z"
        }
      ],
      "totalCount": 1,
      "activeCount": 1,
      "completedCount": 0,
      "expiredCount": 0
    },
    "message": "Avatar missions retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Mission Creation

### Create Mission
```http
POST /api/missions/create
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "title": "Advanced Exploration",
  "description": "Explore 5 different celestial bodies",
  "type": "Exploration",
  "difficulty": "Medium",
  "category": "Adventure",
  "rewards": {
    "karma": 250,
    "experience": 100,
    "items": ["explorer_badge", "celestial_map"]
  },
  "requirements": {
    "level": 5,
    "prerequisites": ["mission_123"]
  },
  "objectives": [
    {
      "description": "Visit Earth",
      "type": "location",
      "target": "celestial_body_earth"
    },
    {
      "description": "Visit Mars",
      "type": "location",
      "target": "celestial_body_mars"
    }
  ],
  "expiresAt": "2024-03-15T10:30:00Z"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "mission_124",
      "title": "Advanced Exploration",
      "description": "Explore 5 different celestial bodies",
      "type": "Exploration",
      "difficulty": "Medium",
      "status": "Active",
      "category": "Adventure",
      "rewards": {
        "karma": 250,
        "experience": 100,
        "items": ["explorer_badge", "celestial_map"]
      },
      "requirements": {
        "level": 5,
        "prerequisites": ["mission_123"]
      },
      "objectives": [
        {
          "id": "obj_3",
          "description": "Visit Earth",
          "type": "location",
          "target": "celestial_body_earth",
          "completed": false
        },
        {
          "id": "obj_4",
          "description": "Visit Mars",
          "type": "location",
          "target": "celestial_body_mars",
          "completed": false
        }
      ],
      "createdAt": "2024-01-20T14:30:00Z",
      "expiresAt": "2024-03-15T10:30:00Z",
      "completionCount": 0,
      "successRate": 0.0
    },
    "message": "Mission created successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Create Mission with Advanced Options
```http
POST /api/missions/create/{providerType}/{setGlobally}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerType` (string): Provider type
- `setGlobally` (boolean): Set as global default

## Mission Updates

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
  "title": "Updated Mission Title",
  "description": "Updated mission description",
  "difficulty": "Hard",
  "rewards": {
    "karma": 500,
    "experience": 200,
    "items": ["premium_badge"]
  },
  "expiresAt": "2024-04-15T10:30:00Z"
}
```

### Update Mission Status
```http
PATCH /api/missions/{missionId}/status
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "status": "Paused",
  "reason": "Under review"
}
```

## Mission Completion

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
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "status": "In Progress",
      "startedAt": "2024-01-20T14:30:00Z",
      "estimatedCompletion": "2024-01-22T14:30:00Z",
      "progress": 0.0
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
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "completionData": {
    "objectivesCompleted": ["obj_1", "obj_2"],
    "timeSpent": 3600,
    "notes": "Mission completed successfully"
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "missionId": "mission_123",
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "status": "Completed",
      "completedAt": "2024-01-20T15:30:00Z",
      "timeSpent": 3600,
      "rewards": {
        "karma": 100,
        "experience": 50,
        "items": ["starter_pack"]
      },
      "newLevel": 2,
      "newKarmaBalance": 1100
    },
    "message": "Mission completed successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Abandon Mission
```http
POST /api/missions/{missionId}/abandon
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "missionId": "mission_123",
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "status": "Abandoned",
      "abandonedAt": "2024-01-20T16:00:00Z",
      "penalty": {
        "karma": -10,
        "experience": 0
      }
    },
    "message": "Mission abandoned successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Mission Search

### Search Missions
```http
POST /api/missions/search
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "query": "exploration",
  "filters": {
    "difficulty": ["Easy", "Medium"],
    "category": "Adventure",
    "status": "Active",
    "minReward": 100
  },
  "sortBy": "rewards",
  "sortOrder": "desc",
  "limit": 20,
  "offset": 0
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "missions": [
        {
          "id": "mission_124",
          "title": "Advanced Exploration",
          "description": "Explore 5 different celestial bodies",
          "difficulty": "Medium",
          "category": "Adventure",
          "rewards": {
            "karma": 250,
            "experience": 100
          },
          "matchScore": 0.95
        }
      ],
      "totalCount": 1,
      "searchTime": 0.05,
      "filters": {
        "query": "exploration",
        "difficulty": ["Easy", "Medium"],
        "category": "Adventure"
      }
    },
    "message": "Mission search completed successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Mission Statistics

### Get Mission Stats
```http
GET /api/missions/stats
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "totalMissions": 150,
      "activeMissions": 120,
      "completedMissions": 25,
      "expiredMissions": 5,
      "averageCompletionTime": 7200,
      "successRate": 0.85,
      "popularCategories": {
        "Tutorial": 30,
        "Adventure": 45,
        "Exploration": 35,
        "Combat": 20,
        "Social": 20
      },
      "difficultyDistribution": {
        "Easy": 60,
        "Medium": 50,
        "Hard": 30,
        "Expert": 10
      },
      "averageRewards": {
        "karma": 150,
        "experience": 75
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Mission statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Avatar Mission Stats
```http
GET /api/missions/stats/avatar/{avatarId}
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "totalMissions": 15,
      "completedMissions": 12,
      "activeMissions": 2,
      "abandonedMissions": 1,
      "successRate": 0.92,
      "averageCompletionTime": 5400,
      "totalKarmaEarned": 1500,
      "totalExperienceEarned": 750,
      "favoriteCategory": "Adventure",
      "achievements": [
        "First Mission",
        "Explorer",
        "Mission Master"
      ],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Avatar mission statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
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
  "exception": "Mission is already in progress"
}
```

### Insufficient Requirements
```json
{
  "result": null,
  "isError": true,
  "message": "Requirements not met",
  "exception": "Avatar level 3 required, current level 2"
}
```

### Mission Expired
```json
{
  "result": null,
  "isError": true,
  "message": "Mission expired",
  "exception": "Mission expired on 2024-01-15T10:30:00Z"
}
```

---

## Navigation

**← Previous:** [README](README.md) | **Next:** [Quests API](Quests-API.md) →
