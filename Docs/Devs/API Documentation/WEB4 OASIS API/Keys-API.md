# Keys API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Key Management](#key-management)
- [Key Operations](#key-operations)
- [Key Security](#key-security)
- [Key Analytics](#key-analytics)
- [Error Responses](#error-responses)

## Overview

The Keys API provides comprehensive cryptographic key management services for the OASIS ecosystem. It handles key generation, storage, rotation, and security with support for multiple key types, encryption algorithms, and advanced security features.

## Key Management

### Get All Keys
```http
GET /api/keys
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by key type (RSA, AES, ECDSA, Ed25519)
- `status` (string, optional): Filter by status (Active, Inactive, Expired, Revoked)
- `owner` (string, optional): Filter by owner ID
- `sortBy` (string, optional): Sort field (name, createdAt, expiresAt)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "keys": [
        {
          "id": "key_123",
          "name": "Main Encryption Key",
          "type": "RSA",
          "status": "Active",
          "owner": {
            "id": "user_123",
            "username": "john_doe"
          },
          "algorithm": "RSA-2048",
          "keySize": 2048,
          "publicKey": "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...\n-----END PUBLIC KEY-----",
          "fingerprint": "SHA256:abc123def456ghi789",
          "expiresAt": "2025-01-20T14:30:00Z",
          "metadata": {
            "description": "Primary encryption key for user data",
            "tags": ["encryption", "primary"],
            "rotation": "automatic",
            "backup": true
          },
          "permissions": {
            "encrypt": true,
            "decrypt": true,
            "sign": true,
            "verify": true
          },
          "createdAt": "2024-01-20T14:30:00Z",
          "lastUsed": "2024-01-20T14:30:00Z"
        }
      ],
      "totalCount": 1,
      "limit": 50,
      "offset": 0
    },
    "message": "Keys retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Key by ID
```http
GET /api/keys/{keyId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `keyId` (string): Key UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "key_123",
      "name": "Main Encryption Key",
      "type": "RSA",
      "status": "Active",
      "owner": {
        "id": "user_123",
        "username": "john_doe"
      },
      "algorithm": "RSA-2048",
      "keySize": 2048,
      "publicKey": "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...\n-----END PUBLIC KEY-----",
      "fingerprint": "SHA256:abc123def456ghi789",
      "expiresAt": "2025-01-20T14:30:00Z",
      "metadata": {
        "description": "Primary encryption key for user data",
        "tags": ["encryption", "primary"],
        "rotation": "automatic",
        "backup": true
      },
      "permissions": {
        "encrypt": true,
        "decrypt": true,
        "sign": true,
        "verify": true
      },
      "security": {
        "encryption": "AES-256",
        "storage": "HSM",
        "access": "restricted"
      },
      "analytics": {
        "usageCount": 150,
        "lastUsed": "2024-01-20T14:30:00Z",
        "successRate": 0.99
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastUsed": "2024-01-20T14:30:00Z"
    },
    "message": "Key retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Create Key
```http
POST /api/keys
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "name": "New Encryption Key",
  "type": "RSA",
  "algorithm": "RSA-2048",
  "keySize": 2048,
  "metadata": {
    "description": "New encryption key for secure communications",
    "tags": ["encryption", "communications"],
    "rotation": "manual",
    "backup": true
  },
  "permissions": {
    "encrypt": true,
    "decrypt": true,
    "sign": true,
    "verify": true
  },
  "security": {
    "encryption": "AES-256",
    "storage": "HSM",
    "access": "restricted"
  },
  "expiresAt": "2025-01-20T14:30:00Z"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "key_124",
      "name": "New Encryption Key",
      "type": "RSA",
      "status": "Active",
      "owner": {
        "id": "user_123",
        "username": "john_doe"
      },
      "algorithm": "RSA-2048",
      "keySize": 2048,
      "publicKey": "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...\n-----END PUBLIC KEY-----",
      "fingerprint": "SHA256:def456ghi789jkl012",
      "expiresAt": "2025-01-20T14:30:00Z",
      "metadata": {
        "description": "New encryption key for secure communications",
        "tags": ["encryption", "communications"],
        "rotation": "manual",
        "backup": true
      },
      "permissions": {
        "encrypt": true,
        "decrypt": true,
        "sign": true,
        "verify": true
      },
      "security": {
        "encryption": "AES-256",
        "storage": "HSM",
        "access": "restricted"
      },
      "analytics": {
        "usageCount": 0,
        "lastUsed": null,
        "successRate": 1.0
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastUsed": null
    },
    "message": "Key created successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Key
```http
PUT /api/keys/{keyId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `keyId` (string): Key UUID

**Request Body:**
```json
{
  "name": "Updated Encryption Key",
  "metadata": {
    "description": "Updated encryption key for secure communications",
    "tags": ["encryption", "communications", "updated"],
    "rotation": "automatic",
    "backup": true
  },
  "permissions": {
    "encrypt": true,
    "decrypt": true,
    "sign": true,
    "verify": true
  }
}
```

### Delete Key
```http
DELETE /api/keys/{keyId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `keyId` (string): Key UUID

## Key Operations

### Encrypt Data
```http
POST /api/keys/{keyId}/encrypt
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `keyId` (string): Key UUID

**Request Body:**
```json
{
  "data": "Hello, World!",
  "encoding": "utf-8",
  "algorithm": "RSA-OAEP",
  "padding": "PKCS1"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "keyId": "key_123",
      "originalData": "Hello, World!",
      "encryptedData": "base64_encoded_encrypted_data",
      "algorithm": "RSA-OAEP",
      "padding": "PKCS1",
      "encoding": "utf-8",
      "encryptedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Data encrypted successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Decrypt Data
```http
POST /api/keys/{keyId}/decrypt
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `keyId` (string): Key UUID

**Request Body:**
```json
{
  "encryptedData": "base64_encoded_encrypted_data",
  "algorithm": "RSA-OAEP",
  "padding": "PKCS1",
  "encoding": "utf-8"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "keyId": "key_123",
      "encryptedData": "base64_encoded_encrypted_data",
      "decryptedData": "Hello, World!",
      "algorithm": "RSA-OAEP",
      "padding": "PKCS1",
      "encoding": "utf-8",
      "decryptedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Data decrypted successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Sign Data
```http
POST /api/keys/{keyId}/sign
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `keyId` (string): Key UUID

**Request Body:**
```json
{
  "data": "Hello, World!",
  "algorithm": "RSA-PSS",
  "hash": "SHA-256",
  "encoding": "utf-8"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "keyId": "key_123",
      "originalData": "Hello, World!",
      "signature": "base64_encoded_signature",
      "algorithm": "RSA-PSS",
      "hash": "SHA-256",
      "encoding": "utf-8",
      "signedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Data signed successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Verify Signature
```http
POST /api/keys/{keyId}/verify
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `keyId` (string): Key UUID

**Request Body:**
```json
{
  "data": "Hello, World!",
  "signature": "base64_encoded_signature",
  "algorithm": "RSA-PSS",
  "hash": "SHA-256",
  "encoding": "utf-8"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "keyId": "key_123",
      "data": "Hello, World!",
      "signature": "base64_encoded_signature",
      "algorithm": "RSA-PSS",
      "hash": "SHA-256",
      "encoding": "utf-8",
      "valid": true,
      "verifiedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Signature verified successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Generate Key Pair
```http
POST /api/keys/generate
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "name": "Generated Key Pair",
  "type": "RSA",
  "algorithm": "RSA-2048",
  "keySize": 2048,
  "metadata": {
    "description": "Auto-generated key pair",
    "tags": ["generated", "auto"],
    "rotation": "automatic",
    "backup": true
  },
  "permissions": {
    "encrypt": true,
    "decrypt": true,
    "sign": true,
    "verify": true
  },
  "security": {
    "encryption": "AES-256",
    "storage": "HSM",
    "access": "restricted"
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "key_125",
      "name": "Generated Key Pair",
      "type": "RSA",
      "status": "Active",
      "owner": {
        "id": "user_123",
        "username": "john_doe"
      },
      "algorithm": "RSA-2048",
      "keySize": 2048,
      "publicKey": "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...\n-----END PUBLIC KEY-----",
      "privateKey": "-----BEGIN PRIVATE KEY-----\nMIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQC...\n-----END PRIVATE KEY-----",
      "fingerprint": "SHA256:ghi789jkl012mno345",
      "expiresAt": "2025-01-20T14:30:00Z",
      "metadata": {
        "description": "Auto-generated key pair",
        "tags": ["generated", "auto"],
        "rotation": "automatic",
        "backup": true
      },
      "permissions": {
        "encrypt": true,
        "decrypt": true,
        "sign": true,
        "verify": true
      },
      "security": {
        "encryption": "AES-256",
        "storage": "HSM",
        "access": "restricted"
      },
      "analytics": {
        "usageCount": 0,
        "lastUsed": null,
        "successRate": 1.0
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastUsed": null
    },
    "message": "Key pair generated successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Key Security

### Rotate Key
```http
POST /api/keys/{keyId}/rotate
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `keyId` (string): Key UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "keyId": "key_123",
      "oldKey": {
        "id": "key_123",
        "status": "Inactive",
        "rotatedAt": "2024-01-20T14:30:00Z"
      },
      "newKey": {
        "id": "key_126",
        "status": "Active",
        "createdAt": "2024-01-20T14:30:00Z"
      },
      "rotationReason": "Scheduled rotation",
      "rotatedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Key rotated successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Revoke Key
```http
POST /api/keys/{keyId}/revoke
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `keyId` (string): Key UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "keyId": "key_123",
      "status": "Revoked",
      "revokedAt": "2024-01-20T14:30:00Z",
      "reason": "Security breach detected"
    },
    "message": "Key revoked successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Key Security
```http
GET /api/keys/{keyId}/security
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `keyId` (string): Key UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "keyId": "key_123",
      "security": {
        "encryption": "AES-256",
        "storage": "HSM",
        "access": "restricted",
        "backup": true,
        "rotation": "automatic"
      },
      "access": {
        "lastAccessed": "2024-01-20T14:30:00Z",
        "accessCount": 150,
        "failedAttempts": 0,
        "locked": false
      },
      "compliance": {
        "fips140": true,
        "commonCriteria": true,
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
    "message": "Key security retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Key Security
```http
PUT /api/keys/{keyId}/security
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `keyId` (string): Key UUID

**Request Body:**
```json
{
  "encryption": "AES-256",
  "storage": "HSM",
  "access": "restricted",
  "backup": true,
  "rotation": "automatic"
}
```

## Key Analytics

### Get Key Statistics
```http
GET /api/keys/stats
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `type` (string, optional): Filter by key type

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "statistics": {
        "keys": {
          "total": 1000,
          "active": 800,
          "inactive": 150,
          "expired": 30,
          "revoked": 20
        },
        "byType": {
          "RSA": 500,
          "AES": 300,
          "ECDSA": 150,
          "Ed25519": 50
        },
        "operations": {
          "encrypt": 5000,
          "decrypt": 4500,
          "sign": 2000,
          "verify": 1800
        },
        "performance": {
          "averageEncryptTime": 0.1,
          "averageDecryptTime": 0.2,
          "averageSignTime": 0.15,
          "averageVerifyTime": 0.25
        }
      },
      "trends": {
        "keys": "increasing",
        "operations": "increasing",
        "performance": "stable"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Key statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Key Performance
```http
GET /api/keys/performance
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "performance": {
        "averageEncryptTime": 0.1,
        "peakEncryptTime": 0.5,
        "averageDecryptTime": 0.2,
        "peakDecryptTime": 1.0,
        "averageSignTime": 0.15,
        "peakSignTime": 0.8,
        "averageVerifyTime": 0.25,
        "peakVerifyTime": 1.2
      },
      "metrics": {
        "operationsPerSecond": 1000,
        "averageLatency": 0.2,
        "p95Latency": 0.5,
        "p99Latency": 1.0,
        "errorRate": 0.001,
        "successRate": 0.999
      },
      "trends": {
        "encryptTime": "stable",
        "decryptTime": "stable",
        "signTime": "stable",
        "verifyTime": "stable"
      },
      "breakdown": {
        "RSA": {
          "averageEncryptTime": 0.1,
          "averageDecryptTime": 0.2,
          "averageSignTime": 0.15,
          "averageVerifyTime": 0.25
        },
        "AES": {
          "averageEncryptTime": 0.05,
          "averageDecryptTime": 0.05,
          "averageSignTime": 0.0,
          "averageVerifyTime": 0.0
        },
        "ECDSA": {
          "averageEncryptTime": 0.0,
          "averageDecryptTime": 0.0,
          "averageSignTime": 0.1,
          "averageVerifyTime": 0.2
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Key performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Key Health
```http
GET /api/keys/health
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
        "generation": {
          "status": "Healthy",
          "health": 0.99,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "storage": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "operations": {
          "status": "Healthy",
          "health": 0.97,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "security": {
          "status": "Healthy",
          "health": 0.96,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Key Generation Test",
          "status": "Pass",
          "responseTime": 0.1,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Key Storage Test",
          "status": "Pass",
          "responseTime": 0.05,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Key Operations Test",
          "status": "Pass",
          "responseTime": 0.2,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Key Security Test",
          "status": "Pass",
          "responseTime": 0.15,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Key health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Error Responses

### Key Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Key not found",
  "exception": "Key with ID key_123 not found"
}
```

### Invalid Key Type
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid key type",
  "exception": "Key type 'InvalidType' is not supported"
}
```

### Key Expired
```json
{
  "result": null,
  "isError": true,
  "message": "Key expired",
  "exception": "Key has expired and cannot be used"
}
```

### Key Revoked
```json
{
  "result": null,
  "isError": true,
  "message": "Key revoked",
  "exception": "Key has been revoked and cannot be used"
}
```

### Encryption Error
```json
{
  "result": null,
  "isError": true,
  "message": "Encryption error",
  "exception": "Failed to encrypt data with the specified key"
}
```

---

## Navigation

**← Previous:** [Files API](Files-API.md) | **Next:** [Competition API](Competition-API.md) →