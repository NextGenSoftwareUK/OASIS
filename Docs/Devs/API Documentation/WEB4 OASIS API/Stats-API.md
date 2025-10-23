# Stats API

## 📋 **Table of Contents**

- [Overview](#overview)
- [System Statistics](#system-statistics)
- [User Statistics](#user-statistics)
- [Network Statistics](#network-statistics)
- [Performance Statistics](#performance-statistics)
- [Error Responses](#error-responses)

## Overview

The Stats API provides comprehensive statistics and analytics for the OASIS ecosystem. It tracks system performance, user activity, network health, and provides real-time insights into the platform's usage and performance.

## System Statistics

### Get System Stats
```http
GET /api/stats/system
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "system": {
        "uptime": "99.9%",
        "version": "1.0.0",
        "build": "2024.01.20.001",
        "environment": "Production",
        "region": "US-East",
        "lastUpdated": "2024-01-20T14:30:00Z"
      },
      "resources": {
        "cpu": {
          "usage": 45.5,
          "cores": 16,
          "load": 7.2
        },
        "memory": {
          "usage": 60.2,
          "total": "32GB",
          "available": "12.8GB"
        },
        "storage": {
          "usage": 75.8,
          "total": "1TB",
          "available": "250GB"
        },
        "bandwidth": {
          "usage": 80.0,
          "total": "10Gbps",
          "available": "2Gbps"
        }
      },
      "performance": {
        "averageResponseTime": 120,
        "peakResponseTime": 500,
        "throughput": 1000,
        "errorRate": 0.001,
        "availability": 99.9
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "System statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get System Health
```http
GET /api/stats/system/health
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
        "api": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "database": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "cache": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "storage": {
          "status": "Warning",
          "health": 0.85,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "API Health Check",
          "status": "Pass",
          "responseTime": 50,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Database Health Check",
          "status": "Pass",
          "responseTime": 100,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Cache Health Check",
          "status": "Pass",
          "responseTime": 25,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Storage Health Check",
          "status": "Warning",
          "responseTime": 200,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [
        {
          "type": "High Storage Usage",
          "severity": "Medium",
          "message": "Storage usage is above 75%",
          "createdAt": "2024-01-20T14:30:00Z"
        }
      ],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "System health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get System Performance
```http
GET /api/stats/system/performance
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `includeDetails` (boolean, optional): Include detailed performance metrics

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "performance": {
        "averageResponseTime": 120,
        "peakResponseTime": 500,
        "throughput": 1000,
        "errorRate": 0.001,
        "availability": 99.9
      },
      "metrics": {
        "requestsPerSecond": 100,
        "averageLatency": 120,
        "p95Latency": 200,
        "p99Latency": 400,
        "errorRate": 0.001,
        "successRate": 0.999
      },
      "trends": {
        "responseTime": "stable",
        "throughput": "increasing",
        "errorRate": "decreasing",
        "availability": "stable"
      },
      "breakdown": {
        "api": {
          "averageResponseTime": 100,
          "throughput": 800,
          "errorRate": 0.0005
        },
        "database": {
          "averageResponseTime": 50,
          "throughput": 1200,
          "errorRate": 0.001
        },
        "cache": {
          "averageResponseTime": 25,
          "throughput": 2000,
          "errorRate": 0.0001
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "System performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## User Statistics

### Get User Stats
```http
GET /api/stats/users
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `includeDetails` (boolean, optional): Include detailed user statistics

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "users": {
        "total": 10000,
        "active": 8000,
        "new": 150,
        "returning": 6500,
        "churned": 50
      },
      "activity": {
        "totalSessions": 15000,
        "averageSessionDuration": 1800,
        "peakConcurrentUsers": 500,
        "averageDailyActiveUsers": 8000
      },
      "engagement": {
        "averageActionsPerUser": 25,
        "mostActiveHour": "14:00-15:00",
        "mostActiveDay": "Tuesday",
        "retentionRate": 0.85
      },
      "geographic": {
        "US": 4000,
        "Europe": 3000,
        "Asia": 2000,
        "Other": 1000
      },
      "devices": {
        "Desktop": 6000,
        "Mobile": 3000,
        "Tablet": 1000
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "User statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get User Activity
```http
GET /api/stats/users/activity
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `userId` (string, optional): Filter by specific user ID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "activity": {
        "totalActions": 250000,
        "uniqueUsers": 8000,
        "averageActionsPerUser": 31.25,
        "peakActivity": "14:00-15:00"
      },
      "actions": {
        "login": 15000,
        "logout": 12000,
        "api_calls": 200000,
        "data_operations": 15000,
        "nft_operations": 5000,
        "wallet_operations": 3000
      },
      "trends": {
        "login": "increasing",
        "api_calls": "stable",
        "data_operations": "increasing",
        "nft_operations": "increasing"
      },
      "geographic": {
        "US": {
          "actions": 100000,
          "users": 4000
        },
        "Europe": {
          "actions": 75000,
          "users": 3000
        },
        "Asia": {
          "actions": 50000,
          "users": 2000
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "User activity retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get User Retention
```http
GET /api/stats/users/retention
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `cohort` (string, optional): Cohort period (day, week, month)
- `periods` (int, optional): Number of periods to analyze (default: 12)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "cohort": "week",
      "retention": {
        "week1": 0.85,
        "week2": 0.75,
        "week3": 0.70,
        "week4": 0.65,
        "week8": 0.60,
        "week12": 0.55
      },
      "cohorts": [
        {
          "cohort": "2024-01-01",
          "size": 1000,
          "retention": {
            "week1": 0.85,
            "week2": 0.75,
            "week3": 0.70,
            "week4": 0.65
          }
        },
        {
          "cohort": "2024-01-08",
          "size": 1200,
          "retention": {
            "week1": 0.88,
            "week2": 0.78,
            "week3": 0.72,
            "week4": 0.67
          }
        }
      ],
      "averageRetention": 0.68,
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "User retention retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Network Statistics

### Get Network Stats
```http
GET /api/stats/network
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "network": {
        "totalNodes": 1250,
        "activeNodes": 1200,
        "inactiveNodes": 50,
        "networkHealth": 0.96,
        "averageLatency": 150,
        "throughput": 1000
      },
      "connections": {
        "total": 5000,
        "active": 4500,
        "pending": 500,
        "averagePerNode": 4
      },
      "traffic": {
        "totalDataTransferred": "2.5TB",
        "averageBandwidth": 1000,
        "peakBandwidth": 1500,
        "packetsPerSecond": 10000
      },
      "geographic": {
        "US-East": 400,
        "US-West": 300,
        "Europe": 350,
        "Asia": 200
      },
      "performance": {
        "averageResponseTime": 120,
        "peakResponseTime": 500,
        "errorRate": 0.001,
        "availability": 99.9
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Network statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Network Health
```http
GET /api/stats/network/health
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "status": "Healthy",
      "overallHealth": 0.96,
      "components": {
        "connectivity": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "latency": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "throughput": {
          "status": "Healthy",
          "health": 0.97,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "reliability": {
          "status": "Healthy",
          "health": 0.94,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Connectivity Test",
          "status": "Pass",
          "responseTime": 150,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Latency Test",
          "status": "Pass",
          "responseTime": 120,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Throughput Test",
          "status": "Pass",
          "responseTime": 100,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Reliability Test",
          "status": "Pass",
          "responseTime": 200,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Network health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Network Performance
```http
GET /api/stats/network/performance
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `includeDetails` (boolean, optional): Include detailed performance metrics

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "performance": {
        "averageLatency": 150,
        "peakLatency": 500,
        "throughput": 1000,
        "errorRate": 0.001,
        "availability": 99.9
      },
      "metrics": {
        "packetsPerSecond": 10000,
        "averageLatency": 150,
        "p95Latency": 300,
        "p99Latency": 500,
        "errorRate": 0.001,
        "successRate": 0.999
      },
      "trends": {
        "latency": "stable",
        "throughput": "increasing",
        "errorRate": "decreasing",
        "availability": "stable"
      },
      "breakdown": {
        "US-East": {
          "latency": 120,
          "throughput": 400,
          "errorRate": 0.0005
        },
        "US-West": {
          "latency": 180,
          "throughput": 300,
          "errorRate": 0.001
        },
        "Europe": {
          "latency": 200,
          "throughput": 200,
          "errorRate": 0.0015
        },
        "Asia": {
          "latency": 250,
          "throughput": 100,
          "errorRate": 0.002
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Network performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Performance Statistics

### Get Performance Stats
```http
GET /api/stats/performance
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `includeDetails` (boolean, optional): Include detailed performance metrics

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "performance": {
        "averageResponseTime": 120,
        "peakResponseTime": 500,
        "throughput": 1000,
        "errorRate": 0.001,
        "availability": 99.9
      },
      "metrics": {
        "requestsPerSecond": 100,
        "averageLatency": 120,
        "p95Latency": 200,
        "p99Latency": 400,
        "errorRate": 0.001,
        "successRate": 0.999
      },
      "trends": {
        "responseTime": "stable",
        "throughput": "increasing",
        "errorRate": "decreasing",
        "availability": "stable"
      },
      "breakdown": {
        "api": {
          "averageResponseTime": 100,
          "throughput": 800,
          "errorRate": 0.0005
        },
        "database": {
          "averageResponseTime": 50,
          "throughput": 1200,
          "errorRate": 0.001
        },
        "cache": {
          "averageResponseTime": 25,
          "throughput": 2000,
          "errorRate": 0.0001
        },
        "storage": {
          "averageResponseTime": 75,
          "throughput": 600,
          "errorRate": 0.002
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Performance statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Performance Trends
```http
GET /api/stats/performance/trends
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `metric` (string, optional): Specific metric to analyze

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "trends": {
        "responseTime": {
          "trend": "stable",
          "change": 0.02,
          "direction": "increasing",
          "confidence": 0.85
        },
        "throughput": {
          "trend": "increasing",
          "change": 0.15,
          "direction": "increasing",
          "confidence": 0.92
        },
        "errorRate": {
          "trend": "decreasing",
          "change": -0.05,
          "direction": "decreasing",
          "confidence": 0.88
        },
        "availability": {
          "trend": "stable",
          "change": 0.01,
          "direction": "increasing",
          "confidence": 0.95
        }
      },
      "forecasts": {
        "responseTime": {
          "nextHour": 125,
          "nextDay": 130,
          "nextWeek": 135
        },
        "throughput": {
          "nextHour": 1050,
          "nextDay": 1100,
          "nextWeek": 1200
        },
        "errorRate": {
          "nextHour": 0.0008,
          "nextDay": 0.0006,
          "nextWeek": 0.0004
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Performance trends retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Error Responses

### Stats Not Available
```json
{
  "result": null,
  "isError": true,
  "message": "Statistics not available",
  "exception": "Statistics service is currently unavailable"
}
```

### Invalid Timeframe
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid timeframe",
  "exception": "Timeframe 'invalid' is not supported"
}
```

### Data Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Data not found",
  "exception": "No statistics data found for the specified timeframe"
}
```

### Service Unavailable
```json
{
  "result": null,
  "isError": true,
  "message": "Service unavailable",
  "exception": "Statistics service is temporarily unavailable"
}
```

---

## Navigation

**← Previous:** [Search API](Search-API.md) | **Next:** [Settings API](Settings-API.md) →
