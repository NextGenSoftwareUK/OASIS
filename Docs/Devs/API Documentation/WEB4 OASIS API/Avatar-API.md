# Avatar API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Avatar Management](#avatar-management)
- [Avatar Operations](#avatar-operations)
- [Avatar Analytics](#avatar-analytics)
- [Avatar Security](#avatar-security)
- [Error Responses](#error-responses)

## Overview

The Avatar API provides comprehensive avatar management services for the OASIS ecosystem. It handles avatar creation, customization, management, and analytics with support for multiple avatar types, real-time updates, and advanced security features.

## Avatar Management

### Get All Avatars
```http
GET /api/avatar
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by type (Human, Animal, Robot, Fantasy)
- `status` (string, optional): Filter by status (Active, Inactive, Archived, Deleted)
- `category` (string, optional): Filter by category (Gaming, Social, Professional, Creative)
- `sortBy` (string, optional): Sort field (name, createdAt, lastModified, popularity)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatars": [
        {
          "id": "avatar_123",
          "name": "John's Avatar",
          "type": "Human",
          "status": "Active",
          "owner": {
            "id": "user_123",
            "username": "john_doe",
            "avatar": "https://example.com/avatars/john.jpg"
          },
          "appearance": {
            "gender": "Male",
            "age": 25,
            "height": "6'0\"",
            "weight": "180 lbs",
            "skinColor": "Light",
            "hairColor": "Brown",
            "eyeColor": "Blue",
            "bodyType": "Athletic"
          },
          "customization": {
            "clothing": {
              "top": "T-Shirt",
              "bottom": "Jeans",
              "shoes": "Sneakers",
              "accessories": ["Watch", "Ring"]
            },
            "features": {
              "facialHair": "Beard",
              "glasses": "None",
              "tattoos": ["Arm Tattoo"],
              "piercings": ["Ear Piercing"]
            }
          },
          "metadata": {
            "description": "A professional avatar for business meetings",
            "tags": ["professional", "business", "formal"],
            "version": "1.0",
            "category": "Professional"
          },
          "permissions": {
            "view": true,
            "edit": true,
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
    "message": "Avatars retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Avatar by ID
```http
GET /api/avatar/{avatarId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `avatarId` (string): Avatar UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "avatar_123",
      "name": "John's Avatar",
      "type": "Human",
      "status": "Active",
      "owner": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "appearance": {
        "gender": "Male",
        "age": 25,
        "height": "6'0\"",
        "weight": "180 lbs",
        "skinColor": "Light",
        "hairColor": "Brown",
        "eyeColor": "Blue",
        "bodyType": "Athletic"
      },
      "customization": {
        "clothing": {
          "top": "T-Shirt",
          "bottom": "Jeans",
          "shoes": "Sneakers",
          "accessories": ["Watch", "Ring"]
        },
        "features": {
          "facialHair": "Beard",
          "glasses": "None",
          "tattoos": ["Arm Tattoo"],
          "piercings": ["Ear Piercing"]
        }
      },
      "metadata": {
        "description": "A professional avatar for business meetings",
        "tags": ["professional", "business", "formal"],
        "version": "1.0",
        "category": "Professional"
      },
      "permissions": {
        "view": true,
        "edit": true,
        "delete": true,
        "share": false
      },
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC"
      },
      "analytics": {
        "views": 100,
        "likes": 25,
        "shares": 10,
        "lastViewed": "2024-01-20T14:30:00Z"
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "Avatar retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Create Avatar
```http
POST /api/avatar
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "name": "Jane's Avatar",
  "type": "Human",
  "appearance": {
    "gender": "Female",
    "age": 28,
    "height": "5'6\"",
    "weight": "140 lbs",
    "skinColor": "Medium",
    "hairColor": "Blonde",
    "eyeColor": "Green",
    "bodyType": "Average"
  },
  "customization": {
    "clothing": {
      "top": "Blouse",
      "bottom": "Skirt",
      "shoes": "Heels",
      "accessories": ["Necklace", "Earrings"]
    },
    "features": {
      "facialHair": "None",
      "glasses": "None",
      "tattoos": [],
      "piercings": ["Ear Piercing"]
    }
  },
  "metadata": {
    "description": "A creative avatar for artistic projects",
    "tags": ["creative", "artistic", "casual"],
    "version": "1.0",
    "category": "Creative"
  },
  "permissions": {
    "view": true,
    "edit": true,
    "delete": true,
    "share": true
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "avatar_124",
      "name": "Jane's Avatar",
      "type": "Human",
      "status": "Active",
      "owner": {
        "id": "user_456",
        "username": "jane_smith",
        "avatar": "https://example.com/avatars/jane.jpg"
      },
      "appearance": {
        "gender": "Female",
        "age": 28,
        "height": "5'6\"",
        "weight": "140 lbs",
        "skinColor": "Medium",
        "hairColor": "Blonde",
        "eyeColor": "Green",
        "bodyType": "Average"
      },
      "customization": {
        "clothing": {
          "top": "Blouse",
          "bottom": "Skirt",
          "shoes": "Heels",
          "accessories": ["Necklace", "Earrings"]
        },
        "features": {
          "facialHair": "None",
          "glasses": "None",
          "tattoos": [],
          "piercings": ["Ear Piercing"]
        }
      },
      "metadata": {
        "description": "A creative avatar for artistic projects",
        "tags": ["creative", "artistic", "casual"],
        "version": "1.0",
        "category": "Creative"
      },
      "permissions": {
        "view": true,
        "edit": true,
        "delete": true,
        "share": true
      },
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC"
      },
      "analytics": {
        "views": 0,
        "likes": 0,
        "shares": 0,
        "lastViewed": null
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "Avatar created successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Avatar
```http
PUT /api/avatar/{avatarId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `avatarId` (string): Avatar UUID

**Request Body:**
```json
{
  "name": "Updated Jane's Avatar",
  "appearance": {
    "gender": "Female",
    "age": 29,
    "height": "5'6\"",
    "weight": "135 lbs",
    "skinColor": "Medium",
    "hairColor": "Blonde",
    "eyeColor": "Green",
    "bodyType": "Athletic"
  },
  "customization": {
    "clothing": {
      "top": "T-Shirt",
      "bottom": "Jeans",
      "shoes": "Sneakers",
      "accessories": ["Watch", "Bracelet"]
    },
    "features": {
      "facialHair": "None",
      "glasses": "Sunglasses",
      "tattoos": ["Ankle Tattoo"],
      "piercings": ["Ear Piercing", "Nose Piercing"]
    }
  },
  "metadata": {
    "description": "Updated creative avatar for artistic projects",
    "tags": ["creative", "artistic", "casual", "updated"],
    "version": "1.1",
    "category": "Creative"
  }
}
```

### Delete Avatar
```http
DELETE /api/avatar/{avatarId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `avatarId` (string): Avatar UUID

## Avatar Operations

### Customize Avatar
```http
POST /api/avatar/{avatarId}/customize
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `avatarId` (string): Avatar UUID

**Request Body:**
```json
{
  "clothing": {
    "top": "Dress Shirt",
    "bottom": "Dress Pants",
    "shoes": "Dress Shoes",
    "accessories": ["Tie", "Watch"]
  },
  "features": {
    "facialHair": "Mustache",
    "glasses": "Reading Glasses",
    "tattoos": [],
    "piercings": []
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "avatar_123",
      "customization": {
        "clothing": {
          "top": "Dress Shirt",
          "bottom": "Dress Pants",
          "shoes": "Dress Shoes",
          "accessories": ["Tie", "Watch"]
        },
        "features": {
          "facialHair": "Mustache",
          "glasses": "Reading Glasses",
          "tattoos": [],
          "piercings": []
        }
      },
      "customizedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Avatar customized successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Change Avatar Appearance
```http
POST /api/avatar/{avatarId}/appearance
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `avatarId` (string): Avatar UUID

**Request Body:**
```json
{
  "gender": "Male",
  "age": 26,
  "height": "6'1\"",
  "weight": "185 lbs",
  "skinColor": "Tan",
  "hairColor": "Black",
  "eyeColor": "Brown",
  "bodyType": "Muscular"
}
```

### Get Avatar Outfits
```http
GET /api/avatar/{avatarId}/outfits
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `avatarId` (string): Avatar UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "avatar_123",
      "outfits": [
        {
          "id": "outfit_123",
          "name": "Business Casual",
          "clothing": {
            "top": "Polo Shirt",
            "bottom": "Khaki Pants",
            "shoes": "Loafers",
            "accessories": ["Watch"]
          },
          "category": "Professional",
          "createdAt": "2024-01-20T14:30:00Z"
        },
        {
          "id": "outfit_124",
          "name": "Casual Friday",
          "clothing": {
            "top": "T-Shirt",
            "bottom": "Jeans",
            "shoes": "Sneakers",
            "accessories": ["Baseball Cap"]
          },
          "category": "Casual",
          "createdAt": "2024-01-20T14:25:00Z"
        }
      ],
      "totalCount": 2
    },
    "message": "Avatar outfits retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Save Avatar Outfit
```http
POST /api/avatar/{avatarId}/outfits
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `avatarId` (string): Avatar UUID

**Request Body:**
```json
{
  "name": "Formal Evening",
  "clothing": {
    "top": "Tuxedo",
    "bottom": "Tuxedo Pants",
    "shoes": "Dress Shoes",
    "accessories": ["Bow Tie", "Cufflinks"]
  },
  "category": "Formal"
}
```

### Get Avatar History
```http
GET /api/avatar/{avatarId}/history
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `avatarId` (string): Avatar UUID

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by type (Created, Updated, Customized, Outfit)
- `startDate` (string, optional): Start date (ISO 8601)
- `endDate` (string, optional): End date (ISO 8601)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "avatar_123",
      "history": [
        {
          "id": "event_123",
          "type": "Created",
          "description": "Avatar created",
          "changes": {
            "name": "John's Avatar",
            "type": "Human"
          },
          "createdAt": "2024-01-20T14:30:00Z"
        },
        {
          "id": "event_124",
          "type": "Customized",
          "description": "Avatar customized",
          "changes": {
            "clothing": {
              "top": "T-Shirt",
              "bottom": "Jeans"
            }
          },
          "createdAt": "2024-01-20T14:25:00Z"
        }
      ],
      "totalCount": 2,
      "limit": 50,
      "offset": 0
    },
    "message": "Avatar history retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Avatar Analytics

### Get Avatar Statistics
```http
GET /api/avatar/stats
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `type` (string, optional): Filter by avatar type

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "statistics": {
        "avatars": {
          "total": 10000,
          "created": 500,
          "updated": 1000,
          "deleted": 50,
          "active": 9500
        },
        "byType": {
          "Human": 6000,
          "Animal": 2000,
          "Robot": 1500,
          "Fantasy": 500
        },
        "byCategory": {
          "Gaming": 4000,
          "Social": 3000,
          "Professional": 2000,
          "Creative": 1000
        },
        "customization": {
          "totalOutfits": 50000,
          "averageOutfitsPerAvatar": 5,
          "mostPopularOutfit": "Casual",
          "customizationRate": 0.85
        },
        "performance": {
          "averageCreationTime": 5.0,
          "averageCustomizationTime": 2.0,
          "userSatisfaction": 0.88,
          "retentionRate": 0.90
        }
      },
      "trends": {
        "avatars": "increasing",
        "customization": "increasing",
        "performance": "stable"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Avatar statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Avatar Performance
```http
GET /api/avatar/performance
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "performance": {
        "averageCreationTime": 5.0,
        "peakCreationTime": 15.0,
        "averageCustomizationTime": 2.0,
        "peakCustomizationTime": 8.0,
        "userSatisfaction": 0.88,
        "retentionRate": 0.90
      },
      "metrics": {
        "avatarsPerHour": 50,
        "averageLatency": 2.0,
        "p95Latency": 5.0,
        "p99Latency": 10.0,
        "errorRate": 0.02,
        "successRate": 0.98
      },
      "trends": {
        "creationTime": "stable",
        "customizationTime": "stable",
        "satisfaction": "increasing",
        "retention": "stable"
      },
      "breakdown": {
        "Human": {
          "averageCreationTime": 5.0,
          "averageCustomizationTime": 2.0,
          "satisfaction": 0.90
        },
        "Animal": {
          "averageCreationTime": 4.0,
          "averageCustomizationTime": 1.5,
          "satisfaction": 0.85
        },
        "Robot": {
          "averageCreationTime": 6.0,
          "averageCustomizationTime": 3.0,
          "satisfaction": 0.88
        },
        "Fantasy": {
          "averageCreationTime": 8.0,
          "averageCustomizationTime": 4.0,
          "satisfaction": 0.92
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Avatar performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Avatar Health
```http
GET /api/avatar/health
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
        "creation": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "customization": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "storage": {
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
          "name": "Creation Test",
          "status": "Pass",
          "responseTime": 5.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Customization Test",
          "status": "Pass",
          "responseTime": 2.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Storage Test",
          "status": "Pass",
          "responseTime": 1.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Analytics Test",
          "status": "Pass",
          "responseTime": 0.5,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Avatar health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Avatar Security

### Get Avatar Security
```http
GET /api/avatar/{avatarId}/security
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `avatarId` (string): Avatar UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "avatar_123",
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC",
        "privacy": "Private",
        "backup": true
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
    "message": "Avatar security retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Avatar Security
```http
PUT /api/avatar/{avatarId}/security
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `avatarId` (string): Avatar UUID

**Request Body:**
```json
{
  "encryption": "AES-256",
  "authentication": "JWT",
  "authorization": "RBAC",
  "privacy": "Private",
  "backup": true
}
```

## Error Responses

### Avatar Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Avatar not found",
  "exception": "Avatar with ID avatar_123 not found"
}
```

### Invalid Avatar Type
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid avatar type",
  "exception": "Avatar type 'InvalidType' is not supported"
}
```

### Customization Failed
```json
{
  "result": null,
  "isError": true,
  "message": "Customization failed",
  "exception": "Failed to customize avatar appearance"
}
```

### Outfit Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Outfit not found",
  "exception": "Outfit with ID outfit_123 not found"
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

**← Previous:** [ONET API](ONET-API.md) | **Next:** [Missions API](Missions-API.md) →