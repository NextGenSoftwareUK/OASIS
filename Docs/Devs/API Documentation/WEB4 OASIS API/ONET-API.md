# ONET API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Network Management](#network-management)
- [Node Operations](#node-operations)
- [Network Analytics](#network-analytics)
- [Network Security](#network-security)
- [Error Responses](#error-responses)

## Overview

The ONET API provides comprehensive network management services for the OASIS ecosystem. It handles network operations, node management, routing, and analytics with support for multiple protocols, real-time updates, and advanced security features.

## Network Management

### Get Network Status
```http
GET /api/onet/network/status
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "network": {
        "id": "onet_mainnet",
        "name": "OASIS Network",
        "version": "1.0.0",
        "status": "Active",
        "type": "P2P",
        "protocol": "ONET",
        "consensus": "Proof of Stake"
      },
      "nodes": {
        "total": 1250,
        "active": 1200,
        "inactive": 50,
        "bootstrap": 10,
        "validators": 100
      },
      "connections": {
        "total": 5000,
        "active": 4500,
        "pending": 500,
        "averagePerNode": 4
      },
      "performance": {
        "averageLatency": 150,
        "peakLatency": 500,
        "throughput": 1000,
        "errorRate": 0.001,
        "availability": 99.9
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Network status retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Network Nodes
```http
GET /api/onet/network/nodes
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `status` (string, optional): Filter by status (Active, Inactive, Bootstrap, Validator)
- `type` (string, optional): Filter by type (Full, Light, Archive)
- `sortBy` (string, optional): Sort field (name, latency, uptime, lastActivity)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "nodes": [
        {
          "id": "node_123",
          "name": "OASIS Node #1",
          "status": "Active",
          "type": "Full",
          "role": "Validator",
          "address": "node1.onet.oasisplatform.world:8080",
          "publicKey": "0x742d35Cc6634C0532925a3b8D4C9db96C4b4d8b6",
          "location": {
            "country": "USA",
            "region": "US-East",
            "city": "New York",
            "coordinates": {
              "latitude": 40.7128,
              "longitude": -74.0060
            }
          },
          "performance": {
            "latency": 120,
            "uptime": 99.9,
            "throughput": 1000,
            "lastActivity": "2024-01-20T14:30:00Z"
          },
          "resources": {
            "cpu": 25.5,
            "memory": 40.2,
            "storage": 60.8,
            "bandwidth": 80.0
          },
          "createdAt": "2024-01-20T14:30:00Z",
          "lastModified": "2024-01-20T14:30:00Z"
        }
      ],
      "totalCount": 1,
      "limit": 50,
      "offset": 0
    },
    "message": "Network nodes retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Connect to Network
```http
POST /api/onet/network/connect
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "nodeId": "node_123",
  "address": "node1.onet.oasisplatform.world:8080",
  "publicKey": "0x742d35Cc6634C0532925a3b8D4C9db96C4b4d8b6",
  "type": "Full",
  "role": "Validator",
  "location": {
    "country": "USA",
    "region": "US-East",
    "city": "New York",
    "coordinates": {
      "latitude": 40.7128,
      "longitude": -74.0060
    }
  },
  "metadata": {
    "description": "OASIS Network Node",
    "tags": ["validator", "full", "us-east"],
    "version": "1.0.0"
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "nodeId": "node_123",
      "name": "OASIS Node #1",
      "status": "Active",
      "type": "Full",
      "role": "Validator",
      "address": "node1.onet.oasisplatform.world:8080",
      "publicKey": "0x742d35Cc6634C0532925a3b8D4C9db96C4b4d8b6",
      "location": {
        "country": "USA",
        "region": "US-East",
        "city": "New York",
        "coordinates": {
          "latitude": 40.7128,
          "longitude": -74.0060
        }
      },
      "performance": {
        "latency": 120,
        "uptime": 99.9,
        "throughput": 1000,
        "lastActivity": "2024-01-20T14:30:00Z"
      },
      "resources": {
        "cpu": 25.5,
        "memory": 40.2,
        "storage": 60.8,
        "bandwidth": 80.0
      },
      "connectedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Node connected to network successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Disconnect from Network
```http
POST /api/onet/network/disconnect
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "nodeId": "node_123",
      "status": "Inactive",
      "disconnectedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Node disconnected from network successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Network Stats
```http
GET /api/onet/network/stats
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "network": {
        "id": "onet_mainnet",
        "name": "OASIS Network",
        "version": "1.0.0",
        "status": "Active"
      },
      "statistics": {
        "nodes": {
          "total": 1250,
          "active": 1200,
          "inactive": 50,
          "bootstrap": 10,
          "validators": 100
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
        "performance": {
          "averageLatency": 150,
          "peakLatency": 500,
          "throughput": 1000,
          "errorRate": 0.001,
          "availability": 99.9
        }
      },
      "trends": {
        "nodes": "increasing",
        "connections": "stable",
        "traffic": "increasing",
        "performance": "stable"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Network statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Start Network
```http
POST /api/onet/network/start
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "network": {
        "id": "onet_mainnet",
        "name": "OASIS Network",
        "version": "1.0.0",
        "status": "Active"
      },
      "startedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Network started successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Stop Network
```http
POST /api/onet/network/stop
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "network": {
        "id": "onet_mainnet",
        "name": "OASIS Network",
        "version": "1.0.0",
        "status": "Inactive"
      },
      "stoppedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Network stopped successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Network Topology
```http
GET /api/onet/network/topology
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "topology": {
        "nodes": [
          {
            "id": "node_123",
            "name": "OASIS Node #1",
            "status": "Active",
            "type": "Full",
            "role": "Validator",
            "address": "node1.onet.oasisplatform.world:8080",
            "location": {
              "country": "USA",
              "region": "US-East",
              "city": "New York",
              "coordinates": {
                "latitude": 40.7128,
                "longitude": -74.0060
              }
            },
            "connections": [
              {
                "nodeId": "node_456",
                "latency": 120,
                "bandwidth": 1000,
                "status": "Active"
              }
            ]
          }
        ],
        "connections": [
          {
            "from": "node_123",
            "to": "node_456",
            "latency": 120,
            "bandwidth": 1000,
            "status": "Active"
          }
        ],
        "lastUpdated": "2024-01-20T14:30:00Z"
      }
    },
    "message": "Network topology retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Broadcast Message
```http
POST /api/onet/network/broadcast
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "message": "Hello, OASIS Network!",
  "type": "Text",
  "priority": "Normal",
  "ttl": 3600,
  "metadata": {
    "sender": "user_123",
    "tags": ["announcement", "network"],
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
      "messageId": "msg_123",
      "message": "Hello, OASIS Network!",
      "type": "Text",
      "priority": "Normal",
      "ttl": 3600,
      "metadata": {
        "sender": "user_123",
        "tags": ["announcement", "network"],
        "version": "1.0"
      },
      "status": "Broadcasted",
      "recipients": 1200,
      "broadcastedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Message broadcasted successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Node Operations

### Get Node Status
```http
GET /api/onet/node/{nodeId}/status
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `nodeId` (string): Node UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "nodeId": "node_123",
      "name": "OASIS Node #1",
      "status": "Active",
      "type": "Full",
      "role": "Validator",
      "address": "node1.onet.oasisplatform.world:8080",
      "publicKey": "0x742d35Cc6634C0532925a3b8D4C9db96C4b4d8b6",
      "performance": {
        "latency": 120,
        "uptime": 99.9,
        "throughput": 1000,
        "lastActivity": "2024-01-20T14:30:00Z"
      },
      "resources": {
        "cpu": 25.5,
        "memory": 40.2,
        "storage": 60.8,
        "bandwidth": 80.0
      },
      "connections": {
        "total": 4,
        "active": 4,
        "pending": 0
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Node status retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Node Performance
```http
GET /api/onet/node/{nodeId}/performance
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `nodeId` (string): Node UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "nodeId": "node_123",
      "performance": {
        "latency": 120,
        "uptime": 99.9,
        "throughput": 1000,
        "errorRate": 0.001,
        "availability": 99.9
      },
      "metrics": {
        "requestsPerSecond": 100,
        "averageLatency": 120,
        "p95Latency": 200,
        "p99Latency": 300,
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
        "cpu": {
          "usage": 25.5,
          "peak": 60.0,
          "average": 30.0
        },
        "memory": {
          "usage": 40.2,
          "peak": 80.0,
          "average": 45.0
        },
        "storage": {
          "usage": 60.8,
          "peak": 90.0,
          "average": 65.0
        },
        "bandwidth": {
          "usage": 80.0,
          "peak": 100.0,
          "average": 85.0
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Node performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Node Health
```http
GET /api/onet/node/{nodeId}/health
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `nodeId` (string): Node UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "nodeId": "node_123",
      "status": "Healthy",
      "overallHealth": 0.95,
      "components": {
        "connectivity": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "performance": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "resources": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "security": {
          "status": "Healthy",
          "health": 0.97,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Connectivity Test",
          "status": "Pass",
          "responseTime": 120,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Performance Test",
          "status": "Pass",
          "responseTime": 100,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Resources Test",
          "status": "Pass",
          "responseTime": 50,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Security Test",
          "status": "Pass",
          "responseTime": 80,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Node health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Network Analytics

### Get Network Analytics
```http
GET /api/onet/network/analytics
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `includeDetails` (boolean, optional): Include detailed analytics

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "analytics": {
        "nodes": {
          "total": 1250,
          "active": 1200,
          "inactive": 50,
          "new": 25,
          "removed": 5
        },
        "connections": {
          "total": 5000,
          "active": 4500,
          "pending": 500,
          "averagePerNode": 4,
          "peakConnections": 6000
        },
        "traffic": {
          "totalDataTransferred": "2.5TB",
          "averageBandwidth": 1000,
          "peakBandwidth": 1500,
          "packetsPerSecond": 10000,
          "averagePacketSize": 1024
        },
        "performance": {
          "averageLatency": 150,
          "peakLatency": 500,
          "throughput": 1000,
          "errorRate": 0.001,
          "availability": 99.9
        }
      },
      "trends": {
        "nodes": "increasing",
        "connections": "stable",
        "traffic": "increasing",
        "performance": "stable"
      },
      "breakdown": {
        "byRegion": {
          "US-East": {
            "nodes": 400,
            "connections": 1600,
            "traffic": "1TB"
          },
          "US-West": {
            "nodes": 300,
            "connections": 1200,
            "traffic": "0.8TB"
          },
          "Europe": {
            "nodes": 350,
            "connections": 1400,
            "traffic": "0.7TB"
          },
          "Asia": {
            "nodes": 200,
            "connections": 800,
            "traffic": "0.5TB"
          }
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Network analytics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Network Performance
```http
GET /api/onet/network/performance
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "performance": {
        "averageLatency": 150,
        "peakLatency": 500,
        "throughput": 1000,
        "errorRate": 0.001,
        "availability": 99.9
      },
      "metrics": {
        "requestsPerSecond": 10000,
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
        "byRegion": {
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

### Get Network Health
```http
GET /api/onet/network/health
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
        "connectivity": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "routing": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "performance": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "security": {
          "status": "Healthy",
          "health": 0.97,
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
          "name": "Routing Test",
          "status": "Pass",
          "responseTime": 100,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Performance Test",
          "status": "Pass",
          "responseTime": 200,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Security Test",
          "status": "Pass",
          "responseTime": 80,
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

## Network Security

### Get Network Security
```http
GET /api/onet/network/security
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "network": {
        "id": "onet_mainnet",
        "name": "OASIS Network",
        "version": "1.0.0"
      },
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC",
        "consensus": "Proof of Stake",
        "auditLogging": true
      },
      "access": {
        "lastAccessed": "2024-01-20T14:30:00Z",
        "accessCount": 10000,
        "failedAttempts": 50,
        "locked": false
      },
      "compliance": {
        "kyc": true,
        "aml": true,
        "sanctions": true,
        "tax": true
      },
      "audit": {
        "lastAudit": "2024-01-20T14:30:00Z",
        "auditCount": 25,
        "complianceScore": 0.98
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Network security retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Network Security
```http
PUT /api/onet/network/security
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "encryption": "AES-256",
  "authentication": "JWT",
  "authorization": "RBAC",
  "consensus": "Proof of Stake",
  "auditLogging": true
}
```

## Error Responses

### Network Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Network not found",
  "exception": "Network with ID onet_mainnet not found"
}
```

### Node Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Node not found",
  "exception": "Node with ID node_123 not found"
}
```

### Connection Failed
```json
{
  "result": null,
  "isError": true,
  "message": "Connection failed",
  "exception": "Failed to connect to network node"
}
```

### Invalid Message
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid message",
  "exception": "Message format is invalid or corrupted"
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

**← Previous:** [NFT API](NFT-API.md) | **Next:** [Avatar API](Avatar-API.md) →