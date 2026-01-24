# NFT API Comparison: OASIS vs Alchemy & Improvement Recommendations

## Executive Summary

This document compares the OASIS NFT API with Alchemy's NFT API and provides recommendations for improvements to both the API design and documentation.

**Key Findings:**
- OASIS has unique features (GeoNFTs, Web4 NFTs, multi-provider support) that Alchemy doesn't offer
- Alchemy has better API organization, pagination, filtering, and metadata management features
- OASIS documentation needs more examples, request/response schemas, and use cases
- API naming conventions could be more RESTful and consistent

---

## 1. Feature Comparison

### 1.1 NFT Ownership Endpoints

#### Alchemy's Approach
| Endpoint | Purpose |
|----------|---------|
| `getNFTsForOwner` | Retrieve NFTs owned by a wallet address |
| `getOwnersForNFT` | Retrieve owners of a given token |
| `getOwnersForContract` | Retrieve all owners for a contract (with block snapshotting) |
| `isHolderOfContract` | Check if wallet owns any NFT in a collection |
| `getContractsForOwner` | Get list of NFT contracts owned by wallet |
| `getCollectionsForOwner` | Get all NFT collections held by owner |

#### OASIS's Current Approach
| Endpoint | Purpose |
|----------|---------|
| `load-all-nfts-for_avatar/{avatarId}` | Load NFTs for avatar (uses avatar ID, not wallet) |
| `load-all-nfts-for-mint-wallet-address/{mintWalletAddress}` | Load NFTs for mint address |
| ❌ Missing: Get owners for a specific NFT |
| ❌ Missing: Check if holder owns NFT in collection |
| ❌ Missing: Get contracts/collections for owner |

**Gap Analysis:**
- ✅ OASIS uses avatar-based ownership (unique advantage)
- ❌ Missing wallet-address-based queries
- ❌ Missing reverse lookup (who owns this NFT?)
- ❌ Missing collection-level ownership checks

---

### 1.2 NFT Metadata Endpoints

#### Alchemy's Approach
| Endpoint | Purpose |
|----------|---------|
| `getNFTsForContract` | Retrieve all NFTs for a contract/collection |
| `getNFTMetadata` | Get metadata for specific tokenId |
| `getNFTMetadataBatch` | Batch metadata retrieval |
| `getContractMetadata` | Get collection/contract metadata |
| `getContractMetadataBatch` | Batch contract metadata |
| `computeRarity` | Compute rarity of NFT attributes |
| `invalidateContract` | Refresh metadata cache |
| `refreshNftMetadata` | Refresh specific token metadata |
| `summarizeNFTAttributes` | Generate attribute summary |
| `searchContractMetadata` | Search metadata by keywords |

#### OASIS's Current Approach
| Endpoint | Purpose |
|----------|---------|
| `load-nft-by-id/{id}` | Load NFT by OASIS ID |
| `load-nft-by-hash/{hash}` | Load NFT by on-chain hash |
| `search-web4-nfts/{searchTerm}/{avatarId}` | Search NFTs |
| ❌ Missing: Batch operations |
| ❌ Missing: Rarity computation |
| ❌ Missing: Metadata refresh/invalidation |
| ❌ Missing: Contract/collection metadata endpoints |
| ❌ Missing: Attribute summarization |

**Gap Analysis:**
- ✅ OASIS has hash-based lookup (good for cross-chain)
- ❌ No batch operations (performance issue)
- ❌ No metadata management tools
- ❌ No rarity/analytics features

---

### 1.3 NFT Sales & Marketplace

#### Alchemy's Approach
| Endpoint | Purpose |
|----------|---------|
| `getFloorPrice` | Get floor price by marketplace |
| `getNFTSales` | Get sales data across marketplaces |

#### OASIS's Current Approach
| Endpoint | Purpose |
|----------|---------|
| ❌ Missing: Floor price |
| ❌ Missing: Sales data |

**Gap Analysis:**
- ❌ No marketplace integration
- ❌ No pricing data

---

### 1.4 NFT Spam Detection

#### Alchemy's Approach
| Endpoint | Purpose |
|----------|---------|
| `getSpamContracts` | Get list of spam contracts |
| `isSpamContract` | Check if contract is spam |
| `isAirdropNFT` | Check if token is airdrop |
| `reportSpam` | Report spam contract |

#### OASIS's Current Approach
| Endpoint | Purpose |
|----------|---------|
| ❌ Missing: All spam detection features |

**Gap Analysis:**
- ❌ No spam protection
- ❌ No airdrop detection

---

### 1.5 Unique OASIS Features (Not in Alchemy)

| Feature | Description |
|---------|-------------|
| **GeoNFTs** | Geospatial NFT placement and management |
| **Web4 NFTs** | Enhanced NFT format with additional metadata |
| **Multi-Provider Support** | Support for multiple blockchain providers |
| **Avatar-Based Ownership** | NFT ownership tied to OASIS avatars |
| **Import/Export** | Import Web3 NFTs, export to JSON |
| **Mint & Place** | Combined minting and geospatial placement |

---

## 2. API Design Improvements

### 2.1 RESTful Naming Conventions

**Current Issues:**
- Inconsistent naming: `load-all-nfts-for_avatar` vs `load-all-nfts-for-mint-wallet-address`
- Verb-based routes (`load-*`, `send-*`) instead of resource-based
- Underscores in URLs (`load-all-nfts-for_avatar`)

**Recommended Improvements:**

```http
# Current (Inconsistent)
GET /api/nft/load-all-nfts-for_avatar/{avatarId}
GET /api/nft/load-all-nfts-for-mint-wallet-address/{address}

# Recommended (RESTful)
GET /api/nft/avatars/{avatarId}/nfts
GET /api/nft/wallets/{address}/nfts
GET /api/nft/collections/{collectionId}/nfts
GET /api/nft/{nftId}/owners
GET /api/nft/{nftId}/metadata
```

**Migration Strategy:**
- Keep old endpoints for backward compatibility
- Add new RESTful endpoints
- Deprecate old endpoints with clear migration path

---

### 2.2 Pagination & Filtering

**Current Issues:**
- No pagination support
- No filtering options
- No sorting capabilities
- Returns all results (could be thousands)

**Recommended Additions:**

```http
# Add query parameters
GET /api/nft/avatars/{avatarId}/nfts?page=1&limit=50&sort=createdDate&order=desc
GET /api/nft/avatars/{avatarId}/nfts?provider=SolanaOASIS&standard=SPL
GET /api/nft/avatars/{avatarId}/nfts?collectionId={id}&minPrice=0.1&maxPrice=10
```

**Response Format:**
```json
{
  "result": {
    "items": [...],
    "pagination": {
      "page": 1,
      "limit": 50,
      "total": 1234,
      "totalPages": 25
    }
  },
  "isError": false
}
```

---

### 2.3 Batch Operations

**Current Issues:**
- No batch endpoints
- Multiple round trips needed for multiple NFTs

**Recommended Additions:**

```http
# Batch metadata retrieval
POST /api/nft/metadata/batch
Body: {
  "nftIds": ["id1", "id2", "id3"],
  "includeAttributes": true,
  "includeRarity": true
}

# Batch ownership check
POST /api/nft/ownership/batch
Body: {
  "walletAddress": "0x...",
  "contractAddresses": ["0x...", "0x..."]
}
```

---

### 2.4 Metadata Management

**Recommended Additions:**

```http
# Refresh metadata
POST /api/nft/{nftId}/metadata/refresh

# Invalidate collection cache
POST /api/nft/collections/{collectionId}/metadata/invalidate

# Get contract metadata
GET /api/nft/collections/{collectionId}/metadata

# Compute rarity
GET /api/nft/{nftId}/rarity
POST /api/nft/collections/{collectionId}/rarity/batch
```

---

### 2.5 Marketplace & Sales

**Recommended Additions:**

```http
# Get floor price
GET /api/nft/collections/{collectionId}/floor-price?marketplace=opensea

# Get sales history
GET /api/nft/{nftId}/sales?limit=50
GET /api/nft/collections/{collectionId}/sales?fromDate=2024-01-01

# Get listings
GET /api/nft/collections/{collectionId}/listings?status=active
```

---

### 2.6 Spam Detection

**Recommended Additions:**

```http
# Check if contract is spam
GET /api/nft/collections/{collectionId}/spam-check

# Report spam
POST /api/nft/collections/{collectionId}/report-spam
Body: {
  "reason": "spam",
  "evidence": "..."
}

# Get spam contracts list
GET /api/nft/spam-contracts?limit=100
```

---

## 3. Documentation Improvements

### 3.1 Current Documentation Issues

1. **Missing Examples**: No request/response examples
2. **No Schema Definitions**: Missing request/response schemas
3. **Limited Use Cases**: No real-world examples
4. **No Error Documentation**: Missing error codes and handling
5. **No Rate Limits**: Missing rate limiting information
6. **No Authentication Guide**: Missing auth examples

### 3.2 Recommended Documentation Structure

```markdown
# NFT API Documentation

## Quick Start
- Authentication
- First API Call
- Common Use Cases

## Endpoints

### Get NFTs for Avatar
**Endpoint:** `GET /api/nft/avatars/{avatarId}/nfts`

**Description:**
Retrieves all NFTs owned by a specific avatar.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| avatarId | GUID | Yes | The OASIS avatar ID |
| page | int | No | Page number (default: 1) |
| limit | int | No | Items per page (default: 50, max: 100) |
| provider | string | No | Filter by provider (e.g., "SolanaOASIS") |
| sort | string | No | Sort field (createdDate, price, etc.) |
| order | string | No | Sort order (asc, desc) |

**Request Example:**
```http
GET /api/nft/avatars/123e4567-e89b-12d3-a456-426614174000/nfts?page=1&limit=20
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response Example:**
```json
{
  "result": {
    "items": [
      {
        "id": "nft-id-123",
        "title": "My Awesome NFT",
        "description": "A unique digital asset",
        "imageUrl": "https://ipfs.io/...",
        "mintWalletAddress": "0x...",
        "onChainHash": "0x...",
        "provider": "SolanaOASIS",
        "standard": "SPL",
        "price": 0.5,
        "createdDate": "2024-01-15T10:30:00Z",
        "metadata": {
          "attributes": [...],
          "properties": {...}
        }
      }
    ],
    "pagination": {
      "page": 1,
      "limit": 20,
      "total": 156,
      "totalPages": 8
    }
  },
  "isError": false,
  "message": "Success"
}
```

**Error Responses:**
| Status Code | Error | Description |
|-------------|-------|-------------|
| 400 | InvalidAvatarId | Avatar ID is not a valid GUID |
| 401 | Unauthorized | Missing or invalid JWT token |
| 404 | AvatarNotFound | Avatar does not exist |
| 429 | RateLimitExceeded | Too many requests |

**Rate Limits:**
- Free tier: 100 requests/minute
- Pro tier: 1000 requests/minute

**Use Cases:**
1. Display user's NFT gallery
2. Check NFT ownership
3. Filter NFTs by collection
```

---

### 3.3 Interactive API Documentation

**Recommended Tools:**
- **Swagger/OpenAPI**: Auto-generate interactive docs
- **Postman Collection**: Pre-configured requests
- **Code Examples**: SDK examples in multiple languages

**Example OpenAPI Schema:**
```yaml
paths:
  /api/nft/avatars/{avatarId}/nfts:
    get:
      summary: Get NFTs for Avatar
      parameters:
        - name: avatarId
          in: path
          required: true
          schema:
            type: string
            format: uuid
        - name: page
          in: query
          schema:
            type: integer
            default: 1
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/NFTListResponse'
```

---

### 3.4 Code Examples

**Add SDK Examples:**

```typescript
// TypeScript/JavaScript
import { OASISClient } from '@oasis/sdk';

const client = new OASISClient({
  apiKey: 'your-api-key',
  baseUrl: 'https://api.oasis.com'
});

// Get NFTs for avatar
const nfts = await client.nft.getNFTsForAvatar(avatarId, {
  page: 1,
  limit: 50,
  provider: 'SolanaOASIS'
});

// Mint NFT
const minted = await client.nft.mint({
  title: 'My NFT',
  description: 'Description',
  imageUrl: 'https://...',
  onChainProvider: 'SolanaOASIS'
});
```

```python
# Python
from oasis_sdk import OASISClient

client = OASISClient(api_key='your-api-key')

# Get NFTs
nfts = client.nft.get_nfts_for_avatar(
    avatar_id=avatar_id,
    page=1,
    limit=50
)

# Mint NFT
minted = client.nft.mint(
    title='My NFT',
    description='Description',
    image_url='https://...'
)
```

---

### 3.5 Migration Guides

**Add Migration Documentation:**

```markdown
## Migrating from Old Endpoints

### Old Endpoint
```
GET /api/nft/load-all-nfts-for_avatar/{avatarId}
```

### New Endpoint
```
GET /api/nft/avatars/{avatarId}/nfts
```

### Changes
- URL structure is more RESTful
- Added pagination support
- Added filtering options
- Response format includes pagination metadata

### Migration Example
```typescript
// Old way
const response = await fetch(`/api/nft/load-all-nfts-for_avatar/${avatarId}`);

// New way
const response = await fetch(`/api/nft/avatars/${avatarId}/nfts?page=1&limit=50`);
```
```

---

## 4. Implementation Priority

### Phase 1: Critical (Immediate)
1. ✅ Add pagination to all list endpoints
2. ✅ Add RESTful endpoint aliases
3. ✅ Improve error responses with proper HTTP status codes
4. ✅ Add request/response examples to documentation
5. ✅ Add OpenAPI/Swagger specification

### Phase 2: High Priority (Next Sprint)
1. ✅ Add batch operations (metadata, ownership)
2. ✅ Add contract/collection metadata endpoints
3. ✅ Add wallet-address-based queries (in addition to avatar)
4. ✅ Add owners lookup for NFTs
5. ✅ Add comprehensive error documentation

### Phase 3: Medium Priority (Future)
1. ✅ Add rarity computation
2. ✅ Add metadata refresh/invalidation
3. ✅ Add marketplace integration (floor price, sales)
4. ✅ Add spam detection
5. ✅ Add attribute summarization

### Phase 4: Nice to Have
1. ✅ Add webhooks for NFT events
2. ✅ Add analytics endpoints
3. ✅ Add NFT comparison tools
4. ✅ Add collection analytics

---

## 5. API Response Standardization

### Current Issues
- Inconsistent response formats
- Missing pagination metadata
- Error messages not standardized

### Recommended Standard Response Format

```typescript
// Success Response
{
  "result": T,  // The actual data
  "isError": false,
  "message": "Success",
  "pagination": {  // Only for list endpoints
    "page": 1,
    "limit": 50,
    "total": 1234,
    "totalPages": 25,
    "hasNext": true,
    "hasPrevious": false
  },
  "metadata": {  // Optional additional metadata
    "requestId": "req-123",
    "timestamp": "2024-01-15T10:30:00Z",
    "version": "v1"
  }
}

// Error Response
{
  "result": null,
  "isError": true,
  "message": "Human-readable error message",
  "errorCode": "INVALID_AVATAR_ID",
  "errors": [  // Optional validation errors
    {
      "field": "avatarId",
      "message": "Avatar ID must be a valid GUID"
    }
  ],
  "metadata": {
    "requestId": "req-123",
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

---

## 6. Performance Optimizations

### Recommendations
1. **Caching**: Cache metadata responses (with TTL)
2. **Batch Operations**: Reduce round trips
3. **Pagination**: Limit response sizes
4. **Field Selection**: Allow clients to request specific fields
5. **Compression**: Enable gzip compression

### Example Field Selection
```http
GET /api/nft/avatars/{avatarId}/nfts?fields=id,title,imageUrl,price
```

---

## 7. Security Enhancements

### Recommendations
1. **Rate Limiting**: Document and enforce rate limits
2. **Input Validation**: Validate all inputs (GUIDs, addresses, etc.)
3. **CORS**: Proper CORS configuration
4. **API Keys**: Support API key authentication
5. **Webhooks**: Signed webhook payloads

---

## 8. Testing & Quality

### Recommendations
1. **Postman Collection**: Public Postman collection
2. **Integration Tests**: Comprehensive test suite
3. **Mock Server**: Mock API for development
4. **Health Checks**: API health endpoint
5. **Versioning**: API versioning strategy

---

## Summary

### OASIS Strengths
- ✅ Unique GeoNFT functionality
- ✅ Multi-provider support
- ✅ Avatar-based ownership model
- ✅ Web4 NFT enhancements
- ✅ Import/export capabilities

### Areas for Improvement
- ❌ RESTful naming conventions
- ❌ Pagination and filtering
- ❌ Batch operations
- ❌ Documentation quality
- ❌ Marketplace integration
- ❌ Metadata management tools

### Quick Wins
1. Add pagination (high impact, medium effort)
2. Improve documentation with examples (high impact, low effort)
3. Add RESTful endpoint aliases (medium impact, low effort)
4. Add OpenAPI specification (high impact, medium effort)

---

## Next Steps

1. **Review this document** with the team
2. **Prioritize improvements** based on user needs
3. **Create implementation tickets** for Phase 1 items
4. **Set up OpenAPI/Swagger** documentation
5. **Create migration guide** for existing users
6. **Gather user feedback** on proposed changes

---

*Last Updated: January 24, 2026*
