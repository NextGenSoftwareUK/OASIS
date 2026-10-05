# Wallet API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Wallet Management](#wallet-management)
- [Wallet Operations](#wallet-operations)
- [Wallet Analytics](#wallet-analytics)
- [Wallet Security](#wallet-security)
- [Error Responses](#error-responses)

## Overview

The Wallet API provides comprehensive wallet management services for the OASIS ecosystem. It handles wallet creation, management, transactions, and analytics with support for multiple cryptocurrencies, real-time updates, and advanced security features.

## Wallet Management

### Get All Wallets
```http
GET /api/wallet
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by type (Hot, Cold, Hardware, Paper)
- `status` (string, optional): Filter by status (Active, Inactive, Suspended, Closed)
- `currency` (string, optional): Filter by currency (BTC, ETH, SOL, USDC)
- `sortBy` (string, optional): Sort field (name, createdAt, balance, lastActivity)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "wallets": [
        {
          "id": "wallet_123",
          "name": "Main Bitcoin Wallet",
          "type": "Hot",
          "status": "Active",
          "owner": {
            "id": "user_123",
            "username": "john_doe",
            "avatar": "https://example.com/avatars/john.jpg"
          },
          "currency": "BTC",
          "address": "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa",
          "balance": {
            "amount": 1.5,
            "currency": "BTC",
            "usdValue": 45000.0,
            "lastUpdated": "2024-01-20T14:30:00Z"
          },
          "metadata": {
            "description": "Primary Bitcoin wallet for transactions",
            "tags": ["bitcoin", "main", "hot"],
            "version": "1.0",
            "network": "mainnet"
          },
          "permissions": {
            "send": true,
            "receive": true,
            "view": true,
            "manage": true
          },
          "createdAt": "2024-01-20T14:30:00Z",
          "lastActivity": "2024-01-20T14:30:00Z"
        }
      ],
      "totalCount": 1,
      "limit": 50,
      "offset": 0
    },
    "message": "Wallets retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Wallet by ID
```http
GET /api/wallet/{walletId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `walletId` (string): Wallet UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "wallet_123",
      "name": "Main Bitcoin Wallet",
      "type": "Hot",
      "status": "Active",
      "owner": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "currency": "BTC",
      "address": "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa",
      "balance": {
        "amount": 1.5,
        "currency": "BTC",
        "usdValue": 45000.0,
        "lastUpdated": "2024-01-20T14:30:00Z"
      },
      "metadata": {
        "description": "Primary Bitcoin wallet for transactions",
        "tags": ["bitcoin", "main", "hot"],
        "version": "1.0",
        "network": "mainnet"
      },
      "permissions": {
        "send": true,
        "receive": true,
        "view": true,
        "manage": true
      },
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC",
        "multiSig": false,
        "hardware": false
      },
      "analytics": {
        "transactions": 25,
        "totalSent": 0.5,
        "totalReceived": 2.0,
        "lastTransaction": "2024-01-20T14:30:00Z"
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastActivity": "2024-01-20T14:30:00Z"
    },
    "message": "Wallet retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Create Wallet
```http
POST /api/wallet
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "name": "Ethereum Wallet",
  "type": "Hot",
  "currency": "ETH",
  "metadata": {
    "description": "Ethereum wallet for DeFi transactions",
    "tags": ["ethereum", "defi", "hot"],
    "version": "1.0",
    "network": "mainnet"
  },
  "permissions": {
    "send": true,
    "receive": true,
    "view": true,
    "manage": true
  },
  "security": {
    "encryption": "AES-256",
    "authentication": "JWT",
    "authorization": "RBAC",
    "multiSig": false,
    "hardware": false
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "wallet_124",
      "name": "Ethereum Wallet",
      "type": "Hot",
      "status": "Active",
      "owner": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "currency": "ETH",
      "address": "0x742d35Cc6634C0532925a3b8D4C9db96C4b4d8b6",
      "balance": {
        "amount": 0.0,
        "currency": "ETH",
        "usdValue": 0.0,
        "lastUpdated": "2024-01-20T14:30:00Z"
      },
      "metadata": {
        "description": "Ethereum wallet for DeFi transactions",
        "tags": ["ethereum", "defi", "hot"],
        "version": "1.0",
        "network": "mainnet"
      },
      "permissions": {
        "send": true,
        "receive": true,
        "view": true,
        "manage": true
      },
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC",
        "multiSig": false,
        "hardware": false
      },
      "analytics": {
        "transactions": 0,
        "totalSent": 0.0,
        "totalReceived": 0.0,
        "lastTransaction": null
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastActivity": "2024-01-20T14:30:00Z"
    },
    "message": "Wallet created successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Wallet
```http
PUT /api/wallet/{walletId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `walletId` (string): Wallet UUID

**Request Body:**
```json
{
  "name": "Updated Ethereum Wallet",
  "metadata": {
    "description": "Updated Ethereum wallet for DeFi transactions",
    "tags": ["ethereum", "defi", "hot", "updated"],
    "version": "1.1",
    "network": "mainnet"
  },
  "permissions": {
    "send": true,
    "receive": true,
    "view": true,
    "manage": false
  }
}
```

### Delete Wallet
```http
DELETE /api/wallet/{walletId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `walletId` (string): Wallet UUID

## Wallet Operations

### Get Wallet Balance
```http
GET /api/wallet/{walletId}/balance
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `walletId` (string): Wallet UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "walletId": "wallet_123",
      "balance": {
        "amount": 1.5,
        "currency": "BTC",
        "usdValue": 45000.0,
        "lastUpdated": "2024-01-20T14:30:00Z"
      },
      "breakdown": {
        "available": 1.5,
        "pending": 0.0,
        "locked": 0.0,
        "reserved": 0.0
      },
      "history": {
        "last24h": {
          "sent": 0.1,
          "received": 0.2,
          "net": 0.1
        },
        "last7d": {
          "sent": 0.5,
          "received": 1.0,
          "net": 0.5
        },
        "last30d": {
          "sent": 2.0,
          "received": 3.5,
          "net": 1.5
        }
      }
    },
    "message": "Wallet balance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Send Transaction
```http
POST /api/wallet/{walletId}/send
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `walletId` (string): Wallet UUID

**Request Body:**
```json
{
  "to": "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2",
  "amount": 0.1,
  "currency": "BTC",
  "fee": 0.0001,
  "memo": "Payment for services",
  "metadata": {
    "category": "Payment",
    "tags": ["services", "payment"],
    "reference": "INV-123456"
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "transactionId": "tx_123",
      "walletId": "wallet_123",
      "from": "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa",
      "to": "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2",
      "amount": 0.1,
      "currency": "BTC",
      "fee": 0.0001,
      "total": 0.1001,
      "memo": "Payment for services",
      "metadata": {
        "category": "Payment",
        "tags": ["services", "payment"],
        "reference": "INV-123456"
      },
      "status": "Pending",
      "hash": "abc123def456ghi789",
      "createdAt": "2024-01-20T14:30:00Z"
    },
    "message": "Transaction sent successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Receive Transaction
```http
POST /api/wallet/{walletId}/receive
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `walletId` (string): Wallet UUID

**Request Body:**
```json
{
  "amount": 0.2,
  "currency": "BTC",
  "memo": "Payment received",
  "metadata": {
    "category": "Payment",
    "tags": ["received", "payment"],
    "reference": "INV-789012"
  }
}
```

### Get Transaction History
```http
GET /api/wallet/{walletId}/transactions
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `walletId` (string): Wallet UUID

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by type (Sent, Received, Internal)
- `status` (string, optional): Filter by status (Pending, Confirmed, Failed)
- `startDate` (string, optional): Start date (ISO 8601)
- `endDate` (string, optional): End date (ISO 8601)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "walletId": "wallet_123",
      "transactions": [
        {
          "id": "tx_123",
          "type": "Sent",
          "from": "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa",
          "to": "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2",
          "amount": 0.1,
          "currency": "BTC",
          "fee": 0.0001,
          "total": 0.1001,
          "memo": "Payment for services",
          "status": "Confirmed",
          "hash": "abc123def456ghi789",
          "blockHeight": 800000,
          "confirmations": 6,
          "createdAt": "2024-01-20T14:30:00Z",
          "confirmedAt": "2024-01-20T14:35:00Z"
        },
        {
          "id": "tx_124",
          "type": "Received",
          "from": "1CvBMSEYstWetqTFn5Au4m4GFg7xJaNVN3",
          "to": "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa",
          "amount": 0.2,
          "currency": "BTC",
          "fee": 0.0,
          "total": 0.2,
          "memo": "Payment received",
          "status": "Confirmed",
          "hash": "def456ghi789jkl012",
          "blockHeight": 800001,
          "confirmations": 5,
          "createdAt": "2024-01-20T14:25:00Z",
          "confirmedAt": "2024-01-20T14:30:00Z"
        }
      ],
      "totalCount": 2,
      "limit": 50,
      "offset": 0
    },
    "message": "Transaction history retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Transaction by ID
```http
GET /api/wallet/{walletId}/transactions/{transactionId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `walletId` (string): Wallet UUID
- `transactionId` (string): Transaction UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "tx_123",
      "walletId": "wallet_123",
      "type": "Sent",
      "from": "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa",
      "to": "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2",
      "amount": 0.1,
      "currency": "BTC",
      "fee": 0.0001,
      "total": 0.1001,
      "memo": "Payment for services",
      "metadata": {
        "category": "Payment",
        "tags": ["services", "payment"],
        "reference": "INV-123456"
      },
      "status": "Confirmed",
      "hash": "abc123def456ghi789",
      "blockHeight": 800000,
      "confirmations": 6,
      "createdAt": "2024-01-20T14:30:00Z",
      "confirmedAt": "2024-01-20T14:35:00Z"
    },
    "message": "Transaction retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Wallet Analytics

### Get Wallet Statistics
```http
GET /api/wallet/{walletId}/stats
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `walletId` (string): Wallet UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "walletId": "wallet_123",
      "statistics": {
        "transactions": {
          "total": 25,
          "sent": 15,
          "received": 10,
          "pending": 0,
          "failed": 0
        },
        "amounts": {
          "totalSent": 2.5,
          "totalReceived": 3.0,
          "netAmount": 0.5,
          "averageSent": 0.17,
          "averageReceived": 0.30
        },
        "fees": {
          "totalFees": 0.001,
          "averageFee": 0.000067,
          "totalFeesUSD": 30.0
        },
        "performance": {
          "successRate": 1.0,
          "averageConfirmationTime": 10.0,
          "fastestConfirmation": 5.0,
          "slowestConfirmation": 30.0
        }
      },
      "trends": {
        "transactions": "increasing",
        "amounts": "stable",
        "fees": "stable",
        "performance": "stable"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Wallet statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Wallet Performance
```http
GET /api/wallet/{walletId}/performance
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `walletId` (string): Wallet UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "walletId": "wallet_123",
      "performance": {
        "successRate": 1.0,
        "averageConfirmationTime": 10.0,
        "fastestConfirmation": 5.0,
        "slowestConfirmation": 30.0,
        "averageFee": 0.000067,
        "totalFees": 0.001
      },
      "metrics": {
        "transactionsPerDay": 2,
        "averageLatency": 10.0,
        "p95Latency": 20.0,
        "p99Latency": 30.0,
        "errorRate": 0.0,
        "successRate": 1.0
      },
      "trends": {
        "confirmationTime": "stable",
        "fees": "stable",
        "successRate": "stable",
        "errorRate": "stable"
      },
      "breakdown": {
        "sent": {
          "count": 15,
          "totalAmount": 2.5,
          "averageAmount": 0.17,
          "totalFees": 0.001
        },
        "received": {
          "count": 10,
          "totalAmount": 3.0,
          "averageAmount": 0.30,
          "totalFees": 0.0
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Wallet performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Wallet Health
```http
GET /api/wallet/{walletId}/health
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `walletId` (string): Wallet UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "walletId": "wallet_123",
      "status": "Healthy",
      "overallHealth": 0.95,
      "components": {
        "balance": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "transactions": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "security": {
          "status": "Healthy",
          "health": 0.97,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "connectivity": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Balance Test",
          "status": "Pass",
          "responseTime": 0.5,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Transaction Test",
          "status": "Pass",
          "responseTime": 1.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Security Test",
          "status": "Pass",
          "responseTime": 0.8,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Connectivity Test",
          "status": "Pass",
          "responseTime": 2.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Wallet health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Wallet Security

### Get Wallet Security
```http
GET /api/wallet/{walletId}/security
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `walletId` (string): Wallet UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "walletId": "wallet_123",
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC",
        "multiSig": false,
        "hardware": false,
        "backup": true
      },
      "access": {
        "lastAccessed": "2024-01-20T14:30:00Z",
        "accessCount": 100,
        "failedAttempts": 2,
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
        "auditCount": 5,
        "complianceScore": 0.98
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Wallet security retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Wallet Security
```http
PUT /api/wallet/{walletId}/security
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `walletId` (string): Wallet UUID

**Request Body:**
```json
{
  "encryption": "AES-256",
  "authentication": "JWT",
  "authorization": "RBAC",
  "multiSig": true,
  "hardware": false,
  "backup": true
}
```

## Error Responses

### Wallet Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Wallet not found",
  "exception": "Wallet with ID wallet_123 not found"
}
```

### Insufficient Balance
```json
{
  "result": null,
  "isError": true,
  "message": "Insufficient balance",
  "exception": "Not enough balance to complete this transaction"
}
```

### Invalid Address
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid address",
  "exception": "Invalid wallet address format"
}
```

### Transaction Failed
```json
{
  "result": null,
  "isError": true,
  "message": "Transaction failed",
  "exception": "Transaction could not be processed"
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

**← Previous:** [Data API](Data-API.md) | **Next:** [NFT API](NFT-API.md) →