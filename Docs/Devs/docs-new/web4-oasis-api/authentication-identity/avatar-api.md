# Avatar API

## Overview

The Avatar API provides comprehensive user identity and profile management for the OASIS ecosystem. It handles avatar registration, authentication, profile management, and session handling with support for multiple avatar types, real-time updates, and advanced security features.

**Base URL:** `/api/avatar`

**Authentication:** Required for most endpoints (Bearer token)

**Rate Limits:**
- Free tier: 100 requests/minute
- Pro tier: 1,000 requests/minute

---

## Quick Start

### Your First API Call

**Step 1: Register a new avatar**

```http
POST http://api.oasisweb4.com/api/avatar/register
Content-Type: application/json

{
  "username": "myusername",
  "email": "myemail@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Step 2: Verify email** (check your email for token)

```http
GET http://api.oasisweb4.com/api/avatar/verify-email?token=YOUR_VERIFICATION_TOKEN
```

**Step 3: Authenticate to get JWT token**

```http
POST http://api.oasisweb4.com/api/avatar/authenticate
Content-Type: application/json

{
  "username": "myusername",
  "password": "SecurePassword123!"
}
```

**Step 4: Use JWT token for authenticated requests**

```http
GET http://api.oasisweb4.com/api/avatar/get-by-id/{avatarId}
Authorization: Bearer YOUR_JWT_TOKEN
```

---

## Authentication Endpoints

### Register Avatar

Register a new avatar with the OASIS system.

**Endpoint:** `POST /api/avatar/register`

**Authentication:** Not required

**Request Body:**
```json
{
  "username": "string (required)",
  "email": "string (required, valid email)",
  "password": "string (required, min 8 characters)",
  "firstName": "string (optional)",
  "lastName": "string (optional)",
  "title": "string (optional)",
  "avatarType": "string (optional, default: 'User')",
  "ownerAvatarId": "string (optional, required for Agent type)"
}
```

**Response:**
```json
{
  "result": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "username": "myusername",
    "email": "myemail@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "isEmailVerified": false,
    "avatarType": {
      "value": 0,
      "name": "User"
    }
  },
  "isError": false,
  "message": "Avatar registered successfully. Please check your email to verify your account."
}
```

**Error Responses:**

| Status Code | Error Code | Description |
|-------------|-----------|-------------|
| 400 | VALIDATION_ERROR | Invalid registration data |
| 400 | USER_EXISTS | Username or email already exists |
| 400 | INVALID_EMAIL | Email format is invalid |

**Code Examples:**

```typescript
// TypeScript/JavaScript
const response = await fetch('http://api.oasisweb4.com/api/avatar/register', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    username: 'myusername',
    email: 'myemail@example.com',
    password: 'SecurePassword123!',
    firstName: 'John',
    lastName: 'Doe'
  })
});
const data = await response.json();
```

```python
# Python
import requests

response = requests.post(
    'http://api.oasisweb4.com/api/avatar/register',
    json={
        'username': 'myusername',
        'email': 'myemail@example.com',
        'password': 'SecurePassword123!',
        'firstName': 'John',
        'lastName': 'Doe'
    }
)
data = response.json()
```

```bash
# cURL
curl -X POST "http://api.oasisweb4.com/api/avatar/register" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "myusername",
    "email": "myemail@example.com",
    "password": "SecurePassword123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

---

### Verify Email

Verify a newly created avatar using the token sent via email.

**Endpoint:** `GET /api/avatar/verify-email?token={token}`

**Alternative:** `POST /api/avatar/verify-email`

**Authentication:** Not required

**Parameters:**

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| token | string | query/body | Yes | Verification token from email |

**Request Example (POST):**
```json
{
  "token": "verification-token-from-email"
}
```

**Response:**
```json
{
  "result": true,
  "isError": false,
  "message": "Email verified successfully"
}
```

---

### Authenticate

Authenticate and log in using avatar credentials. Returns JWT token for subsequent requests.

**Endpoint:** `POST /api/avatar/authenticate`

**Authentication:** Not required

**Request Body:**
```json
{
  "username": "string (or email)",
  "password": "string",
  "providerType": "string (optional)",
  "setGlobally": "boolean (optional, default: false)",
  "autoReplicationMode": "string (optional, default: 'default')",
  "autoFailOverMode": "string (optional, default: 'default')",
  "autoLoadBalanceMode": "string (optional, default: 'default')",
  "waitForAutoReplicationResult": "boolean (optional, default: false)",
  "showDetailedSettings": "boolean (optional, default: false)"
}
```

**Response:**
```json
{
  "result": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "username": "myusername",
    "email": "myemail@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here",
    "avatarType": {
      "value": 0,
      "name": "User"
    }
  },
  "isError": false,
  "message": "Authentication successful"
}
```

**Error Responses:**

**Important:** OASIS API may return HTTP 200 even for errors. Always check the `isError` field in the response body.

| HTTP Status | Error Code | Description |
|-------------|-----------|-------------|
| 200 (isError: true) | INVALID_CREDENTIALS | Username/password incorrect |
| 200 (isError: true) | EMAIL_NOT_VERIFIED | Email not verified |
| 200 (isError: true) | VALIDATION_ERROR | Invalid request data |
| 401 | INVALID_CREDENTIALS | Username/password incorrect |

**Code Example:**
```typescript
const response = await fetch('http://api.oasisweb4.com/api/avatar/authenticate', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    username: 'myusername',
    password: 'SecurePassword123!'
  })
});
const data = await response.json();
const jwtToken = data.result.token; // Save this for future requests
```

---

### Refresh Token

Refresh and generate a new JWT Security Token.

**Endpoint:** `POST /api/avatar/refresh-token`

**Authentication:** Required (refresh token in cookie or header)

**Response:**
```json
{
  "result": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "username": "myusername",
    "token": "new_jwt_token_here",
    "refreshToken": "new_refresh_token_here"
  },
  "isError": false
}
```

---

### Revoke Token

Revoke a JWT token (logout).

**Endpoint:** `POST /api/avatar/revoke-token`

**Authentication:** Required

**Request Body:**
```json
{
  "token": "jwt_token_to_revoke"
}
```

---

## Avatar Management Endpoints

### Get Avatar by ID

Get avatar details by ID.

**Endpoint:** `GET /api/avatar/get-by-id/{id}`

**Authentication:** Required

**Parameters:**

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| id | GUID | path | Yes | Avatar ID |

**Response:**
```json
{
  "result": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "username": "myusername",
    "email": "myemail@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "avatarType": {
      "value": 0,
      "name": "User"
    },
    "createdDate": "2024-01-15T10:30:00Z",
    "modifiedDate": "2024-01-15T10:30:00Z"
  },
  "isError": false
}
```

**Note:** Users can only get their own avatar unless they are a Wizard (admin).

---

### Get Avatar by Username

Get avatar details by username.

**Endpoint:** `GET /api/avatar/get-by-username/{username}`

**Authentication:** Required

**Parameters:**

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| username | string | path | Yes | Avatar username |

---

### Get Avatar by Email

Get avatar details by email.

**Endpoint:** `GET /api/avatar/get-by-email/{email}`

**Authentication:** Required

**Parameters:**

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| email | string | path | Yes | Avatar email address |

---

### Get All Avatars

Get all avatars (Wizard/Admin only).

**Endpoint:** `GET /api/avatar/get-all-avatars`

**Authentication:** Required (Wizard only)

**Response:**
```json
{
  "result": [
    {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "username": "avatar1",
      "email": "email1@example.com"
    },
    {
      "id": "223e4567-e89b-12d3-a456-426614174000",
      "username": "avatar2",
      "email": "email2@example.com"
    }
  ],
  "isError": false
}
```

---

### Get Avatar Detail

Get detailed avatar information including karma, stats, etc.

**Endpoint:** `GET /api/avatar/get-avatar-detail-by-id/{id}`

**Alternative endpoints:**
- `GET /api/avatar/get-avatar-detail-by-username/{username}`
- `GET /api/avatar/get-avatar-detail-by-email/{email}`

**Authentication:** Required

**Response:**
```json
{
  "result": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "username": "myusername",
    "email": "myemail@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "karma": 150,
    "level": 5,
    "xp": 2500,
    "avatarType": {
      "value": 0,
      "name": "User"
    }
  },
  "isError": false
}
```

---

### Get All Avatar Details

Get all avatar details (Wizard/Admin only).

**Endpoint:** `GET /api/avatar/get-all-avatar-details`

**Authentication:** Required (Wizard only)

---

## Avatar Portrait Endpoints

### Get Avatar Portrait

Get avatar portrait image by ID, username, or email.

**Endpoints:**
- `GET /api/avatar/get-avatar-portrait/{id}`
- `GET /api/avatar/get-avatar-portrait-by-username/{username}`
- `GET /api/avatar/get-avatar-portrait-by-email/{email}`

**Authentication:** Required

**Response:**
```json
{
  "result": {
    "avatarId": "123e4567-e89b-12d3-a456-426614174000",
    "portrait": "base64_encoded_image_or_url"
  },
  "isError": false
}
```

---

### Upload Avatar Portrait

Upload or update avatar portrait image.

**Endpoint:** `POST /api/avatar/upload-avatar-portrait`

**Authentication:** Required

**Request Body:**
```json
{
  "avatarId": "123e4567-e89b-12d3-a456-426614174000",
  "portrait": "base64_encoded_image_or_url"
}
```

---

## Avatar Update Endpoints

### Update Avatar by ID

Update avatar information by ID.

**Endpoint:** `POST /api/avatar/update-by-id/{id}`

**Authentication:** Required

**Request Body:**
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "title": "Mr",
  "description": "Updated description"
}
```

**Note:** Users can only update their own avatar unless they are a Wizard.

---

### Update Avatar by Username

Update avatar information by username.

**Endpoint:** `POST /api/avatar/update-by-username/{username}`

**Authentication:** Required

---

### Update Avatar by Email

Update avatar information by email.

**Endpoint:** `POST /api/avatar/update-by-email/{email}`

**Authentication:** Required

---

### Update Avatar Detail

Update detailed avatar information.

**Endpoints:**
- `POST /api/avatar/update-avatar-detail-by-id/{id}`
- `POST /api/avatar/update-avatar-detail-by-username/{username}`
- `POST /api/avatar/update-avatar-detail-by-email/{email}`

**Authentication:** Required

---

## Avatar Search

### Search Avatars

Search for avatars using various criteria.

**Endpoint:** `POST /api/avatar/search`

**Authentication:** Required

**Request Body:**
```json
{
  "searchQuery": "string",
  "searchParams": {
    "searchAllProviders": true,
    "loadChildren": true,
    "recursive": true
  }
}
```

**Response:**
```json
{
  "result": {
    "results": [
      {
        "id": "123e4567-e89b-12d3-a456-426614174000",
        "username": "myusername",
        "email": "myemail@example.com"
      }
    ],
    "totalResults": 1
  },
  "isError": false
}
```

---

## Password Management

### Forgot Password

Request password reset email.

**Endpoint:** `POST /api/avatar/forgot-password`

**Authentication:** Not required

**Request Body:**
```json
{
  "email": "myemail@example.com"
}
```

---

### Validate Reset Token

Validate a password reset token.

**Endpoint:** `POST /api/avatar/validate-reset-token`

**Authentication:** Not required

**Request Body:**
```json
{
  "token": "reset_token_from_email"
}
```

---

### Reset Password

Reset password using reset token.

**Endpoint:** `POST /api/avatar/reset-password`

**Authentication:** Not required

**Request Body:**
```json
{
  "token": "reset_token_from_email",
  "newPassword": "NewSecurePassword123!",
  "confirmPassword": "NewSecurePassword123!"
}
```

---

## Avatar Names

### Get All Avatar Names

Get list of all avatar names (usernames and/or IDs).

**Endpoint:** `GET /api/avatar/get-all-avatar-names/{includeUsernames}/{includeIds}`

**Authentication:** Required

**Parameters:**

| Parameter | Type | Location | Required | Default | Description |
|-----------|------|----------|----------|---------|-------------|
| includeUsernames | boolean | path | No | true | Include usernames in results |
| includeIds | boolean | path | No | true | Include IDs in results |

**Response:**
```json
{
  "result": [
    "username1",
    "username2",
    "123e4567-e89b-12d3-a456-426614174000",
    "223e4567-e89b-12d3-a456-426614174000"
  ],
  "isError": false
}
```

---

### Get All Avatar Names Grouped

Get avatar names grouped by name.

**Endpoint:** `GET /api/avatar/get-all-avatar-names-grouped-by-name/{includeUsernames}/{includeIds}`

**Authentication:** Required

**Response:**
```json
{
  "result": {
    "John Doe": [
      "john_doe",
      "123e4567-e89b-12d3-a456-426614174000"
    ],
    "Jane Smith": [
      "jane_smith",
      "223e4567-e89b-12d3-a456-426614174000"
    ]
  },
  "isError": false
}
```

---

## Avatar Deletion

### Delete Avatar

Delete an avatar by ID.

**Endpoint:** `DELETE /api/avatar/{id}`

**Authentication:** Required

**Note:** Users can only delete their own avatar unless they are a Wizard.

**Response:**
```json
{
  "result": true,
  "isError": false,
  "message": "Avatar deleted successfully"
}
```

---

## Provider-Specific Endpoints

Most endpoints have provider-specific variants that allow you to specify which OASIS provider to use:

**Format:** `{endpoint}/{providerType}/{setGlobally}`

**Example:**
```http
GET /api/avatar/get-by-id/{id}/MongoDBOASIS/false
```

**Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| providerType | string | Provider type (e.g., MongoDBOASIS, SolanaOASIS) |
| setGlobally | boolean | If true, sets provider globally for all future requests |

**Available Providers:**
- `MongoDBOASIS` - MongoDB Provider
- `SolanaOASIS` - Solana Provider
- `EthereumOASIS` - Ethereum Provider
- `IPFSOASIS` - IPFS Provider
- `HoloOASIS` - Holochain Provider
- And 40+ more...

[View all supported providers →](../overview.md#supported-providers)

---

## Request/Response Schemas

### RegisterRequest

```json
{
  "username": "string (required, unique)",
  "email": "string (required, valid email, unique)",
  "password": "string (required, min 8 characters)",
  "firstName": "string (optional)",
  "lastName": "string (optional)",
  "title": "string (optional)",
  "avatarType": "string (optional, values: User, Wizard, Agent, System)",
  "ownerAvatarId": "string (optional, required for Agent type)"
}
```

### AuthenticateRequest

```json
{
  "username": "string (or email)",
  "password": "string",
  "providerType": "string (optional)",
  "setGlobally": "boolean (optional)",
  "autoReplicationMode": "string (optional)",
  "autoFailOverMode": "string (optional)",
  "autoLoadBalanceMode": "string (optional)",
  "waitForAutoReplicationResult": "boolean (optional)",
  "showDetailedSettings": "boolean (optional)"
}
```

### UpdateRequest

```json
{
  "firstName": "string (optional)",
  "lastName": "string (optional)",
  "title": "string (optional)",
  "description": "string (optional)",
  "address": "string (optional)",
  "town": "string (optional)",
  "county": "string (optional)",
  "country": "string (optional)",
  "postcode": "string (optional)",
  "landline": "string (optional)",
  "mobile": "string (optional)"
}
```

---

## Error Handling

All errors follow a standard format:

```json
{
  "result": null,
  "isError": true,
  "message": "Human-readable error message",
  "errorCode": "ERROR_CODE",
  "errors": [
    {
      "field": "email",
      "message": "Email is already registered"
    }
  ]
}
```

**Common Error Codes:**

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| VALIDATION_ERROR | 400 | Request validation failed |
| UNAUTHORIZED | 401 | Missing or invalid authentication |
| FORBIDDEN | 403 | Insufficient permissions |
| NOT_FOUND | 404 | Avatar not found |
| USER_EXISTS | 400 | Username or email already exists |
| INVALID_CREDENTIALS | 401 | Username/password incorrect |
| EMAIL_NOT_VERIFIED | 401 | Email not verified |

---

## Rate Limits

Rate limits are applied per API key:

| Tier | Requests per Minute | Burst Limit |
|------|---------------------|-------------|
| Free | 100 | 200 |
| Pro | 1,000 | 2,000 |
| Enterprise | Custom | Custom |

Rate limit headers are included in responses:

```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1642248000
```

---

## Use Cases

### Use Case 1: User Registration Flow

**Scenario:** New user wants to create an account

1. Register avatar
2. Verify email
3. Authenticate to get JWT
4. Use JWT for all future requests

**Example:**
```typescript
// 1. Register
const registerResponse = await fetch('/api/avatar/register', {
  method: 'POST',
  body: JSON.stringify({ username, email, password, firstName, lastName })
});

// 2. Verify email (user clicks link in email)
// Token comes from email

// 3. Authenticate
const authResponse = await fetch('/api/avatar/authenticate', {
  method: 'POST',
  body: JSON.stringify({ username, password })
});
const { token } = await authResponse.json();

// 4. Use token
const profileResponse = await fetch('/api/avatar/get-by-id/{id}', {
  headers: { 'Authorization': `Bearer ${token}` }
});
```

---

### Use Case 2: Profile Management

**Scenario:** User wants to update their profile

```typescript
const response = await fetch(`/api/avatar/update-by-id/${avatarId}`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    firstName: 'Updated Name',
    description: 'New description'
  })
});
```

---

## Best Practices

1. **Always handle errors** - Check `isError` flag in responses
2. **Store JWT securely** - Never expose JWT tokens in client-side code
3. **Use refresh tokens** - Implement token refresh before expiration
4. **Validate email** - Always verify email before allowing full access
5. **Respect rate limits** - Implement exponential backoff

---

## Related Documentation

- [Karma API](karma-api.md) - Reputation and reward system
- [Keys API](keys-api.md) - Cryptographic key management
- [Authentication Guide](../../getting-started/authentication.md) - Detailed auth documentation

---

## Support

- **Questions?** → [Join Discord](https://discord.gg/oasis)
- **Issues?** → [Report on GitHub](https://github.com/NextGenSoftwareUK/OASIS/issues)
- **Swagger UI:** [http://api.oasisweb4.com/swagger/index.html](http://api.oasisweb4.com/swagger/index.html)

---

*Last Updated: January 24, 2026*
*API Version: v4.4.4*
