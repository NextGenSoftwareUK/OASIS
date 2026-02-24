# WEB4 OASIS API - Complete Documentation

## 📋 **Table of Contents**

### **🔐 Authentication & Identity**
- [Avatar API](#avatar-api) - Complete user management system (80+ endpoints)
- [Keys API](#keys-api) - Cryptographic key management (40+ endpoints)

### **💎 Reputation & Social**
- [Karma API](#karma-api) - Digital reputation system (20+ endpoints)
- [Social API](#social-api) - Social networking features (4+ endpoints)
- [Chat API](#chat-api) - Real-time communication (3+ endpoints)
- [Messaging API](#messaging-api) - Advanced messaging (6+ endpoints)

### **💾 Data Management**
- [Data API](#data-api) - Comprehensive data operations (30+ endpoints)
- [Files API](#files-api) - File management system (6+ endpoints)
- [Search API](#search-api) - Universal search (2+ endpoints)

### **💰 Financial Services**
- [Wallet API](#wallet-api) - Multi-chain wallet management (25+ endpoints)
- [NFT API](#nft-api) - Cross-chain NFT operations (20+ endpoints)
- [Subscription API](#subscription-api) - Billing and quotas (8+ endpoints)

### **🌐 Network & Discovery**
- [ONET API](#onet-api) - Decentralized networking (10+ endpoints)
- [ONODE API](#onode-api) - Node management (12+ endpoints)
- [Provider API](#provider-api) - Provider management (25+ endpoints)

### **🎮 Gamification**
- [Competition API](#competition-api) - Gaming system (9+ endpoints)
- [Gifts API](#gifts-api) - Gift system (6+ endpoints)
- [Eggs API](#eggs-api) - Discovery system (3+ endpoints)

### **🗺️ Location & Mapping**
- [Map API](#map-api) - Advanced mapping (15+ endpoints)

### **🔗 Blockchain Integration**
- [EOSIO API](#eosio-api) - EOSIO integration (9+ endpoints)
- [Holochain API](#holochain-api) - Holochain integration (7+ endpoints)
- [Telos API](#telos-api) - Telos integration (9+ endpoints)
- [Seeds API](#seeds-api) - Seeds integration (15+ endpoints)
- [Solana API](#solana-api) - Solana integration (2+ endpoints)

### **⚙️ System Management**
- [Settings API](#settings-api) - Configuration (12+ endpoints)
- [Stats API](#stats-api) - Analytics (8+ endpoints)
- [Video API](#video-api) - Video calling (3+ endpoints)
- [Share API](#share-api) - Content sharing (2+ endpoints)

## 📋 **Overview**

The WEB4 OASIS API is the core data aggregation layer that unifies all Web2 and Web3 technologies into a single, intelligent, auto-failover system. It provides universal access to blockchain networks, cloud providers, storage systems, and network protocols through a unified interface.

## 🏗️ **Architecture**

### **Core Components**
- **AVATAR API**: Centralized user data and identity management
- **KARMA API**: Digital reputation and positive action tracking
- **DATA API**: Seamless Web2/Web3 data transfer
- **WALLET API**: High-security, cross-chain wallet with fiat integration
- **NFT API**: Cross-chain NFTs with geo-caching
- **KEYS API**: Secure key storage and backup
- **ONET API**: Integration with the ONET decentralized network
- **SEARCH API**: Universal search across all OASIS data
- **STATS API**: Comprehensive statistics and analytics
- **HOLOCHAIN API**: Integration for distributed applications
- **IPFS API**: Decentralized storage
- **ONODE API**: ONODE status, config, providers, stats
- **ONET API**: ONET status, topology, nodes, connect, disconnect
- **SETTINGS API**: Get/Update system settings
- **MAP API**: Load/Save/Search/Update/Delete maps
- **CHAT API**: Conversations, send message, messages, create conversation
- **MESSAGING API**: Inbox, send message, sent, mark read
- **SOCIAL API**: Friends, add/remove friend, followers, follow/unfollow, feed
- **FILES API**: Upload/Download/List/Delete/Share files

### **Key Features**
- **Universal Compatibility**: Works with 50+ blockchain networks
- **Auto-Failover**: Intelligent provider switching
- **Cross-Chain Support**: Native multi-blockchain operations
- **Real-Time Analytics**: Comprehensive monitoring and statistics
- **Secure Authentication**: Multi-layer security with key management
- **Provider Management**: Dynamic provider configuration and monitoring

## 🔗 **Base URL**
```
https://api.oasisweb4.com
```

## 🔐 **Authentication**

### **API Key Authentication**
```http
Authorization: Bearer YOUR_API_KEY
```

### **Avatar Authentication**
```http
Authorization: Avatar YOUR_AVATAR_ID
```

## 📚 **API Endpoints**

## Avatar API

The Avatar API provides comprehensive user management, authentication, and identity services. It handles user registration, authentication, profile management, and session control across all supported providers.

### **User Registration**

#### **Register New User**
```http
POST /api/avatar/register
Content-Type: application/json
```

**Request Body:**
```json
{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "secure_password",
  "firstName": "John",
  "lastName": "Doe",
  "avatar": "https://example.com/avatar.jpg"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "username": "john_doe",
      "email": "john@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "avatar": "https://example.com/avatar.jpg",
      "createdAt": "2024-01-15T10:30:00Z",
      "isActive": true
    },
    "message": "User registered successfully"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Register with Provider**
```http
POST /api/avatar/register/{providerType}/{setGlobally}
Content-Type: application/json
```

**Parameters:**
- `providerType` (string): Provider type (e.g., "Ethereum", "Solana", "MongoDB")
- `setGlobally` (boolean): Set as global default provider

**Request Body:**
```json
{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "secure_password",
  "providerType": "Ethereum"
}
```

### **Authentication**

#### **Authenticate User**
```http
POST /api/avatar/authenticate
Content-Type: application/json
```

**Request Body:**
```json
{
  "username": "john_doe",
  "password": "secure_password"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      "refreshToken": "refresh_token_here",
      "expiresIn": 3600,
      "user": {
        "id": "123e4567-e89b-12d3-a456-426614174000",
        "username": "john_doe",
        "email": "john@example.com",
        "avatar": "https://example.com/avatar.jpg"
      }
    },
    "message": "Authentication successful"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Authenticate with Advanced Options**
```http
POST /api/avatar/authenticate/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{AutoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}
```

**Parameters:**
- `providerType` (string): Provider type
- `setGlobally` (boolean): Set as global default
- `autoReplicationMode` (boolean): Enable auto-replication
- `autoFailOverMode` (boolean): Enable auto-failover
- `autoLoadBalanceMode` (boolean): Enable load balancing
- `autoReplicationProviders` (string): Comma-separated replication providers
- `autoFailOverProviders` (string): Comma-separated failover providers
- `AutoLoadBalanceProviders` (string): Comma-separated load balance providers
- `waitForAutoReplicationResult` (boolean): Wait for replication result
- `showDetailedSettings` (boolean): Show detailed settings

#### **Token Authentication**
```http
POST /api/avatar/authenticate-token/{JWTToken}
```

**Parameters:**
- `JWTToken` (string): JWT token for authentication

#### **Refresh Token**
```http
POST /api/avatar/refresh-token
Content-Type: application/json
```

**Request Body:**
```json
{
  "refreshToken": "your_refresh_token_here"
}
```

#### **Revoke Token**
```http
POST /api/avatar/revoke-token
Content-Type: application/json
```

**Request Body:**
```json
{
  "token": "token_to_revoke"
}
```

### **Password Management**

#### **Forgot Password**
```http
POST /api/avatar/forgot-password
Content-Type: application/json
```

**Request Body:**
```json
{
  "email": "john@example.com"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "resetToken": "reset_token_here",
      "expiresIn": 3600,
      "message": "Password reset email sent"
    },
    "message": "Password reset initiated"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Validate Reset Token**
```http
POST /api/avatar/validate-reset-token
Content-Type: application/json
```

**Request Body:**
```json
{
  "token": "reset_token_here"
}
```

#### **Reset Password**
```http
POST /api/avatar/reset-password
Content-Type: application/json
```

**Request Body:**
```json
{
  "token": "reset_token_here",
  "newPassword": "new_secure_password"
}
```

### **Avatar Management**

#### **Get Avatar by ID**
```http
GET /api/avatar/get-by-id/{id}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `id` (string): Avatar UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "username": "john_doe",
      "email": "john@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "avatar": "https://example.com/avatar.jpg",
      "createdAt": "2024-01-15T10:30:00Z",
      "lastLogin": "2024-01-20T14:30:00Z",
      "isActive": true,
      "karma": 150,
      "reputation": 4.5
    },
    "message": "Avatar retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Get Avatar by Username**
```http
GET /api/avatar/get-by-username/{username}
Authorization: Bearer YOUR_TOKEN
```

#### **Get Avatar by Email**
```http
GET /api/avatar/get-by-email/{email}
Authorization: Bearer YOUR_TOKEN
```

#### **Get All Avatars**
```http
GET /api/avatar/get-all-avatars
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `sortBy` (string, optional): Sort field (default: "createdAt")
- `sortOrder` (string, optional): Sort order (asc/desc, default: "desc")

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatars": [
        {
          "id": "123e4567-e89b-12d3-a456-426614174000",
          "username": "john_doe",
          "email": "john@example.com",
          "firstName": "John",
          "lastName": "Doe",
          "avatar": "https://example.com/avatar.jpg",
          "createdAt": "2024-01-15T10:30:00Z",
          "karma": 150,
          "reputation": 4.5
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

#### **Search Avatars**
```http
POST /api/avatar/search
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "query": "john",
  "filters": {
    "karmaMin": 100,
    "reputationMin": 4.0,
    "isActive": true
  },
  "limit": 20,
  "offset": 0
}
```

### **Avatar Portraits**

#### **Get Avatar Portrait**
```http
GET /api/avatar/get-avatar-portrait/{id}
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "portraitUrl": "https://example.com/portrait.jpg",
      "thumbnailUrl": "https://example.com/thumbnail.jpg",
      "uploadedAt": "2024-01-15T10:30:00Z"
    },
    "message": "Portrait retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Upload Avatar Portrait**
```http
POST /api/avatar/upload-avatar-portrait
Content-Type: multipart/form-data
Authorization: Bearer YOUR_TOKEN
```

**Form Data:**
- `file`: Image file (JPEG, PNG, GIF)
- `avatarId`: Avatar ID (optional, uses current user if not provided)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "portraitUrl": "https://example.com/portrait.jpg",
      "thumbnailUrl": "https://example.com/thumbnail.jpg",
      "uploadedAt": "2024-01-15T10:30:00Z"
    },
    "message": "Portrait uploaded successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### **Avatar Updates**

#### **Update Avatar by ID**
```http
POST /api/avatar/update-by-id/{id}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "firstName": "John",
  "lastName": "Smith",
  "email": "john.smith@example.com",
  "avatar": "https://example.com/new-avatar.jpg"
}
```

#### **Update Avatar by Email**
```http
POST /api/avatar/update-by-email/{email}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

#### **Update Avatar by Username**
```http
POST /api/avatar/update-by-username/{username}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

### **Avatar Deletion**

#### **Delete Avatar by ID**
```http
DELETE /api/avatar/{id:Guid}
Authorization: Bearer YOUR_TOKEN
```

#### **Delete Avatar by Username**
```http
DELETE /api/avatar/delete-by-username/{username}
Authorization: Bearer YOUR_TOKEN
```

#### **Delete Avatar by Email**
```http
DELETE /api/avatar/delete-by-email/{email}
Authorization: Bearer YOUR_TOKEN
```

### **Avatar Sessions**

#### **Get Avatar Sessions**
```http
GET /api/avatar/{avatarId}/sessions
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "sessions": [
        {
          "id": "session_123",
          "device": "Chrome Browser",
          "ipAddress": "192.168.1.100",
          "location": "New York, US",
          "createdAt": "2024-01-15T10:30:00Z",
          "lastActivity": "2024-01-20T14:30:00Z",
          "isActive": true
        }
      ],
      "totalCount": 1
    },
    "message": "Sessions retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Logout Session**
```http
POST /api/avatar/{avatarId}/sessions/logout
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "sessionId": "session_123"
}
```

#### **Logout All Sessions**
```http
POST /api/avatar/{avatarId}/sessions/logout-all
Authorization: Bearer YOUR_TOKEN
```

### **Error Responses**

#### **Authentication Error**
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid credentials",
  "exception": "Authentication failed"
}
```

#### **Validation Error**
```json
{
  "result": null,
  "isError": true,
  "message": "Validation failed",
  "exception": "Email format is invalid"
}
```

#### **Not Found Error**
```json
{
  "result": null,
  "isError": true,
  "message": "Avatar not found",
  "exception": "Avatar with ID 123 not found"
}
```

## Karma API

The Karma API provides a comprehensive digital reputation system that tracks positive and negative actions, manages karma weighting, and maintains historical records. It supports voting mechanisms, statistics, and cross-provider karma management.

### **Karma Retrieval**

#### **Get Karma for Avatar**
```http
GET /api/karma/get-karma-for-avatar/{avatarId}
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
      "totalKarma": 1250,
      "positiveKarma": 1100,
      "negativeKarma": -150,
      "karmaLevel": "Expert",
      "reputationScore": 4.2,
      "lastUpdated": "2024-01-20T14:30:00Z",
      "breakdown": {
        "helpful": 300,
        "creative": 250,
        "collaborative": 200,
        "leadership": 150,
        "mentoring": 100,
        "other": 100
      }
    },
    "message": "Karma retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Get Karma with Provider**
```http
GET /api/karma/get-karma-for-avatar/{avatarId}/{providerType}/{setGlobally}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `avatarId` (string): Avatar UUID
- `providerType` (string): Provider type (e.g., "Ethereum", "MongoDB")
- `setGlobally` (boolean): Set as global default provider

#### **Get Karma Akashic Records**
```http
GET /api/karma/get-karma-akashic-records-for-avatar/{avatarId}
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "records": [
        {
          "id": "record_123",
          "action": "Helped new user with setup",
          "karmaType": "helpful",
          "karmaAmount": 50,
          "timestamp": "2024-01-20T14:30:00Z",
          "source": "Community Forum",
          "verified": true,
          "description": "Provided detailed guidance to new user"
        },
        {
          "id": "record_124",
          "action": "Created useful tutorial",
          "karmaType": "creative",
          "karmaAmount": 100,
          "timestamp": "2024-01-19T10:15:00Z",
          "source": "Documentation",
          "verified": true,
          "description": "Comprehensive tutorial with examples"
        }
      ],
      "totalRecords": 2,
      "totalKarma": 150
    },
    "message": "Akashic records retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### **Karma Weighting**

#### **Get Positive Karma Weighting**
```http
GET /api/karma/get-positive-karma-weighting/{karmaType}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `karmaType` (string): Type of karma (e.g., "helpful", "creative", "collaborative")

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "karmaType": "helpful",
      "currentWeighting": 1.5,
      "totalVotes": 1250,
      "averageWeighting": 1.4,
      "lastUpdated": "2024-01-20T14:30:00Z",
      "trend": "increasing",
      "description": "Weighting for helpful actions"
    },
    "message": "Positive karma weighting retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Get Negative Karma Weighting**
```http
GET /api/karma/get-negative-karma-weighting/{karmaType}
Authorization: Bearer YOUR_TOKEN
```

### **Karma Voting**

#### **Vote for Positive Karma Weighting**
```http
POST /api/karma/vote-for-positive-karma-weighting/{karmaType}/{weighting}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `karmaType` (string): Type of karma
- `weighting` (number): Weighting value (0.1 to 5.0)

**Request Body:**
```json
{
  "reason": "This weighting seems appropriate for helpful actions",
  "experience": "I've seen many helpful actions that deserve this weighting"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "voteId": "vote_123",
      "karmaType": "helpful",
      "weighting": 1.5,
      "voterId": "456e7890-e89b-12d3-a456-426614174001",
      "timestamp": "2024-01-20T14:30:00Z",
      "newAverageWeighting": 1.45,
      "totalVotes": 1251
    },
    "message": "Vote recorded successfully"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Vote for Negative Karma Weighting**
```http
POST /api/karma/vote-for-negative-karma-weighting/{karmaType}/{weighting}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

### **Karma Management**

#### **Set Positive Karma Weighting**
```http
POST /api/karma/set-positive-karma-weighting/{karmaType}/{weighting}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "reason": "Updated based on community feedback",
  "adminOverride": true,
  "effectiveDate": "2024-01-21T00:00:00Z"
}
```

#### **Set Negative Karma Weighting**
```http
POST /api/karma/set-negative-karma-weighting/{karmaType}/{weighting}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

### **Karma Operations**

#### **Add Karma to Avatar**
```http
POST /api/karma/add-karma-to-avatar/{avatarId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "karmaType": "helpful",
  "amount": 50,
  "reason": "Helped new user with technical issue",
  "source": "Community Forum",
  "verified": true,
  "description": "Provided detailed step-by-step solution"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "karmaId": "karma_123",
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "karmaType": "helpful",
      "amount": 50,
      "newTotalKarma": 1300,
      "newKarmaLevel": "Expert",
      "timestamp": "2024-01-20T14:30:00Z"
    },
    "message": "Karma added successfully"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Remove Karma from Avatar**
```http
POST /api/karma/remove-karma-from-avatar/{avatarId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "karmaType": "helpful",
  "amount": 25,
  "reason": "Incorrect karma awarded",
  "adminOverride": true,
  "description": "Removing incorrect karma due to system error"
}
```

### **Karma Statistics**

#### **Get Karma Stats**
```http
GET /api/karma/get-karma-stats/{avatarId}
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "totalKarma": 1250,
      "karmaLevel": "Expert",
      "reputationScore": 4.2,
      "rank": 15,
      "percentile": 85.5,
      "breakdown": {
        "helpful": {
          "total": 300,
          "average": 15.0,
          "count": 20
        },
        "creative": {
          "total": 250,
          "average": 25.0,
          "count": 10
        },
        "collaborative": {
          "total": 200,
          "average": 20.0,
          "count": 10
        }
      },
      "trends": {
        "last30Days": 150,
        "last7Days": 50,
        "today": 10
      },
      "achievements": [
        "First Karma",
        "Helpful Helper",
        "Creative Contributor",
        "Expert Level"
      ]
    },
    "message": "Karma statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Get Karma History**
```http
GET /api/karma/get-karma-history/{avatarId}?limit=50&offset=0
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of records (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `karmaType` (string, optional): Filter by karma type
- `startDate` (string, optional): Start date (ISO format)
- `endDate` (string, optional): End date (ISO format)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "history": [
        {
          "id": "karma_123",
          "karmaType": "helpful",
          "amount": 50,
          "reason": "Helped new user",
          "source": "Community Forum",
          "timestamp": "2024-01-20T14:30:00Z",
          "verified": true
        },
        {
          "id": "karma_124",
          "karmaType": "creative",
          "amount": 100,
          "reason": "Created tutorial",
          "source": "Documentation",
          "timestamp": "2024-01-19T10:15:00Z",
          "verified": true
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

### **Error Responses**

#### **Invalid Karma Type**
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid karma type",
  "exception": "Karma type 'invalid_type' is not supported"
}
```

#### **Insufficient Permissions**
```json
{
  "result": null,
  "isError": true,
  "message": "Insufficient permissions",
  "exception": "You do not have permission to modify karma weightings"
}
```

#### **Avatar Not Found**
```json
{
  "result": null,
  "isError": true,
  "message": "Avatar not found",
  "exception": "Avatar with ID 123 not found"
}
```

## Data API

The Data API provides comprehensive data management capabilities including holon operations, file management, and data storage with advanced provider support, auto-replication, and failover mechanisms.

### **Holon Operations**

#### **Load Holon**
```http
POST /api/data/load-holon
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "loadChildren": true,
  "recursive": true,
  "maxChildDepth": 5,
  "continueOnError": false,
  "version": "latest"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "name": "My Project",
      "type": "Project",
      "description": "A comprehensive project",
      "createdAt": "2024-01-15T10:30:00Z",
      "updatedAt": "2024-01-20T14:30:00Z",
      "version": "1.2.0",
      "children": [
        {
          "id": "child_123",
          "name": "Sub Project",
          "type": "SubProject",
          "level": 1
        }
      ],
      "metadata": {
        "tags": ["project", "development"],
        "category": "software",
        "priority": "high"
      },
      "provider": "MongoDB",
      "replicationStatus": "completed"
    },
    "message": "Holon loaded successfully"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Load Holon with Advanced Options**
```http
GET /api/data/load-holon/{id}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `id` (string): Holon UUID
- `loadChildren` (boolean): Load child holons
- `recursive` (boolean): Load recursively
- `maxChildDepth` (int): Maximum child depth
- `continueOnError` (boolean): Continue on errors
- `version` (string): Version to load
- `providerType` (string): Provider type
- `setGlobally` (boolean): Set as global default
- `autoReplicationMode` (boolean): Enable auto-replication
- `autoFailOverMode` (boolean): Enable auto-failover
- `autoLoadBalanceMode` (boolean): Enable load balancing
- `autoReplicationProviders` (string): Replication providers
- `autoFailOverProviders` (string): Failover providers
- `autoLoadBalanceProviders` (string): Load balance providers
- `waitForAutoReplicationResult` (boolean): Wait for replication
- `showDetailedSettings` (boolean): Show detailed settings

#### **Load All Holons**
```http
POST /api/data/load-all-holons
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "holonType": "Project",
  "loadChildren": true,
  "recursive": false,
  "maxChildDepth": 3,
  "continueOnError": true,
  "version": "latest",
  "filters": {
    "createdAfter": "2024-01-01T00:00:00Z",
    "tags": ["development", "active"]
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "holons": [
        {
          "id": "123e4567-e89b-12d3-a456-426614174000",
          "name": "Project Alpha",
          "type": "Project",
          "description": "Main project",
          "createdAt": "2024-01-15T10:30:00Z",
          "status": "active"
        },
        {
          "id": "456e7890-e89b-12d3-a456-426614174001",
          "name": "Project Beta",
          "type": "Project",
          "description": "Secondary project",
          "createdAt": "2024-01-16T10:30:00Z",
          "status": "active"
        }
      ],
      "totalCount": 2,
      "loadedAt": "2024-01-20T14:30:00Z",
      "provider": "MongoDB",
      "replicationStatus": "completed"
    },
    "message": "Holons loaded successfully"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Load Holons for Parent**
```http
POST /api/data/load-holons-for-parent
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "parentId": "123e4567-e89b-12d3-a456-426614174000",
  "holonType": "Task",
  "loadChildren": true,
  "recursive": true,
  "maxChildDepth": 5,
  "continueOnError": false,
  "version": "latest"
}
```

### **Holon Management**

#### **Save Holon**
```http
POST /api/data/save-holon
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Updated Project",
  "description": "Updated project description",
  "metadata": {
    "tags": ["updated", "project"],
    "category": "software",
    "priority": "high"
  },
  "saveChildren": true,
  "recursive": true,
  "maxChildDepth": 3,
  "continueOnError": false
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "name": "Updated Project",
      "description": "Updated project description",
      "savedAt": "2024-01-20T14:30:00Z",
      "version": "1.3.0",
      "provider": "MongoDB",
      "replicationStatus": "in_progress",
      "childrenSaved": 5,
      "totalChildren": 5
    },
    "message": "Holon saved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Save Holon Off-Chain**
```http
POST /api/data/save-holon-off-chain
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Off-Chain Project",
  "description": "Project stored off-chain",
  "metadata": {
    "offChain": true,
    "encrypted": true
  }
}
```

#### **Delete Holon**
```http
DELETE /api/data/delete-holon/{id}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `id` (string): Holon UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "deletedAt": "2024-01-20T14:30:00Z",
      "softDelete": false,
      "childrenDeleted": 3,
      "provider": "MongoDB"
    },
    "message": "Holon deleted successfully"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Delete Holon with Options**
```http
DELETE /api/data/delete-holon/{id}/{softDelete}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}
Authorization: Bearer YOUR_TOKEN
```

### **File Operations**

#### **Save File**
```http
POST /api/data/save-file
Content-Type: multipart/form-data
Authorization: Bearer YOUR_TOKEN
```

**Form Data:**
- `file`: File to upload
- `metadata`: JSON metadata (optional)
- `encrypt`: Boolean (optional, default: false)
- `compress`: Boolean (optional, default: false)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "fileId": "file_123",
      "filename": "document.pdf",
      "size": 1024000,
      "contentType": "application/pdf",
      "uploadedAt": "2024-01-20T14:30:00Z",
      "url": "https://api.oasisweb4.com/files/file_123",
      "hash": "sha256:abc123...",
      "encrypted": false,
      "compressed": false,
      "provider": "IPFS"
    },
    "message": "File saved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Load File**
```http
GET /api/data/load-file/{id}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `id` (string): File UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "fileId": "file_123",
      "filename": "document.pdf",
      "size": 1024000,
      "contentType": "application/pdf",
      "url": "https://api.oasisweb4.com/files/file_123",
      "hash": "sha256:abc123...",
      "createdAt": "2024-01-20T14:30:00Z",
      "metadata": {
        "tags": ["document", "important"],
        "category": "pdf"
      },
      "provider": "IPFS"
    },
    "message": "File loaded successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### **Data Operations**

#### **Save Data**
```http
POST /api/data/save-data
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "key": "user_preferences",
  "value": {
    "theme": "dark",
    "language": "en",
    "notifications": true
  },
  "metadata": {
    "category": "user_settings",
    "encrypted": true
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
      "savedAt": "2024-01-20T14:30:00Z",
      "version": "1.0",
      "provider": "MongoDB",
      "encrypted": true,
      "replicationStatus": "completed"
    },
    "message": "Data saved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

#### **Load Data**
```http
POST /api/data/load-data
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "key": "user_preferences",
  "version": "latest"
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
        "language": "en",
        "notifications": true
      },
      "version": "1.0",
      "loadedAt": "2024-01-20T14:30:00Z",
      "provider": "MongoDB",
      "encrypted": true
    },
    "message": "Data loaded successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### **Error Responses**

#### **Holon Not Found**
```json
{
  "result": null,
  "isError": true,
  "message": "Holon not found",
  "exception": "Holon with ID 123 not found"
}
```

#### **File Upload Error**
```json
{
  "result": null,
  "isError": true,
  "message": "File upload failed",
  "exception": "File size exceeds maximum limit of 10MB"
}
```

#### **Provider Error**
```json
{
  "result": null,
  "isError": true,
  "message": "Provider error",
  "exception": "MongoDB connection failed"
}
```
