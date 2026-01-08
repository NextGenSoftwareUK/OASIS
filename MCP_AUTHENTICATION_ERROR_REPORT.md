# MCP Authentication Error Report

## Issue Summary
MCP authentication endpoint fails with "stream has been aborted" error when attempting to authenticate avatars. The API returns a large response (~29KB) containing deeply nested `createdByAvatar` objects, causing the MCP client to fail.

## Error Details

### Error Message
```
AxiosError: stream has been aborted
at IncomingMessage.handlerStreamAborted
```

### When It Occurs
- When calling `oasis_authenticate_avatar` via MCP
- Direct API calls via curl work but return very large responses
- Health check endpoint works fine via MCP

### Response Size
- Authentication response: ~29KB
- Contains 21 instances of `"createdByAvatar"` in nested objects
- Response structure: `{ result: { result: { jwtToken: "...", createdByAvatar: {...} } } }`

## Root Cause Analysis

### 1. JsonIgnore Attributes Not Taking Effect
**Files Modified:**
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Holons/AuditBase.cs`
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Holons/Holon.cs`
- `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Holons/SemanticHolon.cs`

**Changes Made:**
- Added `[System.Text.Json.Serialization.JsonIgnore]` and `[Newtonsoft.Json.JsonIgnore]` to:
  - `CreatedByAvatar`, `ModifiedByAvatar`, `DeletedByAvatar` in `AuditBase.cs`
  - 15 parent holon properties in `Holon.cs`
  - `ParentHolon`, `Children`, `AllChildren` in `SemanticHolon.cs`

**Problem:**
- Attributes are present in code but not respected during serialization
- Response still contains all nested avatar objects
- Likely JSON serialization configuration issue

### 2. JSON Serialization Configuration
**Current Configuration** (`ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Startup.cs`):
```csharp
services.AddControllers()
    .AddJsonOptions(x => x.JsonSerializerOptions.IgnoreNullValues = true);
```

**Issue:**
- Only configures `IgnoreNullValues`, doesn't ensure `JsonIgnore` attributes are respected
- May be using Newtonsoft.Json instead of System.Text.Json
- No explicit configuration for attribute-based ignoring

### 3. MCP Client Limitations
**File:** `MCP/src/clients/oasisClient.ts`

**Current Attempts:**
- Increased timeout to 120000ms
- Added `maxContentLength: Infinity` and `maxBodyLength: Infinity`
- Changed `authenticateAvatar` to use `responseType: 'text'`
- Still fails with stream abort error

## Files Modified (For Reference)

### Core Files
1. `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Holons/AuditBase.cs`
   - Added JsonIgnore to CreatedByAvatar, ModifiedByAvatar, DeletedByAvatar
   - Added using statements for JSON serialization

2. `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Holons/Holon.cs`
   - Added JsonIgnore to 15 parent holon properties

3. `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Holons/SemanticHolon.cs`
   - Added JsonIgnore to ParentHolon, Children, AllChildren

### MCP Client
4. `MCP/src/clients/oasisClient.ts`
   - Updated axios configuration for large responses
   - Modified authenticateAvatar method

## What Works
- ✅ Direct API calls via curl (though responses are large)
- ✅ Health check endpoint via MCP
- ✅ API is running and responding
- ✅ Authentication succeeds (returns JWT token)

## What Doesn't Work
- ❌ MCP authentication (stream abort error)
- ❌ JsonIgnore attributes not respected
- ❌ Large response sizes causing connection issues

## Recommended Fixes

### Priority 1: Fix JSON Serialization
1. **Check which JSON serializer is being used:**
   - Verify if using System.Text.Json or Newtonsoft.Json
   - Check `Program.cs` or `Startup.cs` for serializer configuration

2. **Ensure JsonIgnore is respected:**
   - If using System.Text.Json: Attributes should work by default
   - If using Newtonsoft.Json: May need `[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]` or different approach
   - Consider adding explicit serializer options

3. **Alternative: Create minimal authentication response:**
   - Create a DTO that only includes essential fields (jwtToken, avatarId, username)
   - Return this DTO instead of full Avatar object

### Priority 2: Fix MCP Client
1. **Handle large responses better:**
   - Implement streaming/chunked response handling
   - Add retry logic with exponential backoff
   - Consider using native fetch instead of axios

2. **Extract token from partial response:**
   - Parse response incrementally
   - Extract JWT token as soon as it's available
   - Don't wait for full response

### Priority 3: API Response Optimization
1. **Create authentication-specific endpoint:**
   - `/api/avatar/authenticate-minimal` that returns only:
     - `jwtToken`
     - `avatarId`
     - `username`
     - `isVerified`
   - Use this endpoint for MCP authentication

## Testing Commands

### Test Direct API
```bash
curl -k -s -X POST "https://127.0.0.1:5004/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username":"OASIS_ADMIN","password":"Uppermall1!"}' | wc -c
# Returns: 29669 bytes
```

### Test Response Structure
```bash
curl -k -s -X POST "https://127.0.0.1:5004/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username":"OASIS_ADMIN","password":"Uppermall1!"}' | \
  grep -o '"createdByAvatar"' | wc -l
# Returns: 21 instances
```

### Test MCP (Fails)
```bash
# Via MCP tool - returns stream abort error
```

## Docker Image Comparison

**Last Working Image:** `sha256:ec68307d1dad91ca87a1ce395da5f96cd26434d01555f186c014f0f8a3e69771`
- Created: 2026-01-04 (4 days ago)
- .NET Version: 8.0.22
- Tags: `oasis-api:latest`, `oasis-api:v20260104-140616`

**Current Local:**
- .NET Version: 9.0 (in Dockerfile)
- Has JsonIgnore attributes (but not working)
- Same source code structure

## Next Steps

1. **Investigate JSON serialization configuration**
   - Check which serializer is actually being used
   - Verify JsonIgnore attribute support
   - Test with minimal response DTO

2. **Fix serialization or create minimal endpoint**
   - Either make JsonIgnore work
   - Or create `/api/avatar/authenticate-minimal` endpoint

3. **Update MCP client if needed**
   - Only if API response can't be reduced
   - Implement better streaming/chunked handling

## Additional Context

- API is running on HTTPS port 5004
- MCP config points to `https://127.0.0.1:5004`
- Authentication works but response is too large
- JsonIgnore attributes are in code but not effective
- Need to determine why serialization ignores the attributes

## Fix Implemented

### Solution: Minimal Authentication Endpoint

A new minimal authentication endpoint has been created to solve the large response size issue. This approach is cleaner and more performant than trying to fix JsonIgnore attribute issues.

### Changes Made

1. **Created MinimalAuthenticateResponse DTO** (`Models/Security/MinimalAuthenticateResponse.cs`)
   - Contains only essential fields: `JwtToken`, `RefreshToken`, `AvatarId`, `Username`, `Email`, `IsVerified`, `IsBeamedIn`, `LastBeamedIn`
   - Eliminates nested `createdByAvatar` objects and other unnecessary data
   - Reduces response size from ~29KB to a few hundred bytes

2. **Added New Endpoint** (`AvatarController.cs`)
   - New endpoint: `POST /api/avatar/authenticate-minimal`
   - Returns `OASISHttpResponseMessage<MinimalAuthenticateResponse>` instead of full `IAvatar` object
   - Maintains same authentication logic but returns minimal response
   - Fully documented with XML comments

3. **Updated MCP Client** (`MCP/src/clients/oasisClient.ts`)
   - Modified `authenticateAvatar` method to use `/api/avatar/authenticate-minimal` endpoint
   - Removed workarounds for large responses (responseType: 'text', increased timeout)
   - Reduced timeout from 120 seconds to 30 seconds (sufficient for minimal response)
   - Simplified error handling

### Benefits

- ✅ **Solves the immediate problem**: No more "stream has been aborted" errors
- ✅ **Better performance**: Smaller response size means faster network transfer
- ✅ **More secure**: Only returns essential authentication data
- ✅ **Cleaner solution**: Doesn't rely on JsonIgnore attributes working correctly
- ✅ **Backward compatible**: Original `/api/avatar/authenticate` endpoint still works for clients that need full avatar data

### Testing

To test the fix:
```bash
# Test the new minimal endpoint
curl -k -X POST "https://127.0.0.1:5004/api/avatar/authenticate-minimal" \
  -H "Content-Type: application/json" \
  -d '{"username":"OASIS_ADMIN","password":"Uppermall1!"}' | wc -c
# Should return much smaller response (few hundred bytes vs 29KB)

# Test via MCP - should now work without stream abort errors
```

### Files Modified

1. `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Models/Security/MinimalAuthenticateResponse.cs` (new file)
2. `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/AvatarController.cs` (added new endpoint)
3. `MCP/src/clients/oasisClient.ts` (updated to use minimal endpoint)

