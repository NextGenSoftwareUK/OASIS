# Pangea Backend API Usage Guide

## Base URL

```
https://pangea-production-128d.up.railway.app/api
```

**Note:** All routes are prefixed with `/api`. The root path `/` returns a 404 - always use `/api/*` paths.

---

## Authentication

### Step 1: Register a New User

**Endpoint:** `POST /api/auth/register`

**Request:**
```bash
curl -X POST https://pangea-production-128d.up.railway.app/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "securepassword123",
    "username": "username",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

**Response:**
```json
{
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "username": "username",
    "firstName": "John",
    "lastName": "Doe",
    "avatarId": "oasis-avatar-id",
    "role": "user"
  },
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-12-27T17:13:15.682Z"
}
```

### Step 2: Login

**Endpoint:** `POST /api/auth/login`

**Request:**
```bash
curl -X POST https://pangea-production-128d.up.railway.app/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "securepassword123"
  }'
```

**Response:** (Same format as register)

### Step 3: Use the Token

Save the `token` from the login/register response. Use it in the `Authorization` header for protected endpoints:

```bash
curl -X GET https://pangea-production-128d.up.railway.app/api/user/profile \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

---

## Key Endpoints

### Health Check (No Auth Required)

**GET** `/api/health`

```bash
curl https://pangea-production-128d.up.railway.app/api/health
```

**Response:**
```json
{
  "status": "ok",
  "timestamp": "2025-12-20T17:13:15.682Z",
  "service": "Pangea Markets Backend",
  "version": "1.0.0",
  "checks": {
    "database": { "status": "ok", "message": "Database connection successful" },
    "redis": { "status": "ok", "message": "Redis connection successful" }
  }
}
```

---

### User Endpoints (Require Authentication)

#### Get User Profile

**GET** `/api/user/profile`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

#### Update User Profile

**PUT** `/api/user/profile`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

**Body:**
```json
{
  "firstName": "Updated Name",
  "lastName": "Updated Last Name",
  "email": "newemail@example.com"
}
```

---

### Assets Endpoints (Read from Client)

#### Get All Assets

**GET** `/api/assets`

**No authentication required** (public read endpoint)

**Query Parameters:**
- `status` - Filter by status (pending, approved, active, etc.)
- `assetClass` - Filter by asset class
- `limit` - Number of results (default: 20)
- `offset` - Pagination offset

**Example:**
```bash
curl "https://pangea-production-128d.up.railway.app/api/assets?status=approved&limit=10"
```

#### Get Asset by ID

**GET** `/api/assets/:assetId`

**No authentication required**

```bash
curl https://pangea-production-128d.up.railway.app/api/assets/asset-id-here
```

---

### Orders Endpoints

#### Get User's Orders (Requires Auth)

**GET** `/api/orders`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Query Parameters:**
- `status` - Filter by status (pending, open, filled, cancelled)
- `assetId` - Filter by asset
- `limit` - Number of results
- `offset` - Pagination offset

#### Create Order (Write - Should go through Next.js API)

**POST** `/api/orders`

**Note:** According to architecture, order creation should go through your Next.js API route, not directly from client.

---

### Wallet Endpoints (Require Auth)

#### Get Wallet Balance

**GET** `/api/wallet/balance`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

#### Connect Wallet

**POST** `/api/wallet/connect`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

**Body:**
```json
{
  "walletAddress": "0x...",
  "blockchain": "ethereum" // or "solana"
}
```

---

### Transactions Endpoints (Require Auth)

#### Get User Transactions

**GET** `/api/transactions`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Query Parameters:**
- `type` - Filter by type (deposit, withdrawal, trade)
- `status` - Filter by status
- `startDate` - Start date (ISO format)
- `endDate` - End date (ISO format)

---

## Authentication Flow Summary

1. **Register or Login** → Get JWT token
2. **Store token** → Save in localStorage/sessionStorage or httpOnly cookie
3. **Include in requests** → Add `Authorization: Bearer <token>` header
4. **Token expires** → Login again to get new token (tokens expire after 7 days by default)

---

## Important Notes

### Read vs Write Operations

**✅ Direct from Client (Read):**
- GET `/api/assets` - Browse assets
- GET `/api/assets/:id` - View asset details
- GET `/api/health` - Health check

**🔄 Via Next.js API (Write/Modify):**
- POST `/api/orders` - Create orders (should go through Next.js API route)
- POST `/api/assets` - Create assets (admin only)
- POST `/api/transactions` - Initiate transactions (should go through Next.js API route)

### CORS

The backend is configured to accept requests from:
- `https://pangea.rkund.com` (production frontend)
- `http://localhost:3001` (development)

### Error Responses

**401 Unauthorized:**
```json
{
  "statusCode": 401,
  "message": "Unauthorized"
}
```
→ Token missing, invalid, or expired. Login again.

**404 Not Found:**
```json
{
  "statusCode": 404,
  "message": "Not Found"
}
```
→ Endpoint doesn't exist or resource not found.

**400 Bad Request:**
```json
{
  "statusCode": 400,
  "message": ["validation error 1", "validation error 2"]
}
```
→ Request validation failed. Check request body/parameters.

---

## Quick Start Example

```bash
# 1. Register
curl -X POST https://pangea-production-128d.up.railway.app/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"test123456","username":"testuser","firstName":"Test","lastName":"User"}'

# 2. Copy the token from response, then use it:
export TOKEN="your-jwt-token-here"

# 3. Get profile
curl -X GET https://pangea-production-128d.up.railway.app/api/user/profile \
  -H "Authorization: Bearer $TOKEN"

# 4. Browse assets (no auth needed)
curl https://pangea-production-128d.up.railway.app/api/assets
```

---

## Frontend Integration Example (JavaScript/TypeScript)

```typescript
const API_BASE_URL = 'https://pangea-production-128d.up.railway.app/api';

// Login
async function login(email: string, password: string) {
  const response = await fetch(`${API_BASE_URL}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });
  const data = await response.json();
  localStorage.setItem('token', data.token);
  return data;
}

// Authenticated request
async function getUserProfile() {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE_URL}/user/profile`, {
    headers: { 'Authorization': `Bearer ${token}` },
  });
  return response.json();
}

// Public request (no auth)
async function getAssets() {
  const response = await fetch(`${API_BASE_URL}/assets`);
  return response.json();
}
```

---

For more detailed endpoint documentation, see `API_ROUTES_REFERENCE.md`.




