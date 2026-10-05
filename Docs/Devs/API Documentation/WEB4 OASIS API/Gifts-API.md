# Gifts API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Gift Management](#gift-management)
- [Gift Operations](#gift-operations)
- [Gift Analytics](#gift-analytics)
- [Gift Security](#gift-security)
- [Error Responses](#error-responses)

## Overview

The Gifts API provides comprehensive gift management services for the OASIS ecosystem. It handles gift creation, sending, receiving, tracking, and analytics with support for multiple gift types, real-time updates, and advanced security features.

## Gift Management

### Get All Gifts
```http
GET /api/gifts
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by type (Sent, Received, Pending, Expired)
- `status` (string, optional): Filter by status (Active, Inactive, Redeemed, Expired)
- `category` (string, optional): Filter by category (Digital, Physical, Experience, Monetary)
- `sortBy` (string, optional): Sort field (name, createdAt, value, expiresAt)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "gifts": [
        {
          "id": "gift_123",
          "name": "OASIS Premium Subscription",
          "description": "1 year premium subscription to OASIS platform",
          "type": "Digital",
          "category": "Subscription",
          "status": "Active",
          "sender": {
            "id": "user_123",
            "username": "john_doe",
            "avatar": "https://example.com/avatars/john.jpg"
          },
          "recipient": {
            "id": "user_456",
            "username": "jane_smith",
            "avatar": "https://example.com/avatars/jane.jpg"
          },
          "value": {
            "amount": 100.0,
            "currency": "USD",
            "cryptoValue": {
              "amount": 0.05,
              "currency": "ETH"
            }
          },
          "delivery": {
            "method": "Digital",
            "status": "Delivered",
            "deliveredAt": "2024-01-20T14:30:00Z"
          },
          "expiresAt": "2024-12-31T23:59:59Z",
          "createdAt": "2024-01-20T14:30:00Z",
          "lastModified": "2024-01-20T14:30:00Z"
        }
      ],
      "totalCount": 1,
      "limit": 50,
      "offset": 0
    },
    "message": "Gifts retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Gift by ID
```http
GET /api/gifts/{giftId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `giftId` (string): Gift UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "gift_123",
      "name": "OASIS Premium Subscription",
      "description": "1 year premium subscription to OASIS platform",
      "type": "Digital",
      "category": "Subscription",
      "status": "Active",
      "sender": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "recipient": {
        "id": "user_456",
        "username": "jane_smith",
        "avatar": "https://example.com/avatars/jane.jpg"
      },
      "value": {
        "amount": 100.0,
        "currency": "USD",
        "cryptoValue": {
          "amount": 0.05,
          "currency": "ETH"
        }
      },
      "delivery": {
        "method": "Digital",
        "status": "Delivered",
        "deliveredAt": "2024-01-20T14:30:00Z",
        "trackingNumber": null
      },
      "metadata": {
        "tags": ["subscription", "premium", "digital"],
        "customMessage": "Happy Birthday! Enjoy your premium subscription.",
        "wrapping": "Digital",
        "card": "Birthday Card"
      },
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC"
      },
      "analytics": {
        "views": 5,
        "downloads": 1,
        "shares": 0,
        "lastAccessed": "2024-01-20T14:30:00Z"
      },
      "expiresAt": "2024-12-31T23:59:59Z",
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "Gift retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Create Gift
```http
POST /api/gifts
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "name": "OASIS NFT Collection",
  "description": "Exclusive NFT collection for OASIS platform users",
  "type": "Digital",
  "category": "NFT",
  "recipient": {
    "id": "user_456",
    "username": "jane_smith"
  },
  "value": {
    "amount": 50.0,
    "currency": "USD",
    "cryptoValue": {
      "amount": 0.025,
      "currency": "ETH"
    }
  },
  "delivery": {
    "method": "Digital",
    "scheduledAt": "2024-01-25T14:30:00Z"
  },
  "metadata": {
    "tags": ["nft", "exclusive", "digital"],
    "customMessage": "Congratulations on your achievement!",
    "wrapping": "Digital",
    "card": "Achievement Card"
  },
  "expiresAt": "2024-12-31T23:59:59Z"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "gift_124",
      "name": "OASIS NFT Collection",
      "description": "Exclusive NFT collection for OASIS platform users",
      "type": "Digital",
      "category": "NFT",
      "status": "Active",
      "sender": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "recipient": {
        "id": "user_456",
        "username": "jane_smith",
        "avatar": "https://example.com/avatars/jane.jpg"
      },
      "value": {
        "amount": 50.0,
        "currency": "USD",
        "cryptoValue": {
          "amount": 0.025,
          "currency": "ETH"
        }
      },
      "delivery": {
        "method": "Digital",
        "status": "Scheduled",
        "scheduledAt": "2024-01-25T14:30:00Z",
        "trackingNumber": null
      },
      "metadata": {
        "tags": ["nft", "exclusive", "digital"],
        "customMessage": "Congratulations on your achievement!",
        "wrapping": "Digital",
        "card": "Achievement Card"
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
      "expiresAt": "2024-12-31T23:59:59Z",
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "Gift created successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Gift
```http
PUT /api/gifts/{giftId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `giftId` (string): Gift UUID

**Request Body:**
```json
{
  "name": "Updated OASIS NFT Collection",
  "description": "Updated description for the NFT collection",
  "metadata": {
    "tags": ["nft", "exclusive", "digital", "updated"],
    "customMessage": "Updated congratulations message!",
    "wrapping": "Digital",
    "card": "Updated Achievement Card"
  }
}
```

### Delete Gift
```http
DELETE /api/gifts/{giftId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `giftId` (string): Gift UUID

## Gift Operations

### Send Gift
```http
POST /api/gifts/{giftId}/send
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `giftId` (string): Gift UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "giftId": "gift_123",
      "status": "Sent",
      "sentAt": "2024-01-20T14:30:00Z",
      "delivery": {
        "method": "Digital",
        "status": "In Transit",
        "estimatedDelivery": "2024-01-20T14:35:00Z"
      },
      "tracking": {
        "trackingNumber": "TRK123456789",
        "trackingUrl": "https://tracking.oasisplatform.world/TRK123456789"
      }
    },
    "message": "Gift sent successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Receive Gift
```http
POST /api/gifts/{giftId}/receive
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `giftId` (string): Gift UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "giftId": "gift_123",
      "status": "Received",
      "receivedAt": "2024-01-20T14:30:00Z",
      "delivery": {
        "method": "Digital",
        "status": "Delivered",
        "deliveredAt": "2024-01-20T14:30:00Z"
      },
      "content": {
        "type": "Digital",
        "value": {
          "amount": 100.0,
          "currency": "USD"
        },
        "downloadUrl": "https://gifts.oasisplatform.world/gift_123/download"
      }
    },
    "message": "Gift received successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Redeem Gift
```http
POST /api/gifts/{giftId}/redeem
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `giftId` (string): Gift UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "giftId": "gift_123",
      "status": "Redeemed",
      "redeemedAt": "2024-01-20T14:30:00Z",
      "redemption": {
        "code": "RED123456789",
        "value": {
          "amount": 100.0,
          "currency": "USD"
        },
        "expiresAt": "2024-12-31T23:59:59Z"
      }
    },
    "message": "Gift redeemed successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Gift Tracking
```http
GET /api/gifts/{giftId}/tracking
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `giftId` (string): Gift UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "giftId": "gift_123",
      "tracking": {
        "trackingNumber": "TRK123456789",
        "status": "Delivered",
        "events": [
          {
            "timestamp": "2024-01-20T14:30:00Z",
            "status": "Created",
            "description": "Gift created and prepared for delivery"
          },
          {
            "timestamp": "2024-01-20T14:31:00Z",
            "status": "Sent",
            "description": "Gift sent to recipient"
          },
          {
            "timestamp": "2024-01-20T14:32:00Z",
            "status": "Delivered",
            "description": "Gift delivered to recipient"
          }
        ],
        "estimatedDelivery": "2024-01-20T14:35:00Z",
        "actualDelivery": "2024-01-20T14:32:00Z"
      }
    },
    "message": "Gift tracking retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Gift Content
```http
GET /api/gifts/{giftId}/content
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `giftId` (string): Gift UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "giftId": "gift_123",
      "content": {
        "type": "Digital",
        "name": "OASIS Premium Subscription",
        "description": "1 year premium subscription to OASIS platform",
        "value": {
          "amount": 100.0,
          "currency": "USD"
        },
        "downloadUrl": "https://gifts.oasisplatform.world/gift_123/download",
        "activationCode": "ACT123456789",
        "instructions": "Use the activation code to activate your premium subscription"
      }
    },
    "message": "Gift content retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Gift Analytics

### Get Gift Statistics
```http
GET /api/gifts/stats
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `type` (string, optional): Filter by gift type

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "statistics": {
        "gifts": {
          "total": 1000,
          "sent": 800,
          "received": 750,
          "redeemed": 700,
          "expired": 50
        },
        "byType": {
          "Digital": 600,
          "Physical": 200,
          "Experience": 150,
          "Monetary": 50
        },
        "byCategory": {
          "Subscription": 300,
          "NFT": 200,
          "Gaming": 150,
          "Education": 100,
          "Other": 250
        },
        "value": {
          "totalValue": 50000.0,
          "currency": "USD",
          "averageValue": 50.0,
          "highestValue": 1000.0,
          "lowestValue": 1.0
        },
        "performance": {
          "deliveryRate": 0.95,
          "redemptionRate": 0.93,
          "satisfactionRate": 0.88,
          "completionRate": 0.90
        }
      },
      "trends": {
        "gifts": "increasing",
        "value": "increasing",
        "performance": "stable"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Gift statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Gift Performance
```http
GET /api/gifts/performance
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "performance": {
        "deliveryRate": 0.95,
        "redemptionRate": 0.93,
        "satisfactionRate": 0.88,
        "completionRate": 0.90,
        "averageDeliveryTime": 5.0,
        "averageRedemptionTime": 2.0
      },
      "metrics": {
        "giftsPerHour": 50,
        "averageLatency": 2.0,
        "p95Latency": 5.0,
        "p99Latency": 10.0,
        "errorRate": 0.02,
        "successRate": 0.98
      },
      "trends": {
        "deliveryRate": "stable",
        "redemptionRate": "increasing",
        "satisfactionRate": "stable",
        "completionRate": "stable"
      },
      "breakdown": {
        "Digital": {
          "deliveryRate": 0.98,
          "redemptionRate": 0.95,
          "satisfactionRate": 0.90
        },
        "Physical": {
          "deliveryRate": 0.90,
          "redemptionRate": 0.88,
          "satisfactionRate": 0.85
        },
        "Experience": {
          "deliveryRate": 0.92,
          "redemptionRate": 0.90,
          "satisfactionRate": 0.88
        },
        "Monetary": {
          "deliveryRate": 0.99,
          "redemptionRate": 0.98,
          "satisfactionRate": 0.95
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Gift performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Gift Health
```http
GET /api/gifts/health
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
        "delivery": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "redemption": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "tracking": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "analytics": {
          "status": "Healthy",
          "health": 0.97,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Delivery Test",
          "status": "Pass",
          "responseTime": 1.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Redemption Test",
          "status": "Pass",
          "responseTime": 0.5,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Tracking Test",
          "status": "Pass",
          "responseTime": 0.8,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Analytics Test",
          "status": "Pass",
          "responseTime": 1.2,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Gift health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Gift Security

### Get Gift Security
```http
GET /api/gifts/{giftId}/security
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `giftId` (string): Gift UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "giftId": "gift_123",
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC",
        "auditLogging": true,
        "antiFraud": true
      },
      "access": {
        "lastAccessed": "2024-01-20T14:30:00Z",
        "accessCount": 10,
        "failedAttempts": 0,
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
        "auditCount": 5,
        "complianceScore": 0.98
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Gift security retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Gift Security
```http
PUT /api/gifts/{giftId}/security
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `giftId` (string): Gift UUID

**Request Body:**
```json
{
  "encryption": "AES-256",
  "authentication": "JWT",
  "authorization": "RBAC",
  "auditLogging": true,
  "antiFraud": true
}
```

## Error Responses

### Gift Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Gift not found",
  "exception": "Gift with ID gift_123 not found"
}
```

### Gift Expired
```json
{
  "result": null,
  "isError": true,
  "message": "Gift expired",
  "exception": "Gift has expired and cannot be redeemed"
}
```

### Gift Already Redeemed
```json
{
  "result": null,
  "isError": true,
  "message": "Gift already redeemed",
  "exception": "Gift has already been redeemed"
}
```

### Invalid Recipient
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid recipient",
  "exception": "Recipient does not exist or is not accessible"
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

**← Previous:** [Competition API](Competition-API.md) | **Next:** [Karma API](Karma-API.md) →
