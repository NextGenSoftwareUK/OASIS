# NFT API

## 📋 **Table of Contents**

- [Overview](#overview)
- [NFT Management](#nft-management)
- [NFT Operations](#nft-operations)
- [NFT Analytics](#nft-analytics)
- [NFT Security](#nft-security)
- [Error Responses](#error-responses)

## Overview

The NFT API provides comprehensive NFT management services for the OASIS ecosystem. It handles NFT creation, minting, trading, and analytics with support for multiple standards, real-time updates, and advanced security features.

## NFT Management

### Get All NFTs
```http
GET /api/nft
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by type (ERC721, ERC1155, SPL, Custom)
- `status` (string, optional): Filter by status (Active, Inactive, Listed, Sold)
- `category` (string, optional): Filter by category (Art, Gaming, Music, Sports)
- `sortBy` (string, optional): Sort field (name, createdAt, price, rarity)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "nfts": [
        {
          "id": "nft_123",
          "name": "OASIS Digital Art #1",
          "description": "A unique digital artwork from the OASIS collection",
          "type": "ERC721",
          "status": "Active",
          "owner": {
            "id": "user_123",
            "username": "john_doe",
            "avatar": "https://example.com/avatars/john.jpg"
          },
          "creator": {
            "id": "user_456",
            "username": "jane_smith",
            "avatar": "https://example.com/avatars/jane.jpg"
          },
          "collection": {
            "id": "collection_123",
            "name": "OASIS Digital Art",
            "description": "A collection of digital artworks"
          },
          "metadata": {
            "image": "https://example.com/images/nft_123.jpg",
            "animation": "https://example.com/animations/nft_123.mp4",
            "attributes": [
              {
                "trait_type": "Color",
                "value": "Blue"
              },
              {
                "trait_type": "Rarity",
                "value": "Rare"
              }
            ],
            "external_url": "https://example.com/nft/123",
            "background_color": "000000"
          },
          "price": {
            "amount": 1.0,
            "currency": "ETH",
            "usdValue": 2000.0,
            "lastUpdated": "2024-01-20T14:30:00Z"
          },
          "rarity": {
            "score": 85,
            "rank": 150,
            "total": 1000
          },
          "createdAt": "2024-01-20T14:30:00Z",
          "lastModified": "2024-01-20T14:30:00Z"
        }
      ],
      "totalCount": 1,
      "limit": 50,
      "offset": 0
    },
    "message": "NFTs retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get NFT by ID
```http
GET /api/nft/{nftId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `nftId` (string): NFT UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "nft_123",
      "name": "OASIS Digital Art #1",
      "description": "A unique digital artwork from the OASIS collection",
      "type": "ERC721",
      "status": "Active",
      "owner": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "creator": {
        "id": "user_456",
        "username": "jane_smith",
        "avatar": "https://example.com/avatars/jane.jpg"
      },
      "collection": {
        "id": "collection_123",
        "name": "OASIS Digital Art",
        "description": "A collection of digital artworks"
      },
      "metadata": {
        "image": "https://example.com/images/nft_123.jpg",
        "animation": "https://example.com/animations/nft_123.mp4",
        "attributes": [
          {
            "trait_type": "Color",
            "value": "Blue"
          },
          {
            "trait_type": "Rarity",
            "value": "Rare"
          }
        ],
        "external_url": "https://example.com/nft/123",
        "background_color": "000000"
      },
      "price": {
        "amount": 1.0,
        "currency": "ETH",
        "usdValue": 2000.0,
        "lastUpdated": "2024-01-20T14:30:00Z"
      },
      "rarity": {
        "score": 85,
        "rank": 150,
        "total": 1000
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
    "message": "NFT retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Create NFT
```http
POST /api/nft
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "name": "OASIS Digital Art #2",
  "description": "Another unique digital artwork from the OASIS collection",
  "type": "ERC721",
  "collection": {
    "id": "collection_123",
    "name": "OASIS Digital Art"
  },
  "metadata": {
    "image": "https://example.com/images/nft_124.jpg",
    "animation": "https://example.com/animations/nft_124.mp4",
    "attributes": [
      {
        "trait_type": "Color",
        "value": "Red"
      },
      {
        "trait_type": "Rarity",
        "value": "Epic"
      }
    ],
    "external_url": "https://example.com/nft/124",
    "background_color": "FF0000"
  },
  "price": {
    "amount": 1.5,
    "currency": "ETH"
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "nft_124",
      "name": "OASIS Digital Art #2",
      "description": "Another unique digital artwork from the OASIS collection",
      "type": "ERC721",
      "status": "Active",
      "owner": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "creator": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "collection": {
        "id": "collection_123",
        "name": "OASIS Digital Art",
        "description": "A collection of digital artworks"
      },
      "metadata": {
        "image": "https://example.com/images/nft_124.jpg",
        "animation": "https://example.com/animations/nft_124.mp4",
        "attributes": [
          {
            "trait_type": "Color",
            "value": "Red"
          },
          {
            "trait_type": "Rarity",
            "value": "Epic"
          }
        ],
        "external_url": "https://example.com/nft/124",
        "background_color": "FF0000"
      },
      "price": {
        "amount": 1.5,
        "currency": "ETH",
        "usdValue": 3000.0,
        "lastUpdated": "2024-01-20T14:30:00Z"
      },
      "rarity": {
        "score": 90,
        "rank": 100,
        "total": 1000
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
    "message": "NFT created successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update NFT
```http
PUT /api/nft/{nftId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `nftId` (string): NFT UUID

**Request Body:**
```json
{
  "name": "Updated OASIS Digital Art #2",
  "description": "Updated description for the digital artwork",
  "metadata": {
    "image": "https://example.com/images/nft_124_updated.jpg",
    "animation": "https://example.com/animations/nft_124_updated.mp4",
    "attributes": [
      {
        "trait_type": "Color",
        "value": "Red"
      },
      {
        "trait_type": "Rarity",
        "value": "Epic"
      },
      {
        "trait_type": "Edition",
        "value": "Limited"
      }
    ],
    "external_url": "https://example.com/nft/124",
    "background_color": "FF0000"
  },
  "price": {
    "amount": 2.0,
    "currency": "ETH"
  }
}
```

### Delete NFT
```http
DELETE /api/nft/{nftId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `nftId` (string): NFT UUID

## NFT Operations

### Mint NFT
```http
POST /api/nft/{nftId}/mint
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `nftId` (string): NFT UUID

**Request Body:**
```json
{
  "to": "0x742d35Cc6634C0532925a3b8D4C9db96C4b4d8b6",
  "amount": 1,
  "metadata": {
    "name": "OASIS Digital Art #1",
    "description": "A unique digital artwork from the OASIS collection",
    "image": "https://example.com/images/nft_123.jpg"
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "nftId": "nft_123",
      "transactionId": "tx_123",
      "to": "0x742d35Cc6634C0532925a3b8D4C9db96C4b4d8b6",
      "amount": 1,
      "metadata": {
        "name": "OASIS Digital Art #1",
        "description": "A unique digital artwork from the OASIS collection",
        "image": "https://example.com/images/nft_123.jpg"
      },
      "status": "Pending",
      "hash": "abc123def456ghi789",
      "mintedAt": "2024-01-20T14:30:00Z"
    },
    "message": "NFT minted successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Transfer NFT
```http
POST /api/nft/{nftId}/transfer
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `nftId` (string): NFT UUID

**Request Body:**
```json
{
  "to": "0x742d35Cc6634C0532925a3b8D4C9db96C4b4d8b6",
  "amount": 1,
  "memo": "Transfer to new owner"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "nftId": "nft_123",
      "transactionId": "tx_124",
      "from": "0x1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa",
      "to": "0x742d35Cc6634C0532925a3b8D4C9db96C4b4d8b6",
      "amount": 1,
      "memo": "Transfer to new owner",
      "status": "Pending",
      "hash": "def456ghi789jkl012",
      "transferredAt": "2024-01-20T14:30:00Z"
    },
    "message": "NFT transferred successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### List NFT for Sale
```http
POST /api/nft/{nftId}/list
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `nftId` (string): NFT UUID

**Request Body:**
```json
{
  "price": {
    "amount": 2.0,
    "currency": "ETH"
  },
  "duration": 7,
  "memo": "Listing for sale"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "nftId": "nft_123",
      "listingId": "listing_123",
      "price": {
        "amount": 2.0,
        "currency": "ETH",
        "usdValue": 4000.0
      },
      "duration": 7,
      "expiresAt": "2024-01-27T14:30:00Z",
      "memo": "Listing for sale",
      "status": "Active",
      "listedAt": "2024-01-20T14:30:00Z"
    },
    "message": "NFT listed for sale successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Buy NFT
```http
POST /api/nft/{nftId}/buy
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `nftId` (string): NFT UUID

**Request Body:**
```json
{
  "price": {
    "amount": 2.0,
    "currency": "ETH"
  },
  "paymentMethod": "wallet",
  "memo": "Purchase NFT"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "nftId": "nft_123",
      "transactionId": "tx_125",
      "buyer": {
        "id": "user_789",
        "username": "bob_wilson",
        "avatar": "https://example.com/avatars/bob.jpg"
      },
      "seller": {
        "id": "user_123",
        "username": "john_doe",
        "avatar": "https://example.com/avatars/john.jpg"
      },
      "price": {
        "amount": 2.0,
        "currency": "ETH",
        "usdValue": 4000.0
      },
      "paymentMethod": "wallet",
      "memo": "Purchase NFT",
      "status": "Pending",
      "hash": "ghi789jkl012mno345",
      "purchasedAt": "2024-01-20T14:30:00Z"
    },
    "message": "NFT purchased successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get NFT History
```http
GET /api/nft/{nftId}/history
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `nftId` (string): NFT UUID

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by type (Mint, Transfer, Sale, Purchase)
- `startDate` (string, optional): Start date (ISO 8601)
- `endDate` (string, optional): End date (ISO 8601)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "nftId": "nft_123",
      "history": [
        {
          "id": "event_123",
          "type": "Mint",
          "from": null,
          "to": "0x1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa",
          "amount": 1,
          "price": null,
          "memo": "Initial minting",
          "status": "Confirmed",
          "hash": "abc123def456ghi789",
          "createdAt": "2024-01-20T14:30:00Z"
        },
        {
          "id": "event_124",
          "type": "Transfer",
          "from": "0x1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa",
          "to": "0x742d35Cc6634C0532925a3b8D4C9db96C4b4d8b6",
          "amount": 1,
          "price": null,
          "memo": "Transfer to new owner",
          "status": "Confirmed",
          "hash": "def456ghi789jkl012",
          "createdAt": "2024-01-20T14:25:00Z"
        }
      ],
      "totalCount": 2,
      "limit": 50,
      "offset": 0
    },
    "message": "NFT history retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## NFT Analytics

### Get NFT Statistics
```http
GET /api/nft/stats
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `type` (string, optional): Filter by NFT type

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "statistics": {
        "nfts": {
          "total": 10000,
          "minted": 500,
          "transferred": 1000,
          "sold": 200,
          "listed": 300
        },
        "byType": {
          "ERC721": 6000,
          "ERC1155": 3000,
          "SPL": 1000
        },
        "byCategory": {
          "Art": 4000,
          "Gaming": 3000,
          "Music": 2000,
          "Sports": 1000
        },
        "value": {
          "totalVolume": 1000000.0,
          "currency": "ETH",
          "averagePrice": 100.0,
          "highestPrice": 10000.0,
          "lowestPrice": 0.01
        },
        "performance": {
          "mintSuccessRate": 0.98,
          "transferSuccessRate": 0.99,
          "saleSuccessRate": 0.95,
          "averageSaleTime": 7.0
        }
      },
      "trends": {
        "nfts": "increasing",
        "volume": "increasing",
        "performance": "stable"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "NFT statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get NFT Performance
```http
GET /api/nft/performance
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "performance": {
        "mintSuccessRate": 0.98,
        "transferSuccessRate": 0.99,
        "saleSuccessRate": 0.95,
        "averageSaleTime": 7.0,
        "averageMintTime": 2.0,
        "averageTransferTime": 1.0
      },
      "metrics": {
        "nftsPerHour": 50,
        "averageLatency": 2.0,
        "p95Latency": 5.0,
        "p99Latency": 10.0,
        "errorRate": 0.02,
        "successRate": 0.98
      },
      "trends": {
        "mintTime": "stable",
        "transferTime": "stable",
        "saleTime": "stable",
        "successRate": "increasing"
      },
      "breakdown": {
        "ERC721": {
          "mintSuccessRate": 0.99,
          "transferSuccessRate": 0.99,
          "saleSuccessRate": 0.96
        },
        "ERC1155": {
          "mintSuccessRate": 0.97,
          "transferSuccessRate": 0.98,
          "saleSuccessRate": 0.94
        },
        "SPL": {
          "mintSuccessRate": 0.95,
          "transferSuccessRate": 0.97,
          "saleSuccessRate": 0.92
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "NFT performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get NFT Health
```http
GET /api/nft/health
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
        "minting": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "transfers": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "sales": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "metadata": {
          "status": "Healthy",
          "health": 0.97,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Minting Test",
          "status": "Pass",
          "responseTime": 2.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Transfer Test",
          "status": "Pass",
          "responseTime": 1.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Sale Test",
          "status": "Pass",
          "responseTime": 3.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Metadata Test",
          "status": "Pass",
          "responseTime": 0.5,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "NFT health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## NFT Security

### Get NFT Security
```http
GET /api/nft/{nftId}/security
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `nftId` (string): NFT UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "nftId": "nft_123",
      "security": {
        "encryption": "AES-256",
        "authentication": "JWT",
        "authorization": "RBAC",
        "verification": true,
        "backup": true
      },
      "access": {
        "lastAccessed": "2024-01-20T14:30:00Z",
        "accessCount": 50,
        "failedAttempts": 1,
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
        "auditCount": 3,
        "complianceScore": 0.98
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "NFT security retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update NFT Security
```http
PUT /api/nft/{nftId}/security
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `nftId` (string): NFT UUID

**Request Body:**
```json
{
  "encryption": "AES-256",
  "authentication": "JWT",
  "authorization": "RBAC",
  "verification": true,
  "backup": true
}
```

## Error Responses

### NFT Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "NFT not found",
  "exception": "NFT with ID nft_123 not found"
}
```

### Invalid NFT Type
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid NFT type",
  "exception": "NFT type 'InvalidType' is not supported"
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

### NFT Already Listed
```json
{
  "result": null,
  "isError": true,
  "message": "NFT already listed",
  "exception": "NFT is already listed for sale"
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

**← Previous:** [Wallet API](Wallet-API.md) | **Next:** [ONET API](ONET-API.md) →