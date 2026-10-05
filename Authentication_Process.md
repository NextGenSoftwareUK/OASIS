# OASIS API Authentication Process

## Overview

The OASIS API uses JWT (JSON Web Token) authentication for securing endpoints. Users must register, verify their email, and then authenticate to receive a JWT token that is used for subsequent API requests.

## Base URL

- **Production**: `http://api.oasisweb4.com`
- **Local Development**: `http://localhost:5003`

## Authentication Flow

1. **Register** → User creates account
2. **Email Verification** → User verifies email (if enabled)
3. **Authenticate** → User logs in and receives JWT token
4. **Use Protected Endpoints** → Include JWT token in Authorization header

---

## Endpoints

### 1. Register New Avatar

**Endpoint**: `POST /api/avatar/register`

**Request Body**:
```json
{
  "title": "Mr",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "username": "johndoe",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!",
  "avatarType": "User",
  "acceptTerms": true
}
```

**Required Fields**:
- `firstName` (string, required)
- `lastName` (string, required)
- `email` (string, required, valid email format)
- `username` (string, required)
- `password` (string, required, minimum 6 characters)
- `confirmPassword` (string, required, must match password)
- `avatarType` (string, required, typically "User")
- `acceptTerms` (boolean, required, must be `true`)

**Optional Fields**:
- `title` (string, e.g., "Mr", "Mrs", "Ms", "Dr")

**Response** (Success - HTTP 200):
```json
{
  "result": {
    "avatarId": "bfbce7c2-708e-40ae-af79-1d2421037eaa",
    "username": "johndoe",
    "email": "john.doe@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "fullName": "John Doe",
    "isVerified": false,
    "verificationToken": "abc123...",
    ...
  },
  "statusCode": 200,
  "isError": false
}
```

**Email Verification**:
- If email service is configured (`SendVerificationEmail: true` in `OASIS_DNA.json`), a verification email will be sent automatically
- The email contains a verification link with the `verificationToken`
- Users must verify their email before they can authenticate (if email verification is enforced)

---

### 2. Authenticate (Login)

**Endpoint**: `POST /api/avatar/authenticate`

**Request Body**:
```json
{
  "username": "johndoe",
  "password": "SecurePassword123!"
}
```

**Required Fields**:
- `username` (string, required) - Can be username or email
- `password` (string, required)

**Response** (Success - HTTP 200):
```json
{
  "result": {
    "avatarId": "bfbce7c2-708e-40ae-af79-1d2421037eaa",
    "username": "johndoe",
    "email": "john.doe@example.com",
    "fullName": "John Doe",
    "isVerified": true,
    "jwtToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "D248C22477758CD15DBC99C8F70F72E9...",
    "verified": "2025-12-01T10:26:37.47Z",
    "lastBeamedIn": "2025-12-20T00:45:55.8213154+00:00",
    "isBeamedIn": true,
    "wallets": {
      "SolanaOASIS": [...],
      "EthereumOASIS": [...],
      ...
    },
    ...
  },
  "statusCode": 200,
  "isError": false
}
```

**Important Fields**:
- `jwtToken` - Use this token in Authorization header for protected endpoints
- `refreshToken` - Stored in HTTP-only cookie, used to refresh JWT token
- `isVerified` - Must be `true` to authenticate (if email verification is enabled)

**Error Response** (HTTP 401):
```json
{
  "message": "Avatar has not been verified. Please check your email.",
  "statusCode": 401,
  "isError": true
}
```

---

### 3. Refresh Token

**Endpoint**: `POST /api/avatar/refresh-token`

**Headers**:
- Cookie: `refreshToken=<refresh_token_value>` (automatically sent by browser)

**Response** (Success - HTTP 200):
```json
{
  "result": {
    "jwtToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "D248C22477758CD15DBC99C8F70F72E9...",
    ...
  },
  "statusCode": 200
}
```

---

### 4. Authenticate with JWT Token

**Endpoint**: `POST /api/avatar/authenticate-token/{JWTToken}`

**URL Parameter**:
- `JWTToken` - The JWT token to validate

**Response** (Success - HTTP 200):
```json
{
  "result": true,
  "statusCode": 200,
  "isError": false
}
```

---

## Using JWT Tokens

### Protected Endpoints

All protected endpoints require the JWT token in the `Authorization` header:

```
Authorization: Bearer <jwt_token>
```

### Example: Accessing Protected Endpoint

```bash
curl -X GET "http://api.oasisweb4.com/api/avatar/get-avatar-by-id/{avatarId}" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

---

## Test Credentials

### OASIS_ADMIN Account

**Username**: `OASIS_ADMIN`  
**Password**: `Uppermall1!`  
**Email**: `max.gershfield1@gmail.com`  
**Avatar ID**: `bfbce7c2-708e-40ae-af79-1d2421037eaa`  
**Status**: Verified and Active

---

## Example cURL Commands

### 1. Register New User

```bash
curl -X POST http://api.oasisweb4.com/api/avatar/register \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Mr",
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "username": "johndoe",
    "password": "SecurePassword123!",
    "confirmPassword": "SecurePassword123!",
    "avatarType": "User",
    "acceptTerms": true
  }'
```

### 2. Authenticate (Login)

```bash
curl -X POST http://api.oasisweb4.com/api/avatar/authenticate \
  -H "Content-Type: application/json" \
  -d '{
    "username": "OASIS_ADMIN",
    "password": "Uppermall1!"
  }'
```

### 3. Extract JWT Token and Use in Protected Endpoint

```bash
# Authenticate and extract token
TOKEN=$(curl -s -X POST http://api.oasisweb4.com/api/avatar/authenticate \
  -H "Content-Type: application/json" \
  -d '{"username":"OASIS_ADMIN","password":"Uppermall1!"}' | \
  jq -r '.result.jwtToken')

# Use token in protected endpoint
curl -X GET "http://api.oasisweb4.com/api/avatar/get-avatar-by-id/bfbce7c2-708e-40ae-af79-1d2421037eaa" \
  -H "Authorization: Bearer $TOKEN"
```

### 4. Complete Authentication Test Script

```bash
#!/bin/bash

API_URL="http://api.oasisweb4.com"
USERNAME="OASIS_ADMIN"
PASSWORD="Uppermall1!"

# Step 1: Authenticate
echo "🔐 Authenticating..."
RESPONSE=$(curl -s -X POST "$API_URL/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}")

# Extract JWT token
TOKEN=$(echo "$RESPONSE" | jq -r '.result.jwtToken // empty')
AVATAR_ID=$(echo "$RESPONSE" | jq -r '.result.avatarId // empty')

if [ -z "$TOKEN" ] || [ "$TOKEN" == "null" ]; then
  echo "❌ Authentication failed"
  echo "$RESPONSE" | jq .
  exit 1
fi

echo "✅ Authentication successful"
echo "📋 Avatar ID: $AVATAR_ID"
echo "🎫 JWT Token: ${TOKEN:0:50}..."

# Step 2: Use protected endpoint
echo ""
echo "🔒 Accessing protected endpoint..."
PROTECTED_RESPONSE=$(curl -s -X GET "$API_URL/api/avatar/get-avatar-by-id/$AVATAR_ID" \
  -H "Authorization: Bearer $TOKEN")

echo "$PROTECTED_RESPONSE" | jq '{statusCode, isError, username: .result.username, email: .result.email}'
```

---

## Email Verification

### Configuration

Email verification is controlled by `OASIS_DNA.json`:

```json
{
  "OASIS": {
    "Email": {
      "EmailFrom": "oasisweb4@gmail.com",
      "SmtpHost": "smtp.gmail.com",
      "SmtpPort": 587,
      "SmtpUser": "oasisweb4@gmail.com",
      "SmtpPass": "your-app-password",
      "DisableAllEmails": false,
      "SendVerificationEmail": true,
      "OASISWebSiteURL": "https://oasisweb4.com"
    }
  }
}
```

### Verification Process

1. User registers → Receives `verificationToken`
2. System sends email with verification link: `{OASISWebSiteURL}/verify-email?token={verificationToken}`
3. User clicks link → Email is verified
4. User can now authenticate

### Verification Token Format

The verification token is a unique string generated during registration and stored in the avatar's `verificationToken` field.

---

## Error Handling

### Common Errors

**401 Unauthorized**:
- Invalid credentials
- Email not verified (if verification is required)
- Expired JWT token

**400 Bad Request**:
- Missing required fields
- Invalid email format
- Password doesn't meet requirements
- Passwords don't match

**500 Internal Server Error**:
- Server-side error
- Database connection issues
- Provider errors

### Error Response Format

```json
{
  "message": "Error description",
  "statusCode": 400,
  "isError": true,
  "errorCount": 1
}
```

---

## JWT Token Details

### Token Structure

JWT tokens contain:
- **Header**: Algorithm and token type
- **Payload**: User ID, expiration time, issued at time
- **Signature**: HMAC SHA256 signature

### Token Expiration

- JWT tokens expire after a set period (typically 15 minutes)
- Use `refresh-token` endpoint to get a new token
- Refresh tokens are stored in HTTP-only cookies

### Token Validation

The API validates:
- Token signature
- Token expiration
- User still exists and is active
- User is verified (if email verification is enabled)

---

## Provider-Specific Authentication

### Register with Specific Provider

**Endpoint**: `POST /api/avatar/register/{providerType}/{setGlobally}`

**Example**:
```bash
curl -X POST "http://api.oasisweb4.com/api/avatar/register/MongoDBOASIS/false" \
  -H "Content-Type: application/json" \
  -d '{...}'
```

### Authenticate with Specific Provider

**Endpoint**: `POST /api/avatar/authenticate/{providerType}/{setGlobally}/...`

**Example**:
```bash
curl -X POST "http://api.oasisweb4.com/api/avatar/authenticate/MongoDBOASIS/false/default/default/default/default/default/default/false/false" \
  -H "Content-Type: application/json" \
  -d '{"username":"OASIS_ADMIN","password":"Uppermall1!"}'
```

---

## Security Best Practices

1. **Always use HTTPS** in production (currently HTTP for testing)
2. **Store JWT tokens securely** - Use HTTP-only cookies or secure storage
3. **Never expose refresh tokens** - Keep them server-side only
4. **Validate email addresses** - Ensure users verify their email
5. **Use strong passwords** - Enforce minimum password requirements
6. **Implement rate limiting** - Prevent brute force attacks
7. **Monitor authentication logs** - Track failed login attempts

---

## Notes

- The API currently runs on HTTP (not HTTPS) for testing
- Email verification emails are sent via SendGrid (Gmail SMTP)
- JWT tokens are signed with a secret key stored in configuration
- Refresh tokens are stored in HTTP-only cookies for security
- The `OASIS_ADMIN` account is pre-verified and ready for testing

---

## Related Documentation

- [OASIS API Documentation](http://api.oasisweb4.com/swagger)
- Email Service Configuration: `OASIS_DNA.json`
- Avatar Controller: `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/AvatarController.cs`



