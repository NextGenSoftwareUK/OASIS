# Settings API

## 📋 **Table of Contents**

- [Overview](#overview)
- [System Settings](#system-settings)
- [User Settings](#user-settings)
- [Provider Settings](#provider-settings)
- [Network Settings](#network-settings)
- [Error Responses](#error-responses)

## Overview

The Settings API provides comprehensive configuration management for the OASIS ecosystem. It handles system settings, user preferences, provider configurations, and network parameters with advanced validation and real-time updates.

## System Settings

### Get System Settings
```http
GET /api/settings/system
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "system": {
        "name": "OASIS Platform",
        "version": "1.0.0",
        "environment": "Production",
        "region": "US-East",
        "timezone": "UTC",
        "language": "en-US",
        "currency": "USD"
      },
      "api": {
        "rateLimit": 1000,
        "timeout": 30,
        "maxRequestSize": "10MB",
        "cors": {
          "enabled": true,
          "origins": ["https://oasisplatform.world", "https://app.oasisplatform.world"]
        },
        "authentication": {
          "jwtExpiry": 3600,
          "refreshTokenExpiry": 86400,
          "requireMFA": false
        }
      },
      "database": {
        "connectionPool": 100,
        "timeout": 30,
        "retryAttempts": 3,
        "backup": {
          "enabled": true,
          "frequency": "daily",
          "retention": 30
        }
      },
      "cache": {
        "enabled": true,
        "ttl": 3600,
        "maxSize": "1GB",
        "evictionPolicy": "LRU"
      },
      "logging": {
        "level": "INFO",
        "retention": 30,
        "maxSize": "100MB",
        "rotation": "daily"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "System settings retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update System Settings
```http
PUT /api/settings/system
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "api": {
    "rateLimit": 2000,
    "timeout": 45,
    "maxRequestSize": "20MB"
  },
  "database": {
    "connectionPool": 150,
    "timeout": 45,
    "retryAttempts": 5
  },
  "cache": {
    "ttl": 7200,
    "maxSize": "2GB"
  },
  "logging": {
    "level": "DEBUG",
    "retention": 60,
    "maxSize": "200MB"
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "system": {
        "name": "OASIS Platform",
        "version": "1.0.0",
        "environment": "Production",
        "region": "US-East",
        "timezone": "UTC",
        "language": "en-US",
        "currency": "USD"
      },
      "api": {
        "rateLimit": 2000,
        "timeout": 45,
        "maxRequestSize": "20MB",
        "cors": {
          "enabled": true,
          "origins": ["https://oasisplatform.world", "https://app.oasisplatform.world"]
        },
        "authentication": {
          "jwtExpiry": 3600,
          "refreshTokenExpiry": 86400,
          "requireMFA": false
        }
      },
      "database": {
        "connectionPool": 150,
        "timeout": 45,
        "retryAttempts": 5,
        "backup": {
          "enabled": true,
          "frequency": "daily",
          "retention": 30
        }
      },
      "cache": {
        "enabled": true,
        "ttl": 7200,
        "maxSize": "2GB",
        "evictionPolicy": "LRU"
      },
      "logging": {
        "level": "DEBUG",
        "retention": 60,
        "maxSize": "200MB",
        "rotation": "daily"
      },
      "updatedAt": "2024-01-20T14:30:00Z",
      "requiresRestart": true
    },
    "message": "System settings updated successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get System Health
```http
GET /api/settings/system/health
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

## User Settings

### Get User Settings
```http
GET /api/settings/user
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "userId": "user_123",
      "preferences": {
        "language": "en-US",
        "timezone": "UTC",
        "currency": "USD",
        "theme": "dark",
        "notifications": {
          "email": true,
          "push": true,
          "sms": false,
          "frequency": "immediate"
        }
      },
      "privacy": {
        "profileVisibility": "public",
        "dataSharing": "limited",
        "analytics": true,
        "marketing": false
      },
      "security": {
        "twoFactor": false,
        "sessionTimeout": 3600,
        "passwordExpiry": 90,
        "loginAlerts": true
      },
      "api": {
        "rateLimit": 1000,
        "timeout": 30,
        "maxRequestSize": "10MB"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "User settings retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update User Settings
```http
PUT /api/settings/user
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "preferences": {
    "language": "es-ES",
    "timezone": "Europe/Madrid",
    "currency": "EUR",
    "theme": "light",
    "notifications": {
      "email": true,
      "push": false,
      "sms": false,
      "frequency": "daily"
    }
  },
  "privacy": {
    "profileVisibility": "private",
    "dataSharing": "none",
    "analytics": false,
    "marketing": false
  },
  "security": {
    "twoFactor": true,
    "sessionTimeout": 1800,
    "passwordExpiry": 60,
    "loginAlerts": true
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "userId": "user_123",
      "preferences": {
        "language": "es-ES",
        "timezone": "Europe/Madrid",
        "currency": "EUR",
        "theme": "light",
        "notifications": {
          "email": true,
          "push": false,
          "sms": false,
          "frequency": "daily"
        }
      },
      "privacy": {
        "profileVisibility": "private",
        "dataSharing": "none",
        "analytics": false,
        "marketing": false
      },
      "security": {
        "twoFactor": true,
        "sessionTimeout": 1800,
        "passwordExpiry": 60,
        "loginAlerts": true
      },
      "api": {
        "rateLimit": 1000,
        "timeout": 30,
        "maxRequestSize": "10MB"
      },
      "updatedAt": "2024-01-20T14:30:00Z"
    },
    "message": "User settings updated successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get User Preferences
```http
GET /api/settings/user/preferences
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "userId": "user_123",
      "preferences": {
        "language": "en-US",
        "timezone": "UTC",
        "currency": "USD",
        "theme": "dark",
        "notifications": {
          "email": true,
          "push": true,
          "sms": false,
          "frequency": "immediate"
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "User preferences retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update User Preferences
```http
PUT /api/settings/user/preferences
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "language": "fr-FR",
  "timezone": "Europe/Paris",
  "currency": "EUR",
  "theme": "auto",
  "notifications": {
    "email": true,
    "push": true,
    "sms": false,
    "frequency": "weekly"
  }
}
```

## Provider Settings

### Get Provider Settings
```http
GET /api/settings/provider
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providers": {
        "storage": {
          "default": "MongoDB",
          "enabled": ["MongoDB", "SQLServer", "PostgreSQL"],
          "config": {
            "MongoDB": {
              "connectionString": "mongodb://localhost:27017",
              "database": "oasis",
              "maxConnections": 100
            },
            "SQLServer": {
              "connectionString": "Server=localhost;Database=oasis;Trusted_Connection=true;",
              "maxConnections": 50
            }
          }
        },
        "blockchain": {
          "default": "Ethereum",
          "enabled": ["Ethereum", "Bitcoin", "Solana"],
          "config": {
            "Ethereum": {
              "network": "mainnet",
              "rpcUrl": "https://mainnet.infura.io/v3/...",
              "gasPrice": "20"
            },
            "Bitcoin": {
              "network": "mainnet",
              "rpcUrl": "https://api.blockcypher.com/v1/btc/main"
            }
          }
        },
        "cloud": {
          "default": "AWS",
          "enabled": ["AWS", "Azure", "GCP"],
          "config": {
            "AWS": {
              "region": "us-east-1",
              "accessKey": "encrypted_key",
              "secretKey": "encrypted_secret"
            }
          }
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Provider settings retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Provider Settings
```http
PUT /api/settings/provider
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "providers": {
    "storage": {
      "default": "PostgreSQL",
      "enabled": ["MongoDB", "SQLServer", "PostgreSQL"],
      "config": {
        "PostgreSQL": {
          "connectionString": "Host=localhost;Database=oasis;Username=oasis;Password=encrypted_password",
          "maxConnections": 75
        }
      }
    },
    "blockchain": {
      "default": "Solana",
      "enabled": ["Ethereum", "Bitcoin", "Solana"],
      "config": {
        "Solana": {
          "network": "mainnet-beta",
          "rpcUrl": "https://api.mainnet-beta.solana.com",
          "commitment": "confirmed"
        }
      }
    }
  }
}
```

### Get Provider Configuration
```http
GET /api/settings/provider/{providerType}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerType` (string): Provider type (storage, blockchain, cloud, network)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providerType": "storage",
      "default": "MongoDB",
      "enabled": ["MongoDB", "SQLServer", "PostgreSQL"],
      "config": {
        "MongoDB": {
          "connectionString": "mongodb://localhost:27017",
          "database": "oasis",
          "maxConnections": 100,
          "timeout": 30,
          "ssl": true
        },
        "SQLServer": {
          "connectionString": "Server=localhost;Database=oasis;Trusted_Connection=true;",
          "maxConnections": 50,
          "timeout": 30,
          "ssl": true
        },
        "PostgreSQL": {
          "connectionString": "Host=localhost;Database=oasis;Username=oasis;Password=encrypted_password",
          "maxConnections": 75,
          "timeout": 30,
          "ssl": true
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Provider configuration retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Network Settings

### Get Network Settings
```http
GET /api/settings/network
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "network": {
        "name": "OASIS Network",
        "version": "1.0.0",
        "type": "P2P",
        "protocol": "ONET",
        "consensus": "Proof of Stake"
      },
      "nodes": {
        "maxNodes": 10000,
        "minNodes": 100,
        "bootstrapNodes": [
          "node1.onet.oasisplatform.world:8080",
          "node2.onet.oasisplatform.world:8080"
        ],
        "discovery": {
          "enabled": true,
          "method": "mDNS",
          "interval": 300
        }
      },
      "routing": {
        "algorithm": "Dijkstra",
        "maxHops": 10,
        "timeout": 30,
        "retryAttempts": 3
      },
      "security": {
        "encryption": true,
        "authentication": true,
        "authorization": true,
        "auditLogging": true
      },
      "performance": {
        "maxConnections": 100,
        "maxBandwidth": "1Gbps",
        "latencyThreshold": 500,
        "throughputThreshold": 1000
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Network settings retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Network Settings
```http
PUT /api/settings/network
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "nodes": {
    "maxNodes": 15000,
    "minNodes": 150,
    "discovery": {
      "enabled": true,
      "method": "DHT",
      "interval": 600
    }
  },
  "routing": {
    "algorithm": "AStar",
    "maxHops": 15,
    "timeout": 45,
    "retryAttempts": 5
  },
  "performance": {
    "maxConnections": 150,
    "maxBandwidth": "2Gbps",
    "latencyThreshold": 300,
    "throughputThreshold": 1500
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "network": {
        "name": "OASIS Network",
        "version": "1.0.0",
        "type": "P2P",
        "protocol": "ONET",
        "consensus": "Proof of Stake"
      },
      "nodes": {
        "maxNodes": 15000,
        "minNodes": 150,
        "bootstrapNodes": [
          "node1.onet.oasisplatform.world:8080",
          "node2.onet.oasisplatform.world:8080"
        ],
        "discovery": {
          "enabled": true,
          "method": "DHT",
          "interval": 600
        }
      },
      "routing": {
        "algorithm": "AStar",
        "maxHops": 15,
        "timeout": 45,
        "retryAttempts": 5
      },
      "security": {
        "encryption": true,
        "authentication": true,
        "authorization": true,
        "auditLogging": true
      },
      "performance": {
        "maxConnections": 150,
        "maxBandwidth": "2Gbps",
        "latencyThreshold": 300,
        "throughputThreshold": 1500
      },
      "updatedAt": "2024-01-20T14:30:00Z",
      "requiresRestart": true
    },
    "message": "Network settings updated successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Network Configuration
```http
GET /api/settings/network/config
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "networkId": "onet_mainnet",
      "config": {
        "name": "OASIS Network",
        "version": "1.0.0",
        "type": "P2P",
        "protocol": "ONET",
        "consensus": "Proof of Stake"
      },
      "nodes": {
        "maxNodes": 10000,
        "minNodes": 100,
        "bootstrapNodes": [
          "node1.onet.oasisplatform.world:8080",
          "node2.onet.oasisplatform.world:8080"
        ],
        "discovery": {
          "enabled": true,
          "method": "mDNS",
          "interval": 300
        }
      },
      "routing": {
        "algorithm": "Dijkstra",
        "maxHops": 10,
        "timeout": 30,
        "retryAttempts": 3
      },
      "security": {
        "encryption": true,
        "authentication": true,
        "authorization": true,
        "auditLogging": true
      },
      "performance": {
        "maxConnections": 100,
        "maxBandwidth": "1Gbps",
        "latencyThreshold": 500,
        "throughputThreshold": 1000
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Network configuration retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Error Responses

### Settings Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Settings not found",
  "exception": "Settings for the specified category not found"
}
```

### Invalid Configuration
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid configuration",
  "exception": "Invalid value for maxConnections: must be between 1 and 1000"
}
```

### Update Failed
```json
{
  "result": null,
  "isError": true,
  "message": "Update failed",
  "exception": "Failed to update settings due to validation error"
}
```

### Permission Denied
```json
{
  "result": null,
  "isError": true,
  "message": "Permission denied",
  "exception": "Insufficient permissions to modify system settings"
}
```

### Service Unavailable
```json
{
  "result": null,
  "isError": true,
  "message": "Service unavailable",
  "exception": "Settings service is temporarily unavailable"
}
```

---

## Navigation

**← Previous:** [Stats API](Stats-API.md) | **Next:** [Map API](Map-API.md) →
