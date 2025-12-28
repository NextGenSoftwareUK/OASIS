# Architecture Decision: Read from Client, Write from Server-Side

## Decision

**Yes, this is accurate and recommended for our trading platform.**

✅ **Read operations**: Call directly from client  
✅ **Write/modify operations**: Proxy through Next.js API routes (server-side)

---

## Rationale

This approach provides **defense in depth** for financial/trading operations where write operations are critical.

### Benefits

1. **Security Layers**
   - Client → Next.js API route → Backend
   - Each layer can add validation and checks
   - Better protection against malicious requests

2. **Audit Trail**
   - All write operations logged at Next.js level
   - Easier to track and investigate issues
   - Better compliance for financial operations

3. **Business Logic**
   - Can add complex validation in Next.js before backend
   - Can implement fraud detection checks
   - Can add rate limiting specific to write operations

4. **Performance Optimization**
   - Reads are fast (direct client calls, no extra hop)
   - Writes have acceptable overhead for security benefits
   - Can cache read data more easily

5. **Consistency**
   - All writes go through one code path (Next.js API routes)
   - Easier to maintain and update write logic
   - Centralized error handling for writes

---

## Implementation Map

### ✅ Read Operations (Direct from Client)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/assets` | GET | List all assets |
| `/api/assets/:id` | GET | Get asset details |
| `/api/assets/:id/orders` | GET | Get order book |
| `/api/assets/:id/trades` | GET | Get trade history |
| `/api/assets/:id/price` | GET | Get current price |
| `/api/assets/search` | GET | Search assets |
| `/api/orders` | GET | Get user's orders |
| `/api/orders/:id` | GET | Get order details |
| `/api/orders/open` | GET | Get open orders |
| `/api/orders/history` | GET | Get order history |
| `/api/trades` | GET | Get user's trades |
| `/api/trades/:id` | GET | Get trade details |
| `/api/trades/statistics` | GET | Get trade statistics |
| `/api/user/profile` | GET | Get user profile |
| `/api/wallet/balance` | GET | Get wallet balances |
| `/api/wallet/balance/:assetId` | GET | Get asset balance |
| `/api/transactions` | GET | Get transaction history |
| `/api/transactions/:id` | GET | Get transaction details |

### ✅ Write Operations (Via Next.js API Routes)

| Endpoint | Method | Description | Next.js Route |
|----------|--------|-------------|---------------|
| `/api/orders` | POST | Create order | `/app/api/orders/route.ts` |
| `/api/orders/:id` | PUT | Update order | `/app/api/orders/[id]/route.ts` |
| `/api/orders/:id` | DELETE | Cancel order | `/app/api/orders/[id]/route.ts` |
| `/api/user/profile` | PUT | Update profile | `/app/api/user/profile/route.ts` |
| `/api/transactions/deposit` | POST | Initiate deposit | `/app/api/transactions/deposit/route.ts` |
| `/api/transactions/withdraw` | POST | Initiate withdrawal | `/app/api/transactions/withdraw/route.ts` |
| `/api/wallet/connect` | POST | Connect wallet | `/app/api/wallet/connect/route.ts` |
| `/api/wallet/verify` | POST | Verify wallet | `/app/api/wallet/verify/route.ts` |
| `/api/wallet/sync` | POST | Sync balances | `/app/api/wallet/sync/route.ts` |

### ⚠️ Admin Operations (Always Server-Side)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/admin/*` | ALL | All admin endpoints |
| `/api/smart-contracts/*` | ALL | Smart contract operations |
| `/api/assets` | POST/PUT/DELETE | Asset management (admin only) |

---

## Implementation Pattern

### Read Operation (Direct from Client)

```typescript
// Client-side component
'use client';

async function fetchOrders() {
  const response = await fetch(`${BACKEND_URL}/api/orders`, {
    headers: {
      'Authorization': `Bearer ${jwtToken}`,
      'Content-Type': 'application/json'
    }
  });
  return response.json();
}
```

### Write Operation (Via Next.js API Route)

**Client-side:**
```typescript
// Client-side component
'use client';

async function createOrder(orderData: CreateOrderDto) {
  // Call Next.js API route (not backend directly)
  const response = await fetch('/api/orders', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(orderData)
  });
  return response.json();
}
```

**Next.js API Route:**
```typescript
// /app/api/orders/route.ts
import { NextRequest, NextResponse } from 'next/server';
import { getServerSession } from 'next-auth';

export async function POST(request: NextRequest) {
  try {
    // 1. Validate session
    const session = await getServerSession();
    if (!session || !session.jwtToken) {
      return NextResponse.json(
        { error: 'Unauthorized' },
        { status: 401 }
      );
    }

    // 2. Get request data
    const orderData = await request.json();

    // 3. Additional validation (optional)
    // Add any business logic validation here
    if (!orderData.assetId || orderData.quantity <= 0) {
      return NextResponse.json(
        { error: 'Invalid order data' },
        { status: 400 }
      );
    }

    // 4. Log write operation (audit trail)
    console.log(`[AUDIT] User ${session.userId} creating order`, {
      assetId: orderData.assetId,
      quantity: orderData.quantity,
      timestamp: new Date().toISOString()
    });

    // 5. Forward to backend with JWT
    const backendResponse = await fetch(`${process.env.BACKEND_URL}/api/orders`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${session.jwtToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(orderData)
    });

    if (!backendResponse.ok) {
      const error = await backendResponse.json();
      return NextResponse.json(error, { status: backendResponse.status });
    }

    const result = await backendResponse.json();
    return NextResponse.json(result);
  } catch (error) {
    console.error('Error in POST /api/orders:', error);
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    );
  }
}
```

---

## Rate Limiting Strategy

With this architecture, you can apply different rate limits:

### Client-Side Reads
- Backend rate limiting: 100 requests/min per user
- Can be more lenient since reads are less risky

### Server-Side Writes
- Next.js level: 10-20 writes/min per user (stricter)
- Backend level: 100 requests/min per user (backup)
- Two layers of protection

### Auth Endpoints
- Backend rate limiting: 5 requests/min per IP (prevent brute force)
- Already public, but very strict limits

---

## Security Notes

1. **Backend is already secure**: The backend has proper JWT validation, user-scoping, and validation. This approach adds an **extra layer** rather than fixing a security issue.

2. **Session management**: Next.js middleware manages the session and can refresh tokens if needed before forwarding to backend.

3. **Error handling**: Next.js API routes can sanitize error messages before sending to client (hide internal details).

4. **Monitoring**: Easier to add monitoring/alerting at Next.js level for write operations.

---

## Summary

**Yes, "read from client, write/modify from server-side" is the optimal approach for our trading platform.**

This provides:
- ✅ Better security (defense in depth)
- ✅ Better audit trail
- ✅ Better performance (fast reads, secure writes)
- ✅ Better maintainability (consistent write path)

The backend is ready for this approach - it already has all the necessary security measures in place. This architecture decision adds an extra layer of protection and control.








