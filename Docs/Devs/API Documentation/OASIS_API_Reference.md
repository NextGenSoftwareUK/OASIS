# OASIS API Reference for Alpha Testers

## Base URLs
- **Development**: `https://localhost:5002`
- **Staging**: `https://staging-api.oasisweb4.com`
- **Production**: `https://api.oasisweb4.com`

## Authentication

All API requests require authentication via JWT token in the Authorization header:
```
Authorization: Bearer YOUR_JWT_TOKEN
```

---

## Avatar Management

### Register Avatar
**POST** `/api/avatar/register`

Register a new avatar in the OASIS system.

**Request Body:**
```json
{
  "username": "string",
  "email": "string", 
  "password": "string",
  "firstName": "string",
  "lastName": "string"
}
```

**Response:**
```json
{
  "result": {
    "id": "guid",
    "username": "string",
    "email": "string",
    "firstName": "string",
    "lastName": "string",
    "isEmailVerified": false
  },
  "isError": false,
  "message": "Avatar successfully registered"
}
```

### Verify Email
**GET** `/api/avatar/verify-email`

Verify avatar email address using token from registration email.

**Query Parameters:**
- `token` (string, required) - Verification token from email

**Response:**
```json
{
  "result": true,
  "isError": false,
  "message": "Email successfully verified"
}
```

### Authenticate Avatar
**POST** `/api/avatar/authenticate`

Authenticate avatar and receive JWT token.

**Request Body:**
```json
{
  "username": "string",
  "password": "string"
}
```

**Response:**
```json
{
  "result": {
    "id": "guid",
    "username": "string",
    "email": "string",
    "jwtToken": "string",
    "refreshToken": "string",
    "isBeamedIn": true,
    "lastBeamedIn": "2024-01-01T00:00:00Z"
  },
  "isError": false,
  "message": "Avatar successfully authenticated"
}
```

### Get Current Avatar
**GET** `/api/avatar/current`

Get details of currently authenticated avatar.

**Headers:**
- `Authorization: Bearer YOUR_JWT_TOKEN`

**Response:**
```json
{
  "result": {
    "id": "guid",
    "username": "string",
    "email": "string",
    "firstName": "string",
    "lastName": "string",
    "karma": 0,
    "avatarType": "User",
    "isBeamedIn": true,
    "wallets": [],
    "nfts": []
  },
  "isError": false
}
```

### Update Avatar
**PUT** `/api/avatar/{id}`

Update avatar information.

**Headers:**
- `Authorization: Bearer YOUR_JWT_TOKEN`

**Request Body:**
```json
{
  "firstName": "string",
  "lastName": "string",
  "email": "string"
}
```

### Delete Avatar
**DELETE** `/api/avatar/{id}`

Delete avatar account.

**Headers:**
- `Authorization: Bearer YOUR_JWT_TOKEN`

---

## NFT Management

### Mint NFT
**POST** `/api/nft/mint-nft`

Mint a new NFT using specified providers.

**Headers:**
- `Authorization: Bearer YOUR_JWT_TOKEN`

**Request Body:**
```json
{
  "OnChainProvider": "ArbitrumOASIS",
  "OffChainProvider": "MongoDBOASIS",
  "AvatarId": "guid",
  "WalletAddress": "string",
  "Title": "string",
  "Description": "string",
  "ImageUrl": "string",
  "MetaData": {
    "property1": "value1",
    "property2": "value2"
  }
}
```

**Response:**
```json
{
  "result": {
    "oasisnft": {
      "id": "guid",
      "title": "string",
      "description": "string",
      "imageUrl": "string",
      "hash": "string",
      "mintedByAddress": "string",
      "mintedOn": "2024-01-01T00:00:00Z"
    },
    "transactionResult": "0x..."
  },
  "isError": false,
  "message": "Successfully minted the NFT"
}
```

### Load NFT
**GET** `/api/nft/load-nft/{id}`

Load NFT details by ID.

**Headers:**
- `Authorization: Bearer YOUR_JWT_TOKEN`

**Response:**
```json
{
  "result": {
    "id": "guid",
    "title": "string",
    "description": "string",
    "imageUrl": "string",
    "hash": "string",
    "mintedByAddress": "string",
    "mintedOn": "2024-01-01T00:00:00Z",
    "metaData": {}
  },
  "isError": false
}
```

### Load NFTs for Avatar
**GET** `/api/nft/load-nfts-for-avatar/{avatarId}`

Load all NFTs owned by specific avatar.

**Headers:**
- `Authorization: Bearer YOUR_JWT_TOKEN`

**Response:**
```json
{
  "result": [
    {
      "id": "guid",
      "title": "string",
      "description": "string",
      "imageUrl": "string"
    }
  ],
  "isError": false
}
```

---

## Data Management

### Save Holon
**POST** `/api/data/save-holon`

Save a data holon (generic data object).

**Headers:**
- `Authorization: Bearer YOUR_JWT_TOKEN`

**Request Body:**
```json
{
  "name": "string",
  "description": "string",
  "holonType": "string",
  "parentHolonId": "guid",
  "metaData": {}
}
```

**Response:**
```json
{
  "result": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "holonType": "string",
    "parentHolonId": "guid",
    "createdDate": "2024-01-01T00:00:00Z"
  },
  "isError": false,
  "message": "Holon successfully saved"
}
```

### Load Holon
**GET** `/api/data/load-holon/{id}`

Load holon by ID.

**Headers:**
- `Authorization: Bearer YOUR_JWT_TOKEN`

### Load Holons for Parent
**GET** `/api/data/load-holons-for-parent/{parentId}`

Load all child holons for a parent holon.

**Headers:**
- `Authorization: Bearer YOUR_JWT_TOKEN`

### Delete Holon
**DELETE** `/api/data/delete-holon/{id}`

Delete holon by ID.

**Headers:**
- `Authorization: Bearer YOUR_JWT_TOKEN`

---

## Provider Management

### Get Current Storage Provider
**GET** `/api/provider/get-current-storage-provider`

Get currently active storage provider.

**Headers:**
- `Authorization: Bearer YOUR_JWT_TOKEN`

**Response:**
```json
{
  "result": {
    "providerName": "MongoDBOASIS",
    "providerDescription": "MongoDB Provider",
    "providerType": "MongoDBOASIS",
    "isProviderActivated": true
  },
  "isError": false
}
```

### Get Current Storage Provider Type
**GET** `/api/provider/get-current-storage-provider-type`

Get currently active storage provider type.

**Headers:**
- `Authorization: Bearer YOUR_JWT_TOKEN`

### Get All Registered Providers
**GET** `/api/provider/get-all-registered-providers`

Get list of all registered providers.

**Headers:**
- `Authorization: Bearer YOUR_JWT_TOKEN`

### Activate Provider
**POST** `/api/provider/activate-provider/{providerType}`

Activate specific provider.

**Headers:**
- `Authorization: Bearer YOUR_JWT_TOKEN`

**Path Parameters:**
- `providerType` (string) - Provider type to activate

---

## Provider-Specific Endpoints

Most endpoints support provider-specific versions with the format:
`/api/{controller}/{action}/{providerType}/{setGlobally}`

### Parameters:
- `providerType` (string) - Provider to use for this request
- `setGlobally` (boolean) - Whether to set provider globally for future requests

### Examples:

#### Register with Specific Provider
**POST** `/api/avatar/register/MongoDBOASIS/false`

Register avatar using MongoDB provider for this request only.

#### Mint NFT with Specific Provider
**POST** `/api/nft/mint-nft/ArbitrumOASIS/true`

Mint NFT using Arbitrum provider and set it globally for future requests.

#### Save Holon with Specific Provider
**POST** `/api/data/save-holon/Neo4jOASIS/false`

Save holon using Neo4j provider for this request only.

---

## Available Provider Types

### Storage Providers
- `MongoDBOASIS` - MongoDB document database
- `SQLLiteDBOASIS` - SQLite relational database
- `Neo4jOASIS` - Neo4j graph database
- `LocalFileOASIS` - Local file system storage

### Blockchain Providers
- `EthereumOASIS` - Ethereum blockchain
- `ArbitrumOASIS` - Arbitrum layer 2
- `PolygonOASIS` - Polygon network
- `SolanaOASIS` - Solana blockchain
- `EOSIOOASIS` - EOSIO blockchain
- `TRONOASIS` - TRON blockchain
- `RootstockOASIS` - Rootstock blockchain

### Network Providers
- `HoloOASIS` - Holochain network
- `IPFSOASIS` - IPFS decentralized storage
- `PinataOASIS` - Pinata IPFS service

---

## Error Responses

All endpoints return standardized error responses:

```json
{
  "result": null,
  "isError": true,
  "message": "Error description",
  "detailedMessage": "Detailed error information",
  "errorCount": 1,
  "warningCount": 0
}
```

### Common HTTP Status Codes:
- `200` - Success
- `400` - Bad Request
- `401` - Unauthorized
- `403` - Forbidden
- `404` - Not Found
- `500` - Internal Server Error

---

## Rate Limiting

API requests are subject to rate limiting:
- **Authentication endpoints**: 5 requests per minute
- **General endpoints**: 100 requests per minute
- **NFT operations**: 10 requests per minute

Rate limit headers are included in responses:
- `X-RateLimit-Limit` - Request limit per window
- `X-RateLimit-Remaining` - Remaining requests in current window
- `X-RateLimit-Reset` - Time when rate limit resets

---

## WebSocket Support

Some endpoints support WebSocket connections for real-time updates:

### Connection
```javascript
const ws = new WebSocket('wss://localhost:5002/ws');

ws.onopen = function() {
    // Send authentication
    ws.send(JSON.stringify({
        type: 'auth',
        token: 'YOUR_JWT_TOKEN'
    }));
};

ws.onmessage = function(event) {
    const data = JSON.parse(event.data);
    // Handle real-time updates
};
```

---

## SDKs and Libraries

### .NET SDK
```csharp
using NextGenSoftware.OASIS.API.Core.Managers;

var avatarManager = new AvatarManager();
var result = await avatarManager.AuthenticateAsync("username", "password");
```

### JavaScript SDK
```javascript
import { OASISClient } from '@oasis/sdk';

const client = new OASISClient('https://localhost:5002');
const result = await client.avatar.authenticate('username', 'password');
```

---

## Testing with Postman

Import the OASIS API Postman collection:
1. Download: [OASIS API Collection](https://oasisweb4.one/postman/OASIS_API.postman_collection.json)
2. Import into Postman
3. Set environment variables:
   - `baseUrl`: `https://localhost:5002`
   - `jwtToken`: Your JWT token
4. Run the collection tests

---

## Support

For API support and questions:
- **Email**: ourworld@nextgensoftware.co.uk
- **GitHub Issues**: [Create an issue](https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK/issues)
- **Telegram**: [OASIS API Chat](https://t.me/ourworldthegamechat)
- **Discord**: [OASIS Discord](https://discord.gg/q9gMKU6)
