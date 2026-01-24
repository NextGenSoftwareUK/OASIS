# WEB4 OASIS API Overview

## What is WEB4 OASIS API?

The **WEB4 OASIS API** is the foundational data aggregation and identity layer for the OASIS ecosystem. It provides universal data management across Web2 and Web3 technologies, enabling seamless integration with 50+ blockchain networks and storage providers.

**Base URL:** `http://api.oasisweb4.com/api`

**Swagger UI:** [http://api.oasisweb4.com/swagger/index.html](http://api.oasisweb4.com/swagger/index.html)

---

## Key Features

### 🌐 Multi-Chain Support
- **50+ Blockchain Networks** - Ethereum, Solana, Polygon, Arbitrum, Avalanche, and more
- **Unified Interface** - Single API for all chains
- **Cross-Chain Operations** - Transfer assets between chains seamlessly

### 🔄 100% Uptime Architecture
- **HyperDrive Auto-Failover** - Automatic provider switching
- **Auto-Load Balancing** - Intelligent routing to fastest providers
- **Auto-Replication** - Data redundancy across providers
- **Geographic Optimization** - Routes to nearest available nodes

### 🔐 Universal Identity
- **Avatar System** - Single sign-on across all OASIS apps
- **Karma System** - Universal reputation and rewards
- **Multi-Factor Authentication** - Enhanced security options

### 💾 Universal Data Storage
- **50+ Storage Providers** - MongoDB, IPFS, Holochain, and more
- **Provider Abstraction** - Switch providers without code changes
- **Auto-Sync** - Automatic data synchronization

---

## API Categories

### 1. Authentication & Identity

| API | Description | Endpoints |
|-----|-------------|-----------|
| [Avatar API](authentication-identity/avatar-api.md) | Complete user management (80+ endpoints) | `/api/avatar` |
| [Keys API](authentication-identity/keys-api.md) | Cryptographic key management | `/api/keys` |
| [Karma API](authentication-identity/karma-api.md) | Reputation and reward system | `/api/karma` |

### 2. Data & Storage

| API | Description | Endpoints |
|-----|-------------|-----------|
| [Data API](data-storage/data-api.md) | Universal data operations | `/api/data` |
| [Files API](data-storage/files-api.md) | File upload and management | `/api/files` |
| [Holons API](data-storage/holons-api.md) | Hierarchical data structures | `/api/holons` |

### 3. Blockchain & Wallets

| API | Description | Endpoints |
|-----|-------------|-----------|
| [Wallet API](blockchain-wallets/wallet-api.md) | Multi-chain wallet operations | `/api/wallet` |
| [NFT API](blockchain-wallets/nft-api.md) | Cross-chain NFT management | `/api/nft` |
| [Solana API](blockchain-wallets/solana-api.md) | Solana-specific operations | `/api/solana` |

### 4. Network Operations

| API | Description | Endpoints |
|-----|-------------|-----------|
| [HyperDrive API](network-operations/hyperdrive-api.md) | Auto-failover system | `/api/hyperdrive` |
| [ONET API](network-operations/onet-api.md) | OASIS Network protocol | `/api/v1/onet` |
| [ONODE API](network-operations/onode-api.md) | OASIS Node operations | `/api/v1/onode` |

### 5. Core Services

| API | Description | Endpoints |
|-----|-------------|-----------|
| [Search API](core-services/search-api.md) | Universal search | `/api/search` |
| [Stats API](core-services/stats-api.md) | Analytics and statistics | `/api/stats` |
| [Messaging API](core-services/messaging-api.md) | Messaging services | `/api/messaging` |
| [Settings API](core-services/settings-api.md) | Configuration management | `/api/settings` |

---

## Supported Providers

### Blockchain Providers

| Provider | Status | Description |
|----------|--------|-------------|
| EthereumOASIS | ✅ | Ethereum mainnet and testnets |
| SolanaOASIS | ✅ | Solana blockchain |
| PolygonOASIS | ✅ | Polygon network |
| ArbitrumOASIS | ✅ | Arbitrum L2 |
| OptimismOASIS | ✅ | Optimism L2 |
| BaseOASIS | ✅ | Base (Coinbase L2) |
| AvalancheOASIS | ✅ | Avalanche network |
| BNBChainOASIS | ✅ | BNB Smart Chain |
| CardanoOASIS | ✅ | Cardano blockchain |
| NEAROASIS | ✅ | NEAR Protocol |
| SuiOASIS | ✅ | Sui blockchain |
| AptosOASIS | ✅ | Aptos blockchain |
| BitcoinOASIS | ✅ | Bitcoin network |
| And 30+ more... | | |

### Storage Providers

| Provider | Status | Description |
|----------|--------|-------------|
| MongoDBOASIS | ✅ | MongoDB database |
| Neo4jOASIS | ✅ | Neo4j graph database |
| SQLLiteDBOASIS | ✅ | SQLite database |
| IPFSOASIS | ✅ | IPFS distributed storage |
| PinataOASIS | ✅ | Pinata IPFS service |
| HoloOASIS | ✅ | Holochain P2P network |
| LocalFileOASIS | ✅ | Local file storage |
| AzureCosmosDBOASIS | ✅ | Azure Cosmos DB |
| GoogleCloudOASIS | ✅ | Google Cloud Platform |
| AWSOASIS | ✅ | Amazon Web Services |
| And 10+ more... | | |

### Network Providers

| Provider | Status | Description |
|----------|--------|-------------|
| ONET | ✅ | OASIS Network protocol |
| ActivityPubOASIS | ✅ | ActivityPub federation |
| SOLIDOASIS | ✅ | SOLID protocol |
| ThreeFoldOASIS | ✅ | ThreeFold network |
| And more... | | |

---

## Getting Started

### Step 1: Register Your Avatar

```http
POST http://api.oasisweb4.com/api/avatar/register
Content-Type: application/json

{
  "username": "myusername",
  "email": "myemail@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

### Step 2: Verify Email

Check your email and verify your account using the token.

### Step 3: Authenticate

```http
POST http://api.oasisweb4.com/api/avatar/authenticate
Content-Type: application/json

{
  "username": "myusername",
  "password": "SecurePassword123!"
}
```

### Step 4: Use Your JWT Token

Include the JWT token in the Authorization header for all authenticated requests:

```http
GET http://api.oasisweb4.com/api/avatar/get-by-id/{avatarId}
Authorization: Bearer YOUR_JWT_TOKEN
```

[Complete Getting Started Guide →](../../getting-started/overview.md)

---

## Response Format

All API responses follow the standard OASIS format:

```json
{
  "result": {
    // Response data here
  },
  "isError": false,
  "message": "Success"
}
```

**Error Response:**
```json
{
  "result": null,
  "isError": true,
  "message": "Error message",
  "errorCode": "ERROR_CODE"
}
```

---

## Authentication

Most endpoints require authentication using JWT Bearer tokens:

```http
Authorization: Bearer YOUR_JWT_TOKEN
```

**Getting a JWT Token:**
1. Register an avatar
2. Verify your email
3. Authenticate using `/api/avatar/authenticate`
4. Use the `token` field from the response

[Authentication Guide →](../../getting-started/authentication.md)

---

## Provider Selection

You can specify which provider to use for each request:

**Format:** `{endpoint}/{providerType}/{setGlobally}`

**Example:**
```http
GET /api/avatar/get-by-id/{id}/SolanaOASIS/false
```

**Parameters:**
- `providerType` - Provider name (e.g., `SolanaOASIS`, `MongoDBOASIS`)
- `setGlobally` - If `true`, sets provider globally for all future requests

**Default Behavior:**
- If not specified, uses HyperDrive intelligent routing
- Automatically selects fastest available provider
- Auto-fails over if provider is down

---

## Rate Limits

| Tier | Requests per Minute | Burst Limit |
|------|---------------------|-------------|
| Free | 100 | 200 |
| Pro | 1,000 | 2,000 |
| Enterprise | Custom | Custom |

Rate limit headers:
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1642248000
```

---

## Error Codes

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| VALIDATION_ERROR | 400 | Request validation failed |
| UNAUTHORIZED | 401 | Missing or invalid authentication |
| FORBIDDEN | 403 | Insufficient permissions |
| NOT_FOUND | 404 | Resource not found |
| RATE_LIMIT_EXCEEDED | 429 | Too many requests |
| INTERNAL_ERROR | 500 | Server error |

[Complete Error Code Reference →](../../reference/error-codes.md)

---

## SDKs & Libraries

### Official SDKs

- **JavaScript/TypeScript** - [@nextgensoftware/oasis-web4-api-client](https://www.npmjs.com/package/@nextgensoftware/oasis-web4-api-client)
- **C#/.NET** - Available via NuGet
- **Python** - Coming soon

### Postman Collection

[Download Postman Collection](https://oasisweb4.one/postman/OASIS_API.postman_collection.json)

**Environments:**
- [DEV Environment](https://oasisweb4.one/postman/OASIS_API_DEV.postman_environment.json)
- [STAGING Environment](https://oasisweb4.one/postman/OASIS_API_STAGING.postman_environment.json)
- [LIVE Environment](https://oasisweb4.one/postman/OASIS_API_LIVE.postman_environment.json)

---

## Quick Links

- [Swagger UI](http://api.oasisweb4.com/swagger/index.html) - Interactive API documentation
- [Getting Started Guide](../../getting-started/overview.md) - Step-by-step tutorial
- [Complete API Reference](../../reference/api-reference/web4-complete-reference.md) - All endpoints
- [Architecture Guide](../../guides/architecture/system-overview.md) - System design

---

## Support

- **Documentation Issues?** → [Report on GitHub](https://github.com/NextGenSoftwareUK/OASIS/issues)
- **Technical Questions?** → [Join Discord](https://discord.gg/oasis)
- **Feature Requests?** → [Submit Request](https://github.com/NextGenSoftwareUK/OASIS/discussions)

---

*Last Updated: January 24, 2026*
*API Version: v4.4.4*
