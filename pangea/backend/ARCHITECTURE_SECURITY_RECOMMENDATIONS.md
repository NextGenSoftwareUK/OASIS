# Architecture & Security Recommendations for Frontend Integration

## Summary of Current State

Based on the backend codebase analysis, here's the current architecture:

### Current Authentication Setup
- ✅ JWT authentication implemented using `passport-jwt`
- ✅ `@Public()` decorator available to mark routes as public
- ✅ Guards applied per-controller (not globally)
- ✅ JWT tokens are Pangea-generated (not OASIS tokens), 7-day expiration

### Current Route Protection Status

**Public Routes** (currently accessible without authentication):
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`
- `GET /api/assets` (list all assets)
- `GET /api/assets/:assetId` (asset details)
- `GET /api/assets/:assetId/orders` (order book)
- `GET /api/assets/:assetId/trades` (trade history)
- `GET /api/assets/:assetId/price` (current price)
- `GET /api/assets/search` (search assets)

**Protected Routes** (require JWT authentication):
- All `/api/user/*` endpoints
- All `/api/orders/*` endpoints
- All `/api/trades/*` endpoints
- All `/api/wallet/*` endpoints
- All `/api/transactions/*` endpoints
- All `/api/admin/*` endpoints (require admin role)
- `POST /api/assets` (create asset - admin only)
- `PUT /api/assets/:assetId` (update asset - admin only)
- `DELETE /api/assets/:assetId` (delete asset - admin only)
- All `/api/smart-contracts/*` endpoints (admin only)

---

## Answers to Frontend Questions

### #1. Will the backend be an internal service, or will there be public endpoints callable from client?

**Recommendation: Hybrid Approach (Current State is Good)**

The backend **should support both**:

1. **Public endpoints** (already implemented):
   - Asset browsing/read-only operations (list, search, details, order book, prices, trade history)
   - Authentication endpoints (register, login, password reset)
   - These are safe to call directly from the client

2. **Protected endpoints** (user-specific operations):
   - User profile, orders, trades, wallet, transactions
   - These require JWT authentication and should be called from client with JWT token

3. **Internal-only endpoints** (future consideration):
   - Admin operations
   - Currently accessible with JWT + admin role, but you may want to restrict these to internal services only

**CORS is already enabled** in `main.ts` with configurable origins, so client-side calls are supported.

### #2. Rate Limiting

**Current Status**: ❌ No rate limiting implemented

**Recommendation: Implement `@nestjs/throttler`**

You're correct that rate limiting is essential, especially for:
- Public endpoints (prevent abuse from clients)
- Authentication endpoints (prevent brute force attacks)
- Internal protection (prevent accidental flooding)

**Implementation Plan**:
```typescript
// 1. Install package
npm install @nestjs/throttler

// 2. Configure in app.module.ts with different limits:
//    - Public routes: stricter limits (e.g., 10-20 requests/min)
//    - Auth routes: very strict (e.g., 5 requests/min per IP)
//    - Authenticated routes: more lenient (e.g., 100 requests/min per user)
```

**Suggested Limits**:
- Auth endpoints (`/api/auth/*`): 5 requests/min per IP
- Public asset endpoints: 60 requests/min per IP
- Authenticated endpoints: 100 requests/min per user
- Admin endpoints: 200 requests/min per user

We can use Redis for distributed rate limiting since Redis is already set up.

### #3. JWT Strategy & Route Authorization

**Current Strategy** (already implemented):
- Next.js middleware manages session between Application → Client
- Client can call backend directly with JWT token in `Authorization: Bearer <token>` header
- Backend validates JWT and extracts user info

**Recommendation for Client-Accessible Routes**:

#### ✅ Safe for Direct Client Access (with JWT):
These routes are already properly secured and can be called directly from the client:

1. **User Profile** (`/api/user/*`)
   - `GET /api/user/profile` - Get user profile
   - `PUT /api/user/profile` - Update user profile

2. **Asset Browsing** (`/api/assets/*`) - Already public, no JWT needed
   - All GET endpoints are public (safe)

3. **Orders** (`/api/orders/*`)
   - All endpoints require JWT and are user-scoped
   - ✅ Safe for client access

4. **Trades** (`/api/trades/*`)
   - All endpoints require JWT and are user-scoped
   - ✅ Safe for client access

5. **Wallet** (`/api/wallet/*`)
   - All endpoints require JWT and are user-scoped
   - ✅ Safe for client access

6. **Transactions** (`/api/transactions/*`)
   - All endpoints require JWT and are user-scoped
   - ✅ Safe for client access

#### ⚠️ Consider Restricting (Admin Only):
These should probably remain server-side only or require admin role:

- `/api/admin/*` - All admin endpoints
- `/api/smart-contracts/*` - Smart contract deployment (admin only)
- `POST/PUT/DELETE /api/assets` - Asset management (admin only)

#### 🔄 Authorization Flow:

**Option A: Direct Client Calls (Recommended for most routes)**
```
Client → Backend API (with JWT)
├─ JWT validated by JwtAuthGuard
├─ User extracted from token
└─ Route handlers ensure user-scoped data
```

**Option B: Next.js API Route Proxy (For sensitive operations)**
```
Client → Next.js API Route → Backend API
├─ Next.js validates session
├─ Next.js adds internal service token (if needed)
└─ Backend validates
```

**Recommendation**: Use **Option A** for most routes (simpler, better performance). Use **Option B** only if you need to:
- Add additional server-side validation
- Mask sensitive implementation details
- Implement additional rate limiting at Next.js level

---

## Implementation Recommendations

### 1. Add Rate Limiting (Priority: HIGH)

Create a throttler configuration:

```typescript
// src/config/throttler.config.ts
import { ThrottlerModuleOptions } from '@nestjs/throttler';

export const throttlerConfig: ThrottlerModuleOptions = {
  throttlers: [
    {
      ttl: 60000, // 1 minute
      limit: 100, // 100 requests per minute (for authenticated users)
    },
  ],
  storage: new ThrottlerStorageRedisService(), // Use Redis for distributed rate limiting
};
```

Then configure different limits per route using `@Throttle()` decorator:
- Auth routes: `@Throttle({ default: { limit: 5, ttl: 60000 } })`
- Public routes: `@Throttle({ default: { limit: 60, ttl: 60000 } })`
- Authenticated routes: default (100/min)

### 2. Document Public vs Protected Routes

Consider adding route metadata or a route documentation file that clearly lists:
- Which routes are public
- Which routes require authentication
- Which routes require admin role

### 3. Consider API Key Strategy for Internal Services (Future)

If you want to distinguish between:
- Client requests (JWT from users)
- Internal service requests (API keys)

You could implement an API key guard for internal routes, but this is optional and not immediately necessary.

---

## Summary

1. **Architecture**: Hybrid approach is correct ✅ - Public endpoints for browsing, protected endpoints for user actions
2. **Rate Limiting**: **Implement ASAP** using `@nestjs/throttler` with Redis storage ✅
3. **JWT Strategy**: Current approach is good ✅ - Client can call most routes directly with JWT token

**Next Steps**:
1. Implement rate limiting with `@nestjs/throttler`
2. Document which routes should be called from client vs Next.js API routes
3. Consider adding request logging/monitoring for rate limit violations
4. Test rate limiting in staging before production deployment








