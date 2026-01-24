# NFT API

## Overview

The NFT API provides comprehensive NFT management services for the OASIS ecosystem. It handles NFT creation, minting, trading, and analytics with support for multiple blockchain standards, real-time updates, and advanced security features.

**Base URL:** `/api/nft`

**Authentication:** Required (Bearer token)

**Rate Limits:**
- Free tier: 100 requests/minute
- Pro tier: 1,000 requests/minute

**Key Features:**
- ✅ **Cross-Chain Support** - Works with 50+ blockchain networks
- ✅ **Web4 NFTs** - Enhanced NFT format with additional metadata
- ✅ **GeoNFTs** - Location-based NFTs for AR/VR experiences
- ✅ **Multi-Provider** - Support for multiple on-chain and off-chain providers
- ✅ **Avatar-Based** - NFT ownership tied to OASIS avatars

---

## Quick Start

### Your First NFT Operation

**Step 1: Get NFTs for your avatar**

```http
GET http://api.oasisweb4.com/api/nft/load-all-nfts-for_avatar/{avatarId}
Authorization: Bearer YOUR_JWT_TOKEN
```

**Step 2: Mint a new NFT**

```http
POST http://api.oasisweb4.com/api/nft/mint-nft
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "title": "My First NFT",
  "description": "A unique digital asset",
  "imageUrl": "https://ipfs.io/...",
  "symbol": "MYNFT",
  "onChainProvider": "SolanaOASIS",
  "offChainProvider": "MongoDBOASIS"
}
```

---

## NFT Ownership Endpoints

### Get NFTs for Avatar

Retrieve all NFTs owned by a specific avatar.

**Endpoint:** `GET /api/nft/load-all-nfts-for_avatar/{avatarId}`

**Authentication:** Required

**Parameters:**

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| avatarId | GUID | path | Yes | The OASIS avatar ID |

**Response:**
```json
{
  "result": [
    {
      "id": "nft-id-123",
      "title": "My Awesome NFT",
      "description": "A unique digital asset",
      "imageUrl": "https://ipfs.io/...",
      "mintWalletAddress": "0x...",
      "onChainHash": "0x...",
      "provider": {
        "value": 3,
        "name": "SolanaOASIS"
      },
      "standard": {
        "value": 0,
        "name": "SPL"
      },
      "price": 0.5,
      "createdDate": "2024-01-15T10:30:00Z"
    }
  ],
  "isError": false,
  "message": "Success"
}
```

**Code Examples:**

```typescript
// TypeScript/JavaScript
const response = await fetch(
  `http://api.oasisweb4.com/api/nft/load-all-nfts-for_avatar/${avatarId}`,
  {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  }
);
const data = await response.json();
```

```python
# Python
import requests

response = requests.get(
    f'http://api.oasisweb4.com/api/nft/load-all-nfts-for_avatar/{avatar_id}',
    headers={'Authorization': f'Bearer {token}'}
)
data = response.json()
```

---

### Get NFTs for Mint Wallet Address

Retrieve all NFTs for a specific mint wallet address.

**Endpoint:** `GET /api/nft/load-all-nfts-for-mint-wallet-address/{mintWalletAddress}`

**Authentication:** Required

**Parameters:**

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| mintWalletAddress | string | path | Yes | Wallet address (e.g., Solana or Ethereum address) |

**Response:**
```json
{
  "result": [
    {
      "id": "nft-id-123",
      "title": "NFT Title",
      "mintWalletAddress": "0x...",
      "onChainHash": "0x..."
    }
  ],
  "isError": false
}
```

---

### Get NFT by ID

Load a specific NFT by its OASIS ID.

**Endpoint:** `GET /api/nft/load-nft-by-id/{id}`

**Authentication:** Required

**Parameters:**

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| id | GUID | path | Yes | OASIS NFT ID |

**Response:**
```json
{
  "result": {
    "id": "nft-id-123",
    "title": "My Awesome NFT",
    "description": "A unique digital asset",
    "imageUrl": "https://ipfs.io/...",
    "thumbnailUrl": "https://ipfs.io/...",
    "mintWalletAddress": "0x...",
    "onChainHash": "0x...",
    "provider": {
      "value": 3,
      "name": "SolanaOASIS"
    },
    "standard": {
      "value": 0,
      "name": "SPL"
    },
    "price": 0.5,
    "symbol": "MYNFT",
    "createdDate": "2024-01-15T10:30:00Z",
    "metadata": {
      "attributes": [
        {
          "trait_type": "Color",
          "value": "Blue"
        }
      ]
    }
  },
  "isError": false
}
```

---

### Get NFT by Hash

Load a specific NFT by its on-chain hash.

**Endpoint:** `GET /api/nft/load-nft-by-hash/{hash}`

**Authentication:** Required

**Parameters:**

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| hash | string | path | Yes | On-chain transaction hash |

---

### Get All NFTs (Admin)

Get all NFTs in the system (Wizard/Admin only).

**Endpoint:** `GET /api/nft/load-all-nfts`

**Authentication:** Required (Wizard only)

---

## NFT Minting

### Mint NFT

Mint a new Web4 NFT on the blockchain.

**Endpoint:** `POST /api/nft/mint-nft`

**Authentication:** Required

**Request Body:**
```json
{
  "title": "My Awesome NFT",
  "description": "A unique digital asset",
  "imageUrl": "https://ipfs.io/ipfs/...",
  "thumbnailUrl": "https://ipfs.io/ipfs/...",
  "symbol": "MYNFT",
  "price": 0.5,
  "numberToMint": 1,
  "onChainProvider": "SolanaOASIS",
  "offChainProvider": "MongoDBOASIS",
  "nftStandardType": "SPL",
  "nftOffChainMetaType": "OASIS",
  "jsonMetaDataURL": "https://ipfs.io/ipfs/...",
  "sendToAddressAfterMinting": "0x...",
  "sendToAvatarAfterMintingId": "avatar-id-here",
  "waitTillNFTMinted": true,
  "waitForNFTToMintInSeconds": 60
}
```

**Request Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| title | string | Yes | - | NFT title |
| description | string | No | - | NFT description |
| imageUrl | string | No | Placeholder | Image URL |
| thumbnailUrl | string | No | - | Thumbnail URL |
| symbol | string | Yes | - | NFT symbol/ticker |
| price | number | No | 0 | NFT price |
| numberToMint | number | No | 1 | Number of NFTs to mint |
| onChainProvider | string | No | SolanaOASIS | Blockchain provider |
| offChainProvider | string | No | MongoDBOASIS | Storage provider |
| nftStandardType | string | No | Auto-detect | NFT standard (SPL, ERC1155, etc.) |
| nftOffChainMetaType | string | No | OASIS | Metadata type |
| jsonMetaDataURL | string | No | - | Metadata JSON URL |
| sendToAddressAfterMinting | string | No | - | Wallet address to send to |
| sendToAvatarAfterMintingId | string | No | Authenticated avatar | Avatar ID to send to |

**Response:**
```json
{
  "result": {
    "id": "nft-id-123",
    "title": "My Awesome NFT",
    "mintWalletAddress": "0x...",
    "onChainHash": "0x...",
    "provider": {
      "value": 3,
      "name": "SolanaOASIS"
    }
  },
  "isError": false,
  "message": "NFT minted successfully"
}
```

**Code Example:**
```typescript
const response = await fetch('http://api.oasisweb4.com/api/nft/mint-nft', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    title: 'My Awesome NFT',
    description: 'A unique digital asset',
    imageUrl: 'https://ipfs.io/ipfs/...',
    symbol: 'MYNFT',
    onChainProvider: 'SolanaOASIS'
  })
});
const data = await response.json();
```

---

### Remint NFT

Remint an existing NFT.

**Endpoint:** `POST /api/nft/remint-nft`

**Authentication:** Required

**Request Body:**
```json
{
  "originalNFTId": "original-nft-id",
  "title": "Reminted NFT",
  "description": "Reminted version"
}
```

---

## NFT Transfer

### Send NFT

Transfer an NFT from one wallet to another.

**Endpoint:** `POST /api/nft/send-nft`

**Authentication:** Required

**Request Body:**
```json
{
  "fromWalletAddress": "0x...",
  "toWalletAddress": "0x...",
  "fromProvider": "SolanaOASIS",
  "toProvider": "SolanaOASIS",
  "amount": 1,
  "memoText": "Transfer message",
  "waitTillNFTSent": true,
  "waitForNFTToSendInSeconds": 60,
  "attemptToSendEveryXSeconds": 5
}
```

**Request Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| fromWalletAddress | string | Yes | Source wallet address |
| toWalletAddress | string | Yes | Destination wallet address |
| fromProvider | string | Yes | Source provider (e.g., SolanaOASIS) |
| toProvider | string | Yes | Destination provider |
| amount | number | No | Amount to send (default: 1) |
| memoText | string | No | Transfer memo |
| waitTillNFTSent | boolean | No | Wait for confirmation |
| waitForNFTToSendInSeconds | number | No | Timeout in seconds |
| attemptToSendEveryXSeconds | number | No | Retry interval |

**Response:**
```json
{
  "result": {
    "transactionHash": "0x...",
    "status": "completed",
    "fromAddress": "0x...",
    "toAddress": "0x..."
  },
  "isError": false,
  "message": "NFT sent successfully"
}
```

---

## GeoNFT Endpoints

### Get GeoNFTs for Avatar

Get all GeoNFTs (location-based NFTs) for an avatar.

**Endpoint:** `GET /api/nft/load-all-geo-nfts-for-avatar/{avatarId}`

**Authentication:** Required

**Response:**
```json
{
  "result": [
    {
      "id": "geonft-id-123",
      "title": "Location NFT",
      "latitude": 51.5074,
      "longitude": -0.1278,
      "originalNFTId": "nft-id-123",
      "placedByAvatarId": "avatar-id"
    }
  ],
  "isError": false
}
```

---

### Get GeoNFTs for Mint Address

Get all GeoNFTs for a mint wallet address.

**Endpoint:** `GET /api/nft/load-all-geo-nfts-for-mint-wallet-address/{mintWalletAddress}`

**Authentication:** Required

---

### Place GeoNFT

Place an existing NFT at real-world coordinates.

**Endpoint:** `POST /api/nft/place-geo-nft`

**Authentication:** Required

**Request Body:**
```json
{
  "originalOASISNFTId": "nft-id-123",
  "lat": 51.5074,
  "long": -0.1278,
  "allowOtherPlayersToAlsoCollect": true,
  "permSpawn": false,
  "globalSpawnQuantity": 1,
  "playerSpawnQuantity": 1,
  "respawnDurationInSeconds": 0
}
```

---

### Mint and Place GeoNFT

Mint a new NFT and place it at coordinates in one operation.

**Endpoint:** `POST /api/nft/mint-and-place-geo-nft`

**Authentication:** Required

**Request Body:**
```json
{
  "title": "Location NFT",
  "description": "NFT placed at location",
  "imageUrl": "https://ipfs.io/...",
  "symbol": "LOCNFT",
  "lat": 51.5074,
  "long": -0.1278,
  "onChainProvider": "SolanaOASIS",
  "offChainProvider": "MongoDBOASIS"
}
```

---

## NFT Import/Export

### Import Web3 NFT

Import an existing Web3 NFT into the OASIS system.

**Endpoint:** `POST /api/nft/import-web3-nft`

**Authentication:** Required

**Request Body:**
```json
{
  "contractAddress": "0x...",
  "tokenId": "123",
  "chainId": 1,
  "provider": "EthereumOASIS"
}
```

---

### Import Web4 NFT

Import a Web4 NFT from JSON file or object.

**Endpoints:**
- `POST /api/nft/import-web4-nft-from-file/{importedByAvatarId}/{fullPathToOASISNFTJsonFile}`
- `POST /api/nft/import-web4-nft/{importedByAvatarId}`

**Authentication:** Required

---

### Export Web4 NFT

Export a Web4 NFT to JSON file or return as object.

**Endpoints:**
- `POST /api/nft/export-web4-nft-to-file/{oasisNFTId}/{fullPathToExportTo}`
- `POST /api/nft/export-web4-nft`

**Authentication:** Required

---

## NFT Search

### Search Web4 NFTs

Search for NFTs using keywords and filters.

**Endpoint:** `GET /api/nft/search-web4-nfts/{searchTerm}/{avatarId}`

**Authentication:** Required

**Parameters:**

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| searchTerm | string | path | Yes | Search keywords |
| avatarId | GUID | path | Yes | Avatar ID |
| searchOnlyForCurrentAvatar | boolean | query | No | Filter to current avatar |
| providerType | string | query | No | Filter by provider |

**Response:**
```json
{
  "result": [
    {
      "id": "nft-id-123",
      "title": "Matching NFT",
      "description": "NFT matching search term"
    }
  ],
  "isError": false
}
```

---

## NFT Collections

### Create Web4 NFT Collection

Create a new NFT collection.

**Endpoint:** `POST /api/nft/create-web4-nft-collection`

**Authentication:** Required

**Request Body:**
```json
{
  "name": "My Collection",
  "description": "Collection description",
  "imageUrl": "https://ipfs.io/...",
  "symbol": "MYCOLL"
}
```

---

### Search Web4 NFT Collections

Search for NFT collections.

**Endpoint:** `GET /api/nft/search-web4-nft-collections/{searchTerm}/{avatarId}`

**Authentication:** Required

---

## Web3 NFT Endpoints

### Get Web3 NFT by ID

Load a Web3 NFT by its OASIS ID.

**Endpoint:** `GET /api/nft/load-web3-nft-by-id/{id}`

**Authentication:** Required

---

### Get Web3 NFT by Hash

Load a Web3 NFT by its on-chain hash.

**Endpoint:** `GET /api/nft/load-web3-nft-by-hash/{onChainNftHash}`

**Authentication:** Required

---

### Get All Web3 NFTs for Avatar

Get all Web3 NFTs for an avatar.

**Endpoint:** `GET /api/nft/load-all-web3-nfts-for-avatar/{avatarId}`

**Authentication:** Required

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| parentWeb4NFTId | GUID | No | Filter by parent Web4 NFT |
| providerType | string | No | Filter by provider |

---

## Provider-Specific Endpoints

Most endpoints support provider-specific variants:

**Format:** `{endpoint}/{providerType}/{setGlobally}`

**Example:**
```http
GET /api/nft/load-all-nfts-for_avatar/{avatarId}/SolanaOASIS/false
```

**Available Providers:**
- `SolanaOASIS` - Solana blockchain
- `EthereumOASIS` - Ethereum blockchain
- `PolygonOASIS` - Polygon network
- `ArbitrumOASIS` - Arbitrum L2
- And 40+ more...

---

## Request/Response Schemas

### MintNFTTransactionRequest

```json
{
  "title": "string (required)",
  "description": "string (optional)",
  "imageUrl": "string (optional)",
  "thumbnailUrl": "string (optional)",
  "symbol": "string (required)",
  "price": "number (optional, default: 0)",
  "numberToMint": "number (optional, default: 1)",
  "onChainProvider": "string (optional, default: SolanaOASIS)",
  "offChainProvider": "string (optional, default: MongoDBOASIS)",
  "nftStandardType": "string (optional, auto-detected)",
  "nftOffChainMetaType": "string (optional, default: OASIS)",
  "jsonMetaDataURL": "string (optional)",
  "sendToAddressAfterMinting": "string (optional)",
  "sendToAvatarAfterMintingId": "string (optional)",
  "metaData": "object (optional)"
}
```

### NFTWalletTransactionRequest

```json
{
  "fromWalletAddress": "string (required)",
  "toWalletAddress": "string (required)",
  "fromProvider": "string (required)",
  "toProvider": "string (required)",
  "amount": "number (optional, default: 1)",
  "memoText": "string (optional)",
  "waitTillNFTSent": "boolean (optional)",
  "waitForNFTToSendInSeconds": "number (optional)",
  "attemptToSendEveryXSeconds": "number (optional)"
}
```

---

## Error Handling

All errors follow a standard format:

```json
{
  "result": null,
  "isError": true,
  "message": "Human-readable error message",
  "errorCode": "ERROR_CODE"
}
```

**Common Error Codes:**

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| INVALID_NFT_ID | 400 | NFT ID is invalid |
| NFT_NOT_FOUND | 404 | NFT does not exist |
| UNAUTHORIZED | 401 | Missing or invalid authentication |
| INVALID_PROVIDER | 400 | Provider not supported |
| MINT_FAILED | 500 | NFT minting failed |
| TRANSFER_FAILED | 500 | NFT transfer failed |

---

## Use Cases

### Use Case 1: Mint Your First NFT

**Scenario:** Create and mint a new NFT

```typescript
// 1. Mint NFT
const mintResponse = await fetch('http://api.oasisweb4.com/api/nft/mint-nft', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    title: 'My First NFT',
    description: 'A unique digital asset',
    imageUrl: 'https://ipfs.io/ipfs/...',
    symbol: 'MYNFT',
    onChainProvider: 'SolanaOASIS'
  })
});
const mintedNFT = await mintResponse.json();

// 2. Get your NFTs
const nftsResponse = await fetch(
  `http://api.oasisweb4.com/api/nft/load-all-nfts-for_avatar/${avatarId}`,
  {
    headers: { 'Authorization': `Bearer ${token}` }
  }
);
const nfts = await nftsResponse.json();
```

---

### Use Case 2: Transfer NFT

**Scenario:** Send an NFT to another wallet

```typescript
const response = await fetch('http://api.oasisweb4.com/api/nft/send-nft', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    fromWalletAddress: '0x...',
    toWalletAddress: '0x...',
    fromProvider: 'SolanaOASIS',
    toProvider: 'SolanaOASIS',
    amount: 1,
    memoText: 'Transfer message'
  })
});
```

---

### Use Case 3: Create Location-Based NFT

**Scenario:** Mint and place an NFT at a real-world location

```typescript
const response = await fetch('http://api.oasisweb4.com/api/nft/mint-and-place-geo-nft', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    title: 'London Landmark NFT',
    description: 'NFT placed at London location',
    imageUrl: 'https://ipfs.io/ipfs/...',
    symbol: 'LONDON',
    lat: 51.5074,
    long: -0.1278,
    onChainProvider: 'SolanaOASIS'
  })
});
```

---

## Best Practices

1. **Always handle errors** - Check `isError` flag in responses
2. **Use appropriate providers** - Choose provider based on your use case
3. **Wait for confirmations** - Use `waitTillNFTMinted` for important operations
4. **Store metadata on IPFS** - Use IPFS for decentralized metadata storage
5. **Respect rate limits** - Implement exponential backoff

---

## Known Limitations & Future Improvements

### Current Limitations
- ❌ No pagination support (returns all results)
- ❌ No batch operations
- ❌ No owners lookup (who owns this NFT?)
- ❌ No rarity computation
- ❌ No metadata refresh/invalidation
- ❌ No marketplace integration (floor price, sales)

### Planned Improvements
- ✅ Add pagination to all list endpoints
- ✅ Add batch metadata operations
- ✅ Add owners lookup endpoint
- ✅ Add collection metadata endpoints
- ✅ Add rarity computation
- ✅ Add marketplace integration

[View improvement recommendations →](../../../NFT_API_COMPARISON_AND_IMPROVEMENTS.md)

---

## Related Documentation

- [Wallet API](wallet-api.md) - Multi-chain wallet operations
- [Avatar API](../authentication-identity/avatar-api.md) - User identity management
- [HyperDrive API](../network-operations/hyperdrive-api.md) - Auto-failover system

---

## Support

- **Questions?** → [Join Discord](https://discord.gg/oasis)
- **Issues?** → [Report on GitHub](https://github.com/NextGenSoftwareUK/OASIS/issues)
- **Swagger UI:** [http://api.oasisweb4.com/swagger/index.html](http://api.oasisweb4.com/swagger/index.html)

---

*Last Updated: January 24, 2026*
*API Version: v4.4.4*
