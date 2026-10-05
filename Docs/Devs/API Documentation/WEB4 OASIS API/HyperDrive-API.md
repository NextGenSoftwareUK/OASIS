# HyperDrive API

## 📋 **Table of Contents**

- [Overview](#overview)
- [HyperDrive Management](#hyperdrive-management)
- [HyperDrive Operations](#hyperdrive-operations)
- [HyperDrive Analytics](#hyperdrive-analytics)
- [HyperDrive Security](#hyperdrive-security)
- [Error Responses](#error-responses)

## Overview

The HyperDrive API provides comprehensive hyperdrive management services for the OASIS ecosystem. It handles hyperdrive creation, configuration, execution, and analytics with support for multiple hyperdrive types, real-time monitoring, and advanced security features.

## HyperDrive Management

### Get All HyperDrives
```http
GET /api/hyperdrive
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by type (Standard, Quantum, Temporal, Dimensional)
- `status` (string, optional): Filter by status (Active, Inactive, Running, Stopped)
- `sortBy` (string, optional): Sort field (name, createdAt, speed, efficiency)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "hyperdrives": [
        {
          "id": "hyperdrive_123",
          "name": "OASIS HyperDrive",
          "description": "High-performance hyperdrive for OASIS network",
          "type": "Standard",
          "status": "Active",
          "configuration": {
            "maxSpeed": 1000,
            "efficiency": 0.95,
            "powerConsumption": 500,
            "range": 10000
          },
          "creator": {
            "id": "user_123",
            "username": "john_doe",
            "avatar": "https://example.com/avatars/john.jpg"
          },
          "metadata": {
            "tags": ["high-performance", "network", "oasis"],
            "category": "Network",
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
    "message": "HyperDrives retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get HyperDrive by ID
```http
GET /api/hyperdrive/{hyperdriveId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `hyperdriveId` (string): HyperDrive UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "hyperdrive_123",
      "name": "OASIS HyperDrive",
      "description": "High-performance hyperdrive for OASIS network",
      "type": "Standard",
      "status": "Active",
      "configuration": {
        "maxSpeed": 1000,
        "efficiency": 0.95,
        "powerConsumption": 500,
        "range": 10000
      },
      "creator": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "metadata": {
        "tags": ["high-performance", "network", "oasis"],
        "category": "Network",
        "version": "1.0"
      },
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC"
      },
      "analytics": {
        "usage": 500,
        "efficiency": 0.95,
        "averageSpeed": 800,
        "totalDistance": 50000
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "HyperDrive retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Create HyperDrive
```http
POST /api/hyperdrive
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "name": "Quantum HyperDrive",
  "description": "Advanced quantum hyperdrive for maximum performance",
  "type": "Quantum",
  "configuration": {
    "maxSpeed": 2000,
    "efficiency": 0.98,
    "powerConsumption": 1000,
    "range": 20000
  },
  "metadata": {
    "tags": ["quantum", "advanced", "performance"],
    "category": "Advanced",
    "version": "2.0"
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "hyperdrive_124",
      "name": "Quantum HyperDrive",
      "description": "Advanced quantum hyperdrive for maximum performance",
      "type": "Quantum",
      "status": "Active",
      "configuration": {
        "maxSpeed": 2000,
        "efficiency": 0.98,
        "powerConsumption": 1000,
        "range": 20000
      },
      "creator": {
        "id": "user_456",
        "username": "jane_smith",
        "avatar": "https://example.com/avatars/jane.jpg"
      },
      "metadata": {
        "tags": ["quantum", "advanced", "performance"],
        "category": "Advanced",
        "version": "2.0"
      },
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC"
      },
      "analytics": {
        "usage": 0,
        "efficiency": 0.98,
        "averageSpeed": 0,
        "totalDistance": 0
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "HyperDrive created successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update HyperDrive
```http
PUT /api/hyperdrive/{hyperdriveId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `hyperdriveId` (string): HyperDrive UUID

**Request Body:**
```json
{
  "name": "Updated Quantum HyperDrive",
  "description": "Updated description for quantum hyperdrive",
  "configuration": {
    "maxSpeed": 2500,
    "efficiency": 0.99,
    "powerConsumption": 1200,
    "range": 25000
  }
}
```

### Delete HyperDrive
```http
DELETE /api/hyperdrive/{hyperdriveId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `hyperdriveId` (string): HyperDrive UUID

## HyperDrive Operations

### Start HyperDrive
```http
POST /api/hyperdrive/{hyperdriveId}/start
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `hyperdriveId` (string): HyperDrive UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "hyperdriveId": "hyperdrive_123",
      "status": "Running",
      "startedAt": "2024-01-20T14:30:00Z",
      "configuration": {
        "maxSpeed": 1000,
        "efficiency": 0.95,
        "powerConsumption": 500,
        "range": 10000
      },
      "performance": {
        "currentSpeed": 0,
        "efficiency": 0.95,
        "powerUsage": 0,
        "distanceTraveled": 0
      }
    },
    "message": "HyperDrive started successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Stop HyperDrive
```http
POST /api/hyperdrive/{hyperdriveId}/stop
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `hyperdriveId` (string): HyperDrive UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "hyperdriveId": "hyperdrive_123",
      "status": "Stopped",
      "stoppedAt": "2024-01-20T14:30:00Z",
      "performance": {
        "currentSpeed": 0,
        "efficiency": 0.95,
        "powerUsage": 0,
        "distanceTraveled": 5000
      }
    },
    "message": "HyperDrive stopped successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get HyperDrive Status
```http
GET /api/hyperdrive/{hyperdriveId}/status
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `hyperdriveId` (string): HyperDrive UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "hyperdriveId": "hyperdrive_123",
      "status": "Running",
      "startedAt": "2024-01-20T14:30:00Z",
      "performance": {
        "currentSpeed": 800,
        "efficiency": 0.95,
        "powerUsage": 400,
        "distanceTraveled": 5000
      },
      "health": {
        "overall": 0.95,
        "engine": 0.98,
        "navigation": 0.92,
        "power": 0.96
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "HyperDrive status retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Configure HyperDrive
```http
PUT /api/hyperdrive/{hyperdriveId}/configure
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `hyperdriveId` (string): HyperDrive UUID

**Request Body:**
```json
{
  "maxSpeed": 1500,
  "efficiency": 0.97,
  "powerConsumption": 750,
  "range": 15000
}
```

## HyperDrive Analytics

### Get HyperDrive Statistics
```http
GET /api/hyperdrive/stats
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `type` (string, optional): Filter by hyperdrive type

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "statistics": {
        "hyperdrives": {
          "total": 100,
          "active": 80,
          "running": 60,
          "stopped": 20
        },
        "byType": {
          "Standard": 50,
          "Quantum": 30,
          "Temporal": 15,
          "Dimensional": 5
        },
        "performance": {
          "averageSpeed": 800,
          "averageEfficiency": 0.95,
          "totalDistance": 500000,
          "totalPowerConsumption": 250000
        },
        "usage": {
          "total": 1000,
          "active": 800,
          "completed": 150,
          "failed": 50
        }
      },
      "trends": {
        "hyperdrives": "increasing",
        "performance": "stable",
        "usage": "increasing"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "HyperDrive statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get HyperDrive Performance
```http
GET /api/hyperdrive/performance
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "performance": {
        "averageSpeed": 800,
        "averageEfficiency": 0.95,
        "totalDistance": 500000,
        "totalPowerConsumption": 250000,
        "averagePowerUsage": 500,
        "peakSpeed": 2000
      },
      "metrics": {
        "hyperdrivesPerHour": 10,
        "averageLatency": 2.0,
        "p95Latency": 5.0,
        "p99Latency": 10.0,
        "errorRate": 0.02,
        "successRate": 0.98
      },
      "trends": {
        "speed": "stable",
        "efficiency": "stable",
        "powerUsage": "stable",
        "distance": "increasing"
      },
      "breakdown": {
        "Standard": {
          "averageSpeed": 600,
          "efficiency": 0.90,
          "powerUsage": 400
        },
        "Quantum": {
          "averageSpeed": 1200,
          "efficiency": 0.98,
          "powerUsage": 800
        },
        "Temporal": {
          "averageSpeed": 1000,
          "efficiency": 0.95,
          "powerUsage": 600
        },
        "Dimensional": {
          "averageSpeed": 1500,
          "efficiency": 0.99,
          "powerUsage": 1000
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "HyperDrive performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get HyperDrive Health
```http
GET /api/hyperdrive/health
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
        "engine": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "navigation": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "power": {
          "status": "Healthy",
          "health": 0.96,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "monitoring": {
          "status": "Healthy",
          "health": 0.94,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Engine Test",
          "status": "Pass",
          "responseTime": 2.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Navigation Test",
          "status": "Pass",
          "responseTime": 1.5,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Power Test",
          "status": "Pass",
          "responseTime": 1.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Monitoring Test",
          "status": "Pass",
          "responseTime": 0.8,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "HyperDrive health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## HyperDrive Security

### Get HyperDrive Security
```http
GET /api/hyperdrive/{hyperdriveId}/security
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `hyperdriveId` (string): HyperDrive UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "hyperdriveId": "hyperdrive_123",
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
    "message": "HyperDrive security retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update HyperDrive Security
```http
PUT /api/hyperdrive/{hyperdriveId}/security
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `hyperdriveId` (string): HyperDrive UUID

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

### HyperDrive Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "HyperDrive not found",
  "exception": "HyperDrive with ID hyperdrive_123 not found"
}
```

### HyperDrive Already Running
```json
{
  "result": null,
  "isError": true,
  "message": "HyperDrive already running",
  "exception": "HyperDrive is already running"
}
```

### HyperDrive Not Running
```json
{
  "result": null,
  "isError": true,
  "message": "HyperDrive not running",
  "exception": "HyperDrive is not running"
}
```

### Insufficient Power
```json
{
  "result": null,
  "isError": true,
  "message": "Insufficient power",
  "exception": "Insufficient power to start HyperDrive"
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

**← Previous:** [Avatar API](Avatar-API.md) | **Next:** [Keys API](Keys-API.md) →
