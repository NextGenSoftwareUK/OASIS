# Avatar and Wallet Generation Process - OASIS Integration

**Date:** December 22, 2025  
**Status:** ✅ Fully Implemented and Working

---

## Overview

Pangea Markets uses the OASIS platform for user authentication (Avatar management) and wallet generation. This document explains the complete flow from user registration to wallet creation.

---

## Architecture

### Hybrid Approach

**OASIS Platform:**
- Avatar management (user accounts)
- Wallet generation and key management
- Blockchain integration (Solana, Ethereum, etc.)

**Pangea Backend:**
- Local user database (linked to OASIS by `avatarId`)
- Pangea JWT token generation
- Business logic and API endpoints

**Pattern:** "Read from client, write/modify from server-side"

---

## Part 1: Avatar Creation (User Registration)

### Flow Diagram

```
User Registration Request
    ↓
POST /api/auth/register
    ↓
1. OASIS Avatar API → Create Avatar
    ↓
2. Extract avatarId from OASIS response
    ↓
3. Sync to Local Database → Create User record
    ↓
4. Generate Pangea JWT Token
    ↓
Return: { user, token, expiresAt }
```

### Step-by-Step Process

#### Step 1: Register with OASIS Avatar API

**Endpoint:** `POST /api/avatar/register` (OASIS API)

**Request:**
```typescript
POST http://api.oasisweb4.com/api/avatar/register
Content-Type: application/json

{
  "username": "johndoe",
  "email": "john@example.com",
  "password": "securepassword123",
  "confirmPassword": "securepassword123",  // Must match password
  "firstName": "John",
  "lastName": "Doe",
  "avatarType": "User",
  "acceptTerms": true
}
```

**Response Structure:**
```typescript
{
  "result": {
    "result": {
      "avatarId": "bfbce7c2-708e-40ae-af79-1d2421037eaa",  // Unique avatar ID
      "jwtToken": "eyJhbGciOiJIUzI1NiIs...",               // OASIS JWT token
      "email": "john@example.com",
      "username": "johndoe",
      "firstName": "John",
      "lastName": "Doe",
      // ... other avatar data
    },
    "isError": false,
    "message": "Avatar Successfully Created"
  }
}
```

**Key Points:**
- OASIS creates a unique `avatarId` (UUID)
- Avatar is stored in OASIS platform
- Email verification may be sent (if configured)
- Returns OASIS JWT token (used for OASIS API calls, not Pangea API)

**Implementation:**
- File: `src/auth/services/oasis-auth.service.ts`
- Method: `register()`
- Handles nested response structure (`.result.result`)

---

#### Step 2: Sync Avatar to Local Database

**Purpose:** Create a local user record linked to OASIS Avatar

**Process:**
1. Extract `avatarId` from OASIS response
2. Check if user already exists (by `avatarId` or `email`)
3. Create or update user record in local database
4. Link user to OASIS via `avatarId` field

**User Record:**
```typescript
{
  id: "local-uuid",                    // Pangea user ID
  email: "john@example.com",
  username: "johndoe",
  avatarId: "bfbce7c2-708e-40ae-af79-1d2421037eaa",  // Link to OASIS
  role: "user",                        // Default role
  kycStatus: "pending",
  isActive: true,
  // ... other fields
}
```

**Implementation:**
- File: `src/auth/services/user-sync.service.ts`
- Method: `syncOasisUserToLocal()`
- Creates or updates user record

---

#### Step 3: Generate Pangea JWT Token

**Purpose:** Generate our own JWT token for Pangea API authentication

**Token Payload:**
```typescript
{
  sub: "local-uuid",                   // Pangea user ID
  email: "john@example.com",
  username: "johndoe",
  avatarId: "bfbce7c2-708e-40ae-af79-1d2421037eaa",
  role: "user",
  iat: 1734892800,                     // Issued at
  exp: 1735497600                      // Expires in 7 days
}
```

**Key Points:**
- Token expires in 7 days (configurable via `JWT_EXPIRES_IN`)
- Token contains both Pangea `id` and OASIS `avatarId`
- Used for all Pangea API authentication
- **Not the same as OASIS JWT token**

**Implementation:**
- File: `src/auth/services/auth.service.ts`
- Method: `generateJwtToken()`

---

### Final Registration Response

```typescript
POST /api/auth/register
Response: {
  user: {
    id: "local-uuid",
    email: "john@example.com",
    username: "johndoe",
    avatarId: "bfbce7c2-708e-40ae-af79-1d2421037eaa",
    role: "user"
  },
  token: "eyJhbGciOiJIUzI1NiIs...",     // Pangea JWT token
  expiresAt: "2025-12-29T20:00:00Z"
}
```

**Client Actions:**
- Save token: `localStorage.setItem('token', token)`
- Use token in requests: `Authorization: Bearer <token>`
- User can now make authenticated API calls

---

## Part 2: Wallet Generation

### Flow Diagram

```
Wallet Generation Request
    ↓
POST /api/wallet/generate
Headers: Authorization: Bearer <pangea-jwt-token>
Body: { providerType: "SolanaOASIS", setAsDefault: true }
    ↓
1. Extract avatarId from JWT token
    ↓
2. OASIS Keys API → Generate Keypair
    ↓
3. OASIS Keys API → Link Private Key (creates wallet)
    ↓
4. OASIS Keys API → Link Public Key (completes wallet)
    ↓
5. OASIS Wallet API → Set as Default (optional)
    ↓
6. Return wallet details
```

### Step-by-Step Process

#### Step 1: Generate Keypair

**Endpoint:** `POST /api/keys/generate_keypair_for_provider/{providerType}` (OASIS API)

**Request:**
```typescript
POST http://api.oasisweb4.com/api/keys/generate_keypair_for_provider/SolanaOASIS
Headers: Authorization: Bearer <oasis-jwt-token>

// No body required - provider type is in URL
```

**Response:**
```typescript
{
  "result": {
    "privateKey": "25wP9beHR32ZokknEZZ28zmX1KXLzPePE8wCa9HX5cQyboN6yM77GwMCzZQkTyr3udeGydTxBEQuga1gmn88Ru7H",
    "publicKey": "7ZNz58jwxCmcfXNu1ZXDPAXR174L3RGjwKj1ifPHFeLT",
    "walletAddress": "7ZNz58jwxCmcfXNu1ZXDPAXR174L3RGjwKj1ifPHFeLT"  // For Solana, same as public key
  },
  "isError": false
}
```

**Supported Provider Types:**
- `SolanaOASIS` - Solana blockchain
- `EthereumOASIS` - Ethereum/EVM chains
- `PolygonOASIS` - Polygon
- `ArbitrumOASIS` - Arbitrum
- `ZcashOASIS` - Zcash

**Key Points:**
- Keys are generated securely by OASIS
- Private key is returned only once (must be stored securely)
- Public key and wallet address are used for wallet operations

---

#### Step 2: Link Private Key (Creates Wallet)

**Endpoint:** `POST /api/keys/link_provider_private_key_to_avatar_by_id` (OASIS API)

**Request:**
```typescript
POST http://api.oasisweb4.com/api/keys/link_provider_private_key_to_avatar_by_id
Headers: Authorization: Bearer <oasis-jwt-token>
Content-Type: application/json

{
  "AvatarID": "bfbce7c2-708e-40ae-af79-1d2421037eaa",
  "ProviderType": "SolanaOASIS",
  "ProviderKey": "25wP9beHR32ZokknEZZ28zmX1KXLzPePE8wCa9HX5cQyboN6yM77GwMCzZQkTyr3udeGydTxBEQuga1gmn88Ru7H"
  // WalletId omitted - creates new wallet
}
```

**Response:**
```typescript
{
  "result": {
    "walletId": "2da2379c-69e1-41b0-8a9a-9029a28dcb20",  // New wallet ID
    "message": "Private key was successfully linked to wallet..."
  },
  "isError": false,
  "isSaved": true
}
```

**Key Points:**
- This step **creates the wallet** in OASIS
- Returns `walletId` (UUID) - save this!
- Wallet is now linked to the avatar
- Private key is stored securely by OASIS

---

#### Step 3: Link Public Key (Completes Wallet Setup)

**Endpoint:** `POST /api/keys/link_provider_public_key_to_avatar_by_id` (OASIS API)

**Request:**
```typescript
POST http://api.oasisweb4.com/api/keys/link_provider_public_key_to_avatar_by_id
Headers: Authorization: Bearer <oasis-jwt-token>
Content-Type: application/json

{
  "WalletId": "2da2379c-69e1-41b0-8a9a-9029a28dcb20",
  "AvatarID": "bfbce7c2-708e-40ae-af79-1d2421037eaa",
  "ProviderType": "SolanaOASIS",
  "ProviderKey": "7ZNz58jwxCmcfXNu1ZXDPAXR174L3RGjwKj1ifPHFeLT",
  "WalletAddress": "7ZNz58jwxCmcfXNu1ZXDPAXR174L3RGjwKj1ifPHFeLT"
}
```

**Response:**
```typescript
{
  "result": {
    "walletId": "2da2379c-69e1-41b0-8a9a-9029a28dcb20",
    "message": "Public key linked successfully"
  },
  "isError": false
}
```

**Key Points:**
- Completes the wallet setup
- Links public key to the wallet
- Wallet is now fully functional
- Can receive/send transactions

---

#### Step 4: Set as Default Wallet (Optional)

**Endpoint:** `POST /api/wallet/avatar/{avatarId}/default-wallet/{walletId}?providerType={providerType}` (OASIS API)

**Request:**
```typescript
POST http://api.oasisweb4.com/api/wallet/avatar/bfbce7c2-708e-40ae-af79-1d2421037eaa/default-wallet/2da2379c-69e1-41b0-8a9a-9029a28dcb20?providerType=SolanaOASIS
Headers: Authorization: Bearer <oasis-jwt-token>
```

**Key Points:**
- Sets this wallet as the default for the avatar
- Useful when avatar has multiple wallets
- Only one default wallet per provider type

---

#### Step 5: Return Wallet Details

**Option A: Fetch Complete Details** (Preferred)
```typescript
GET /api/wallet/avatar/{avatarId}/wallets/false/false?providerType={providerType}
// Returns complete wallet object with all details
```

**Option B: Return Creation Data** (Fallback)
```typescript
// If fetching fails, return data from creation steps
{
  walletId: "2da2379c-69e1-41b0-8a9a-9029a28dcb20",
  avatarId: "bfbce7c2-708e-40ae-af79-1d2421037eaa",
  publicKey: "7ZNz58jwxCmcfXNu1ZXDPAXR174L3RGjwKj1ifPHFeLT",
  walletAddress: "7ZNz58jwxCmcfXNu1ZXDPAXR174L3RGjwKj1ifPHFeLT",
  providerType: "SolanaOASIS",
  isDefaultWallet: true,
  balance: 0
}
```

---

### Final Wallet Generation Response

```typescript
POST /api/wallet/generate
Headers: Authorization: Bearer <pangea-jwt-token>
Body: { "providerType": "SolanaOASIS", "setAsDefault": true }

Response: {
  "success": true,
  "message": "Wallet generated successfully for SolanaOASIS",
  "wallet": {
    "walletId": "2da2379c-69e1-41b0-8a9a-9029a28dcb20",
    "walletAddress": "J27ZrzuoG1rao9P74RnWEKU6M2RA8ASvPfFtZGhrG82P",
    "providerType": "SolanaOASIS",
    "isDefaultWallet": true,
    "balance": 0
  }
}
```

---

## Authentication Flow

### OASIS API Authentication

The backend uses an **admin account** to authenticate with OASIS API:

**Credentials:**
- Username: `OASIS_ADMIN` (from `OASIS_ADMIN_USERNAME` env var)
- Password: `Uppermall1!` (from `OASIS_ADMIN_PASSWORD` env var)

**Token Management:**
- File: `src/services/oasis-token-manager.service.ts`
- Automatically refreshes OASIS JWT token before expiration
- Tokens expire in 15 minutes (OASIS default)
- Token cached in Redis to reduce API calls
- Background job refreshes token every 10 minutes

**Token Extraction:**
- OASIS returns nested structure: `.result.result.jwtToken`
- Service handles multiple response formats
- Logs token extraction for debugging

---

## Data Flow

### User Registration Flow

```
Frontend
    ↓ POST /api/auth/register
Pangea Backend (AuthService)
    ↓ POST /api/avatar/register
OASIS Avatar API
    ↓ Returns avatarId
AuthService
    ↓ Sync to local DB
Local Database (User record)
    ↓ Generate JWT
Frontend
    ← Returns { user, token }
```

### Wallet Generation Flow

```
Frontend
    ↓ POST /api/wallet/generate (with Pangea JWT)
Pangea Backend (WalletController)
    ↓ Extract avatarId from JWT
OasisWalletService
    ↓ Step 1: POST /api/keys/generate_keypair_for_provider/{providerType}
OASIS Keys API
    ↓ Returns { privateKey, publicKey }
OasisWalletService
    ↓ Step 2: POST /api/keys/link_provider_private_key_to_avatar_by_id
OASIS Keys API
    ↓ Returns walletId
OasisWalletService
    ↓ Step 3: POST /api/keys/link_provider_public_key_to_avatar_by_id
OASIS Keys API
    ↓ Returns success
OasisWalletService
    ↓ Step 4: POST /api/wallet/avatar/{id}/default-wallet/{walletId}
OASIS Wallet API
    ↓ Returns success
OasisWalletService
    ↓ Step 5: GET /api/wallet/avatar/{id}/wallets/false/false
OASIS Wallet API
    ↓ Returns wallet details
Frontend
    ← Returns { success, wallet }
```

---

## Implementation Files

### Avatar/Authentication

- `src/auth/services/auth.service.ts` - Main auth service
- `src/auth/services/oasis-auth.service.ts` - OASIS Avatar API wrapper
- `src/auth/services/user-sync.service.ts` - Sync avatar to local DB
- `src/auth/controllers/auth.controller.ts` - Auth endpoints
- `src/auth/dto/register.dto.ts` - Registration DTO

### Wallet Generation

- `src/services/oasis-wallet.service.ts` - Wallet operations
- `src/services/oasis-token-manager.service.ts` - OASIS token management
- `src/wallet/wallet.controller.ts` - Wallet endpoints
- `src/wallet/dto/generate-wallet.dto.ts` - Wallet generation DTO

---

## API Endpoints

### Pangea Backend Endpoints

**Authentication:**
- `POST /api/auth/register` - Register user (creates avatar)
- `POST /api/auth/login` - Login user (authenticates avatar)

**Wallet:**
- `POST /api/wallet/generate` - Generate new wallet
- `GET /api/wallet/balance` - Get wallet balances
- `POST /api/wallet/sync` - Sync balances

### OASIS API Endpoints Used

**Avatar API:**
- `POST /api/avatar/register` - Create avatar
- `POST /api/avatar/authenticate` - Authenticate avatar

**Keys API:**
- `POST /api/keys/generate_keypair_for_provider/{providerType}` - Generate keypair
- `POST /api/keys/link_provider_private_key_to_avatar_by_id` - Link private key (creates wallet)
- `POST /api/keys/link_provider_public_key_to_avatar_by_id` - Link public key

**Wallet API:**
- `POST /api/wallet/avatar/{id}/default-wallet/{walletId}?providerType={providerType}` - Set default wallet
- `GET /api/wallet/avatar/{id}/wallets/false/false` - Get all wallets
- `GET /api/wallet/{walletId}/balance` - Get wallet balance

---

## Error Handling

### Common Errors

**Avatar Creation:**
- `400 Bad Request` - Invalid input (email format, password too short, etc.)
- `409 Conflict` - Email or username already exists
- `500 Internal Server Error` - OASIS API error

**Wallet Generation:**
- `401 Unauthorized` - Invalid or missing JWT token
- `404 Not Found` - Avatar not found
- `405 Method Not Allowed` - Authentication issue with OASIS API
- `500 Internal Server Error` - OASIS API error or keypair generation failure

### Error Response Format

```typescript
{
  "statusCode": 500,
  "message": "Failed to generate wallet: <error details>",
  "error": "Wallet generation failed"
}
```

---

## Security Considerations

### Key Management

- ✅ Private keys are generated by OASIS (secure generation)
- ✅ Private keys are stored securely by OASIS
- ✅ Private keys are never exposed to frontend
- ✅ Only public keys and wallet addresses are returned

### Token Management

- ✅ OASIS JWT tokens are managed server-side only
- ✅ Tokens auto-refresh before expiration
- ✅ Tokens cached in Redis (encrypted connection)
- ✅ Admin credentials stored as environment variables

### Authentication

- ✅ Pangea JWT tokens for API authentication (separate from OASIS)
- ✅ Avatar ID extracted from JWT (validated server-side)
- ✅ All wallet operations require valid JWT token

---

## Testing

### Test Script

```bash
cd backend
./scripts/test-wallet-generation-debug.sh
```

**What it tests:**
1. User authentication
2. Wallet generation (Solana)
3. Balance retrieval

### Manual Testing

```bash
# 1. Register user
curl -X POST "https://pangea-production-128d.up.railway.app/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password123","username":"testuser"}'

# 2. Login and get token
TOKEN=$(curl -s -X POST "https://pangea-production-128d.up.railway.app/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password123"}' | jq -r '.token')

# 3. Generate wallet
curl -X POST "https://pangea-production-128d.up.railway.app/api/wallet/generate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"providerType":"SolanaOASIS","setAsDefault":true}'

# 4. Get balances
curl -X GET "https://pangea-production-128d.up.railway.app/api/wallet/balance" \
  -H "Authorization: Bearer $TOKEN"
```

---

## Configuration

### Environment Variables

**Required:**
- `OASIS_API_URL` - OASIS API base URL (e.g., `http://api.oasisweb4.com`)
- `OASIS_ADMIN_USERNAME` - Admin username for OASIS API auth
- `OASIS_ADMIN_PASSWORD` - Admin password for OASIS API auth
- `JWT_SECRET` - Secret for Pangea JWT token generation
- `REDIS_URL` - Redis connection for token caching

**Optional:**
- `JWT_EXPIRES_IN` - JWT expiration (default: `7d`)
- `OASIS_API_KEY` - API key (if required by OASIS)

---

## Troubleshooting

### Wallet Generation Fails

**Check:**
1. OASIS API token is valid (check logs for token extraction)
2. Avatar exists (user must register/login first)
3. Provider type is correct (`SolanaOASIS`, `EthereumOASIS`, etc.)
4. OASIS API is accessible (check `OASIS_API_URL`)

**Logs to Check:**
- Token extraction logs
- Token injection logs
- OASIS API response logs
- Error messages from each step

### Balance Retrieval Returns 404

**Check:**
1. Wallet exists (verify `walletId` is correct)
2. Provider type matches wallet type
3. Endpoint format is correct: `/api/wallet/{walletId}/balance`

---

## Best Practices

1. **Always register/login first** - Avatar must exist before wallet generation
2. **Handle errors gracefully** - Wallet creation steps are idempotent
3. **Store walletId** - Needed for future wallet operations
4. **Use default wallets** - Set one wallet per provider as default
5. **Monitor token expiration** - OASIS tokens expire in 15 minutes

---

**Last Updated:** December 22, 2025  
**Status:** Implemented and Working ✅


