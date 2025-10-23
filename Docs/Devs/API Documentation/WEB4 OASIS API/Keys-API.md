# Keys API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Key Management](#key-management)
- [Key Generation](#key-generation)
- [Key Retrieval](#key-retrieval)
- [Key Operations](#key-operations)
- [Key Lookup](#key-lookup)
- [WiFi Operations](#wifi-operations)
- [Key CRUD](#key-crud)
- [Error Responses](#error-responses)

## Overview

The Keys API provides comprehensive cryptographic key management, generation, and storage across all supported providers. It handles key linking, retrieval, and advanced cryptographic operations with secure backup and recovery mechanisms.

## Key Management

### Clear Cache
```http
POST /api/keys/clear_cache
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "clearedAt": "2024-01-20T14:30:00Z",
      "itemsCleared": 150,
      "cacheSize": "2.5MB"
    },
    "message": "Cache cleared successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Link Provider Public Key to Avatar by ID
```http
POST /api/keys/link_provider_public_key_to_avatar_by_id
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "avatarId": "123e4567-e89b-12d3-a456-426614174000",
  "providerType": "Ethereum",
  "publicKey": "0x742d35Cc6634C0532925a3b8D4C9db96C4b4d8b6",
  "keyName": "Main Ethereum Key"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "keyId": "key_123",
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "providerType": "Ethereum",
      "publicKey": "0x742d35Cc6634C0532925a3b8D4C9db96C4b4d8b6",
      "linkedAt": "2024-01-20T14:30:00Z",
      "isActive": true
    },
    "message": "Public key linked successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Link Provider Public Key to Avatar by Username
```http
POST /api/keys/link_provider_public_key_to_avatar_by_username
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "username": "john_doe",
  "providerType": "Solana",
  "publicKey": "9WzDXwBbmkg8ZTbNMqUxvQRAyrZzDsGYdLVL9zYtAWWM",
  "keyName": "Main Solana Key"
}
```

### Link Provider Public Key to Avatar by Email
```http
POST /api/keys/link_provider_public_key_to_avatar_by_email
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

### Link Provider Private Key to Avatar by ID
```http
POST /api/keys/link_provider_private_key_to_avatar_by_id
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "avatarId": "123e4567-e89b-12d3-a456-426614174000",
  "providerType": "Ethereum",
  "privateKey": "encrypted_private_key_here",
  "keyName": "Main Ethereum Private Key",
  "encrypted": true
}
```

### Link Provider Private Key to Avatar by Username
```http
POST /api/keys/link_provider_private_key_to_avatar_by_username
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

### Link Provider Private Key to Avatar by Email
```http
POST /api/keys/link_provider_private_key_to_avatar_by_email
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

## Key Generation

### Generate Keypair and Link Provider Keys to Avatar by ID
```http
POST /api/keys/generate_keypair_and_link_provider_keys_to_avatar_by_id
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "avatarId": "123e4567-e89b-12d3-a456-426614174000",
  "providerType": "Ethereum",
  "keyName": "Generated Ethereum Key",
  "backupPhrase": true,
  "encryptPrivateKey": true
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "keyId": "key_124",
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "providerType": "Ethereum",
      "publicKey": "0x8ba1f109551bD432803012645Hac136c",
      "privateKey": "encrypted_private_key_here",
      "backupPhrase": "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about",
      "generatedAt": "2024-01-20T14:30:00Z",
      "isActive": true
    },
    "message": "Keypair generated and linked successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Generate Keypair and Link Provider Keys to Avatar by Username
```http
POST /api/keys/generate_keypair_and_link_provider_keys_to_avatar_by_username
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

### Generate Keypair and Link Provider Keys to Avatar by Email
```http
POST /api/keys/generate_keypair_and_link_provider_keys_to_avatar_by_email
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

### Generate Keypair for Provider
```http
POST /api/keys/generate_keypair_for_provider/{providerType}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerType` (string): Provider type (e.g., "Ethereum", "Solana", "Bitcoin")

**Request Body:**
```json
{
  "keyName": "New Provider Key",
  "backupPhrase": true,
  "encryptPrivateKey": true
}
```

### Generate Keypair with Prefix
```http
POST /api/keys/generate_keypair/{keyPrefix}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `keyPrefix` (string): Key prefix for identification

## Key Retrieval

### Get Provider Unique Storage Key for Avatar by ID
```http
GET /api/keys/get_provider_unique_storage_key_for_avatar_by_id
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `avatarId` (string): Avatar UUID
- `providerType` (string, optional): Provider type

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "providerType": "Ethereum",
      "uniqueStorageKey": "eth_storage_key_123",
      "createdAt": "2024-01-15T10:30:00Z",
      "lastUsed": "2024-01-20T14:30:00Z"
    },
    "message": "Storage key retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Provider Unique Storage Key for Avatar by Username
```http
GET /api/keys/get_provider_unique_storage_key_for_avatar_by_username
Authorization: Bearer YOUR_TOKEN
```

### Get Provider Unique Storage Key for Avatar by Email
```http
GET /api/keys/get_provider_unique_storage_key_for_avatar_by_email
Authorization: Bearer YOUR_TOKEN
```

### Get Provider Private Key for Avatar by ID
```http
GET /api/keys/get_provider_private_key_for_avatar_by_id
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "providerType": "Ethereum",
      "privateKey": "encrypted_private_key_here",
      "isEncrypted": true,
      "retrievedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Private key retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Provider Private Key for Avatar by Username
```http
GET /api/keys/get_provider_private_key_for_avatar_by_username
Authorization: Bearer YOUR_TOKEN
```

### Get Provider Public Keys for Avatar by ID
```http
GET /api/keys/get_provider_public_keys_for_avatar_by_id
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "publicKeys": [
        {
          "keyId": "key_123",
          "providerType": "Ethereum",
          "publicKey": "0x742d35Cc6634C0532925a3b8D4C9db96C4b4d8b6",
          "keyName": "Main Ethereum Key",
          "createdAt": "2024-01-15T10:30:00Z",
          "isActive": true
        },
        {
          "keyId": "key_124",
          "providerType": "Solana",
          "publicKey": "9WzDXwBbmkg8ZTbNMqUxvQRAyrZzDsGYdLVL9zYtAWWM",
          "keyName": "Main Solana Key",
          "createdAt": "2024-01-16T10:30:00Z",
          "isActive": true
        }
      ],
      "totalKeys": 2
    },
    "message": "Public keys retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Provider Public Keys for Avatar by Username
```http
GET /api/keys/get_provider_public_keys_for_avatar_by_username
Authorization: Bearer YOUR_TOKEN
```

## Key Operations

### Get All Provider Public Keys for Avatar by ID
```http
GET /api/keys/get_all_provider_public_keys_for_avatar_by_id/{id}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `id` (string): Avatar UUID

### Get All Provider Public Keys for Avatar by Username
```http
GET /api/keys/get_all_provider_public_keys_for_avatar_by_username/{username}
Authorization: Bearer YOUR_TOKEN
```

### Get All Provider Private Keys for Avatar by ID
```http
GET /api/keys/get_all_provider_private_keys_for_avatar_by_id/{id}
Authorization: Bearer YOUR_TOKEN
```

### Get All Provider Private Keys for Avatar by Username
```http
GET /api/keys/get_all_provider_private_keys_for_avatar_by_username/{username}
Authorization: Bearer YOUR_TOKEN
```

### Get All Provider Unique Storage Keys for Avatar by ID
```http
GET /api/keys/get_all_provider_unique_storage_keys_for_avatar_by_id/{id}
Authorization: Bearer YOUR_TOKEN
```

### Get All Provider Unique Storage Keys for Avatar by Username
```http
GET /api/keys/get_all_provider_unique_storage_keys_for_avatar_by_username/{username}
Authorization: Bearer YOUR_TOKEN
```

### Get All Provider Unique Storage Keys for Avatar by Email
```http
GET /api/keys/get_all_provider_unique_storage_keys_for_avatar_by_email/{email}
Authorization: Bearer YOUR_TOKEN
```

## Key Lookup

### Get Avatar ID for Provider Unique Storage Key
```http
GET /api/keys/get_avatar_id_for_provider_unique_storage_key/{providerKey}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerKey` (string): Provider unique storage key

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "avatarId": "123e4567-e89b-12d3-a456-426614174000",
      "providerKey": "eth_storage_key_123",
      "providerType": "Ethereum",
      "createdAt": "2024-01-15T10:30:00Z"
    },
    "message": "Avatar ID retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Avatar Username for Provider Unique Storage Key
```http
GET /api/keys/get_avatar_username_for_provider_unique_storage_key/{providerKey}
Authorization: Bearer YOUR_TOKEN
```

### Get Avatar Email for Provider Unique Storage Key
```http
GET /api/keys/get_avatar_email_for_provider_unique_storage_key/{providerKey}
Authorization: Bearer YOUR_TOKEN
```

### Get Avatar for Provider Unique Storage Key
```http
GET /api/keys/get_avatar_for_provider_unique_storage_key/{providerKey}
Authorization: Bearer YOUR_TOKEN
```

### Get Avatar ID for Provider Public Key
```http
GET /api/keys/get_avatar_id_for_provider_public_key/{providerKey}
Authorization: Bearer YOUR_TOKEN
```

### Get Avatar Username for Provider Public Key
```http
GET /api/keys/get_avatar_username_for_provider_public_key/{providerKey}
Authorization: Bearer YOUR_TOKEN
```

### Get Avatar Email for Provider Public Key
```http
GET /api/keys/get_avatar_email_for_provider_public_key/{providerKey}
Authorization: Bearer YOUR_TOKEN
```

### Get Avatar for Provider Public Key
```http
GET /api/keys/get_avatar_for_provider_public_key/{providerKey}
Authorization: Bearer YOUR_TOKEN
```

### Get Avatar ID for Provider Private Key
```http
GET /api/keys/get_avatar_id_for_provider_private_key/{providerKey}
Authorization: Bearer YOUR_TOKEN
```

### Get Avatar Username for Provider Private Key
```http
GET /api/keys/get_avatar_username_for_provider_private_key/{providerKey}
Authorization: Bearer YOUR_TOKEN
```

### Get Avatar for Provider Private Key
```http
GET /api/keys/get_avatar_for_provider_private_key/{providerKey}
Authorization: Bearer YOUR_TOKEN
```

## WiFi Operations

### Get Private WiFi
```http
POST /api/keys/get_private_wifi/{source}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `source` (string): Source identifier

**Request Body:**
```json
{
  "password": "wifi_password",
  "encrypt": true
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "privateKey": "encrypted_wifi_private_key",
      "source": "wifi_source_123",
      "encrypted": true,
      "generatedAt": "2024-01-20T14:30:00Z"
    },
    "message": "Private WiFi key generated successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Public WiFi
```http
POST /api/keys/get_public_wifi
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

### Decode Private WiFi
```http
POST /api/keys/decode_private_wif/{data}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

### Base58 Check Decode
```http
POST /api/keys/base58_check_decode/{data}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

### Encode Signature
```http
POST /api/keys/encode_signature/{source}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

## Key CRUD

### Get All Keys
```http
GET /api/keys/all
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `providerType` (string, optional): Filter by provider type
- `avatarId` (string, optional): Filter by avatar ID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "keys": [
        {
          "id": "key_123",
          "avatarId": "123e4567-e89b-12d3-a456-426614174000",
          "providerType": "Ethereum",
          "publicKey": "0x742d35Cc6634C0532925a3b8D4C9db96C4b4d8b6",
          "keyName": "Main Ethereum Key",
          "createdAt": "2024-01-15T10:30:00Z",
          "isActive": true
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

### Create Key
```http
POST /api/keys/create
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "avatarId": "123e4567-e89b-12d3-a456-426614174000",
  "providerType": "Bitcoin",
  "publicKey": "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa",
  "privateKey": "encrypted_private_key",
  "keyName": "Main Bitcoin Key"
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
  "keyName": "Updated Bitcoin Key",
  "isActive": true
}
```

### Delete Key
```http
DELETE /api/keys/{keyId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `keyId` (string): Key UUID

### Get Key Stats
```http
GET /api/keys/stats
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "totalKeys": 1250,
      "activeKeys": 1200,
      "inactiveKeys": 50,
      "keysByProvider": {
        "Ethereum": 500,
        "Solana": 300,
        "Bitcoin": 200,
        "Polygon": 150,
        "Other": 100
      },
      "keysByAvatar": 850,
      "averageKeysPerAvatar": 1.47,
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Key statistics retrieved successfully"
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

### Invalid Key Format
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid key format",
  "exception": "Invalid Ethereum public key format"
}
```

### Encryption Error
```json
{
  "result": null,
  "isError": true,
  "message": "Encryption failed",
  "exception": "Failed to encrypt private key"
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

---

## Navigation

**← Previous:** [Avatar API](Avatar-API.md) | **Next:** [Karma API](Karma-API.md) →
