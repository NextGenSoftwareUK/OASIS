# Authentication Guide

## Overview

OASIS API uses JWT (JSON Web Token) Bearer token authentication. Most endpoints require authentication, except for registration and email verification.

---

## Authentication Flow

### Step 1: Register

Create a new avatar account:

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

**Response:**
```json
{
  "result": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "username": "myusername",
    "email": "myemail@example.com",
    "isEmailVerified": false
  },
  "isError": false,
  "message": "Avatar registered successfully. Please check your email to verify your account."
}
```

---

### Step 2: Verify Email

Check your email for a verification token, then verify:

```http
GET http://api.oasisweb4.com/api/avatar/verify-email?token=YOUR_VERIFICATION_TOKEN
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

### Step 3: Authenticate

Login to get your JWT token:

```http
POST http://api.oasisweb4.com/api/avatar/authenticate
Content-Type: application/json

{
  "username": "myusername",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "result": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "username": "myusername",
    "email": "myemail@example.com",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here"
  },
  "isError": false,
  "message": "Authentication successful"
}
```

**Save the `token` field** - you'll need it for all authenticated requests.

---

### Step 4: Use JWT Token

Include the JWT token in the Authorization header:

```http
GET http://api.oasisweb4.com/api/avatar/get-by-id/{avatarId}
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## Token Management

### Refresh Token

Refresh your JWT token before it expires:

```http
POST http://api.oasisweb4.com/api/avatar/refresh-token
```

**Note:** Refresh token should be in cookie or Authorization header.

---

### Revoke Token

Logout by revoking your token:

```http
POST http://api.oasisweb4.com/api/avatar/revoke-token
Content-Type: application/json

{
  "token": "jwt_token_to_revoke"
}
```

---

## Code Examples

### JavaScript/TypeScript

```typescript
class OASISClient {
  private baseUrl = 'http://api.oasisweb4.com/api';
  private token: string | null = null;

  async authenticate(username: string, password: string) {
    const response = await fetch(`${this.baseUrl}/avatar/authenticate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });
    
    const data = await response.json();
    if (!data.isError && data.result?.token) {
      this.token = data.result.token;
      return data.result;
    }
    throw new Error(data.message || 'Authentication failed');
  }

  async get(endpoint: string) {
    if (!this.token) {
      throw new Error('Not authenticated. Call authenticate() first.');
    }
    
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      headers: {
        'Authorization': `Bearer ${this.token}`
      }
    });
    
    return response.json();
  }
}

// Usage
const client = new OASISClient();
await client.authenticate('myusername', 'password');
const avatar = await client.get('/avatar/get-by-id/123e4567-e89b-12d3-a456-426614174000');
```

---

### Python

```python
import requests

class OASISClient:
    def __init__(self, base_url='http://api.oasisweb4.com/api'):
        self.base_url = base_url
        self.token = None
    
    def authenticate(self, username, password):
        response = requests.post(
            f'{self.base_url}/avatar/authenticate',
            json={'username': username, 'password': password}
        )
        data = response.json()
        if not data.get('isError') and data.get('result', {}).get('token'):
            self.token = data['result']['token']
            return data['result']
        raise Exception(data.get('message', 'Authentication failed'))
    
    def get(self, endpoint):
        if not self.token:
            raise Exception('Not authenticated. Call authenticate() first.')
        
        response = requests.get(
            f'{self.base_url}{endpoint}',
            headers={'Authorization': f'Bearer {self.token}'}
        )
        return response.json()

# Usage
client = OASISClient()
client.authenticate('myusername', 'password')
avatar = client.get('/avatar/get-by-id/123e4567-e89b-12d3-a456-426614174000')
```

---

### cURL

```bash
# 1. Authenticate
TOKEN=$(curl -X POST "http://api.oasisweb4.com/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username":"myusername","password":"SecurePassword123!"}' \
  | jq -r '.result.token')

# 2. Use token
curl -X GET "http://api.oasisweb4.com/api/avatar/get-by-id/{avatarId}" \
  -H "Authorization: Bearer $TOKEN"
```

---

## Error Handling

### Authentication Errors

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| INVALID_CREDENTIALS | 401 | Username/password incorrect |
| EMAIL_NOT_VERIFIED | 401 | Email not verified |
| TOKEN_EXPIRED | 401 | JWT token expired |
| UNAUTHORIZED | 401 | Missing or invalid token |

### Example Error Response

```json
{
  "result": null,
  "isError": true,
  "message": "Unauthorized. Try Logging In First With api/avatar/authenticate REST API Route.",
  "errorCode": "UNAUTHORIZED"
}
```

---

## Security Best Practices

1. **Never expose tokens** - Store tokens securely, never in client-side code
2. **Use HTTPS** - Always use HTTPS in production
3. **Token expiration** - Implement token refresh before expiration
4. **Secure storage** - Use secure storage mechanisms (keychain, secure storage)
5. **Validate tokens** - Always validate token on server side

---

## Related Documentation

- [Avatar API](../web4-oasis-api/authentication-identity/avatar-api.md) - Complete avatar management
- [Getting Started Guide](overview.md) - Quick start tutorial

---

*Last Updated: January 24, 2026*
