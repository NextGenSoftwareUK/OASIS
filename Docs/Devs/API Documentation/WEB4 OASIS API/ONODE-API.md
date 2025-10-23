# ONODE API

## 📋 **Table of Contents**

- [Overview](#overview)
- [ONODE Management](#onode-management)
- [ONODE Configuration](#onode-configuration)
- [Provider Management](#provider-management)
- [ONODE Statistics](#onode-statistics)
- [ONODE Monitoring](#onode-monitoring)
- [Error Responses](#error-responses)

## Overview

The ONODE API provides comprehensive management of OASIS Nodes (ONODEs), which are the core components of the OASIS ecosystem. Each ONODE can run multiple OASIS providers and provides various services to the network.

## ONODE Management

### Get ONODE Status
```http
GET /api/onode/status
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "onodeId": "onode_123",
      "name": "Main ONODE",
      "status": "Running",
      "version": "1.0.0",
      "uptime": "7 days, 12 hours",
      "startedAt": "2024-01-13T02:30:00Z",
      "lastActivity": "2024-01-20T14:30:00Z",
      "networkId": "onet_mainnet",
      "region": "US-East",
      "capabilities": ["Storage", "Compute", "Routing", "API"],
      "resources": {
        "cpu": 45.5,
        "memory": 60.2,
        "storage": 75.8,
        "bandwidth": 80.0
      },
      "connections": {
        "active": 45,
        "max": 100,
        "pending": 5
      },
      "performance": {
        "averageResponseTime": 120,
        "throughput": 1000,
        "errorRate": 0.001
      }
    },
    "message": "ONODE status retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Start ONODE
```http
POST /api/onode/start
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "name": "Main ONODE",
  "region": "US-East",
  "maxConnections": 100,
  "enableAPI": true,
  "enableStorage": true,
  "enableCompute": true,
  "enableRouting": true,
  "bootstrapNodes": [
    "node1.onet.oasisplatform.world:8080",
    "node2.onet.oasisplatform.world:8080"
  ]
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "onodeId": "onode_123",
      "name": "Main ONODE",
      "status": "Starting",
      "region": "US-East",
      "maxConnections": 100,
      "capabilities": ["API", "Storage", "Compute", "Routing"],
      "bootstrapNodes": [
        "node1.onet.oasisplatform.world:8080",
        "node2.onet.oasisplatform.world:8080"
      ],
      "startedAt": "2024-01-20T14:30:00Z",
      "estimatedStartTime": "30 seconds"
    },
    "message": "ONODE start initiated successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Stop ONODE
```http
POST /api/onode/stop
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "onodeId": "onode_123",
      "status": "Stopping",
      "stoppedAt": "2024-01-20T14:30:00Z",
      "gracefulShutdown": true,
      "estimatedStopTime": "10 seconds"
    },
    "message": "ONODE stop initiated successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Restart ONODE
```http
POST /api/onode/restart
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "onodeId": "onode_123",
      "status": "Restarting",
      "restartedAt": "2024-01-20T14:30:00Z",
      "estimatedRestartTime": "45 seconds"
    },
    "message": "ONODE restart initiated successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## ONODE Configuration

### Get ONODE Configuration
```http
GET /api/onode/config
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "onodeId": "onode_123",
      "name": "Main ONODE",
      "region": "US-East",
      "maxConnections": 100,
      "capabilities": ["API", "Storage", "Compute", "Routing"],
      "network": {
        "networkId": "onet_mainnet",
        "bootstrapNodes": [
          "node1.onet.oasisplatform.world:8080",
          "node2.onet.oasisplatform.world:8080"
        ],
        "enableDiscovery": true,
        "enableRouting": true
      },
      "api": {
        "enabled": true,
        "port": 8080,
        "ssl": true,
        "rateLimit": 1000
      },
      "storage": {
        "enabled": true,
        "maxSize": "1TB",
        "encryption": true,
        "replication": 3
      },
      "compute": {
        "enabled": true,
        "maxTasks": 100,
        "timeout": 3600
      },
      "routing": {
        "enabled": true,
        "algorithm": "Dijkstra",
        "maxHops": 10
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "ONODE configuration retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update ONODE Configuration
```http
PUT /api/onode/config
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "name": "Updated ONODE Name",
  "maxConnections": 150,
  "capabilities": ["API", "Storage", "Compute", "Routing", "Analytics"],
  "network": {
    "enableDiscovery": true,
    "enableRouting": true,
    "maxHops": 15
  },
  "api": {
    "rateLimit": 2000,
    "timeout": 30
  },
  "storage": {
    "maxSize": "2TB",
    "replication": 5
  },
  "compute": {
    "maxTasks": 200,
    "timeout": 7200
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "onodeId": "onode_123",
      "name": "Updated ONODE Name",
      "maxConnections": 150,
      "capabilities": ["API", "Storage", "Compute", "Routing", "Analytics"],
      "network": {
        "enableDiscovery": true,
        "enableRouting": true,
        "maxHops": 15
      },
      "api": {
        "rateLimit": 2000,
        "timeout": 30
      },
      "storage": {
        "maxSize": "2TB",
        "replication": 5
      },
      "compute": {
        "maxTasks": 200,
        "timeout": 7200
      },
      "updatedAt": "2024-01-20T14:30:00Z",
      "requiresRestart": true
    },
    "message": "ONODE configuration updated successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get ONODE DNA
```http
GET /api/onode/dna
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "onodeId": "onode_123",
      "dna": {
        "version": "1.0.0",
        "name": "Main ONODE",
        "description": "Primary OASIS Node",
        "author": "OASIS Team",
        "created": "2024-01-20T14:30:00Z",
        "lastModified": "2024-01-20T14:30:00Z",
        "providers": [
          {
            "name": "MongoDBProvider",
            "type": "Storage",
            "enabled": true,
            "config": {
              "connectionString": "mongodb://localhost:27017",
              "database": "oasis"
            }
          },
          {
            "name": "EthereumProvider",
            "type": "Blockchain",
            "enabled": true,
            "config": {
              "network": "mainnet",
              "rpcUrl": "https://mainnet.infura.io/v3/..."
            }
          }
        ],
        "network": {
          "networkId": "onet_mainnet",
          "bootstrapNodes": [
            "node1.onet.oasisplatform.world:8080",
            "node2.onet.oasisplatform.world:8080"
          ]
        },
        "capabilities": ["API", "Storage", "Compute", "Routing"],
        "security": {
          "encryption": true,
          "authentication": true,
          "authorization": true
        }
      }
    },
    "message": "ONODE DNA retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Provider Management

### Get ONODE Providers
```http
GET /api/onode/providers
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "onodeId": "onode_123",
      "providers": [
        {
          "name": "MongoDBProvider",
          "type": "Storage",
          "status": "Running",
          "version": "1.0.0",
          "enabled": true,
          "startedAt": "2024-01-20T14:30:00Z",
          "uptime": "7 days, 12 hours",
          "performance": {
            "averageResponseTime": 50,
            "throughput": 500,
            "errorRate": 0.001
          },
          "resources": {
            "cpu": 25.5,
            "memory": 40.2,
            "storage": 60.8
          }
        },
        {
          "name": "EthereumProvider",
          "type": "Blockchain",
          "status": "Running",
          "version": "1.0.0",
          "enabled": true,
          "startedAt": "2024-01-20T14:30:00Z",
          "uptime": "7 days, 12 hours",
          "performance": {
            "averageResponseTime": 200,
            "throughput": 100,
            "errorRate": 0.005
          },
          "resources": {
            "cpu": 15.5,
            "memory": 20.2,
            "storage": 15.0
          }
        }
      ],
      "totalProviders": 2,
      "runningProviders": 2,
      "stoppedProviders": 0,
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "ONODE providers retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Start Provider
```http
POST /api/onode/providers/{providerName}/start
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerName` (string): Provider name

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providerName": "MongoDBProvider",
      "status": "Starting",
      "startedAt": "2024-01-20T14:30:00Z",
      "estimatedStartTime": "10 seconds"
    },
    "message": "Provider start initiated successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Stop Provider
```http
POST /api/onode/providers/{providerName}/stop
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerName` (string): Provider name

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providerName": "MongoDBProvider",
      "status": "Stopping",
      "stoppedAt": "2024-01-20T14:30:00Z",
      "estimatedStopTime": "5 seconds"
    },
    "message": "Provider stop initiated successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Restart Provider
```http
POST /api/onode/providers/{providerName}/restart
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerName` (string): Provider name

### Get Provider Status
```http
GET /api/onode/providers/{providerName}/status
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerName` (string): Provider name

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providerName": "MongoDBProvider",
      "type": "Storage",
      "status": "Running",
      "version": "1.0.0",
      "enabled": true,
      "startedAt": "2024-01-20T14:30:00Z",
      "uptime": "7 days, 12 hours",
      "lastActivity": "2024-01-20T14:30:00Z",
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
      "lastHealthCheck": "2024-01-20T14:30:00Z"
    },
    "message": "Provider status retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## ONODE Statistics

### Get ONODE Stats
```http
GET /api/onode/stats
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `includeDetails` (boolean, optional): Include detailed statistics

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "onodeId": "onode_123",
      "timeframe": "day",
      "uptime": "99.9%",
      "totalRequests": 1000000,
      "successfulRequests": 999000,
      "failedRequests": 1000,
      "errorRate": 0.001,
      "averageResponseTime": 120,
      "peakResponseTime": 500,
      "throughput": 1000,
      "dataTransferred": "2.5TB",
      "connections": {
        "total": 1000,
        "active": 45,
        "max": 100,
        "average": 50
      },
      "providers": {
        "total": 2,
        "running": 2,
        "stopped": 0,
        "failed": 0
      },
      "resources": {
        "cpu": {
          "average": 45.5,
          "peak": 80.0,
          "current": 50.0
        },
        "memory": {
          "average": 60.2,
          "peak": 90.0,
          "current": 65.0
        },
        "storage": {
          "average": 75.8,
          "peak": 95.0,
          "current": 80.0
        },
        "bandwidth": {
          "average": 80.0,
          "peak": 100.0,
          "current": 85.0
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "ONODE statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get ONODE Performance
```http
GET /api/onode/performance
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "onodeId": "onode_123",
      "performance": {
        "averageResponseTime": 120,
        "peakResponseTime": 500,
        "throughput": 1000,
        "errorRate": 0.001,
        "availability": 99.9,
        "reliability": 99.8
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
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "ONODE performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## ONODE Monitoring

### Get ONODE Health
```http
GET /api/onode/health
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "onodeId": "onode_123",
      "status": "Healthy",
      "overallHealth": 0.95,
      "components": {
        "api": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "storage": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "compute": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "routing": {
          "status": "Healthy",
          "health": 0.97,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "providers": [
        {
          "name": "MongoDBProvider",
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "EthereumProvider",
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "ONODE health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get ONODE Logs
```http
GET /api/onode/logs
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 100)
- `offset` (int, optional): Number to skip (default: 0)
- `level` (string, optional): Filter by log level (DEBUG, INFO, WARN, ERROR)
- `component` (string, optional): Filter by component
- `startDate` (string, optional): Start date (ISO 8601)
- `endDate` (string, optional): End date (ISO 8601)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "onodeId": "onode_123",
      "logs": [
        {
          "timestamp": "2024-01-20T14:30:00Z",
          "level": "INFO",
          "component": "API",
          "message": "Request processed successfully",
          "details": {
            "requestId": "req_123",
            "responseTime": 120,
            "statusCode": 200
          }
        },
        {
          "timestamp": "2024-01-20T14:29:00Z",
          "level": "WARN",
          "component": "Storage",
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
    "message": "ONODE logs retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Error Responses

### ONODE Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "ONODE not found",
  "exception": "ONODE with ID onode_123 not found"
}
```

### ONODE Not Running
```json
{
  "result": null,
  "isError": true,
  "message": "ONODE not running",
  "exception": "ONODE is currently stopped"
}
```

### Provider Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Provider not found",
  "exception": "Provider MongoDBProvider not found"
}
```

### Configuration Error
```json
{
  "result": null,
  "isError": true,
  "message": "Configuration error",
  "exception": "Invalid configuration: maxConnections must be greater than 0"
}
```

### Resource Exhausted
```json
{
  "result": null,
  "isError": true,
  "message": "Resource exhausted",
  "exception": "ONODE has reached maximum connection limit"
}
```

---

## Navigation

**← Previous:** [ONET API](ONET-API.md) | **Next:** [Provider API](Provider-API.md) →
