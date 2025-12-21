# OASIS API Reference

**Base URL:** `https://api.oasisweb4.com`  
**Version:** 1.0  
**Contact:** Telegram @maxgershfield

---

## Authentication

All API requests (except registration) require a JWT token.

### Get Your Token

**1. Register:**
```bash
curl -X POST "https://api.oasisweb4.com/api/avatar/register" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "your_username",
    "email": "your@email.com",
    "password": "yourPassword123",
    "avatarType": "User",
    "acceptTerms": true
  }'
```

**2. Login (Future Sessions):**
```bash
curl -X POST "https://api.oasisweb4.com/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "your_username",
    "password": "yourPassword123"
  }'
```

**3. Use Token in Requests:**
```bash
curl -X GET "https://api.oasisweb4.com/api/data/load-holon/{id}" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## Core APIs

### Data API

Store and retrieve data with automatic multi-provider replication.

#### Save Data

```http
POST /api/data/save-holon
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json
```

**Body:**
```json
{
  "name": "UserProfile_123",
  "holonType": "UserProfile",
  "description": "User profile data",
  "metadata": {
    "userId": "123",
    "preferences": { "theme": "dark" },
    "any": "json data you want"
  }
}
```

**Response:**
```json
{
  "result": {
    "id": "holon-uuid",
    "name": "UserProfile_123",
    "metadata": { ... }
  },
  "isError": false
}
```

**What happens:** Data saved to MongoDB (primary) and Arbitrum blockchain (backup).

#### Load Data

```http
GET /api/data/load-holon/{holonId}
Authorization: Bearer YOUR_TOKEN
```

**What happens:** Loads from MongoDB. If MongoDB is down, automatically tries Arbitrum, then Ethereum.

#### Delete Data

```http
DELETE /api/data/delete-holon/{holonId}
Authorization: Bearer YOUR_TOKEN
```

---

### Wallet API

Generate and manage multi-chain wallets for your users.

#### Generate Wallet

```http
POST /api/wallet/avatar/{avatarId}/generate
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json
```

**Body:**
```json
{
  "providerType": "SolanaOASIS",
  "setAsDefault": true
}
```

**Response:**
```json
{
  "result": {
    "walletId": "wallet-uuid",
    "publicKey": "AfpSpMjNyoHTZ...",
    "providerType": "SolanaOASIS",
    "balance": 0
  }
}
```

**Supported Providers:**
- `SolanaOASIS` - Solana
- `EthereumOASIS` - Ethereum
- `ArbitrumOASIS` - Arbitrum
- `PolygonOASIS` - Polygon
- `BaseOASIS` - Base

#### Get All Wallets

```http
GET /api/wallet/avatar/{avatarId}/wallets
Authorization: Bearer YOUR_TOKEN
```

#### Get Default Wallet

```http
GET /api/wallet/avatar/{avatarId}/default-wallet
Authorization: Bearer YOUR_TOKEN
```

---

### NFT API

Mint and manage NFTs across multiple blockchains.

#### Mint NFT

```http
POST /api/nft/mint
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json
```

**Body:**
```json
{
  "avatarId": "your-avatar-id",
  "name": "My Cool NFT",
  "description": "NFT description",
  "imageUrl": "ipfs://QmX...",
  "metadata": {
    "attributes": [
      { "trait_type": "Rarity", "value": "Legendary" }
    ]
  }
}
```

**Current Support:** Arbitrum (more chains coming soon)

---

### Provider API

Monitor and manage OASIS providers.

#### Get Provider Health

```http
GET /api/provider/health
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "providers": [
    {
      "name": "MongoDBOASIS",
      "status": "online",
      "latency": "45ms"
    },
    {
      "name": "ArbitrumOASIS",
      "status": "online",
      "latency": "900ms"
    }
  ],
  "healthyCount": 12,
  "totalCount": 15
}
```

#### Get Active Providers

```http
GET /api/provider/active
Authorization: Bearer YOUR_TOKEN
```

---

## Advanced Features

### Provider-Specific Operations

Save to a specific provider:

```http
POST /api/data/save-holon/{providerType}/{setGlobally}
Authorization: Bearer YOUR_TOKEN
```

**Example:**
```bash
curl -X POST "https://api.oasisweb4.com/api/data/save-holon/SolanaOASIS/false" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "name": "TestData", "holonType": "Test", "metadata": {} }'
```

**Parameters:**
- `providerType`: Which provider to use (e.g., `SolanaOASIS`, `ArbitrumOASIS`)
- `setGlobally`: `true` = set as default provider, `false` = use once

---

## Rate Limits & Quotas

**Current Limits:**
- Free tier: 10,000 requests/hour
- Rate limit headers included in responses
- Contact @maxgershfield for enterprise limits

**Headers:**
```
X-RateLimit-Limit: 10000
X-RateLimit-Remaining: 9850
X-RateLimit-Reset: 1699564800
```

---

## Error Handling

All responses follow this format:

**Success:**
```json
{
  "result": { ... },
  "isError": false,
  "message": "Success"
}
```

**Error:**
```json
{
  "result": null,
  "isError": true,
  "message": "Detailed error message"
}
```

**Common Errors:**

| Status Code | Meaning | Solution |
|-------------|---------|----------|
| 401 | Unauthorized | Check JWT token |
| 404 | Not found | Verify resource ID |
| 429 | Rate limit | Wait for reset |
| 500 | Server error | Contact support |

---

## Code Examples

### JavaScript

```javascript
const OASIS_API = 'https://api.oasisweb4.com';

class OASISClient {
  constructor(token) {
    this.token = token;
  }
  
  async saveData(data) {
    const res = await fetch(`${OASIS_API}/api/data/save-holon`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        name: data.name,
        holonType: data.type,
        metadata: data
      })
    });
    return await res.json();
  }
  
  async loadData(holonId) {
    const res = await fetch(
      `${OASIS_API}/api/data/load-holon/${holonId}`,
      { headers: { 'Authorization': `Bearer ${this.token}` } }
    );
    const result = await res.json();
    return result.result.metadata;
  }
}
```

### Python

```python
import requests

class OASISClient:
    def __init__(self, token):
        self.api = 'https://api.oasisweb4.com'
        self.headers = {'Authorization': f'Bearer {token}'}
    
    def save_data(self, data):
        return requests.post(
            f'{self.api}/api/data/save-holon',
            headers=self.headers,
            json={
                'name': data['name'],
                'holonType': data['type'],
                'metadata': data
            }
        ).json()
    
    def load_data(self, holon_id):
        result = requests.get(
            f'{self.api}/api/data/load-holon/{holon_id}',
            headers=self.headers
        ).json()
        return result['result']['metadata']
```

---

## What's Currently Available

### ✅ Production Ready

**Data Storage:**
- Save/load/delete data (holons)
- Automatic backup to MongoDB + Arbitrum
- Auto-failover if MongoDB down

**User Management:**
- Avatar registration
- Authentication (JWT)
- Profile management

**Wallets:**
- Generate wallets (Solana, Ethereum, Arbitrum, Polygon)
- Query wallet balances
- Wallet management

**NFTs:**
- Mint NFTs (Arbitrum)
- Query NFT ownership

### 🔧 In Development

**Additional Blockchains:**
- Full auto-replication to all EVM chains
- Cross-chain NFT minting
- Enhanced wallet features

**Advanced Features:**
- HyperDrive analytics dashboard
- Cost optimization tools
- Provider performance monitoring

---

## Getting Help

**If you encounter issues:**

1. Check API health: `curl https://api.oasisweb4.com/health`
2. Verify JWT token is valid (expires after 24 hours)
3. Check response for error messages
4. Contact: Telegram @maxgershfield

---

## Next Steps

1. **Try the Quick Start:** [QUICKSTART.md](QUICKSTART.md)
2. **Understand the Architecture:** [Provider Architecture Guide](OASIS_PROVIDER_ARCHITECTURE_GUIDE.md)
3. **Deep Dive:** [HyperDrive Architecture](HYPERDRIVE_ARCHITECTURE_DIAGRAM.md)

---

**Last Updated:** November 6, 2025  
**API Status:** Production (Stable)



