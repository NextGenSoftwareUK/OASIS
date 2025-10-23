# Subscription API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Subscription Management](#subscription-management)
- [Subscription Operations](#subscription-operations)
- [Subscription Analytics](#subscription-analytics)
- [Subscription Security](#subscription-security)
- [Error Responses](#error-responses)

## Overview

The Subscription API provides comprehensive subscription management services for the OASIS ecosystem. It handles subscription creation, management, billing, and analytics with support for multiple subscription types, real-time updates, and advanced security features.

## Subscription Management

### Get All Subscriptions
```http
GET /api/subscription
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by type (Basic, Premium, Enterprise, Custom)
- `status` (string, optional): Filter by status (Active, Inactive, Suspended, Cancelled)
- `sortBy` (string, optional): Sort field (name, createdAt, price, duration)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "subscriptions": [
        {
          "id": "sub_123",
          "name": "OASIS Premium",
          "description": "Premium subscription with advanced features",
          "type": "Premium",
          "status": "Active",
          "pricing": {
            "amount": 29.99,
            "currency": "USD",
            "interval": "monthly",
            "trialDays": 14
          },
          "features": [
            "Advanced Analytics",
            "Priority Support",
            "Custom Integrations",
            "API Access"
          ],
          "limits": {
            "apiCalls": 10000,
            "storage": "100GB",
            "users": 50,
            "projects": 25
          },
          "creator": {
            "id": "user_123",
            "username": "john_doe",
            "avatar": "https://example.com/avatars/john.jpg"
          },
          "metadata": {
            "tags": ["premium", "advanced", "enterprise"],
            "category": "Premium",
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
    "message": "Subscriptions retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Subscription by ID
```http
GET /api/subscription/{subscriptionId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `subscriptionId` (string): Subscription UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "sub_123",
      "name": "OASIS Premium",
      "description": "Premium subscription with advanced features",
      "type": "Premium",
      "status": "Active",
      "pricing": {
        "amount": 29.99,
        "currency": "USD",
        "interval": "monthly",
        "trialDays": 14
      },
      "features": [
        "Advanced Analytics",
        "Priority Support",
        "Custom Integrations",
        "API Access"
      ],
      "limits": {
        "apiCalls": 10000,
        "storage": "100GB",
        "users": 50,
        "projects": 25
      },
      "creator": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "metadata": {
        "tags": ["premium", "advanced", "enterprise"],
        "category": "Premium",
        "version": "1.0"
      },
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC"
      },
      "analytics": {
        "subscribers": 500,
        "revenue": 14995.0,
        "churnRate": 0.05,
        "averageLifetime": 12.5
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "Subscription retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Create Subscription
```http
POST /api/subscription
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "name": "OASIS Enterprise",
  "description": "Enterprise subscription with unlimited features",
  "type": "Enterprise",
  "pricing": {
    "amount": 99.99,
    "currency": "USD",
    "interval": "monthly",
    "trialDays": 30
  },
  "features": [
    "Unlimited Analytics",
    "24/7 Support",
    "Custom Integrations",
    "Full API Access",
    "White-label Options"
  ],
  "limits": {
    "apiCalls": 100000,
    "storage": "1TB",
    "users": 1000,
    "projects": 100
  },
  "metadata": {
    "tags": ["enterprise", "unlimited", "custom"],
    "category": "Enterprise",
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
      "id": "sub_124",
      "name": "OASIS Enterprise",
      "description": "Enterprise subscription with unlimited features",
      "type": "Enterprise",
      "status": "Active",
      "pricing": {
        "amount": 99.99,
        "currency": "USD",
        "interval": "monthly",
        "trialDays": 30
      },
      "features": [
        "Unlimited Analytics",
        "24/7 Support",
        "Custom Integrations",
        "Full API Access",
        "White-label Options"
      ],
      "limits": {
        "apiCalls": 100000,
        "storage": "1TB",
        "users": 1000,
        "projects": 100
      },
      "creator": {
        "id": "user_456",
        "username": "jane_smith",
        "avatar": "https://example.com/avatars/jane.jpg"
      },
      "metadata": {
        "tags": ["enterprise", "unlimited", "custom"],
        "category": "Enterprise",
        "version": "1.0"
      },
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC"
      },
      "analytics": {
        "subscribers": 0,
        "revenue": 0.0,
        "churnRate": 0.0,
        "averageLifetime": 0.0
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "Subscription created successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Subscription
```http
PUT /api/subscription/{subscriptionId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `subscriptionId` (string): Subscription UUID

**Request Body:**
```json
{
  "name": "Updated OASIS Premium",
  "description": "Updated premium subscription with enhanced features",
  "pricing": {
    "amount": 39.99,
    "currency": "USD",
    "interval": "monthly",
    "trialDays": 21
  },
  "features": [
    "Advanced Analytics",
    "Priority Support",
    "Custom Integrations",
    "API Access",
    "Advanced Reporting"
  ],
  "limits": {
    "apiCalls": 15000,
    "storage": "150GB",
    "users": 75,
    "projects": 35
  }
}
```

### Delete Subscription
```http
DELETE /api/subscription/{subscriptionId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `subscriptionId` (string): Subscription UUID

## Subscription Operations

### Subscribe User
```http
POST /api/subscription/{subscriptionId}/subscribe
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `subscriptionId` (string): Subscription UUID

**Request Body:**
```json
{
  "userId": "user_456",
  "paymentMethod": "card_123",
  "billingAddress": {
    "street": "123 Main St",
    "city": "New York",
    "state": "NY",
    "zipCode": "10001",
    "country": "US"
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "subscriptionId": "sub_123",
      "userId": "user_456",
      "status": "Active",
      "startedAt": "2024-01-20T14:30:00Z",
      "nextBilling": "2024-02-20T14:30:00Z",
      "trialEndsAt": "2024-02-03T14:30:00Z",
      "paymentMethod": "card_123",
      "billingAddress": {
        "street": "123 Main St",
        "city": "New York",
        "state": "NY",
        "zipCode": "10001",
        "country": "US"
      }
    },
    "message": "User subscribed successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Cancel Subscription
```http
POST /api/subscription/{subscriptionId}/cancel
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `subscriptionId` (string): Subscription UUID

**Request Body:**
```json
{
  "userId": "user_456",
  "reason": "No longer needed",
  "effectiveDate": "2024-02-20T14:30:00Z"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "subscriptionId": "sub_123",
      "userId": "user_456",
      "status": "Cancelled",
      "cancelledAt": "2024-01-20T14:30:00Z",
      "effectiveDate": "2024-02-20T14:30:00Z",
      "reason": "No longer needed"
    },
    "message": "Subscription cancelled successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get User Subscriptions
```http
GET /api/subscription/user/{userId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `userId` (string): User UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "userId": "user_456",
      "subscriptions": [
        {
          "id": "sub_123",
          "name": "OASIS Premium",
          "type": "Premium",
          "status": "Active",
          "startedAt": "2024-01-20T14:30:00Z",
          "nextBilling": "2024-02-20T14:30:00Z",
          "trialEndsAt": "2024-02-03T14:30:00Z",
          "pricing": {
            "amount": 29.99,
            "currency": "USD",
            "interval": "monthly"
          }
        }
      ],
      "totalCount": 1,
      "activeCount": 1,
      "cancelledCount": 0
    },
    "message": "User subscriptions retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Subscription Usage
```http
GET /api/subscription/{subscriptionId}/usage
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `subscriptionId` (string): Subscription UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "subscriptionId": "sub_123",
      "usage": {
        "apiCalls": {
          "used": 2500,
          "limit": 10000,
          "percentage": 25.0
        },
        "storage": {
          "used": "25GB",
          "limit": "100GB",
          "percentage": 25.0
        },
        "users": {
          "used": 15,
          "limit": 50,
          "percentage": 30.0
        },
        "projects": {
          "used": 8,
          "limit": 25,
          "percentage": 32.0
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Subscription usage retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Subscription Analytics

### Get Subscription Statistics
```http
GET /api/subscription/stats
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `type` (string, optional): Filter by subscription type

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "statistics": {
        "subscriptions": {
          "total": 1000,
          "active": 800,
          "cancelled": 150,
          "suspended": 50
        },
        "byType": {
          "Basic": 400,
          "Premium": 300,
          "Enterprise": 200,
          "Custom": 100
        },
        "revenue": {
          "total": 50000.0,
          "monthly": 25000.0,
          "yearly": 25000.0,
          "average": 50.0
        },
        "performance": {
          "churnRate": 0.05,
          "retentionRate": 0.95,
          "averageLifetime": 12.5,
          "conversionRate": 0.15
        }
      },
      "trends": {
        "subscriptions": "increasing",
        "revenue": "increasing",
        "performance": "stable"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Subscription statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Subscription Performance
```http
GET /api/subscription/performance
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "performance": {
        "churnRate": 0.05,
        "retentionRate": 0.95,
        "averageLifetime": 12.5,
        "conversionRate": 0.15,
        "revenueGrowth": 0.20,
        "customerSatisfaction": 0.88
      },
      "metrics": {
        "subscriptionsPerHour": 5,
        "averageLatency": 2.0,
        "p95Latency": 5.0,
        "p99Latency": 10.0,
        "errorRate": 0.02,
        "successRate": 0.98
      },
      "trends": {
        "churnRate": "stable",
        "retentionRate": "stable",
        "conversionRate": "increasing",
        "satisfaction": "increasing"
      },
      "breakdown": {
        "Basic": {
          "churnRate": 0.08,
          "retentionRate": 0.92,
          "averageLifetime": 10.0
        },
        "Premium": {
          "churnRate": 0.05,
          "retentionRate": 0.95,
          "averageLifetime": 15.0
        },
        "Enterprise": {
          "churnRate": 0.02,
          "retentionRate": 0.98,
          "averageLifetime": 20.0
        },
        "Custom": {
          "churnRate": 0.03,
          "retentionRate": 0.97,
          "averageLifetime": 18.0
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Subscription performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Subscription Health
```http
GET /api/subscription/health
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
        "billing": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "subscriptions": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "payments": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "notifications": {
          "status": "Healthy",
          "health": 0.97,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Billing Test",
          "status": "Pass",
          "responseTime": 2.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Subscription Test",
          "status": "Pass",
          "responseTime": 1.5,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Payment Test",
          "status": "Pass",
          "responseTime": 1.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Notification Test",
          "status": "Pass",
          "responseTime": 0.8,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Subscription health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Subscription Security

### Get Subscription Security
```http
GET /api/subscription/{subscriptionId}/security
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `subscriptionId` (string): Subscription UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "subscriptionId": "sub_123",
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
    "message": "Subscription security retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Subscription Security
```http
PUT /api/subscription/{subscriptionId}/security
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `subscriptionId` (string): Subscription UUID

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

### Subscription Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Subscription not found",
  "exception": "Subscription with ID sub_123 not found"
}
```

### Subscription Already Active
```json
{
  "result": null,
  "isError": true,
  "message": "Subscription already active",
  "exception": "User already has an active subscription"
}
```

### Payment Failed
```json
{
  "result": null,
  "isError": true,
  "message": "Payment failed",
  "exception": "Payment processing failed"
}
```

### Insufficient Permissions
```json
{
  "result": null,
  "isError": true,
  "message": "Insufficient permissions",
  "exception": "User does not have permission to access this subscription"
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

**← Previous:** [HyperDrive API](HyperDrive-API.md) | **Next:** [Solana API](Solana-API.md) →
