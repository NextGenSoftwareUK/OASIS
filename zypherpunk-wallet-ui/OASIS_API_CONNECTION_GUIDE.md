# OASIS API Connection Guide - Zypherpunk Wallet UI

**Date:** December 22, 2025  
**Status:** Documentation for connecting to OASIS API

---

## Overview

This guide explains how to connect **zypherpunk-wallet-ui** to the OASIS API. The UI uses Next.js API proxy routes to handle CORS and authentication, while making direct API calls to OASIS endpoints.

---

## Architecture

### Connection Pattern

```
Frontend (React/Next.js)
    ↓
Next.js API Proxy Routes (/api/proxy/*)
    ↓
OASIS API (http://api.oasisweb4.com)
```

### Architecture Overview

- **Platform:** Next.js (React) with TypeScript
- **Authentication:** User-side authentication with OASIS API
- **Token Storage:** Browser localStorage (via Zustand persist)
- **CORS Handling:** Next.js proxy routes
- **API Calls:** Next.js API routes proxy to OASIS API

---

## Configuration

### Environment Variables

Create a `.env.local` file in the project root:

```bash
# OASIS API Configuration
NEXT_PUBLIC_OASIS_API_URL=http://api.oasisweb4.com

# Optional: Force proxy usage (default: uses proxy in development)
NEXT_PUBLIC_USE_API_PROXY=true

# Node environment
NODE_ENV=production
```

### Current Defaults

If not set, the app defaults to:
- `NEXT_PUBLIC_OASIS_API_URL`: `http://localhost:5004` (fallback)
- Proxy usage: `true` in development, `false` in production

**Important:** Update to use `http://api.oasisweb4.com` for production.

---

## Authentication Flow

### User Registration/Login

The UI uses **user credentials** to authenticate directly with OASIS API.

#### 1. User Login

**Flow:**
```
Frontend Component
    ↓
useAvatarStore.login(username, password)
    ↓
avatarAPI.login(username, password)
    ↓
POST /api/authenticate (Next.js proxy route)
    ↓
POST http://api.oasisweb4.com/api/avatar/authenticate (OASIS API)
    ↓
Returns: { token, avatarId, avatar }
    ↓
Store in Zustand (localStorage)
    ↓
Set token in all API classes
```

**Implementation:**
- File: `lib/avatarStore.ts`
- File: `lib/avatarApi.ts`
- File: `app/api/authenticate/route.ts` (Next.js proxy)

**Code Example:**
```typescript
import { useAvatarStore } from './lib/avatarStore';

// Login
await useAvatarStore.getState().login(username, password);

// Token is automatically stored and set in all API classes
```

**Response Handling:**
```typescript
{
  token: "eyJhbGciOiJIUzI1NiIs...",  // OASIS JWT token
  avatarId: "bfbce7c2-708e-40ae-af79-1d2421037eaa",
  avatar: {
    avatarId: "...",
    username: "...",
    email: "...",
    // ... other avatar data
  }
}
```

#### 2. Token Storage

**Location:** Browser `localStorage` (via Zustand persist middleware)

**Key:** `oasis-avatar-auth`

**Structure:**
```typescript
{
  avatar: AvatarProfile,
  token: string,           // OASIS JWT token
  refreshToken: string | null,
  isAuthenticating: boolean,
  authError: string | null
}
```

**Token Propagation:**
When token is set, it's automatically propagated to all API classes:
- `oasisWalletAPI.setAuthToken(token)`
- `keysAPI.setAuthToken(token)`
- `stablecoinAPI.setAuthToken(token)`

---

## API Classes

### 1. Avatar API (`lib/avatarApi.ts`)

**Purpose:** User authentication and avatar management

**Methods:**
- `login(username, password)` - Authenticate user
- `register(payload)` - Register new user
- `getProfile(avatarId)` - Get avatar profile
- `verifyEmail(token)` - Verify email address

**Usage:**
```typescript
import { avatarAPI } from './lib/avatarApi';

// Login
const auth = await avatarAPI.login('username', 'password');
// Returns: { avatar, jwtToken, refreshToken }
```

**Base URL:** Uses `NEXT_PUBLIC_OASIS_API_URL` or defaults to `http://localhost:5004`

---

### 2. Wallet API (`lib/api.ts`)

**Purpose:** Wallet operations (load, create, send, etc.)

**Methods:**
- `loadWalletsById(avatarId, providerType?)` - Get all wallets
- `loadWallet(walletId)` - Get single wallet
- `sendTransaction(request)` - Send tokens
- `getBalance(walletId)` - Get wallet balance

**Usage:**
```typescript
import { oasisWalletAPI } from './lib/api';

// Set token first (done automatically after login)
oasisWalletAPI.setAuthToken(token);

// Load wallets
const wallets = await oasisWalletAPI.loadWalletsById(avatarId);
```

**Endpoints Used:**
- `GET /api/wallet/avatar/{id}/wallets` - Get wallets
- `POST /api/wallet/send_token` - Send tokens
- `GET /api/wallet/{walletId}/balance` - Get balance

---

### 3. Keys API (`lib/keysApi.ts`)

**Purpose:** Keypair generation and wallet creation

**Methods:**
- `generateKeypair(avatarId, providerType)` - Generate keypair
- `linkPrivateKey(avatarId, providerType, privateKey)` - Link private key (creates wallet)
- `linkPublicKey(avatarId, providerType, publicKey, walletAddress)` - Link public key
- `linkKeys(request)` - Combined operation

**Usage:**
```typescript
import { keysAPI } from './lib/keysApi';

// Generate keypair
const keypair = await keysAPI.generateKeypair(avatarId, 'SolanaOASIS');

// Link keys to create wallet
const wallet = await keysAPI.linkKeys({
  avatarId,
  providerType: 'SolanaOASIS',
  privateKey: keypair.result.privateKey,
  publicKey: keypair.result.publicKey,
  walletAddress: keypair.result.walletAddress
});
```

**Endpoints Used:**
- `POST /api/keys/generate_keypair_for_provider/{providerType}`
- `POST /api/keys/link_provider_private_key_to_avatar_by_id`
- `POST /api/keys/link_provider_public_key_to_avatar_by_id`

---

## Proxy Routes

### Purpose

Next.js API proxy routes handle:
1. **CORS issues** - Bypass browser CORS restrictions
2. **SSL certificate issues** - Handle self-signed certificates
3. **Request forwarding** - Forward requests to OASIS API

### Proxy Routes

#### 1. Authentication Proxy (`app/api/authenticate/route.ts`)

**Purpose:** Handle user authentication with OASIS API

**Endpoint:** `POST /api/authenticate`

**Request:**
```typescript
POST /api/authenticate
Content-Type: application/json

{
  "username": "johndoe",
  "password": "password123",
  "baseUrl": "http://api.oasisweb4.com"  // Optional
}
```

**Response:**
```typescript
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "avatarId": "bfbce7c2-708e-40ae-af79-1d2421037eaa",
  "avatar": { /* avatar data */ },
  "message": "Authenticated"
}
```

**Implementation Notes:**
- Uses `curl` command to handle SSL certificate issues
- Extracts token from nested OASIS response structure (`.result.result.jwtToken`)
- Handles both JSON parsing and regex extraction as fallback

---

#### 2. Wallet API Proxy (`app/api/proxy/wallet/[...path]/route.ts`)

**Purpose:** Proxy all wallet API requests

**Endpoint:** `/api/proxy/wallet/*`

**Example:**
```
GET /api/proxy/wallet/avatar/{id}/wallets
→ Forwards to: http://api.oasisweb4.com/api/wallet/avatar/{id}/wallets
```

**Implementation:**
- Forwards all HTTP methods (GET, POST, PUT, DELETE)
- Preserves headers (especially `Authorization`)
- Returns response as-is from OASIS API

---

#### 3. Keys API Proxy (`app/api/proxy/[...path]/route.ts`)

**Purpose:** Proxy all other API requests (Keys, etc.)

**Endpoint:** `/api/proxy/api/keys/*`

**Example:**
```
POST /api/proxy/api/keys/generate_keypair_for_provider/SolanaOASIS
→ Forwards to: http://api.oasisweb4.com/api/keys/generate_keypair_for_provider/SolanaOASIS
```

---

## Setup Instructions

### Step 1: Configure Environment Variables

Create `.env.local` file:

```bash
# OASIS API URL
NEXT_PUBLIC_OASIS_API_URL=http://api.oasisweb4.com

# Use proxy for all requests (recommended)
NEXT_PUBLIC_USE_API_PROXY=true

# Node environment
NODE_ENV=production
```

### Step 2: Update API Base URL in Code (if needed)

Check these files use the environment variable correctly:
- `lib/config.ts` - Should use `process.env.NEXT_PUBLIC_OASIS_API_URL`
- `lib/api.ts` - Should use `process.env.NEXT_PUBLIC_OASIS_API_URL`
- `lib/keysApi.ts` - Should use `process.env.NEXT_PUBLIC_OASIS_API_URL`
- `lib/avatarApi.ts` - Should use `process.env.NEXT_PUBLIC_OASIS_API_URL`

### Step 3: Verify Proxy Routes

Ensure proxy routes are configured:
- ✅ `app/api/authenticate/route.ts` exists
- ✅ `app/api/proxy/wallet/[...path]/route.ts` exists
- ✅ `app/api/proxy/[...path]/route.ts` exists

### Step 4: Test Connection

**Test Authentication:**
```typescript
// In browser console or component
import { useAvatarStore } from './lib/avatarStore';

const store = useAvatarStore.getState();
await store.login('OASIS_ADMIN', 'Uppermall1!');
console.log('Logged in:', store.avatar, store.token);
```

**Test Wallet Loading:**
```typescript
import { oasisWalletAPI } from './lib/api';

const wallets = await oasisWalletAPI.loadWalletsById(avatarId);
console.log('Wallets:', wallets);
```

---

## Authentication Token Management

### Token Storage

**Location:** Browser `localStorage`
**Key:** `oasis-avatar-auth`
**Format:** Zustand persisted state

### Token Lifecycle

1. **Login:** Token received from OASIS API
2. **Storage:** Saved to localStorage via Zustand
3. **Hydration:** Token restored on page load
4. **Usage:** Token included in all API requests
5. **Expiration:** OASIS tokens expire in 15 minutes

### Token Propagation

After login, token is automatically set in all API classes:

```typescript
// In avatarStore.ts - applyAuthState()
oasisWalletAPI.setAuthToken(jwtToken);
keysAPI.setAuthToken(jwtToken);
stablecoinAPI.setAuthToken(jwtToken);
```

### Token Refresh

**Current Implementation:** ❌ No automatic refresh

**Recommendation:** Implement token refresh:
1. Check token expiration before API calls
2. Refresh token if expiring soon
3. Update stored token

**Example Implementation:**
```typescript
// In avatarApi.ts or avatarStore.ts
async refreshTokenIfNeeded() {
  const state = useAvatarStore.getState();
  if (!state.token) return;
  
  // Decode JWT to check expiration
  const payload = JSON.parse(atob(state.token.split('.')[1]));
  const expiresAt = payload.exp * 1000;
  const timeUntilExpiry = expiresAt - Date.now();
  
  // Refresh if expiring in next 5 minutes
  if (timeUntilExpiry < 5 * 60 * 1000) {
    // Re-authenticate to get new token
    // (OASIS doesn't have refresh token endpoint)
  }
}
```

---

## API Endpoints Reference

### Avatar API Endpoints

**Base URL:** `http://api.oasisweb4.com`

**Authentication:**
- `POST /api/avatar/authenticate` - Login user
- `POST /api/avatar/register` - Register new user

**Profile:**
- `GET /api/avatar/{id}` - Get avatar profile
- `PUT /api/avatar/{id}` - Update avatar profile

---

### Wallet API Endpoints

**Get Wallets:**
- `GET /api/wallet/avatar/{id}/wallets/false/false` - Get all wallets
- `GET /api/wallet/avatar/{id}/wallets/false/false?providerType={type}` - Get wallets by provider

**Wallet Operations:**
- `GET /api/wallet/{walletId}/balance` - Get wallet balance
- `POST /api/wallet/send_token` - Send tokens

**Default Wallet:**
- `GET /api/wallet/avatar/{id}/default-wallet` - Get default wallet
- `POST /api/wallet/avatar/{id}/default-wallet/{walletId}?providerType={type}` - Set default wallet

---

### Keys API Endpoints

**Keypair Generation:**
- `POST /api/keys/generate_keypair_for_provider/{providerType}` - Generate keypair

**Key Linking:**
- `POST /api/keys/link_provider_private_key_to_avatar_by_id` - Link private key (creates wallet)
- `POST /api/keys/link_provider_public_key_to_avatar_by_id` - Link public key

---

## Response Format Handling

### OASIS API Response Structure

OASIS API returns nested response structures:

```typescript
{
  "result": {
    "result": {
      "jwtToken": "...",
      "avatarId": "...",
      // ... actual data
    },
    "isError": false,
    "message": "Success"
  }
}
```

**Handling:**
All API classes should check multiple paths:
- `data.result.result.*` (most common)
- `data.result.*`
- `data.*`

**Example:**
```typescript
const token = 
  data?.result?.result?.jwtToken ||
  data?.result?.jwtToken ||
  data?.jwtToken;
```

---

## Error Handling

### Common Errors

**401 Unauthorized:**
- Token expired or invalid
- **Solution:** Re-authenticate user

**404 Not Found:**
- Resource doesn't exist (wallet, avatar, etc.)
- **Solution:** Check if resource exists before accessing

**405 Method Not Allowed:**
- Usually indicates authentication failure
- **Solution:** Verify token is being sent correctly

**CORS Errors:**
- Browser blocking cross-origin requests
- **Solution:** Use proxy routes (already implemented)

### Error Response Format

```typescript
{
  "isError": true,
  "message": "Error description",
  "detailedMessage": "Full error details"
}
```

---

## Testing

### Test Authentication

```bash
# Via Next.js API route (recommended)
curl -X POST http://localhost:3001/api/authenticate \
  -H "Content-Type: application/json" \
  -d '{"username":"OASIS_ADMIN","password":"Uppermall1!"}'
```

### Test Direct OASIS API

```bash
# Direct to OASIS API (for debugging)
curl -X POST http://api.oasisweb4.com/api/avatar/authenticate \
  -H "Content-Type: application/json" \
  -d '{"username":"OASIS_ADMIN","password":"Uppermall1!"}'
```

### Test Wallet Loading

```typescript
// In browser console (after login)
import { oasisWalletAPI } from './lib/api';
import { useAvatarStore } from './lib/avatarStore';

const store = useAvatarStore.getState();
const wallets = await oasisWalletAPI.loadWalletsById(store.avatar?.avatarId);
console.log('Wallets:', wallets);
```

---

## Troubleshooting

### Issue: CORS Errors

**Symptom:** Browser console shows CORS errors

**Solution:**
- Ensure `NEXT_PUBLIC_USE_API_PROXY=true` is set
- Verify proxy routes are working
- Check that requests go through `/api/proxy/*` routes

---

### Issue: Authentication Fails

**Symptom:** Login returns error or no token

**Solutions:**
1. **Check credentials:** Verify username/password are correct
2. **Check API URL:** Ensure `NEXT_PUBLIC_OASIS_API_URL=http://api.oasisweb4.com`
3. **Check proxy route:** Verify `/api/authenticate` route is working
4. **Check response parsing:** Look at browser console for response structure
5. **Test direct API:** Try calling OASIS API directly to verify it's working

---

### Issue: Token Not Persisting

**Symptom:** User logged out after page refresh

**Solutions:**
1. **Check localStorage:** Open browser DevTools → Application → Local Storage
2. **Verify Zustand persist:** Check if `oasis-avatar-auth` key exists
3. **Check hydration:** Verify token is restored on page load (check console logs)

---

### Issue: API Calls Fail with 401

**Symptom:** Wallet/Keys API calls return 401 Unauthorized

**Solutions:**
1. **Verify token is set:** Check if token exists in API classes
2. **Check token expiration:** OASIS tokens expire in 15 minutes
3. **Re-authenticate:** User needs to login again
4. **Check Authorization header:** Verify token is being sent in requests

---

### Issue: HTML Response Instead of JSON

**Symptom:** API returns HTML (bot protection page)

**Solutions:**
1. **Use proxy routes:** Proxy routes handle this better
2. **Check headers:** Ensure proper `Content-Type` and `Accept` headers
3. **Use authentication:** Authenticated requests are less likely to be blocked

---

## Best Practices

1. **Always use proxy routes** in development to avoid CORS
2. **Store tokens securely** in localStorage (already using Zustand persist)
3. **Handle token expiration** - implement token refresh or re-login flow
4. **Check response structure** - OASIS returns nested structures
5. **Handle errors gracefully** - Show user-friendly error messages
6. **Log API calls** - Use console.log for debugging (remove in production)

---

## Migration from Local API

If currently using local OASIS API (`http://localhost:5000` or `http://localhost:5004`):

1. **Update environment variable:**
   ```bash
   NEXT_PUBLIC_OASIS_API_URL=http://api.oasisweb4.com
   ```

2. **Test authentication:**
   - Try logging in with existing credentials
   - Verify token is received

3. **Test wallet operations:**
   - Load wallets
   - Generate new wallet
   - Check balances

4. **Update any hardcoded URLs** in code to use environment variable

---

## Next Steps

1. ✅ **Configure environment variables** (`.env.local`)
2. ✅ **Test authentication** (login flow)
3. ✅ **Test wallet operations** (load wallets, generate wallet)
4. ⚠️ **Implement token refresh** (optional but recommended)
5. ⚠️ **Add error handling UI** (show errors to users)
6. ⚠️ **Add loading states** (better UX during API calls)

---

## Additional Resources

- **OASIS API Documentation:** Check OASIS project documentation
- **Current Setup Guide:** `API_SETUP.md` (in zypherpunk-wallet-ui)

---

**Last Updated:** December 22, 2025  
**Status:** Ready for Implementation

