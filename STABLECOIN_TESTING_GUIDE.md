# Stablecoin (zUSD) Testing Guide

## Current Status

### ✅ Backend (Ready)
The stablecoin backend is **fully implemented** and integrated:
- **API Controller**: `StablecoinController` at `/api/v1/stablecoin`
- **Services**: All services implemented (ZcashCollateralService, AztecStablecoinService, StablecoinRepository, ViewingKeyService)
- **Endpoints**: All endpoints are ready and secured with JWT authentication

### ❌ Frontend (Not Yet Integrated)
The frontend **does not yet have**:
- Stablecoin API client methods in `lib/api.ts`
- Stablecoin UI components/pages
- Integration with the wallet UI

## Backend API Endpoints

All endpoints require JWT authentication (Bearer token in Authorization header).

Base URL: `http://api.oasisplatform.world` or `http://localhost:5000`

### 1. Mint Stablecoin (ZEC → zUSD)
**POST** `/api/v1/stablecoin/mint`

Request body:
```json
{
  "zecAmount": 10.0,
  "stablecoinAmount": 500.0,
  "zcashAddress": "zs1abc123...",
  "aztecAddress": "0x123abc..."
}
```

Response:
```json
{
  "positionId": "guid-here",
  "avatarId": "guid-here",
  "collateralAmount": 10.0,
  "debtAmount": 500.0,
  "collateralRatio": 2000.0,
  "health": "Safe",
  "createdAt": "2024-01-01T00:00:00Z",
  "lastUpdated": "2024-01-01T00:00:00Z",
  "viewingKeyHash": "hash-here",
  "zcashAddress": "zs1abc123...",
  "aztecAddress": "0x123abc..."
}
```

### 2. Redeem Stablecoin (zUSD → ZEC)
**POST** `/api/v1/stablecoin/redeem`

Request body:
```json
{
  "positionId": "guid-here",
  "stablecoinAmount": 100.0
}
```

Response:
```json
{
  "message": "Successfully redeemed 100.0 zUSD. ZEC collateral unlocked."
}
```

### 3. Get Position
**GET** `/api/v1/stablecoin/position/{positionId}`

Response:
```json
{
  "positionId": "guid-here",
  "avatarId": "guid-here",
  "collateralAmount": 10.0,
  "debtAmount": 500.0,
  "collateralRatio": 2000.0,
  "health": "Safe",
  ...
}
```

### 4. Get All Positions
**GET** `/api/v1/stablecoin/positions`

Response:
```json
[
  {
    "positionId": "guid-1",
    "avatarId": "guid-here",
    "collateralAmount": 10.0,
    ...
  },
  {
    "positionId": "guid-2",
    ...
  }
]
```

### 5. Health Check
**GET** `/api/v1/stablecoin/health`

Response:
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-01T00:00:00Z",
  "zecPrice": 45.50
}
```

## Testing Methods

### Option 1: Using cURL

#### 1. Get Authentication Token First
```bash
curl -X POST http://localhost:5000/api/avatar/authenticate \
  -H "Content-Type: application/json" \
  -d '{
    "username": "your_username",
    "password": "your_password"
  }'
```

Save the `jwtToken` from the response.

#### 2. Mint Stablecoin
```bash
curl -X POST http://localhost:5000/api/v1/stablecoin/mint \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "zecAmount": 10.0,
    "stablecoinAmount": 500.0,
    "zcashAddress": "zs1test123...",
    "aztecAddress": "0xtest123..."
  }'
```

#### 3. Get Positions
```bash
curl -X GET http://localhost:5000/api/v1/stablecoin/positions \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### 4. Redeem Stablecoin
```bash
curl -X POST http://localhost:5000/api/v1/stablecoin/redeem \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "positionId": "position-guid-here",
    "stablecoinAmount": 100.0
  }'
```

### Option 2: Using Postman

1. **Setup Environment**:
   - Create a new environment variable `baseUrl` = `http://localhost:5000`
   - Create variable `token` (will be set after login)

2. **Authenticate**:
   - POST `{{baseUrl}}/api/avatar/authenticate`
   - Body: JSON with username/password
   - Save `jwtToken` to environment variable `token`

3. **Mint Stablecoin**:
   - POST `{{baseUrl}}/api/v1/stablecoin/mint`
   - Headers:
     - `Authorization: Bearer {{token}}`
     - `Content-Type: application/json`
   - Body: JSON with mint request

4. **Get Positions**:
   - GET `{{baseUrl}}/api/v1/stablecoin/positions`
   - Headers: `Authorization: Bearer {{token}}`

### Option 3: Using Browser DevTools Console

If you're already logged into the wallet UI:

```javascript
// Get your auth token from localStorage or state
const token = 'YOUR_JWT_TOKEN'; // Get from your auth state

// Mint stablecoin
fetch('http://localhost:5000/api/v1/stablecoin/mint', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({
    zecAmount: 10.0,
    stablecoinAmount: 500.0,
    zcashAddress: 'zs1test123...',
    aztecAddress: '0xtest123...'
  })
})
.then(res => res.json())
.then(data => console.log('Mint result:', data));

// Get positions
fetch('http://localhost:5000/api/v1/stablecoin/positions', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
})
.then(res => res.json())
.then(data => console.log('Positions:', data));
```

## Starting the Backend

Make sure the backend is running:

```bash
cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run
```

The API should be available at `http://localhost:5000`

## Frontend Integration Needed

To fully integrate stablecoin into the UI, you need to:

### 1. Add API Client Methods (`lib/api.ts`)

```typescript
// Add to OASISWalletAPI class

// Mint stablecoin
async mintStablecoin(request: {
  zecAmount: number;
  stablecoinAmount: number;
  zcashAddress: string;
  aztecAddress: string;
}): Promise<OASISResult<StablecoinPosition>> {
  return this.request<StablecoinPosition>('v1/stablecoin/mint', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

// Redeem stablecoin
async redeemStablecoin(request: {
  positionId: string;
  stablecoinAmount: number;
}): Promise<OASISResult<string>> {
  return this.request<string>('v1/stablecoin/redeem', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

// Get position
async getStablecoinPosition(positionId: string): Promise<OASISResult<StablecoinPosition>> {
  return this.request<StablecoinPosition>(`v1/stablecoin/position/${positionId}`);
}

// Get all positions
async getStablecoinPositions(): Promise<OASISResult<StablecoinPosition[]>> {
  return this.request<StablecoinPosition[]>('v1/stablecoin/positions');
}

// Health check
async getStablecoinHealth(): Promise<OASISResult<StablecoinHealth>> {
  return this.request<StablecoinHealth>('v1/stablecoin/health');
}
```

**Note**: Update the `request` method to handle `/api/v1/` endpoints (currently it only handles `/api/wallet/`)

### 2. Create Types (`lib/types.ts`)

```typescript
export interface StablecoinPosition {
  positionId: string;
  avatarId: string;
  collateralAmount: number;
  debtAmount: number;
  collateralRatio: number;
  health: 'Safe' | 'Warning' | 'Critical' | 'Liquidation';
  createdAt: string;
  lastUpdated: string;
  viewingKeyHash?: string;
  zcashAddress: string;
  aztecAddress: string;
}

export interface StablecoinHealth {
  status: string;
  timestamp: string;
  zecPrice: number;
}
```

### 3. Create UI Components

- `components/stablecoin/StablecoinDashboard.tsx` - Main dashboard
- `components/stablecoin/MintForm.tsx` - Mint zUSD form
- `components/stablecoin/RedeemForm.tsx` - Redeem zUSD form
- `components/stablecoin/PositionsList.tsx` - List of positions
- `components/stablecoin/PositionCard.tsx` - Individual position card

### 4. Create Page

- `app/stablecoin/page.tsx` - Stablecoin page route

## Quick Test Checklist

- [ ] Backend is running (`dotnet run`)
- [ ] Can authenticate and get JWT token
- [ ] Can call `/api/v1/stablecoin/health` (no auth required)
- [ ] Can mint stablecoin (requires auth + valid addresses)
- [ ] Can get positions (requires auth)
- [ ] Can redeem stablecoin (requires auth + valid position ID)

## Troubleshooting

### "Avatar not authenticated" Error
- Make sure you're sending the JWT token in the `Authorization: Bearer {token}` header
- Verify the token hasn't expired

### "Provider not available" Error
- Ensure ZcashOASIS and AztecOASIS providers are registered in `Startup.cs`
- Check that provider configuration is correct

### CORS Errors
- Make sure CORS is configured in the backend `Startup.cs`
- If testing from browser, ensure the API URL is correct

## Next Steps

1. **Immediate**: Test backend using cURL/Postman to verify endpoints work
2. **Short-term**: Add API client methods to `lib/api.ts`
3. **Short-term**: Create basic UI components for mint/redeem
4. **Medium-term**: Integrate with wallet UI and avatar auth
5. **Long-term**: Add advanced features (position management, health monitoring, liquidation alerts)

