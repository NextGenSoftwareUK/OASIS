# STAR CLI vs REST API: NFT Minting Comparison

**Date:** January 2026  
**Purpose:** Understanding how STAR CLI mints NFTs vs REST API approach

---

## Key Difference

**STAR CLI** uses **native C# managers** directly (no HTTP calls)  
**REST API** uses **HTTP endpoints** with JWT authentication

---

## STAR CLI Approach (Native Managers)

### Authentication Flow

```csharp
// STAR CLI uses native authentication
await STAR.BeamInAsync(username, password);

// This calls:
OASISAPI.Avatars.AuthenticateAsync(username, password, IPAddress);

// Sets:
STAR.BeamedInAvatar = authenticated avatar
```

**Key Points:**
- Uses native `AvatarManager.AuthenticateAsync()` directly
- No JWT tokens needed
- Avatar is stored in `STAR.BeamedInAvatar`
- Providers are managed natively (no HTTP registration/activation needed)

### NFT Minting Flow

```csharp
// STAR CLI NFT minting
public async Task<OASISResult<IWeb4NFT>> MintNFTAsync(object mintParams = null)
{
    // Generate request interactively
    IMintWeb4NFTRequest request = await NFTCommon.GenerateNFTRequestAsync();
    
    // Call native manager directly
    result = await STAR.OASISAPI.NFTs.MintNftAsync(request);
    
    // STAR.OASISAPI.NFTs uses NFTManager directly
    // No HTTP calls, no JWT tokens needed
}
```

**What STAR CLI Does:**
1. ✅ Authenticates via native `AvatarManager.AuthenticateAsync()`
2. ✅ Providers are already registered/activated in OASISDNA
3. ✅ Calls `NFTManager.MintNftAsync()` directly
4. ✅ No HTTP authentication needed
5. ✅ No provider registration/activation via API

---

## REST API Approach (HTTP Endpoints)

### Authentication Flow

```http
POST /api/avatar/authenticate
{
  "username": "OASIS_ADMIN",
  "password": "Uppermall1!"
}

Response:
{
  "result": {
    "jwtToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "id": "avatar-id"
  }
}
```

**Key Points:**
- Uses HTTP endpoint
- Returns JWT token
- Token must be sent in `Authorization: Bearer {token}` header
- Token is validated by `JwtMiddleware`

### NFT Minting Flow (Required Steps)

```http
# Step 1: Authenticate
POST /api/avatar/authenticate
→ Get JWT token

# Step 2: Register Provider
POST /api/provider/register-provider-type/SolanaOASIS
Authorization: Bearer {jwt_token}

# Step 3: Activate Provider  
POST /api/provider/activate-provider/SolanaOASIS
Authorization: Bearer {jwt_token}

# Step 4: Mint NFT
POST /api/nft/mint-nft
Authorization: Bearer {jwt_token}
{
  "Title": "My NFT",
  "OnChainProvider": "SolanaOASIS",
  ...
}
```

**What REST API Needs:**
1. ✅ Authenticate via HTTP → Get JWT token
2. ✅ Register provider via HTTP endpoint
3. ✅ Activate provider via HTTP endpoint
4. ✅ Mint NFT via HTTP endpoint with JWT token
5. ⚠️ **JWT token must be validated by middleware**

---

## Why STAR CLI Doesn't Need Provider Registration/Activation

**STAR CLI:**
- Providers are configured in `OASIS_DNA.json`
- Providers are automatically registered/activated during `IgniteStar()`
- Uses native managers that have direct access to providers
- No HTTP authentication layer

**REST API:**
- Each request is stateless
- Providers must be registered/activated per session (or globally)
- Requires JWT authentication for protected endpoints
- Middleware validates JWT token on each request

---

## Current Issue: JWT Token Not Recognized

**Problem:**
- Authentication succeeds (token received)
- Token is sent in `Authorization: Bearer {token}` header
- But middleware returns "Unauthorized" for protected endpoints

**Possible Causes:**
1. JWT middleware not running/configured properly
2. Token validation failing (secret key mismatch, expired token)
3. Token claims missing or incorrect
4. Middleware order issue (runs after [Authorize] check)
5. Avatar not being loaded into `HttpContext.Items["Avatar"]`

**What to Check in API Logs:**
- JWT validation errors
- Token parsing issues
- Missing "id" claim in token
- Avatar loading failures
- Middleware execution order

---

## Key Insight

**STAR CLI** and **REST API** are fundamentally different:

- **STAR CLI:** Native C# → Direct manager calls → No HTTP/JWT
- **REST API:** HTTP → JWT authentication → Middleware validation → Manager calls

The REST API requires:
1. Proper JWT middleware configuration
2. Token validation working correctly
3. Avatar loading into HttpContext
4. Provider registration/activation via API

---

## Recommendation

Since STAR CLI works but REST API doesn't, the issue is likely:
- JWT middleware configuration
- Token validation logic
- Middleware execution order

Check your API logs for JWT-related errors when the test runs.

---

**Status:** ✅ Analysis Complete  
**Last Updated:** January 2026
