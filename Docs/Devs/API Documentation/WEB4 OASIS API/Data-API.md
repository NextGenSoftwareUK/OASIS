# Data API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Data Management](#data-management)
- [Data Operations](#data-operations)
- [Data Analytics](#data-analytics)
- [Data Security](#data-security)
- [Error Responses](#error-responses)

## Overview

The Data API provides comprehensive data management services for the OASIS ecosystem. It handles data storage, retrieval, processing, and analytics with support for multiple data types, real-time updates, and advanced security features.

## Data Management

### Get All Data
```http
GET /api/data
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by type (User, System, Application, External)
- `status` (string, optional): Filter by status (Active, Inactive, Archived, Deleted)
- `category` (string, optional): Filter by category (Profile, Settings, Analytics, Logs)
- `sortBy` (string, optional): Sort field (name, createdAt, lastModified, size)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "data": [
        {
          "id": "data_123",
          "name": "User Profile Data",
          "type": "User",
          "category": "Profile",
          "status": "Active",
          "owner": {
            "id": "user_123",
            "username": "john_doe",
            "avatar": "https://example.com/avatars/john.jpg"
          },
          "size": 1024,
          "format": "JSON",
          "encryption": "AES-256",
          "metadata": {
            "description": "User profile information",
            "tags": ["profile", "user", "personal"],
            "version": "1.0",
            "schema": "UserProfile"
          },
          "permissions": {
            "read": true,
            "write": true,
            "delete": true,
            "share": false
          },
          "createdAt": "2024-01-20T14:30:00Z",
          "lastModified": "2024-01-20T14:30:00Z"
        }
      ],
      "totalCount": 1,
      "limit": 50,
      "offset": 0
    },
    "message": "Data retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Data by ID
```http
GET /api/data/{dataId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `dataId` (string): Data UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "data_123",
      "name": "User Profile Data",
      "type": "User",
      "category": "Profile",
      "status": "Active",
      "owner": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "size": 1024,
      "format": "JSON",
      "encryption": "AES-256",
      "content": {
        "firstName": "John",
        "lastName": "Doe",
        "email": "john@example.com",
        "phone": "+1234567890",
        "address": {
          "street": "123 Main St",
          "city": "New York",
          "state": "NY",
          "zipCode": "10001",
          "country": "USA"
        },
        "preferences": {
          "language": "en-US",
          "timezone": "UTC",
          "currency": "USD",
          "notifications": true
        }
      },
      "metadata": {
        "description": "User profile information",
        "tags": ["profile", "user", "personal"],
        "version": "1.0",
        "schema": "UserProfile"
      },
      "permissions": {
        "read": true,
        "write": true,
        "delete": true,
        "share": false
      },
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC"
      },
      "analytics": {
        "views": 10,
        "downloads": 5,
        "shares": 0,
        "lastAccessed": "2024-01-20T14:30:00Z"
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "Data retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Create Data
```http
POST /api/data
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "name": "User Settings Data",
  "type": "User",
  "category": "Settings",
  "content": {
    "theme": "dark",
    "language": "en-US",
    "timezone": "UTC",
    "currency": "USD",
    "notifications": {
      "email": true,
      "push": true,
      "sms": false
    },
    "privacy": {
      "profileVisibility": "public",
      "dataSharing": "limited",
      "analytics": true
    }
  },
  "metadata": {
    "description": "User settings and preferences",
    "tags": ["settings", "user", "preferences"],
    "version": "1.0",
    "schema": "UserSettings"
  },
  "permissions": {
    "read": true,
    "write": true,
    "delete": true,
    "share": false
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "data_124",
      "name": "User Settings Data",
      "type": "User",
      "category": "Settings",
      "status": "Active",
      "owner": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "size": 512,
      "format": "JSON",
      "encryption": "AES-256",
      "content": {
        "theme": "dark",
        "language": "en-US",
        "timezone": "UTC",
        "currency": "USD",
        "notifications": {
          "email": true,
          "push": true,
          "sms": false
        },
        "privacy": {
          "profileVisibility": "public",
          "dataSharing": "limited",
          "analytics": true
        }
      },
      "metadata": {
        "description": "User settings and preferences",
        "tags": ["settings", "user", "preferences"],
        "version": "1.0",
        "schema": "UserSettings"
      },
      "permissions": {
        "read": true,
        "write": true,
        "delete": true,
        "share": false
      },
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC"
      },
      "analytics": {
        "views": 0,
        "downloads": 0,
        "shares": 0,
        "lastAccessed": null
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "Data created successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Data
```http
PUT /api/data/{dataId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `dataId` (string): Data UUID

**Request Body:**
```json
{
  "name": "Updated User Settings Data",
  "content": {
    "theme": "light",
    "language": "es-ES",
    "timezone": "Europe/Madrid",
    "currency": "EUR",
    "notifications": {
      "email": true,
      "push": false,
      "sms": false
    },
    "privacy": {
      "profileVisibility": "private",
      "dataSharing": "none",
      "analytics": false
    }
  },
  "metadata": {
    "description": "Updated user settings and preferences",
    "tags": ["settings", "user", "preferences", "updated"],
    "version": "1.1",
    "schema": "UserSettings"
  }
}
```

### Delete Data
```http
DELETE /api/data/{dataId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `dataId` (string): Data UUID

## Data Operations

### Save Data
```http
POST /api/data/save
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "key": "user_preferences",
  "value": {
    "theme": "dark",
    "language": "en-US",
    "timezone": "UTC",
    "currency": "USD"
  },
  "metadata": {
    "description": "User preferences",
    "tags": ["preferences", "user"],
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
      "key": "user_preferences",
      "value": {
        "theme": "dark",
        "language": "en-US",
        "timezone": "UTC",
        "currency": "USD"
      },
      "metadata": {
        "description": "User preferences",
        "tags": ["preferences", "user"],
        "version": "1.0"
      },
      "savedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Data saved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Load Data
```http
GET /api/data/load/{key}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `key` (string): Data key

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "key": "user_preferences",
      "value": {
        "theme": "dark",
        "language": "en-US",
        "timezone": "UTC",
        "currency": "USD"
      },
      "metadata": {
        "description": "User preferences",
        "tags": ["preferences", "user"],
        "version": "1.0"
      },
      "loadedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Data loaded successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Delete Data
```http
DELETE /api/data/delete/{key}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `key` (string): Data key

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "key": "user_preferences",
      "deletedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Data deleted successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### List Data
```http
GET /api/data/list
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `filter` (string, optional): Filter by key pattern
- `sortBy` (string, optional): Sort field (key, createdAt, lastModified)
- `sortOrder` (string, optional): Sort order (asc/desc, default: asc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "data": [
        {
          "key": "user_preferences",
          "value": {
            "theme": "dark",
            "language": "en-US",
            "timezone": "UTC",
            "currency": "USD"
          },
          "metadata": {
            "description": "User preferences",
            "tags": ["preferences", "user"],
            "version": "1.0"
          },
          "createdAt": "2024-01-20T14:30:00Z",
          "lastModified": "2024-01-20T14:30:00Z"
        },
        {
          "key": "user_settings",
          "value": {
            "notifications": true,
            "privacy": "public",
            "sharing": "limited"
          },
          "metadata": {
            "description": "User settings",
            "tags": ["settings", "user"],
            "version": "1.0"
          },
          "createdAt": "2024-01-20T14:25:00Z",
          "lastModified": "2024-01-20T14:25:00Z"
        }
      ],
      "totalCount": 2,
      "limit": 50,
      "offset": 0
    },
    "message": "Data list retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Data Analytics

### Get Data Statistics
```http
GET /api/data/stats
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `type` (string, optional): Filter by data type

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "statistics": {
        "data": {
          "total": 10000,
          "created": 500,
          "updated": 1000,
          "deleted": 100,
          "accessed": 2000
        },
        "byType": {
          "User": 6000,
          "System": 2000,
          "Application": 1500,
          "External": 500
        },
        "byCategory": {
          "Profile": 3000,
          "Settings": 2000,
          "Analytics": 1500,
          "Logs": 1000,
          "Other": 2500
        },
        "storage": {
          "totalSize": "10GB",
          "averageSize": "1MB",
          "largestSize": "100MB",
          "smallestSize": "1KB"
        },
        "performance": {
          "averageAccessTime": 0.1,
          "peakAccessTime": 1.0,
          "accessSuccessRate": 0.99,
          "storageEfficiency": 0.95
        }
      },
      "trends": {
        "data": "increasing",
        "storage": "increasing",
        "performance": "stable"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Data statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Data Performance
```http
GET /api/data/performance
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "performance": {
        "averageAccessTime": 0.1,
        "peakAccessTime": 1.0,
        "averageStorageTime": 0.05,
        "peakStorageTime": 0.5,
        "accessSuccessRate": 0.99,
        "storageSuccessRate": 0.98
      },
      "metrics": {
        "operationsPerSecond": 1000,
        "averageLatency": 0.1,
        "p95Latency": 0.5,
        "p99Latency": 1.0,
        "errorRate": 0.01,
        "successRate": 0.99
      },
      "trends": {
        "accessTime": "stable",
        "storageTime": "stable",
        "successRate": "stable",
        "errorRate": "decreasing"
      },
      "breakdown": {
        "User": {
          "averageAccessTime": 0.1,
          "averageStorageTime": 0.05,
          "successRate": 0.99
        },
        "System": {
          "averageAccessTime": 0.2,
          "averageStorageTime": 0.1,
          "successRate": 0.98
        },
        "Application": {
          "averageAccessTime": 0.15,
          "averageStorageTime": 0.08,
          "successRate": 0.97
        },
        "External": {
          "averageAccessTime": 0.3,
          "averageStorageTime": 0.15,
          "successRate": 0.95
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Data performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Data Health
```http
GET /api/data/health
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
        "storage": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "access": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "encryption": {
          "status": "Healthy",
          "health": 0.97,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "analytics": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Storage Test",
          "status": "Pass",
          "responseTime": 0.1,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Access Test",
          "status": "Pass",
          "responseTime": 0.05,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Encryption Test",
          "status": "Pass",
          "responseTime": 0.02,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Analytics Test",
          "status": "Pass",
          "responseTime": 0.08,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Data health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Data Security

### Get Data Security
```http
GET /api/data/{dataId}/security
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `dataId` (string): Data UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "dataId": "data_123",
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC",
        "auditLogging": true,
        "dataMasking": true
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
    "message": "Data security retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Data Security
```http
PUT /api/data/{dataId}/security
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `dataId` (string): Data UUID

**Request Body:**
```json
{
  "encryption": "AES-256",
  "authentication": "JWT",
  "authorization": "RBAC",
  "auditLogging": true,
  "dataMasking": true
}
```

## Error Responses

### Data Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Data not found",
  "exception": "Data with ID data_123 not found"
}
```

### Invalid Data Format
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid data format",
  "exception": "Data format 'InvalidFormat' is not supported"
}
```

### Data Too Large
```json
{
  "result": null,
  "isError": true,
  "message": "Data too large",
  "exception": "Data size 100MB exceeds maximum allowed size of 50MB"
}
```

### Permission Denied
```json
{
  "result": null,
  "isError": true,
  "message": "Permission denied",
  "exception": "Insufficient permissions to access this data"
}
```

### Storage Quota Exceeded
```json
{
  "result": null,
  "isError": true,
  "message": "Storage quota exceeded",
  "exception": "Storage quota of 1GB exceeded. Current usage: 1.2GB"
}
```

---

## Navigation

**← Previous:** [Karma API](Karma-API.md) | **Next:** [Wallet API](Wallet-API.md) →