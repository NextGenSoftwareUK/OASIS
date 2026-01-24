# NFT API Improvements - Quick Reference Guide

## 🎯 Priority Improvements

### 1. Add Pagination (HIGH PRIORITY)

**Current:**
```http
GET /api/nft/load-all-nfts-for_avatar/{avatarId}
```

**Improved:**
```http
GET /api/nft/avatars/{avatarId}/nfts?page=1&limit=50&sort=createdDate&order=desc
```

**Implementation:**
```csharp
[HttpGet]
[Route("avatars/{avatarId}/nfts")]
public async Task<OASISResult<PagedResult<IWeb4NFT>>> GetNFTsForAvatarAsync(
    Guid avatarId,
    [FromQuery] int page = 1,
    [FromQuery] int limit = 50,
    [FromQuery] string sort = "createdDate",
    [FromQuery] string order = "desc")
{
    // Validate limit (max 100)
    limit = Math.Min(limit, 100);
    
    var result = await NFTManager.LoadAllWeb4NFTsForAvatarAsync(avatarId);
    
    // Apply pagination
    var pagedResult = new PagedResult<IWeb4NFT>
    {
        Items = result.Result.Skip((page - 1) * limit).Take(limit).ToList(),
        Page = page,
        Limit = limit,
        Total = result.Result.Count(),
        TotalPages = (int)Math.Ceiling(result.Result.Count() / (double)limit)
    };
    
    return new OASISResult<PagedResult<IWeb4NFT>> { Result = pagedResult };
}
```

---

### 2. Add RESTful Endpoint Aliases

**Add alongside existing endpoints for backward compatibility:**

```csharp
// New RESTful endpoint
[HttpGet]
[Route("avatars/{avatarId}/nfts")]
public async Task<OASISResult<IEnumerable<IWeb4NFT>>> GetNFTsForAvatarRESTAsync(Guid avatarId)
{
    // Call existing method
    return await LoadAllWeb4NFTsForAvatarAsync(avatarId);
}

// Keep old endpoint with [Obsolete] attribute
[Obsolete("Use GET /api/nft/avatars/{avatarId}/nfts instead")]
[HttpGet]
[Route("load-all-nfts-for_avatar/{avatarId}")]
public async Task<OASISResult<IEnumerable<IWeb4NFT>>> LoadAllWeb4NFTsForAvatarAsync(Guid avatarId)
{
    // Existing implementation
}
```

---

### 3. Add Batch Operations

**New Endpoint:**
```csharp
[HttpPost]
[Route("metadata/batch")]
public async Task<OASISResult<Dictionary<string, IWeb4NFT>>> GetNFTMetadataBatchAsync(
    [FromBody] BatchNFTMetadataRequest request)
{
    var results = new Dictionary<string, IWeb4NFT>();
    
    var tasks = request.NFTIds.Select(async id =>
    {
        var result = await NFTManager.LoadWeb4NftAsync(id);
        if (!result.IsError && result.Result != null)
        {
            results[id] = result.Result;
        }
    });
    
    await Task.WhenAll(tasks);
    
    return new OASISResult<Dictionary<string, IWeb4NFT>> { Result = results };
}
```

**Request Model:**
```csharp
public class BatchNFTMetadataRequest
{
    public List<string> NFTIds { get; set; }
    public bool IncludeAttributes { get; set; } = true;
    public bool IncludeRarity { get; set; } = false;
}
```

---

### 4. Add Owners Lookup

**New Endpoint:**
```csharp
[HttpGet]
[Route("{nftId}/owners")]
public async Task<OASISResult<IEnumerable<NFTOwner>>> GetNFTOwnersAsync(Guid nftId)
{
    // Load NFT
    var nftResult = await NFTManager.LoadWeb4NftAsync(nftId);
    if (nftResult.IsError || nftResult.Result == null)
    {
        return new OASISResult<IEnumerable<NFTOwner>> 
        { 
            IsError = true, 
            Message = "NFT not found" 
        };
    }
    
    // Get owners from blockchain provider
    var provider = NFTManager.GetNFTProvider(nftResult.Result.Provider.Value);
    var owners = await provider.Result.GetOwnersForNFTAsync(nftResult.Result.OnChainHash);
    
    return new OASISResult<IEnumerable<NFTOwner>> { Result = owners.Result };
}
```

---

### 5. Add Contract/Collection Metadata

**New Endpoint:**
```csharp
[HttpGet]
[Route("collections/{collectionId}/metadata")]
public async Task<OASISResult<CollectionMetadata>> GetCollectionMetadataAsync(Guid collectionId)
{
    // Load collection
    var collection = await NFTManager.LoadCollectionAsync(collectionId);
    
    // Aggregate metadata
    var metadata = new CollectionMetadata
    {
        CollectionId = collectionId,
        Name = collection.Result.Name,
        Description = collection.Result.Description,
        TotalSupply = collection.Result.TotalSupply,
        FloorPrice = await GetFloorPriceAsync(collectionId),
        Owners = await GetCollectionOwnersAsync(collectionId),
        Attributes = await SummarizeAttributesAsync(collectionId)
    };
    
    return new OASISResult<CollectionMetadata> { Result = metadata };
}
```

---

### 6. Add Wallet-Based Queries

**New Endpoint:**
```csharp
[HttpGet]
[Route("wallets/{walletAddress}/nfts")]
public async Task<OASISResult<IEnumerable<IWeb4NFT>>> GetNFTsForWalletAsync(
    string walletAddress,
    [FromQuery] string provider = null)
{
    // If provider specified, use it; otherwise try all providers
    if (!string.IsNullOrEmpty(provider))
    {
        var providerType = Enum.Parse<ProviderType>(provider);
        return await NFTManager.LoadAllWeb4NFTsForMintAddressAsync(walletAddress, providerType);
    }
    
    // Try to find wallet across all providers
    var allNFTs = new List<IWeb4NFT>();
    var providers = new[] { ProviderType.SolanaOASIS, ProviderType.EthereumOASIS };
    
    foreach (var providerType in providers)
    {
        var result = await NFTManager.LoadAllWeb4NFTsForMintAddressAsync(walletAddress, providerType);
        if (!result.IsError && result.Result != null)
        {
            allNFTs.AddRange(result.Result);
        }
    }
    
    return new OASISResult<IEnumerable<IWeb4NFT>> { Result = allNFTs };
}
```

---

### 7. Improve Error Responses

**Standard Error Response:**
```csharp
public class OASISErrorResponse
{
    public string ErrorCode { get; set; }
    public string Message { get; set; }
    public List<ValidationError> Errors { get; set; }
    public string RequestId { get; set; }
}

// Usage in controller
if (avatarId == Guid.Empty)
{
    return BadRequest(new OASISErrorResponse
    {
        ErrorCode = "INVALID_AVATAR_ID",
        Message = "Avatar ID must be a valid GUID",
        RequestId = HttpContext.TraceIdentifier
    });
}
```

**Error Codes:**
- `INVALID_AVATAR_ID` - Invalid avatar GUID
- `NFT_NOT_FOUND` - NFT doesn't exist
- `UNAUTHORIZED` - Missing/invalid token
- `RATE_LIMIT_EXCEEDED` - Too many requests
- `INVALID_PROVIDER` - Provider not supported
- `VALIDATION_ERROR` - Request validation failed

---

### 8. Add OpenAPI/Swagger Documentation

**Install Package:**
```bash
dotnet add package Swashbuckle.AspNetCore
```

**Configure in Startup.cs:**
```csharp
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "OASIS NFT API", 
        Version = "v1",
        Description = "Comprehensive NFT management API for the OASIS ecosystem"
    });
    
    // Add JWT authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

**Add XML Comments:**
```csharp
/// <summary>
/// Retrieves all NFTs owned by a specific avatar
/// </summary>
/// <param name="avatarId">The OASIS avatar ID (GUID)</param>
/// <param name="page">Page number (default: 1)</param>
/// <param name="limit">Items per page (default: 50, max: 100)</param>
/// <returns>Paginated list of NFTs</returns>
/// <response code="200">Returns the list of NFTs</response>
/// <response code="400">Invalid avatar ID</response>
/// <response code="401">Unauthorized - missing or invalid JWT token</response>
[HttpGet]
[Route("avatars/{avatarId}/nfts")]
[ProducesResponseType(typeof(OASISResult<PagedResult<IWeb4NFT>>), 200)]
[ProducesResponseType(typeof(OASISErrorResponse), 400)]
[ProducesResponseType(typeof(OASISErrorResponse), 401)]
public async Task<OASISResult<PagedResult<IWeb4NFT>>> GetNFTsForAvatarAsync(...)
```

---

## 📝 Documentation Template

### Endpoint Documentation Template

```markdown
## Get NFTs for Avatar

**Endpoint:** `GET /api/nft/avatars/{avatarId}/nfts`

**Description:**
Retrieves all NFTs owned by a specific OASIS avatar with pagination support.

**Authentication:** Required (JWT Bearer token)

**Parameters:**

| Parameter | Type | Location | Required | Default | Description |
|-----------|------|----------|----------|---------|-------------|
| avatarId | GUID | Path | Yes | - | The OASIS avatar ID |
| page | int | Query | No | 1 | Page number |
| limit | int | Query | No | 50 | Items per page (max: 100) |
| sort | string | Query | No | createdDate | Sort field |
| order | string | Query | No | desc | Sort order (asc/desc) |
| provider | string | Query | No | - | Filter by provider |

**Request Example:**
```http
GET /api/nft/avatars/123e4567-e89b-12d3-a456-426614174000/nfts?page=1&limit=20
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response Example:**
```json
{
  "result": {
    "items": [
      {
        "id": "nft-id-123",
        "title": "My Awesome NFT",
        "imageUrl": "https://ipfs.io/...",
        "price": 0.5
      }
    ],
    "pagination": {
      "page": 1,
      "limit": 20,
      "total": 156,
      "totalPages": 8
    }
  },
  "isError": false
}
```

**Error Responses:**

| Status | Error Code | Description |
|--------|-----------|-------------|
| 400 | INVALID_AVATAR_ID | Avatar ID is not a valid GUID |
| 401 | UNAUTHORIZED | Missing or invalid JWT token |
| 404 | AVATAR_NOT_FOUND | Avatar does not exist |
| 429 | RATE_LIMIT_EXCEEDED | Too many requests |

**Rate Limits:**
- Free: 100 requests/minute
- Pro: 1000 requests/minute

**Code Examples:**

```typescript
// TypeScript
const response = await fetch(
  `/api/nft/avatars/${avatarId}/nfts?page=1&limit=20`,
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
    f'/api/nft/avatars/{avatar_id}/nfts',
    params={'page': 1, 'limit': 20},
    headers={'Authorization': f'Bearer {token}'}
)
data = response.json()
```
```

---

## 🔧 Helper Classes

### PagedResult Model

```csharp
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
    public int TotalPages => (int)Math.Ceiling(Total / (double)Limit);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}
```

### Standard Error Response

```csharp
public class OASISErrorResponse
{
    public string ErrorCode { get; set; }
    public string Message { get; set; }
    public List<ValidationError> Errors { get; set; }
    public string RequestId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ValidationError
{
    public string Field { get; set; }
    public string Message { get; set; }
}
```

---

## ✅ Implementation Checklist

### Phase 1: Critical (Week 1)
- [ ] Add pagination to all list endpoints
- [ ] Add RESTful endpoint aliases
- [ ] Improve error responses
- [ ] Add OpenAPI/Swagger
- [ ] Update documentation with examples

### Phase 2: High Priority (Week 2-3)
- [ ] Add batch operations
- [ ] Add owners lookup endpoint
- [ ] Add collection metadata endpoint
- [ ] Add wallet-based queries
- [ ] Add comprehensive error codes

### Phase 3: Medium Priority (Week 4+)
- [ ] Add rarity computation
- [ ] Add metadata refresh
- [ ] Add marketplace integration
- [ ] Add spam detection
- [ ] Add webhooks

---

## 📚 Additional Resources

- [Alchemy NFT API Docs](https://www.alchemy.com/docs/reference/nft-api-endpoints)
- [RESTful API Design Best Practices](https://restfulapi.net/)
- [OpenAPI Specification](https://swagger.io/specification/)

---

*Last Updated: January 24, 2026*
