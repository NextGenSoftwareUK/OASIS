# Provider API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Provider Management](#provider-management)
- [Provider Configuration](#provider-configuration)
- [Provider Monitoring](#provider-monitoring)
- [Provider Statistics](#provider-statistics)
- [Provider Health](#provider-health)
- [Error Responses](#error-responses)

## Overview

The Provider API provides comprehensive management of OASIS providers across all supported categories. It handles provider registration, configuration, monitoring, and performance optimization for storage, blockchain, cloud, network, and other provider types.

## Provider Management

### Get All Providers
```http
GET /api/provider/all
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `category` (string, optional): Filter by category (Storage, Blockchain, Cloud, Network, Other)
- `status` (string, optional): Filter by status (Active, Inactive, Error, Maintenance)
- `sortBy` (string, optional): Sort field (name, category, status, lastActivity)
- `sortOrder` (string, optional): Sort order (asc/desc, default: asc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providers": [
        {
          "id": "provider_123",
          "name": "MongoDBProvider",
          "category": "Storage",
          "type": "MongoDB",
          "status": "Active",
          "version": "1.0.0",
          "description": "MongoDB database provider",
          "capabilities": ["CRUD", "Query", "Indexing", "Replication"],
          "performance": {
            "averageResponseTime": 50,
            "throughput": 500,
            "errorRate": 0.001
          },
          "resources": {
            "cpu": 25.5,
            "memory": 40.2,
            "storage": 60.8
          },
          "lastActivity": "2024-01-20T14:30:00Z",
          "uptime": "99.9%"
        }
      ],
      "totalCount": 1,
      "limit": 50,
      "offset": 0
    },
    "message": "Providers retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Provider by ID
```http
GET /api/provider/{providerId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerId` (string): Provider UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "provider_123",
      "name": "MongoDBProvider",
      "category": "Storage",
      "type": "MongoDB",
      "status": "Active",
      "version": "1.0.0",
      "description": "MongoDB database provider",
      "capabilities": ["CRUD", "Query", "Indexing", "Replication"],
      "configuration": {
        "connectionString": "mongodb://localhost:27017",
        "database": "oasis",
        "maxConnections": 100,
        "timeout": 30,
        "ssl": true,
        "authentication": true
      },
      "performance": {
        "averageResponseTime": 50,
        "throughput": 500,
        "errorRate": 0.001,
        "totalRequests": 1000000,
        "successfulRequests": 999000,
        "failedRequests": 1000
      },
      "resources": {
        "cpu": 25.5,
        "memory": 40.2,
        "storage": 60.8,
        "bandwidth": 80.0
      },
      "health": "Healthy",
      "lastActivity": "2024-01-20T14:30:00Z",
      "uptime": "99.9%",
      "createdAt": "2024-01-15T10:30:00Z",
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Provider retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Providers by Category
```http
GET /api/provider/category/{category}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `category` (string): Provider category (Storage, Blockchain, Cloud, Network, Other)

### Get Providers by Status
```http
GET /api/provider/status/{status}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `status` (string): Provider status (Active, Inactive, Error, Maintenance)

## Provider Configuration

### Get Provider Configuration
```http
GET /api/provider/{providerId}/config
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerId` (string): Provider UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providerId": "provider_123",
      "name": "MongoDBProvider",
      "category": "Storage",
      "type": "MongoDB",
      "configuration": {
        "connectionString": "mongodb://localhost:27017",
        "database": "oasis",
        "maxConnections": 100,
        "timeout": 30,
        "ssl": true,
        "authentication": true,
        "username": "oasis_user",
        "password": "encrypted_password",
        "replicaSet": "rs0",
        "readPreference": "primary",
        "writeConcern": "majority",
        "readConcern": "majority"
      },
      "advanced": {
        "poolSize": 10,
        "maxPoolSize": 100,
        "minPoolSize": 5,
        "maxIdleTime": 30000,
        "serverSelectionTimeout": 5000,
        "socketTimeout": 0,
        "connectTimeout": 10000
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Provider configuration retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Provider Configuration
```http
PUT /api/provider/{providerId}/config
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerId` (string): Provider UUID

**Request Body:**
```json
{
  "configuration": {
    "maxConnections": 150,
    "timeout": 45,
    "ssl": true,
    "authentication": true,
    "readPreference": "secondary",
    "writeConcern": "majority"
  },
  "advanced": {
    "poolSize": 15,
    "maxPoolSize": 150,
    "minPoolSize": 10,
    "maxIdleTime": 45000,
    "serverSelectionTimeout": 7500,
    "socketTimeout": 5000,
    "connectTimeout": 15000
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providerId": "provider_123",
      "name": "MongoDBProvider",
      "configuration": {
        "maxConnections": 150,
        "timeout": 45,
        "ssl": true,
        "authentication": true,
        "readPreference": "secondary",
        "writeConcern": "majority"
      },
      "advanced": {
        "poolSize": 15,
        "maxPoolSize": 150,
        "minPoolSize": 10,
        "maxIdleTime": 45000,
        "serverSelectionTimeout": 7500,
        "socketTimeout": 5000,
        "connectTimeout": 15000
      },
      "updatedAt": "2024-01-20T14:30:00Z",
      "requiresRestart": true
    },
    "message": "Provider configuration updated successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Test Provider Configuration
```http
POST /api/provider/{providerId}/test
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerId` (string): Provider UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providerId": "provider_123",
      "name": "MongoDBProvider",
      "testResults": {
        "connection": "Success",
        "authentication": "Success",
        "read": "Success",
        "write": "Success",
        "query": "Success",
        "performance": "Good"
      },
      "metrics": {
        "connectionTime": 150,
        "readTime": 50,
        "writeTime": 75,
        "queryTime": 100
      },
      "testedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Provider test completed successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Provider Monitoring

### Get Provider Status
```http
GET /api/provider/{providerId}/status
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerId` (string): Provider UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providerId": "provider_123",
      "name": "MongoDBProvider",
      "status": "Active",
      "health": "Healthy",
      "uptime": "99.9%",
      "lastActivity": "2024-01-20T14:30:00Z",
      "performance": {
        "averageResponseTime": 50,
        "throughput": 500,
        "errorRate": 0.001,
        "availability": 99.9
      },
      "resources": {
        "cpu": 25.5,
        "memory": 40.2,
        "storage": 60.8,
        "bandwidth": 80.0
      },
      "connections": {
        "active": 45,
        "max": 100,
        "pending": 5
      },
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Provider status retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Provider Logs
```http
GET /api/provider/{providerId}/logs
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerId` (string): Provider UUID

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 100)
- `offset` (int, optional): Number to skip (default: 0)
- `level` (string, optional): Filter by log level (DEBUG, INFO, WARN, ERROR)
- `startDate` (string, optional): Start date (ISO 8601)
- `endDate` (string, optional): End date (ISO 8601)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providerId": "provider_123",
      "name": "MongoDBProvider",
      "logs": [
        {
          "timestamp": "2024-01-20T14:30:00Z",
          "level": "INFO",
          "message": "Connection established successfully",
          "details": {
            "connectionId": "conn_123",
            "responseTime": 150
          }
        },
        {
          "timestamp": "2024-01-20T14:29:00Z",
          "level": "WARN",
          "message": "High memory usage detected",
          "details": {
            "memoryUsage": 85.5,
            "threshold": 80.0
          }
        }
      ],
      "totalCount": 2,
      "limit": 100,
      "offset": 0
    },
    "message": "Provider logs retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Provider Alerts
```http
GET /api/provider/{providerId}/alerts
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerId` (string): Provider UUID

**Query Parameters:**
- `status` (string, optional): Filter by alert status (Active, Resolved, Acknowledged)
- `severity` (string, optional): Filter by severity (Low, Medium, High, Critical)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providerId": "provider_123",
      "name": "MongoDBProvider",
      "alerts": [
        {
          "alertId": "alert_123",
          "type": "High Memory Usage",
          "severity": "Medium",
          "status": "Active",
          "message": "Memory usage is above 80%",
          "details": {
            "currentUsage": 85.5,
            "threshold": 80.0,
            "trend": "increasing"
          },
          "createdAt": "2024-01-20T14:30:00Z",
          "lastUpdated": "2024-01-20T14:30:00Z"
        }
      ],
      "totalCount": 1,
      "activeCount": 1,
      "resolvedCount": 0
    },
    "message": "Provider alerts retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Provider Statistics

### Get Provider Stats
```http
GET /api/provider/{providerId}/stats
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerId` (string): Provider UUID

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `includeDetails` (boolean, optional): Include detailed statistics

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providerId": "provider_123",
      "name": "MongoDBProvider",
      "timeframe": "day",
      "uptime": "99.9%",
      "totalRequests": 1000000,
      "successfulRequests": 999000,
      "failedRequests": 1000,
      "errorRate": 0.001,
      "averageResponseTime": 50,
      "peakResponseTime": 200,
      "throughput": 500,
      "dataTransferred": "2.5TB",
      "connections": {
        "total": 1000,
        "active": 45,
        "max": 100,
        "average": 50
      },
      "resources": {
        "cpu": {
          "average": 25.5,
          "peak": 60.0,
          "current": 30.0
        },
        "memory": {
          "average": 40.2,
          "peak": 85.0,
          "current": 45.0
        },
        "storage": {
          "average": 60.8,
          "peak": 90.0,
          "current": 65.0
        },
        "bandwidth": {
          "average": 80.0,
          "peak": 100.0,
          "current": 85.0
        }
      },
      "performance": {
        "averageResponseTime": 50,
        "p95ResponseTime": 100,
        "p99ResponseTime": 200,
        "throughput": 500,
        "errorRate": 0.001,
        "availability": 99.9
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Provider statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Provider Performance
```http
GET /api/provider/{providerId}/performance
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerId` (string): Provider UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providerId": "provider_123",
      "name": "MongoDBProvider",
      "performance": {
        "averageResponseTime": 50,
        "peakResponseTime": 200,
        "throughput": 500,
        "errorRate": 0.001,
        "availability": 99.9,
        "reliability": 99.8
      },
      "metrics": {
        "requestsPerSecond": 100,
        "averageLatency": 50,
        "p95Latency": 100,
        "p99Latency": 200,
        "errorRate": 0.001,
        "successRate": 0.999
      },
      "trends": {
        "responseTime": "stable",
        "throughput": "increasing",
        "errorRate": "decreasing",
        "availability": "stable"
      },
      "benchmarks": {
        "read": 50,
        "write": 75,
        "query": 100,
        "connection": 150
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Provider performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Provider Health

### Get Provider Health
```http
GET /api/provider/{providerId}/health
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerId` (string): Provider UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providerId": "provider_123",
      "name": "MongoDBProvider",
      "status": "Healthy",
      "overallHealth": 0.95,
      "components": {
        "connection": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "authentication": {
          "status": "Healthy",
          "health": 0.99,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "performance": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "resources": {
          "status": "Warning",
          "health": 0.85,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Connection Test",
          "status": "Pass",
          "responseTime": 150,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Authentication Test",
          "status": "Pass",
          "responseTime": 50,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Performance Test",
          "status": "Pass",
          "responseTime": 100,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Resource Test",
          "status": "Warning",
          "responseTime": 200,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [
        {
          "type": "High Memory Usage",
          "severity": "Medium",
          "message": "Memory usage is above 80%",
          "createdAt": "2024-01-20T14:30:00Z"
        }
      ],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Provider health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Run Provider Health Check
```http
POST /api/provider/{providerId}/health/check
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerId` (string): Provider UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providerId": "provider_123",
      "name": "MongoDBProvider",
      "healthCheck": {
        "status": "Completed",
        "overallHealth": 0.95,
        "checks": [
          {
            "name": "Connection Test",
            "status": "Pass",
            "responseTime": 150,
            "details": "Connection established successfully"
          },
          {
            "name": "Authentication Test",
            "status": "Pass",
            "responseTime": 50,
            "details": "Authentication successful"
          },
          {
            "name": "Performance Test",
            "status": "Pass",
            "responseTime": 100,
            "details": "Performance within acceptable limits"
          },
          {
            "name": "Resource Test",
            "status": "Warning",
            "responseTime": 200,
            "details": "High memory usage detected"
          }
        ],
        "startedAt": "2024-01-20T14:30:00Z",
        "completedAt": "2024-01-20T14:30:05Z",
        "duration": 5.0
      }
    },
    "message": "Provider health check completed successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Error Responses

### Provider Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Provider not found",
  "exception": "Provider with ID provider_123 not found"
}
```

### Provider Not Available
```json
{
  "result": null,
  "isError": true,
  "message": "Provider not available",
  "exception": "Provider is currently offline or in maintenance mode"
}
```

### Configuration Error
```json
{
  "result": null,
  "isError": true,
  "message": "Configuration error",
  "exception": "Invalid configuration: connectionString is required"
}
```

### Health Check Failed
```json
{
  "result": null,
  "isError": true,
  "message": "Health check failed",
  "exception": "Provider failed health check: connection timeout"
}
```

### Resource Exhausted
```json
{
  "result": null,
  "isError": true,
  "message": "Resource exhausted",
  "exception": "Provider has reached maximum connection limit"
}
```

---

## Navigation

**← Previous:** [ONODE API](ONODE-API.md) | **Next:** [Search API](Search-API.md) →
