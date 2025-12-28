# API Routes Reference - Quick Guide for Frontend

This document provides a quick reference for frontend developers on which routes can be called directly from the client vs which should go through Next.js API routes.

## Route Access Matrix

**Architecture Decision: Read from client directly, write/modify through Next.js API routes**

| Route | Auth Required | Role Required | Access Pattern | Notes |
|-------|---------------|---------------|----------------------|-------|
| **Authentication** |
| `POST /api/auth/register` | ❌ | - | ✅ Yes | Public endpoint |
| `POST /api/auth/login` | ❌ | - | ✅ Yes | Public endpoint |
| `POST /api/auth/forgot-password` | ❌ | - | ✅ Yes | Public endpoint |
| `POST /api/auth/reset-password` | ❌ | - | ✅ Yes | Public endpoint |
| **User** |
| `GET /api/user/profile` | ✅ JWT | - | ✅ Yes | Pass JWT token |
| `PUT /api/user/profile` | ✅ JWT | - | ✅ Yes | Pass JWT token |
| **Assets** |
| `GET /api/assets` | ❌ | - | ✅ Direct | Read - public, call directly |
| `GET /api/assets/:assetId` | ❌ | - | ✅ Direct | Read - public, call directly |
| `GET /api/assets/:assetId/orders` | ❌ | - | ✅ Direct | Read - public, call directly |
| `GET /api/assets/:assetId/trades` | ❌ | - | ✅ Direct | Read - public, call directly |
| `GET /api/assets/:assetId/price` | ❌ | - | ✅ Direct | Read - public, call directly |
| `GET /api/assets/search` | ❌ | - | ✅ Direct | Read - public, call directly |
| `POST /api/assets` | ✅ JWT | Admin | 🔄 Via Next.js | Write - admin only, proxy through Next.js |
| `PUT /api/assets/:assetId` | ✅ JWT | Admin | 🔄 Via Next.js | Write - admin only, proxy through Next.js |
| `DELETE /api/assets/:assetId` | ✅ JWT | Admin | 🔄 Via Next.js | Write - admin only, proxy through Next.js |
| **Orders** |
| `GET /api/orders` | ✅ JWT | - | ✅ Direct | Read - call directly with JWT |
| `GET /api/orders/open` | ✅ JWT | - | ✅ Direct | Read - call directly with JWT |
| `GET /api/orders/history` | ✅ JWT | - | ✅ Direct | Read - call directly with JWT |
| `GET /api/orders/:orderId` | ✅ JWT | - | ✅ Direct | Read - call directly with JWT |
| `POST /api/orders` | ✅ JWT | - | 🔄 Via Next.js | Write - proxy through Next.js |
| `PUT /api/orders/:orderId` | ✅ JWT | - | 🔄 Via Next.js | Write - proxy through Next.js |
| `DELETE /api/orders/:orderId` | ✅ JWT | - | 🔄 Via Next.js | Write - proxy through Next.js |
| **Trades** |
| `GET /api/trades` | ✅ JWT | - | ✅ Yes | User's trades |
| `GET /api/trades/history` | ✅ JWT | - | ✅ Yes | User's trade history |
| `GET /api/trades/statistics` | ✅ JWT | - | ✅ Yes | User's trade stats |
| `GET /api/trades/:tradeId` | ✅ JWT | - | ✅ Yes | Trade details |
| **Wallet** |
| `GET /api/wallet/balance` | ✅ JWT | - | ✅ Direct | Read - call directly with JWT |
| `GET /api/wallet/balance/:assetId` | ✅ JWT | - | ✅ Direct | Read - call directly with JWT |
| `POST /api/wallet/connect` | ✅ JWT | - | 🔄 Via Next.js | Write - proxy through Next.js |
| `POST /api/wallet/verify` | ✅ JWT | - | 🔄 Via Next.js | Write - proxy through Next.js |
| `POST /api/wallet/sync` | ✅ JWT | - | 🔄 Via Next.js | Write - proxy through Next.js |
| **Transactions** |
| `GET /api/transactions` | ✅ JWT | - | ✅ Direct | Read - call directly with JWT |
| `GET /api/transactions/:txId` | ✅ JWT | - | ✅ Direct | Read - call directly with JWT |
| `POST /api/transactions/deposit` | ✅ JWT | - | 🔄 Via Next.js | Write - proxy through Next.js |
| `POST /api/transactions/withdraw` | ✅ JWT | - | 🔄 Via Next.js | Write - proxy through Next.js |
| `POST /api/transactions/:txId/confirm` | ✅ JWT | Admin | 🔄 Via Next.js | Write - admin only, proxy through Next.js |
| **Admin** |
| All `/api/admin/*` | ✅ JWT | Admin | 🔄 Via Next.js | All admin operations - proxy through Next.js |
| **Smart Contracts** |
| All `/api/smart-contracts/*` | ✅ JWT | Admin | 🔄 Via Next.js | All admin operations - proxy through Next.js |

## Key

- ✅ **Direct**: Call directly from client (reads, public endpoints)
- 🔄 **Via Next.js**: Proxy through Next.js API route (writes, admin operations)
- ❌ **No**: Should not be called from client

## Architecture Pattern

**Read Operations (GET)**: Call directly from client with JWT token  
**Write Operations (POST/PUT/DELETE)**: Proxy through Next.js API routes for additional security and audit trail

## Authentication

For protected routes, include JWT token in request header:

```typescript
headers: {
  'Authorization': `Bearer ${jwtToken}`
}
```

## Rate Limiting (To Be Implemented)

Once rate limiting is implemented, expect these limits:
- Auth endpoints: 5 requests/min per IP
- Public asset endpoints: 60 requests/min per IP
- Authenticated endpoints: 100 requests/min per user

Rate limit headers will be included in responses:
- `X-RateLimit-Limit`: Request limit
- `X-RateLimit-Remaining`: Remaining requests
- `X-RateLimit-Reset`: Reset time








