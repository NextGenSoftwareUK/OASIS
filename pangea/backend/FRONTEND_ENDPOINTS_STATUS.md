# Frontend Endpoints Status

**Base URL:** `https://pangea-production-128d.up.railway.app/api`

**Last Updated:** December 21, 2025

---

## ✅ Working Endpoints (Ready for Frontend)

### Authentication (Public)
- ✅ `POST /auth/register` - Register new user
- ✅ `POST /auth/login` - Login user (returns JWT token + user data)
- ✅ `POST /auth/forgot-password` - Request password reset

### User Profile
- ✅ `GET /user/profile` - Get current user profile

### Assets (Public Reads)
- ✅ `GET /assets` - List all assets

### Orders (Read)
- ✅ `GET /orders` - Get all orders for authenticated user
- ✅ `GET /orders/open` - Get open orders
- ✅ `GET /orders/history` - Get order history

### Trades (Read)
- ✅ `GET /trades` - Get all trades for authenticated user
- ✅ `GET /trades/history` - Get trade history
- ✅ `GET /trades/statistics` - Get trade statistics

### Transactions (Read)
- ✅ `GET /transactions` - Get all transactions for authenticated user

### Wallet (Partial)
- ✅ `GET /wallet/verification-message` - Get wallet verification message

---

## ⚠️ Partially Working / Needs Attention

### User Profile
- ❌ `PUT /user/profile` - Update profile (500 error - needs investigation)

### Wallet
- ❌ `GET /wallet/balance` - Get wallet balances (500 error - OASIS API issue)
- ❌ `POST /wallet/generate` - Generate wallet (500 error - in progress)
- ❌ `POST /wallet/sync` - Sync balances (500 error)
- ⏳ `POST /wallet/connect` - Not tested
- ⏳ `POST /wallet/verify` - Not tested
- ⏳ `GET /wallet/balance/:assetId` - Not tested
- ⏳ `GET /wallet/transactions/:walletId` - Not tested

### Assets
- ⏳ `GET /assets/:assetId` - Not tested (but should work)
- ⏳ `GET /assets/:assetId/orders` - Not tested
- ⏳ `GET /assets/:assetId/trades` - Not tested
- ⏳ `GET /assets/:assetId/price` - Not tested
- ⏳ `GET /assets/search` - Returns 404 (endpoint may not exist)

### Orders (Write)
- ⏳ `POST /orders` - Create order (not tested)
- ⏳ `PUT /orders/:orderId` - Update order (not tested)
- ⏳ `DELETE /orders/:orderId` - Cancel order (not tested)
- ⏳ `GET /orders/:orderId` - Get order details (not tested)
- ⏳ `GET /orders/asset/:assetId` - Get orders for asset (not tested)

### Trades
- ⏳ `GET /trades/:tradeId` - Get trade details (not tested)
- ⏳ `GET /trades/asset/:assetId` - Get trades for asset (not tested)

### Transactions (Write)
- ⏳ `POST /transactions/deposit` - Initiate deposit (not tested)
- ⏳ `POST /transactions/withdraw` - Initiate withdrawal (not tested)
- ⏳ `GET /transactions/:txId` - Get transaction details (not tested)
- ⏳ `GET /transactions/pending` - Returns 404 (endpoint may not exist)

---

## 🔒 Admin Endpoints (Require Admin Role)

All `/admin/*` endpoints require admin role - returning 403 as expected:
- `/admin/users`
- `/admin/assets`
- `/admin/orders`
- `/admin/trades`
- `/admin/transactions`
- `/admin/stats`
- `/admin/analytics`

---

## 📝 Frontend Integration Guide

### Authentication Flow

1. **Register User:**
```typescript
POST /api/auth/register
Body: {
  email: string;
  password: string;
  username: string;
  firstName?: string;
  lastName?: string;
}
Response: {
  user: { id, email, username, avatarId, ... },
  token: string,
  expiresAt: string
}
```

2. **Login:**
```typescript
POST /api/auth/login
Body: {
  email: string;
  password: string;
}
Response: {
  user: { id, email, username, avatarId, ... },
  token: string,
  expiresAt: string
}
```

3. **Use Token in Requests:**
```typescript
Headers: {
  'Authorization': `Bearer ${token}`
}
```

### Example: Get User Profile
```typescript
GET /api/user/profile
Headers: {
  'Authorization': `Bearer ${token}`
}
```

### Example: Get Orders
```typescript
GET /api/orders
Headers: {
  'Authorization': `Bearer ${token}`
}
Response: {
  orders: [...],
  total: number
}
```

---

## 🚧 Known Issues

1. **Wallet Endpoints** - Currently failing with 500 errors due to OASIS API integration issues (in progress)
2. **PUT /user/profile** - Returns 500 error (needs investigation)
3. **Some endpoints return 404** - May not be implemented yet or have different paths

---

## ✅ Recommended Frontend Development Path

**Phase 1 (Can Start Now):**
- ✅ User authentication (register/login)
- ✅ User profile display (GET)
- ✅ Assets listing
- ✅ Orders listing and history
- ✅ Trades listing and history
- ✅ Transactions listing

**Phase 2 (Wait for Fixes):**
- ⏳ User profile updates (PUT /user/profile)
- ⏳ Wallet operations (once wallet endpoints are fixed)
- ⏳ Order creation/modification
- ⏳ Transaction initiation (deposit/withdraw)

---

## 📊 Test Results Summary

- **Total Tested:** 29 endpoints
- **✅ Working:** 14 endpoints
- **❌ Failing:** 5 endpoints
- **⚠️ Skipped/Not Tested:** 10 endpoints

---

## 🔗 Quick Reference

**Working Endpoints List:**
- `GET /health`
- `POST /auth/register`
- `POST /auth/login`
- `POST /auth/forgot-password`
- `GET /user/profile`
- `GET /assets`
- `GET /orders`
- `GET /orders/open`
- `GET /orders/history`
- `GET /trades`
- `GET /trades/history`
- `GET /trades/statistics`
- `GET /transactions`
- `GET /wallet/verification-message`



