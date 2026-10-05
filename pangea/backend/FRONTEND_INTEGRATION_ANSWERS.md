# Answers to Frontend Integration Questions

## Question #1: Internal Service vs Public Endpoints?

**Answer: Hybrid Approach (Both)**

The backend supports **both internal service calls and direct client calls**:

✅ **Public Endpoints** (callable directly from client, no auth):
- Authentication: `/api/auth/register`, `/api/auth/login`, `/api/auth/forgot-password`, `/api/auth/reset-password`
- Asset Browsing: All `GET /api/assets/*` endpoints (list, search, details, order book, prices, trade history)

✅ **Protected Endpoints** (callable directly from client with JWT token):
- User profile: `/api/user/*`
- Orders: `/api/orders/*` (all endpoints)
- Trades: `/api/trades/*` (all endpoints)
- Wallet: `/api/wallet/*` (all endpoints)
- Transactions: `/api/transactions/*` (except admin operations)

⚠️ **Internal/Admin Endpoints** (should go through Next.js API routes):
- `/api/admin/*` (all admin operations)
- `/api/smart-contracts/*` (admin only)
- Asset management (POST/PUT/DELETE `/api/assets/*`) (admin only)

**CORS is already configured** in the backend to accept requests from your frontend origin.

---

## Question #2: Rate Limiting

**Answer: Not Currently Implemented - Needs to be Added ASAP**

✅ **Recommendation: Use `@nestjs/throttler`**

This is the right choice for NestJS. Here's what needs to happen:

1. **Install package**: `npm install @nestjs/throttler`
2. **Configure with Redis** (you already have Redis set up):
   - Use Redis storage for distributed rate limiting
   - Different limits for different route types

3. **Suggested Rate Limits**:
   - **Auth endpoints**: 5 requests/min per IP (prevent brute force)
   - **Public asset endpoints**: 60 requests/min per IP (browsing)
   - **Authenticated endpoints**: 100 requests/min per user (normal usage)

4. **Implementation**: Can be applied globally with per-route overrides using `@Throttle()` decorator

**Priority: HIGH** - This should be implemented before staging/production.

---

## Question #3: JWT Strategy & Client-Accessible Routes

**Recommended Approach: Read from Client, Write/Modify from Server-Side**

This is a **solid architectural decision** for a financial/trading platform. Here's why:

### Recommended Architecture

✅ **Read Operations** → Call directly from client
- `GET /api/assets/*` (public asset browsing)
- `GET /api/orders/*` (user's orders)
- `GET /api/trades/*` (user's trades)
- `GET /api/user/profile` (user profile)
- `GET /api/wallet/balance` (wallet balances)
- `GET /api/transactions` (transaction history)

✅ **Write/Modify Operations** → Proxy through Next.js API routes
- `POST /api/orders` (create order)
- `PUT /api/orders/:orderId` (update order)
- `DELETE /api/orders/:orderId` (cancel order)
- `POST /api/transactions/deposit` (deposit)
- `POST /api/transactions/withdraw` (withdrawal)
- `PUT /api/user/profile` (update profile)
- `POST /api/wallet/connect` (connect wallet)

### Benefits of This Approach

1. **Defense in Depth**: Extra security layer for critical write operations
2. **Better Audit Trail**: All writes logged at Next.js level before hitting backend
3. **Additional Validation**: Can add business logic/validation in Next.js before backend
4. **Rate Limiting**: Stricter rate limits on writes (e.g., 10 writes/min per user)
5. **Fraud Prevention**: Can add fraud detection checks at Next.js layer
6. **Consistency**: All writes go through one path, easier to monitor and secure
7. **Performance**: Reads are faster (direct calls), writes have security overhead (acceptable)

### Implementation Pattern

**Read operations (direct from client):**
```typescript
// Client-side code
const orders = await fetch('https://api.pangea.com/api/orders', {
  headers: { 'Authorization': `Bearer ${jwtToken}` }
});
```

**Write operations (via Next.js API route):**
```typescript
// Client-side code
const response = await fetch('/api/orders', {  // Next.js API route
  method: 'POST',
  body: JSON.stringify(orderData)
});

// Next.js API route: /app/api/orders/route.ts
export async function POST(request: Request) {
  const session = await getServerSession(); // Validate session
  const orderData = await request.json();
  
  // Additional validation/business logic here if needed
  
  // Forward to backend
  return fetch('https://api.pangea.com/api/orders', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${session.jwtToken}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(orderData)
  });
}
```

### Note on Backend Security

The backend is already secure (JWT validation, user-scoping, validation). This approach adds an **extra layer** rather than fixing a security issue. It's a "best practice" for financial applications where write operations are critical.

---

## Summary & Next Steps

### Immediate Actions Needed:
1. ✅ **Implement rate limiting** using `@nestjs/throttler` with Redis
2. ✅ **Test rate limiting** in development before staging
3. ✅ **Document** which routes are public vs protected (see `API_ROUTES_REFERENCE.md`)

### Architecture Decision:
- **Hybrid approach**: Public endpoints for browsing, protected endpoints for user actions ✅
- **JWT authentication**: Works well for client-side calls ✅
- **Route protection**: Already properly implemented with user-scoping ✅

### Files Created:
- `ARCHITECTURE_SECURITY_RECOMMENDATIONS.md` - Detailed recommendations
- `API_ROUTES_REFERENCE.md` - Complete route matrix with access patterns
- This file - Quick answers to your questions

---

## Code Example for Frontend

```typescript
// Example: Calling protected endpoint from client
const response = await fetch('https://api.pangea.com/api/orders', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'Content-Type': 'application/json'
  }
});

// Example: Calling public endpoint (no auth needed)
const assetsResponse = await fetch('https://api.pangea.com/api/assets', {
  method: 'GET',
  headers: {
    'Content-Type': 'application/json'
  }
});
```








