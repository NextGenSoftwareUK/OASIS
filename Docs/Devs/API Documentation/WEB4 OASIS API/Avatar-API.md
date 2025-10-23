# Avatar API

## 📋 **Table of Contents**

- [Overview](#overview)
- [User Registration](#user-registration)
- [Authentication](#authentication)
- [Password Management](#password-management)
- [Avatar Management](#avatar-management)
- [Avatar Portraits](#avatar-portraits)
- [Avatar Updates](#avatar-updates)
- [Avatar Deletion](#avatar-deletion)
- [Avatar Sessions](#avatar-sessions)
- [Error Responses](#error-responses)

## Overview

The Avatar API provides comprehensive user management, authentication, and identity services. It handles user registration, authentication, profile management, and session control across all supported providers.

## User Registration

### Register New User
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

### Register with Provider
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

## Authentication

### Authenticate User
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

### Authenticate with Advanced Options
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

### Token Authentication
```http
POST /api/avatar/authenticate-token/{JWTToken}
```

**Parameters:**
- `JWTToken` (string): JWT token for authentication

### Refresh Token
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

### Revoke Token
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

## Password Management

### Forgot Password
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

### Validate Reset Token
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

### Reset Password
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

## Avatar Management

### Get Avatar by ID
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

### Get Avatar by Username
```http
GET /api/avatar/get-by-username/{username}
Authorization: Bearer YOUR_TOKEN
```

### Get Avatar by Email
```http
GET /api/avatar/get-by-email/{email}
Authorization: Bearer YOUR_TOKEN
```

### Get All Avatars
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

### Search Avatars
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

## Avatar Portraits

### Get Avatar Portrait
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

### Upload Avatar Portrait
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

## Avatar Updates

### Update Avatar by ID
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

### Update Avatar by Email
```http
POST /api/avatar/update-by-email/{email}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

### Update Avatar by Username
```http
POST /api/avatar/update-by-username/{username}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

## Avatar Deletion

### Delete Avatar by ID
```http
DELETE /api/avatar/{id:Guid}
Authorization: Bearer YOUR_TOKEN
```

### Delete Avatar by Username
```http
DELETE /api/avatar/delete-by-username/{username}
Authorization: Bearer YOUR_TOKEN
```

### Delete Avatar by Email
```http
DELETE /api/avatar/delete-by-email/{email}
Authorization: Bearer YOUR_TOKEN
```

## Avatar Sessions

### Get Avatar Sessions
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

### Logout Session
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

### Logout All Sessions
```http
POST /api/avatar/{avatarId}/sessions/logout-all
Authorization: Bearer YOUR_TOKEN
```

## Error Responses

### Authentication Error
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid credentials",
  "exception": "Authentication failed"
}
```

### Validation Error
```json
{
  "result": null,
  "isError": true,
  "message": "Validation failed",
  "exception": "Email format is invalid"
}
```

### Not Found Error
```json
{
  "result": null,
  "isError": true,
  "message": "Avatar not found",
  "exception": "Avatar with ID 123 not found"
}
```

---

## Navigation

**← Previous:** [README](README.md) | **Next:** [Keys API](Keys-API.md) →
