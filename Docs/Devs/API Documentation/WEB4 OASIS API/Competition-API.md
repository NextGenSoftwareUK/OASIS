# Competition API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Competition Management](#competition-management)
- [Competition Operations](#competition-operations)
- [Competition Analytics](#competition-analytics)
- [Competition Security](#competition-security)
- [Error Responses](#error-responses)

## Overview

The Competition API provides comprehensive competition management services for the OASIS ecosystem. It handles competition creation, management, participation, scoring, and analytics with support for multiple competition types, real-time updates, and advanced security features.

## Competition Management

### Get All Competitions
```http
GET /api/competition
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `status` (string, optional): Filter by status (Active, Inactive, Completed, Cancelled)
- `type` (string, optional): Filter by type (Tournament, League, Challenge, Contest)
- `category` (string, optional): Filter by category (Gaming, Sports, Academic, Creative)
- `sortBy` (string, optional): Sort field (name, startDate, endDate, participants)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "competitions": [
        {
          "id": "comp_123",
          "name": "OASIS Gaming Championship",
          "description": "Annual gaming championship for OASIS platform users",
          "type": "Tournament",
          "category": "Gaming",
          "status": "Active",
          "organizer": {
            "id": "user_123",
            "username": "john_doe",
            "avatar": "https://example.com/avatars/john.jpg"
          },
          "rules": {
            "maxParticipants": 100,
            "minParticipants": 10,
            "entryFee": 0.1,
            "currency": "ETH",
            "prizePool": 10.0,
            "currency": "ETH"
          },
          "schedule": {
            "startDate": "2024-01-20T14:30:00Z",
            "endDate": "2024-01-27T14:30:00Z",
            "registrationDeadline": "2024-01-19T14:30:00Z"
          },
          "participants": {
            "total": 50,
            "registered": 45,
            "confirmed": 40,
            "waiting": 5
          },
          "prizes": [
            {
              "position": 1,
              "amount": 5.0,
              "currency": "ETH",
              "description": "First Place Prize"
            },
            {
              "position": 2,
              "amount": 3.0,
              "currency": "ETH",
              "description": "Second Place Prize"
            },
            {
              "position": 3,
              "amount": 2.0,
              "currency": "ETH",
              "description": "Third Place Prize"
            }
          ],
          "createdAt": "2024-01-15T10:30:00Z",
          "lastModified": "2024-01-20T14:30:00Z"
        }
      ],
      "totalCount": 1,
      "limit": 50,
      "offset": 0
    },
    "message": "Competitions retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Competition by ID
```http
GET /api/competition/{competitionId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `competitionId` (string): Competition UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "comp_123",
      "name": "OASIS Gaming Championship",
      "description": "Annual gaming championship for OASIS platform users",
      "type": "Tournament",
      "category": "Gaming",
      "status": "Active",
      "organizer": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "rules": {
        "maxParticipants": 100,
        "minParticipants": 10,
        "entryFee": 0.1,
        "currency": "ETH",
        "prizePool": 10.0,
        "currency": "ETH",
        "format": "Single Elimination",
        "scoring": "Points",
        "tiebreaker": "Head-to-Head"
      },
      "schedule": {
        "startDate": "2024-01-20T14:30:00Z",
        "endDate": "2024-01-27T14:30:00Z",
        "registrationDeadline": "2024-01-19T14:30:00Z",
        "checkInTime": "2024-01-20T14:00:00Z",
        "warmUpTime": "2024-01-20T14:15:00Z"
      },
      "participants": {
        "total": 50,
        "registered": 45,
        "confirmed": 40,
        "waiting": 5,
        "list": [
          {
            "id": "user_456",
            "username": "jane_smith",
            "avatar": "https://example.com/avatars/jane.jpg",
            "status": "Confirmed",
            "registeredAt": "2024-01-16T10:30:00Z",
            "rank": 1,
            "score": 1500
          }
        ]
      },
      "prizes": [
        {
          "position": 1,
          "amount": 5.0,
          "currency": "ETH",
          "description": "First Place Prize",
          "winner": null
        },
        {
          "position": 2,
          "amount": 3.0,
          "currency": "ETH",
          "description": "Second Place Prize",
          "winner": null
        },
        {
          "position": 3,
          "amount": 2.0,
          "currency": "ETH",
          "description": "Third Place Prize",
          "winner": null
        }
      ],
      "brackets": {
        "rounds": 6,
        "currentRound": 1,
        "matches": [
          {
            "id": "match_123",
            "round": 1,
            "participant1": {
              "id": "user_456",
              "username": "jane_smith"
            },
            "participant2": {
              "id": "user_789",
              "username": "bob_wilson"
            },
            "status": "Scheduled",
            "scheduledAt": "2024-01-20T15:00:00Z",
            "result": null
          }
        ]
      },
      "analytics": {
        "views": 1000,
        "registrations": 45,
        "completionRate": 0.8,
        "averageScore": 1200
      },
      "createdAt": "2024-01-15T10:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "Competition retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Create Competition
```http
POST /api/competition
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "name": "New Gaming Tournament",
  "description": "A new gaming tournament for OASIS platform users",
  "type": "Tournament",
  "category": "Gaming",
  "rules": {
    "maxParticipants": 50,
    "minParticipants": 5,
    "entryFee": 0.05,
    "currency": "ETH",
    "prizePool": 5.0,
    "currency": "ETH",
    "format": "Double Elimination",
    "scoring": "Points",
    "tiebreaker": "Head-to-Head"
  },
  "schedule": {
    "startDate": "2024-01-25T14:30:00Z",
    "endDate": "2024-01-30T14:30:00Z",
    "registrationDeadline": "2024-01-24T14:30:00Z",
    "checkInTime": "2024-01-25T14:00:00Z",
    "warmUpTime": "2024-01-25T14:15:00Z"
  },
  "prizes": [
    {
      "position": 1,
      "amount": 2.5,
      "currency": "ETH",
      "description": "First Place Prize"
    },
    {
      "position": 2,
      "amount": 1.5,
      "currency": "ETH",
      "description": "Second Place Prize"
    },
    {
      "position": 3,
      "amount": 1.0,
      "currency": "ETH",
      "description": "Third Place Prize"
    }
  ],
  "settings": {
    "public": true,
    "allowSpectators": true,
    "requireVerification": true,
    "allowLateRegistration": false
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "comp_124",
      "name": "New Gaming Tournament",
      "description": "A new gaming tournament for OASIS platform users",
      "type": "Tournament",
      "category": "Gaming",
      "status": "Active",
      "organizer": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "rules": {
        "maxParticipants": 50,
        "minParticipants": 5,
        "entryFee": 0.05,
        "currency": "ETH",
        "prizePool": 5.0,
        "currency": "ETH",
        "format": "Double Elimination",
        "scoring": "Points",
        "tiebreaker": "Head-to-Head"
      },
      "schedule": {
        "startDate": "2024-01-25T14:30:00Z",
        "endDate": "2024-01-30T14:30:00Z",
        "registrationDeadline": "2024-01-24T14:30:00Z",
        "checkInTime": "2024-01-25T14:00:00Z",
        "warmUpTime": "2024-01-25T14:15:00Z"
      },
      "participants": {
        "total": 0,
        "registered": 0,
        "confirmed": 0,
        "waiting": 0,
        "list": []
      },
      "prizes": [
        {
          "position": 1,
          "amount": 2.5,
          "currency": "ETH",
          "description": "First Place Prize",
          "winner": null
        },
        {
          "position": 2,
          "amount": 1.5,
          "currency": "ETH",
          "description": "Second Place Prize",
          "winner": null
        },
        {
          "position": 3,
          "amount": 1.0,
          "currency": "ETH",
          "description": "Third Place Prize",
          "winner": null
        }
      ],
      "brackets": {
        "rounds": 0,
        "currentRound": 0,
        "matches": []
      },
      "analytics": {
        "views": 0,
        "registrations": 0,
        "completionRate": 0.0,
        "averageScore": 0
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "Competition created successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Competition
```http
PUT /api/competition/{competitionId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `competitionId` (string): Competition UUID

**Request Body:**
```json
{
  "name": "Updated Gaming Tournament",
  "description": "Updated description for the gaming tournament",
  "rules": {
    "maxParticipants": 75,
    "minParticipants": 10,
    "entryFee": 0.08,
    "currency": "ETH",
    "prizePool": 8.0,
    "currency": "ETH"
  },
  "schedule": {
    "startDate": "2024-01-26T14:30:00Z",
    "endDate": "2024-01-31T14:30:00Z",
    "registrationDeadline": "2024-01-25T14:30:00Z"
  }
}
```

### Delete Competition
```http
DELETE /api/competition/{competitionId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `competitionId` (string): Competition UUID

## Competition Operations

### Register for Competition
```http
POST /api/competition/{competitionId}/register
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `competitionId` (string): Competition UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "competitionId": "comp_123",
      "participant": {
        "id": "user_456",
        "username": "jane_smith",
        "avatar": "https://example.com/avatars/jane.jpg"
      },
      "status": "Registered",
      "registeredAt": "2024-01-20T14:30:00Z",
      "entryFee": {
        "amount": 0.1,
        "currency": "ETH",
        "paid": true,
        "transactionId": "tx_123"
      }
    },
    "message": "Successfully registered for competition"
  },
  "isError": false,
  "message": "Success"
}
```

### Unregister from Competition
```http
DELETE /api/competition/{competitionId}/register
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `competitionId` (string): Competition UUID

### Get Competition Participants
```http
GET /api/competition/{competitionId}/participants
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `competitionId` (string): Competition UUID

**Query Parameters:**
- `status` (string, optional): Filter by status (Registered, Confirmed, Waiting)
- `sortBy` (string, optional): Sort field (rank, score, registeredAt)
- `sortOrder` (string, optional): Sort order (asc/desc, default: asc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "competitionId": "comp_123",
      "participants": [
        {
          "id": "user_456",
          "username": "jane_smith",
          "avatar": "https://example.com/avatars/jane.jpg",
          "status": "Confirmed",
          "registeredAt": "2024-01-16T10:30:00Z",
          "rank": 1,
          "score": 1500,
          "wins": 5,
          "losses": 0,
          "draws": 0
        },
        {
          "id": "user_789",
          "username": "bob_wilson",
          "avatar": "https://example.com/avatars/bob.jpg",
          "status": "Confirmed",
          "registeredAt": "2024-01-17T14:20:00Z",
          "rank": 2,
          "score": 1400,
          "wins": 4,
          "losses": 1,
          "draws": 0
        }
      ],
      "totalCount": 2,
      "confirmedCount": 2,
      "waitingCount": 0
    },
    "message": "Competition participants retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Competition Brackets
```http
GET /api/competition/{competitionId}/brackets
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `competitionId` (string): Competition UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "competitionId": "comp_123",
      "brackets": {
        "rounds": 6,
        "currentRound": 1,
        "matches": [
          {
            "id": "match_123",
            "round": 1,
            "participant1": {
              "id": "user_456",
              "username": "jane_smith",
              "score": 1500
            },
            "participant2": {
              "id": "user_789",
              "username": "bob_wilson",
              "score": 1400
            },
            "status": "Scheduled",
            "scheduledAt": "2024-01-20T15:00:00Z",
            "result": null,
            "winner": null
          },
          {
            "id": "match_124",
            "round": 1,
            "participant1": {
              "id": "user_101",
              "username": "alice_brown",
              "score": 1300
            },
            "participant2": {
              "id": "user_102",
              "username": "charlie_davis",
              "score": 1200
            },
            "status": "Completed",
            "scheduledAt": "2024-01-20T15:30:00Z",
            "result": {
              "participant1Score": 2,
              "participant2Score": 1
            },
            "winner": {
              "id": "user_101",
              "username": "alice_brown"
            }
          }
        ],
        "standings": [
          {
            "rank": 1,
            "participant": {
              "id": "user_101",
              "username": "alice_brown"
            },
            "score": 1300,
            "wins": 1,
            "losses": 0,
            "draws": 0
          }
        ]
      }
    },
    "message": "Competition brackets retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Submit Match Result
```http
POST /api/competition/{competitionId}/matches/{matchId}/result
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `competitionId` (string): Competition UUID
- `matchId` (string): Match UUID

**Request Body:**
```json
{
  "participant1Score": 2,
  "participant2Score": 1,
  "winner": "user_456",
  "notes": "Great match!",
  "evidence": "https://example.com/evidence/match_123.jpg"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "matchId": "match_123",
      "competitionId": "comp_123",
      "result": {
        "participant1Score": 2,
        "participant2Score": 1,
        "winner": "user_456"
      },
      "notes": "Great match!",
      "evidence": "https://example.com/evidence/match_123.jpg",
      "submittedAt": "2024-01-20T14:30:00Z",
      "submittedBy": "user_123"
    },
    "message": "Match result submitted successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Competition Analytics

### Get Competition Statistics
```http
GET /api/competition/{competitionId}/stats
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `competitionId` (string): Competition UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "competitionId": "comp_123",
      "statistics": {
        "participants": {
          "total": 50,
          "registered": 45,
          "confirmed": 40,
          "waiting": 5,
          "active": 35,
          "eliminated": 5
        },
        "matches": {
          "total": 25,
          "completed": 20,
          "scheduled": 5,
          "inProgress": 0
        },
        "performance": {
          "averageMatchTime": 30,
          "averageScore": 1200,
          "completionRate": 0.8,
          "participationRate": 0.9
        },
        "prizes": {
          "totalPool": 10.0,
          "currency": "ETH",
          "distributed": 0.0,
          "remaining": 10.0
        }
      },
      "trends": {
        "participants": "stable",
        "matches": "increasing",
        "performance": "stable"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Competition statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Competition Performance
```http
GET /api/competition/{competitionId}/performance
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `competitionId` (string): Competition UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "competitionId": "comp_123",
      "performance": {
        "averageMatchTime": 30,
        "peakMatchTime": 60,
        "averageScore": 1200,
        "peakScore": 1500,
        "completionRate": 0.8,
        "participationRate": 0.9
      },
      "metrics": {
        "matchesPerHour": 2,
        "averageLatency": 5.0,
        "p95Latency": 10.0,
        "p99Latency": 15.0,
        "errorRate": 0.001,
        "successRate": 0.999
      },
      "trends": {
        "matchTime": "stable",
        "score": "increasing",
        "completionRate": "stable",
        "participationRate": "stable"
      },
      "breakdown": {
        "round1": {
          "averageMatchTime": 25,
          "averageScore": 1100,
          "completionRate": 0.9
        },
        "round2": {
          "averageMatchTime": 35,
          "averageScore": 1300,
          "completionRate": 0.8
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Competition performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Competition Health
```http
GET /api/competition/{competitionId}/health
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `competitionId` (string): Competition UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "competitionId": "comp_123",
      "status": "Healthy",
      "overallHealth": 0.95,
      "components": {
        "registration": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "brackets": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "scoring": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "prizes": {
          "status": "Healthy",
          "health": 0.97,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Registration Test",
          "status": "Pass",
          "responseTime": 1.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Brackets Test",
          "status": "Pass",
          "responseTime": 2.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Scoring Test",
          "status": "Pass",
          "responseTime": 1.5,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Prizes Test",
          "status": "Pass",
          "responseTime": 0.5,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Competition health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Competition Security

### Get Competition Security
```http
GET /api/competition/{competitionId}/security
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `competitionId` (string): Competition UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "competitionId": "comp_123",
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC",
        "auditLogging": true,
        "antiCheat": true
      },
      "access": {
        "lastAccessed": "2024-01-20T14:30:00Z",
        "accessCount": 1000,
        "failedAttempts": 5,
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
        "auditCount": 10,
        "complianceScore": 0.98
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Competition security retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Competition Security
```http
PUT /api/competition/{competitionId}/security
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `competitionId` (string): Competition UUID

**Request Body:**
```json
{
  "encryption": "AES-256",
  "authentication": "JWT",
  "authorization": "RBAC",
  "auditLogging": true,
  "antiCheat": true
}
```

## Error Responses

### Competition Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Competition not found",
  "exception": "Competition with ID comp_123 not found"
}
```

### Registration Closed
```json
{
  "result": null,
  "isError": true,
  "message": "Registration closed",
  "exception": "Registration deadline has passed"
}
```

### Competition Full
```json
{
  "result": null,
  "isError": true,
  "message": "Competition full",
  "exception": "Maximum number of participants reached"
}
```

### Invalid Match Result
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid match result",
  "exception": "Match result must be submitted by authorized user"
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

**← Previous:** [Keys API](Keys-API.md) | **Next:** [Gifts API](Gifts-API.md) →
