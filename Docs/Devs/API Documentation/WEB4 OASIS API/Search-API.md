# Search API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Universal Search](#universal-search)
- [Search Filters](#search-filters)
- [Search Results](#search-results)
- [Search Statistics](#search-statistics)
- [Error Responses](#error-responses)

## Overview

The Search API provides universal search capabilities across all OASIS data, including avatars, NFTs, wallets, missions, and other entities. It offers advanced filtering, sorting, and ranking capabilities with real-time search results.

## Universal Search

### Search All Data
```http
POST /api/search
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "query": "digital art",
  "filters": {
    "types": ["NFT", "Avatar", "Mission"],
    "networks": ["Ethereum", "Solana"],
    "status": ["Active", "Published"],
    "dateRange": {
      "start": "2024-01-01T00:00:00Z",
      "end": "2024-12-31T23:59:59Z"
    },
    "priceRange": {
      "min": 0.1,
      "max": 10.0,
      "currency": "ETH"
    }
  },
  "sortBy": "relevance",
  "sortOrder": "desc",
  "limit": 20,
  "offset": 0
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "query": "digital art",
      "results": [
        {
          "id": "nft_123",
          "type": "NFT",
          "title": "Digital Art #1",
          "description": "A unique digital artwork",
          "network": "Ethereum",
          "owner": "0x742d35Cc6634C0532925a3b8D4C9db96C4b4d8b6",
          "price": {
            "amount": 2.5,
            "currency": "ETH",
            "fiatValue": {
              "USD": 5000.00
            }
          },
          "imageUrl": "https://example.com/artwork.jpg",
          "createdAt": "2024-01-20T14:30:00Z",
          "relevanceScore": 0.95,
          "matchFields": ["title", "description", "metadata.artist"]
        },
        {
          "id": "mission_123",
          "type": "Mission",
          "title": "Digital Art Creation",
          "description": "Create your first digital artwork",
          "difficulty": "Easy",
          "rewards": {
            "karma": 100,
            "experience": 50
          },
          "status": "Active",
          "createdAt": "2024-01-20T14:30:00Z",
          "relevanceScore": 0.85,
          "matchFields": ["title", "description"]
        }
      ],
      "totalCount": 2,
      "searchTime": 0.05,
      "filters": {
        "types": ["NFT", "Avatar", "Mission"],
        "networks": ["Ethereum", "Solana"]
      },
      "suggestions": [
        "digital artwork",
        "digital painting",
        "digital sculpture"
      ]
    },
    "message": "Search completed successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Search by Type
```http
POST /api/search/{type}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `type` (string): Search type (NFT, Avatar, Mission, Wallet, etc.)

**Request Body:**
```json
{
  "query": "john doe",
  "filters": {
    "status": ["Active"],
    "network": ["Ethereum"]
  },
  "sortBy": "createdAt",
  "sortOrder": "desc",
  "limit": 10,
  "offset": 0
}
```

### Search with Advanced Options
```http
POST /api/search/advanced
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "query": "digital art",
  "filters": {
    "types": ["NFT", "Mission"],
    "networks": ["Ethereum", "Solana"],
    "status": ["Active", "Published"],
    "dateRange": {
      "start": "2024-01-01T00:00:00Z",
      "end": "2024-12-31T23:59:59Z"
    },
    "priceRange": {
      "min": 0.1,
      "max": 10.0,
      "currency": "ETH"
    },
    "location": {
      "latitude": 40.7128,
      "longitude": -74.0060,
      "radius": 1000
    }
  },
  "sortBy": "relevance",
  "sortOrder": "desc",
  "limit": 20,
  "offset": 0,
  "includeMetadata": true,
  "includeStats": true,
  "highlight": true
}
```

## Search Filters

### Get Available Filters
```http
GET /api/search/filters
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "types": [
        {
          "value": "NFT",
          "label": "Non-Fungible Tokens",
          "count": 15000
        },
        {
          "value": "Avatar",
          "label": "User Avatars",
          "count": 5000
        },
        {
          "value": "Mission",
          "label": "Missions & Quests",
          "count": 2500
        },
        {
          "value": "Wallet",
          "label": "Wallets",
          "count": 10000
        }
      ],
      "networks": [
        {
          "value": "Ethereum",
          "label": "Ethereum",
          "count": 8000
        },
        {
          "value": "Solana",
          "label": "Solana",
          "count": 5000
        },
        {
          "value": "Bitcoin",
          "label": "Bitcoin",
          "count": 2000
        }
      ],
      "statuses": [
        {
          "value": "Active",
          "label": "Active",
          "count": 20000
        },
        {
          "value": "Inactive",
          "label": "Inactive",
          "count": 5000
        },
        {
          "value": "Published",
          "label": "Published",
          "count": 15000
        }
      ],
      "categories": [
        {
          "value": "Art",
          "label": "Digital Art",
          "count": 5000
        },
        {
          "value": "Gaming",
          "label": "Gaming",
          "count": 3000
        },
        {
          "value": "Music",
          "label": "Music",
          "count": 2000
        }
      ],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Search filters retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Filter Options
```http
GET /api/search/filters/{type}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `type` (string): Filter type (types, networks, statuses, categories)

### Get Filter Counts
```http
POST /api/search/filters/counts
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "query": "digital art",
  "filters": {
    "types": ["NFT", "Mission"],
    "networks": ["Ethereum", "Solana"]
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "query": "digital art",
      "counts": {
        "types": {
          "NFT": 1500,
          "Mission": 250
        },
        "networks": {
          "Ethereum": 1200,
          "Solana": 550
        },
        "statuses": {
          "Active": 1400,
          "Published": 350
        },
        "categories": {
          "Art": 1000,
          "Gaming": 300,
          "Music": 200
        }
      },
      "totalCount": 1750,
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Filter counts retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Search Results

### Get Search Suggestions
```http
GET /api/search/suggestions
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `query` (string): Search query
- `limit` (int, optional): Number of suggestions (default: 10)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "query": "digital art",
      "suggestions": [
        {
          "text": "digital artwork",
          "type": "query",
          "count": 1500
        },
        {
          "text": "digital painting",
          "type": "query",
          "count": 800
        },
        {
          "text": "digital sculpture",
          "type": "query",
          "count": 600
        },
        {
          "text": "digital art NFT",
          "type": "query",
          "count": 1200
        }
      ],
      "totalCount": 4,
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Search suggestions retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Search History
```http
GET /api/search/history
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `startDate` (string, optional): Start date (ISO 8601)
- `endDate` (string, optional): End date (ISO 8601)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "history": [
        {
          "query": "digital art",
          "filters": {
            "types": ["NFT"],
            "networks": ["Ethereum"]
          },
          "resultsCount": 1500,
          "searchTime": 0.05,
          "searchedAt": "2024-01-20T14:30:00Z"
        },
        {
          "query": "john doe",
          "filters": {
            "types": ["Avatar"]
          },
          "resultsCount": 25,
          "searchTime": 0.02,
          "searchedAt": "2024-01-20T14:25:00Z"
        }
      ],
      "totalCount": 2,
      "limit": 50,
      "offset": 0
    },
    "message": "Search history retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Clear Search History
```http
DELETE /api/search/history
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "clearedAt": "2024-01-20T14:30:00Z",
      "itemsCleared": 25
    },
    "message": "Search history cleared successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Search Statistics

### Get Search Stats
```http
GET /api/search/stats
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
      "timeframe": "day",
      "totalSearches": 5000,
      "uniqueSearches": 3500,
      "averageSearchTime": 0.05,
      "averageResultsPerSearch": 25,
      "topQueries": [
        {
          "query": "digital art",
          "count": 150,
          "averageResults": 30
        },
        {
          "query": "NFT",
          "count": 120,
          "averageResults": 50
        },
        {
          "query": "mission",
          "count": 100,
          "averageResults": 15
        }
      ],
      "searchTypes": {
        "NFT": 2000,
        "Avatar": 1500,
        "Mission": 1000,
        "Wallet": 500
      },
      "networks": {
        "Ethereum": 3000,
        "Solana": 1500,
        "Bitcoin": 500
      },
      "performance": {
        "averageResponseTime": 0.05,
        "peakResponseTime": 0.2,
        "errorRate": 0.001,
        "availability": 99.9
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Search statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Search Performance
```http
GET /api/search/performance
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "performance": {
        "averageResponseTime": 0.05,
        "peakResponseTime": 0.2,
        "throughput": 1000,
        "errorRate": 0.001,
        "availability": 99.9
      },
      "metrics": {
        "searchesPerSecond": 100,
        "averageLatency": 0.05,
        "p95Latency": 0.1,
        "p99Latency": 0.2,
        "errorRate": 0.001,
        "successRate": 0.999
      },
      "trends": {
        "responseTime": "stable",
        "throughput": "increasing",
        "errorRate": "decreasing",
        "availability": "stable"
      },
      "indexes": {
        "totalDocuments": 1000000,
        "indexedDocuments": 1000000,
        "indexSize": "2.5GB",
        "lastIndexed": "2024-01-20T14:30:00Z"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Search performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Search Health
```http
GET /api/search/health
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "status": "Healthy",
      "overallHealth": 0.98,
      "components": {
        "searchEngine": {
          "status": "Healthy",
          "health": 0.99,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "indexes": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "filters": {
          "status": "Healthy",
          "health": 0.97,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Search Engine Test",
          "status": "Pass",
          "responseTime": 0.05,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Index Test",
          "status": "Pass",
          "responseTime": 0.02,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Filter Test",
          "status": "Pass",
          "responseTime": 0.01,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Search health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Error Responses

### Search Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Search not found",
  "exception": "Search with ID search_123 not found"
}
```

### Invalid Search Query
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid search query",
  "exception": "Search query must be at least 2 characters long"
}
```

### Search Timeout
```json
{
  "result": null,
  "isError": true,
  "message": "Search timeout",
  "exception": "Search request timed out after 30 seconds"
}
```

### Index Not Available
```json
{
  "result": null,
  "isError": true,
  "message": "Index not available",
  "exception": "Search index is currently being updated"
}
```

### Filter Error
```json
{
  "result": null,
  "isError": true,
  "message": "Filter error",
  "exception": "Invalid filter value: 'InvalidStatus' is not a valid status"
}
```

---

## Navigation

**← Previous:** [Provider API](Provider-API.md) | **Next:** [Stats API](Stats-API.md) →
