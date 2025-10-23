# Karma API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Karma Management](#karma-management)
- [Karma History](#karma-history)
- [Karma Statistics](#karma-statistics)
- [Karma Transactions](#karma-transactions)
- [Karma Events](#karma-events)
- [Karma Leaderboard](#karma-leaderboard)
- [Error Responses](#error-responses)

## Overview

The Karma API provides comprehensive digital reputation management, tracking positive actions, and maintaining karma scores across all supported providers. It handles karma transactions, history tracking, and reputation analytics.

## Karma Management

### Get Karma Balance
```http
GET /api/karma/balance/{avatarId}
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
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "karmaBalance": 1250,
      "karmaLevel": "Expert",
      "reputation": 4.8,
      "totalEarned": 1500,
      "totalSpent": 250,
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Karma balance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Karma Balance by Username
```http
GET /api/karma/balance/username/{username}
Authorization: Bearer YOUR_TOKEN
```

### Get Karma Balance by Email
```http
GET /api/karma/balance/email/{email}
Authorization: Bearer YOUR_TOKEN
```

### Get All Karma Balances
```http
GET /api/karma/balance/all
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `sortBy` (string, optional): Sort field (karma, reputation, lastUpdated)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "balances": [
        {
          "avatarId": "123e4567-e89b-12d3-a456-426614174000",
          "username": "john_doe",
          "karmaBalance": 1250,
          "karmaLevel": "Expert",
          "reputation": 4.8,
          "lastUpdated": "2024-01-20T14:30:00Z"
        }
      ],
      "totalCount": 1,
      "limit": 50,
      "offset": 0
    },
    "message": "Karma balances retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Karma History

### Get Karma History
```http
GET /api/karma/history/{avatarId}
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `startDate` (string, optional): Start date (ISO 8601)
- `endDate` (string, optional): End date (ISO 8601)
- `transactionType` (string, optional): Transaction type (earned, spent, transferred)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "history": [
        {
          "transactionId": "karma_tx_123",
          "type": "earned",
          "amount": 50,
          "description": "Helped another user",
          "source": "Community Help",
          "timestamp": "2024-01-20T14:30:00Z",
          "balanceAfter": 1250
        },
        {
          "transactionId": "karma_tx_124",
          "type": "spent",
          "amount": 25,
          "description": "Premium feature unlock",
          "source": "Feature Purchase",
          "timestamp": "2024-01-19T10:15:00Z",
          "balanceAfter": 1200
        }
      ],
      "totalCount": 2,
      "limit": 50,
      "offset": 0
    },
    "message": "Karma history retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Karma History by Username
```http
GET /api/karma/history/username/{username}
Authorization: Bearer YOUR_TOKEN
```

### Get Karma History by Email
```http
GET /api/karma/history/email/{email}
Authorization: Bearer YOUR_TOKEN
```

## Karma Statistics

### Get Karma Stats
```http
GET /api/karma/stats/{avatarId}
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "currentBalance": 1250,
      "totalEarned": 1500,
      "totalSpent": 250,
      "averageEarnedPerDay": 5.2,
      "averageSpentPerDay": 0.8,
      "highestBalance": 1500,
      "lowestBalance": 200,
      "totalTransactions": 45,
      "earnedTransactions": 30,
      "spentTransactions": 15,
      "reputation": 4.8,
      "karmaLevel": "Expert",
      "rank": 15,
      "percentile": 95.5,
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Karma statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Karma Stats by Username
```http
GET /api/karma/stats/username/{username}
Authorization: Bearer YOUR_TOKEN
```

### Get Karma Stats by Email
```http
GET /api/karma/stats/email/{email}
Authorization: Bearer YOUR_TOKEN
```

### Get Global Karma Stats
```http
GET /api/karma/stats/global
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "totalUsers": 10000,
      "totalKarma": 5000000,
      "averageKarmaPerUser": 500,
      "topKarmaUser": {
        "avatarId": "top_user_id",
        "username": "karma_king",
        "karmaBalance": 5000
      },
      "karmaDistribution": {
        "0-100": 2000,
        "101-500": 4000,
        "501-1000": 2500,
        "1001-2000": 1000,
        "2000+": 500
      },
      "averageReputation": 3.8,
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Global karma statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Karma Transactions

### Add Karma
```http
POST /api/karma/add
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "avatarId": "123e4567-e89b-12d3-a456-426614174000",
  "amount": 50,
  "description": "Helped another user with technical issue",
  "source": "Community Help",
  "category": "Technical Support"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "transactionId": "karma_tx_125",
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "amount": 50,
      "newBalance": 1300,
      "description": "Helped another user with technical issue",
      "source": "Community Help",
      "category": "Technical Support",
      "timestamp": "2024-01-20T15:00:00Z"
    },
    "message": "Karma added successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Subtract Karma
```http
POST /api/karma/subtract
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "avatarId": "123e4567-e89b-12d3-a456-426614174000",
  "amount": 25,
  "description": "Premium feature unlock",
  "source": "Feature Purchase",
  "category": "Premium Features"
}
```

### Transfer Karma
```http
POST /api/karma/transfer
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "fromAvatarId": "123e4567-e89b-12d3-a456-426614174000",
  "toAvatarId": "987fcdeb-51a2-43d1-b456-426614174000",
  "amount": 100,
  "description": "Gift to friend",
  "source": "Direct Transfer"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "transactionId": "karma_tx_126",
      "fromAvatarId": "123e4567-e89b-12d3-a456-426614174000",
      "toAvatarId": "987fcdeb-51a2-43d1-b456-426614174000",
      "amount": 100,
      "fromNewBalance": 1200,
      "toNewBalance": 600,
      "description": "Gift to friend",
      "source": "Direct Transfer",
      "timestamp": "2024-01-20T15:30:00Z"
    },
    "message": "Karma transferred successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Karma Events

### Get Karma Events
```http
GET /api/karma/events/{avatarId}
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `eventType` (string, optional): Event type filter
- `startDate` (string, optional): Start date (ISO 8601)
- `endDate` (string, optional): End date (ISO 8601)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "events": [
        {
          "eventId": "event_123",
          "type": "karma_milestone",
          "title": "Reached Expert Level",
          "description": "Congratulations! You've reached Expert karma level",
          "karmaAmount": 1000,
          "timestamp": "2024-01-20T14:30:00Z",
          "isRead": false
        },
        {
          "eventId": "event_124",
          "type": "karma_achievement",
          "title": "Helpful Helper",
          "description": "Helped 10 users in a single day",
          "karmaAmount": 50,
          "timestamp": "2024-01-19T18:00:00Z",
          "isRead": true
        }
      ],
      "totalCount": 2,
      "unreadCount": 1,
      "limit": 50,
      "offset": 0
    },
    "message": "Karma events retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Mark Event as Read
```http
POST /api/karma/events/{eventId}/read
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `eventId` (string): Event UUID

### Mark All Events as Read
```http
POST /api/karma/events/mark-all-read
Authorization: Bearer YOUR_TOKEN
```

## Karma Leaderboard

### Get Karma Leaderboard
```http
GET /api/karma/leaderboard
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 100)
- `timeframe` (string, optional): Timeframe (daily, weekly, monthly, alltime)
- `category` (string, optional): Category filter

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "leaderboard": [
        {
          "rank": 1,
          "avatarId": "123e4567-e89b-12d3-a456-426614174000",
          "username": "karma_king",
          "karmaBalance": 5000,
          "karmaLevel": "Legend",
          "reputation": 5.0,
          "avatar": "https://example.com/avatar1.jpg"
        },
        {
          "rank": 2,
          "avatarId": "987fcdeb-51a2-43d1-b456-426614174000",
          "username": "helpful_helper",
          "karmaBalance": 4500,
          "karmaLevel": "Expert",
          "reputation": 4.9,
          "avatar": "https://example.com/avatar2.jpg"
        }
      ],
      "totalCount": 2,
      "timeframe": "alltime",
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Karma leaderboard retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Karma Leaderboard by Category
```http
GET /api/karma/leaderboard/category/{category}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `category` (string): Category (technical, community, creative, etc.)

### Get User Rank
```http
GET /api/karma/rank/{avatarId}
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "rank": 15,
      "percentile": 95.5,
      "karmaBalance": 1250,
      "karmaLevel": "Expert",
      "reputation": 4.8,
      "totalUsers": 10000,
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "User rank retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Error Responses

### Insufficient Karma
```json
{
  "result": null,
  "isError": true,
  "message": "Insufficient karma balance",
  "exception": "Avatar has 100 karma but needs 250 for this action"
}
```

### Invalid Karma Amount
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid karma amount",
  "exception": "Karma amount must be positive"
}
```

### Avatar Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Avatar not found",
  "exception": "Avatar with ID 123 not found"
}
```

### Transfer Error
```json
{
  "result": null,
  "isError": true,
  "message": "Karma transfer failed",
  "exception": "Cannot transfer karma to the same avatar"
}
```

---

## Navigation

**← Previous:** [Keys API](Keys-API.md) | **Next:** [Data API](Data-API.md) →
