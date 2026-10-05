# Frontend Integration Guide

**For Frontend Developers - How to Use the Pangea Backend API**

---

## 🚀 Quick Start

### Base URL
```
Development: http://localhost:3000/api
Production: https://api.pangea.markets/api (or your production URL)
```

### Authentication
All endpoints (except public ones) require a JWT token in the Authorization header:
```typescript
headers: {
  'Authorization': `Bearer ${token}`,
  'Content-Type': 'application/json'
}
```

---

## 📋 Essential Endpoints

### 1. Authentication

#### Register User
```typescript
POST /api/auth/register
Body: {
  email: string
  password: string
  username: string
  firstName?: string
  lastName?: string
}

Response: {
  user: {
    id: string
    email: string
    username: string
    avatarId: string
    role: string
  }
  token: string
  expiresAt: string
}
```

#### Login
```typescript
POST /api/auth/login
Body: {
  email: string
  password: string
}

Response: {
  user: { ... }
  token: string
  expiresAt: string
}
```

#### Get Current User
```typescript
GET /api/auth/me
Headers: { Authorization: `Bearer ${token}` }

Response: {
  id: string
  email: string
  username: string
  avatarId: string
  role: string
  walletAddressSolana?: string
  walletAddressEthereum?: string
}
```

---

### 2. Assets (Public - No Auth Required)

#### List All Assets
```typescript
GET /api/assets?page=1&limit=20&status=active&assetClass=real-estate

Response: {
  data: [
    {
      id: string
      name: string
      symbol: string
      description: string
      assetClass: string
      totalSupply: string
      currentPrice: number
      status: 'active' | 'closed'
      blockchain: 'solana' | 'ethereum'
      contractAddress?: string
    }
  ]
  total: number
  page: number
  limit: number
}
```

#### Get Asset Details
```typescript
GET /api/assets/:assetId

Response: {
  id: string
  name: string
  symbol: string
  description: string
  assetClass: string
  totalSupply: string
  currentPrice: number
  status: string
  blockchain: string
  contractAddress?: string
  metadata: object
}
```

#### Get Order Book (Live Prices)
```typescript
GET /api/assets/:assetId/orders

Response: {
  bids: [  // Buy orders (highest price first)
    {
      orderId: string
      pricePerTokenUsd: string
      quantity: string
      remainingQuantity: string
    }
  ]
  asks: [  // Sell orders (lowest price first)
    {
      orderId: string
      pricePerTokenUsd: string
      quantity: string
      remainingQuantity: string
    }
  ]
}
```

#### Get Current Price
```typescript
GET /api/assets/:assetId/price

Response: {
  assetId: string
  currentPrice: number
  priceChange24h?: number
  volume24h?: number
}
```

#### Search Assets
```typescript
GET /api/assets/search?q=real+estate

Response: {
  data: [ ... ]  // Same format as list assets
  total: number
}
```

---

### 3. Wallet Integration

#### Connect Wallet (Phantom/MetaMask)
```typescript
POST /api/wallet/connect
Body: {
  blockchain: 'solana' | 'ethereum'
  walletAddress: string
  signature: string
  message: string
}

Response: {
  success: true
  message: 'Wallet connected successfully'
  wallet: {
    address: string
    blockchain: string
  }
}
```

**Frontend Flow:**
1. User clicks "Connect Wallet"
2. Frontend calls Phantom/MetaMask to get wallet address
3. Frontend requests verification message: `GET /api/wallet/verification-message`
4. User signs message in wallet
5. Frontend sends signature to `POST /api/wallet/connect`

#### Get All Balances
```typescript
GET /api/wallet/balance

Response: {
  success: true
  balances: [
    {
      walletId: string
      walletAddress: string
      providerType: 'SolanaOASIS' | 'EthereumOASIS'
      balance: string
      tokenSymbol: string
    }
  ]
}
```

#### Get Balance for Specific Asset
```typescript
GET /api/wallet/balance/:assetId

Response: {
  success: true
  balance: {
    userId: string
    assetId: string
    balance: string
    lockedBalance: string
    availableBalance: string
  }
}
```

---

### 4. Orders

#### Create Order (Buy or Sell)
```typescript
POST /api/orders
Body: {
  assetId: string
  orderType: 'buy' | 'sell'
  pricePerTokenUsd: number  // Min 0.01
  quantity: number          // Min 1
  isLimitOrder?: boolean    // Default: true
  isMarketOrder?: boolean   // Default: false
  expiresAt?: string        // ISO date string
  blockchain?: 'solana' | 'ethereum'
}

Response: {
  orderId: string
  assetId: string
  orderType: 'buy' | 'sell'
  orderStatus: 'pending' | 'open' | 'filled' | 'partially_filled' | 'cancelled'
  pricePerTokenUsd: string
  quantity: string
  remainingQuantity: string
  filledQuantity: string
  createdAt: string
}
```

**Important Notes:**
- Sell orders require sufficient balance (tokens are locked)
- Orders are automatically matched when created
- Market orders execute immediately at best available price

#### Get User's Orders
```typescript
GET /api/orders?status=open&orderType=buy&page=1&limit=20

Response: {
  data: [ ... ]  // Array of orders
  total: number
  page: number
  limit: number
}
```

#### Get Open Orders
```typescript
GET /api/orders/open

Response: {
  data: [ ... ]  // Only open orders
}
```

#### Get Order Details
```typescript
GET /api/orders/:orderId

Response: {
  orderId: string
  assetId: string
  orderType: 'buy' | 'sell'
  orderStatus: string
  pricePerTokenUsd: string
  quantity: string
  remainingQuantity: string
  filledQuantity: string
  createdAt: string
  filledAt?: string
}
```

#### Cancel Order
```typescript
DELETE /api/orders/:orderId

Response: {
  orderId: string
  orderStatus: 'cancelled'
  message: 'Order cancelled successfully'
}
```

---

### 5. Trades

#### Get Trade History
```typescript
GET /api/trades?assetId=xxx&status=completed&page=1&limit=20

Response: {
  data: [
    {
      tradeId: string
      assetId: string
      buyerId: string
      sellerId: string
      quantity: string
      pricePerTokenUsd: string
      totalValueUsd: string
      transactionHash?: string
      executedAt: string
      settlementStatus: 'pending' | 'completed' | 'failed'
    }
  ]
  total: number
  page: number
  limit: number
}
```

#### Get Trade Statistics
```typescript
GET /api/trades/statistics?assetId=xxx

Response: {
  totalTrades: number
  totalVolume: string
  averagePrice: number
  highestPrice: number
  lowestPrice: number
  lastTradePrice: number
}
```

---

### 6. Deposits & Withdrawals

#### Initiate Deposit
```typescript
POST /api/transactions/deposit
Body: {
  assetId: string
  amount: string
  blockchain: 'solana' | 'ethereum'
  fromAddress?: string
}

Response: {
  transactionId: string
  assetId: string
  amount: string
  vaultAddress: string  // Send tokens to this address
  status: 'pending'
  createdAt: string
}
```

**Frontend Flow:**
1. User requests deposit
2. Backend returns vault address
3. Frontend prompts user to send tokens to vault address
4. Backend monitors blockchain and updates balance automatically

#### Initiate Withdrawal
```typescript
POST /api/transactions/withdraw
Body: {
  assetId: string
  amount: string
  toAddress: string
  blockchain: 'solana' | 'ethereum'
}

Response: {
  transactionId: string
  assetId: string
  amount: string
  toAddress: string
  status: 'processing'
  transactionHash?: string
  createdAt: string
}
```

#### Get Transaction History
```typescript
GET /api/transactions?assetId=xxx&transactionType=deposit&status=completed

Response: {
  data: [
    {
      transactionId: string
      assetId: string
      transactionType: 'deposit' | 'withdrawal'
      amount: string
      status: 'pending' | 'processing' | 'completed' | 'failed'
      transactionHash?: string
      createdAt: string
      completedAt?: string
    }
  ]
  total: number
}
```

---

### 7. Real-time Updates (WebSocket)

#### Connect to WebSocket
```typescript
import { io } from 'socket.io-client';

const socket = io('http://localhost:3000/trading', {
  auth: {
    token: 'YOUR_JWT_TOKEN'
  }
});

// Connection confirmation
socket.on('connected', (data) => {
  console.log('Connected:', data);
  // { userId: string, message: string }
});

// Error handling
socket.on('error', (error) => {
  console.error('WebSocket error:', error);
});
```

#### Subscribe to Order Book Updates
```typescript
socket.emit('subscribe:orderbook', { assetId: 'xxx' });

socket.on('subscribed', (data) => {
  console.log('Subscribed:', data);
  // { subscription: 'orderbook', assetId: string, room: string }
});

socket.on('orderbook:update', (data) => {
  console.log('Order book updated:', data);
  // {
  //   assetId: string
  //   orderBook: { bids: [...], asks: [...] }
  //   timestamp: string
  // }
});
```

#### Subscribe to Trade Feed
```typescript
socket.emit('subscribe:trades', { assetId: 'xxx' });

socket.on('trade:executed', (trade) => {
  console.log('Trade executed:', trade);
  // {
  //   tradeId: string
  //   assetId: string
  //   quantity: string
  //   pricePerTokenUsd: string
  //   totalValueUsd: string
  //   executedAt: string
  // }
});
```

#### Subscribe to User Events
```typescript
socket.emit('subscribe:user');

socket.on('order:updated', (order) => {
  console.log('Order updated:', order);
  // {
  //   orderId: string
  //   orderStatus: string
  //   filledQuantity: string
  //   remainingQuantity: string
  // }
});

socket.on('balance:update', (balance) => {
  console.log('Balance updated:', balance);
  // {
  //   userId: string
  //   assetId: string
  //   balance: string
  //   timestamp: string
  // }
});
```

#### Unsubscribe
```typescript
socket.emit('unsubscribe:orderbook', { assetId: 'xxx' });
socket.emit('unsubscribe:trades', { assetId: 'xxx' });
```

---

## 🎯 Common Frontend Patterns

### 1. Authentication Flow
```typescript
// 1. Register or Login
const response = await fetch('/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email, password })
});
const { token, user } = await response.json();

// 2. Store token
localStorage.setItem('token', token);

// 3. Use token for all requests
const headers = {
  'Authorization': `Bearer ${token}`,
  'Content-Type': 'application/json'
};
```

### 2. Wallet Connection Flow
```typescript
// 1. Connect to Phantom/MetaMask
const provider = window.solana || window.ethereum;
const walletAddress = await provider.connect();

// 2. Get verification message
const messageRes = await fetch('/api/wallet/verification-message', {
  method: 'GET',
  headers: { 'Authorization': `Bearer ${token}` },
  body: JSON.stringify({ walletAddress, blockchain: 'solana' })
});
const { message } = await messageRes.json();

// 3. Sign message
const signature = await provider.signMessage(message);

// 4. Connect wallet
await fetch('/api/wallet/connect', {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${token}` },
  body: JSON.stringify({
    blockchain: 'solana',
    walletAddress,
    signature,
    message
  })
});
```

### 3. Create Order Flow
```typescript
// 1. Check balance (for sell orders)
const balance = await fetch(`/api/wallet/balance/${assetId}`, {
  headers: { 'Authorization': `Bearer ${token}` }
});

// 2. Create order
const order = await fetch('/api/orders', {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${token}` },
  body: JSON.stringify({
    assetId,
    orderType: 'buy',
    pricePerTokenUsd: 10.50,
    quantity: 100,
    isLimitOrder: true
  })
});

// 3. Listen for updates via WebSocket
socket.on('order:updated', (updatedOrder) => {
  if (updatedOrder.orderId === order.orderId) {
    // Update UI
  }
});
```

### 4. Real-time Order Book
```typescript
// 1. Get initial order book
const orderBook = await fetch(`/api/assets/${assetId}/orders`);

// 2. Subscribe to updates
socket.emit('subscribe:orderbook', { assetId });

// 3. Update UI on changes
socket.on('orderbook:update', (update) => {
  setOrderBook(update.orderBook);
});
```

---

## ⚠️ Important Notes

### Error Handling
All endpoints return standard HTTP status codes:
- `200` - Success
- `201` - Created
- `400` - Bad Request (validation errors)
- `401` - Unauthorized (invalid/missing token)
- `403` - Forbidden (insufficient permissions)
- `404` - Not Found
- `500` - Server Error

Error response format:
```json
{
  "statusCode": 400,
  "message": "Validation failed",
  "error": "Bad Request"
}
```

### Pagination
Most list endpoints support pagination:
```
GET /api/assets?page=1&limit=20
```

Response includes:
```json
{
  "data": [...],
  "total": 100,
  "page": 1,
  "limit": 20
}
```

### Filtering
Many endpoints support filtering:
```
GET /api/orders?status=open&orderType=buy&assetId=xxx
GET /api/trades?assetId=xxx&status=completed
GET /api/transactions?transactionType=deposit&status=completed
```

### Token Expiration
JWT tokens expire after 24 hours. Handle token refresh:
```typescript
// Check if token is expired
if (isTokenExpired(token)) {
  // Re-login or refresh token
  const { token: newToken } = await login(email, password);
  localStorage.setItem('token', newToken);
}
```

---

## 🔗 Quick Reference

### Public Endpoints (No Auth)
- `GET /api/assets` - List assets
- `GET /api/assets/:id` - Asset details
- `GET /api/assets/:id/orders` - Order book
- `GET /api/assets/:id/price` - Current price
- `GET /api/assets/search` - Search assets

### Protected Endpoints (Require Auth)
- All `/api/auth/*` (except register/login)
- All `/api/orders/*`
- All `/api/trades/*`
- All `/api/wallet/*`
- All `/api/transactions/*`

### WebSocket Events
- `trade:executed` - New trade
- `orderbook:update` - Order book changed
- `order:updated` - Order status changed
- `balance:update` - Balance changed
- `price:update` - Price changed

---

## 📚 Additional Resources

- **Complete API Reference**: See [API_COVERAGE_ANALYSIS.md](./API_COVERAGE_ANALYSIS.md)
- **Backend Setup**: See [backend/README.md](./backend/README.md)
- **Smart Contracts**: See [CONTRACT_SPECIFICATIONS.md](./CONTRACT_SPECIFICATIONS.md)

---

**Need Help?** Check the main [README.md](./README.md) for architecture overview and how the backend works.








