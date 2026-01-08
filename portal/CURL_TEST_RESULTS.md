# STAR API CURL Test Results

## Authentication ✅

```bash
# Get token from OASIS API
AUTH_RESPONSE=$(curl -s -X POST "http://api.oasisweb4.com/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username": "OASIS_ADMIN", "password": "Uppermall1!"}')

# Extract JWT token (path is result.result.jwtToken)
TOKEN=$(echo "$AUTH_RESPONSE" | jq -r '.result.result.jwtToken')

echo "Token: $TOKEN"
```

**Result**: ✅ Authentication works. Token is at `result.result.jwtToken`

---

## GET Endpoints

### GET /api/GeoNFTs
```bash
curl -X GET "http://localhost:50564/api/GeoNFTs" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" | jq '.'
```

**Result**: ❌ Error - "Error loading geo NFTs: Exception of type 'NextGenSoftware.OASIS.API.Core.Exceptions.OASISException' was thrown."

**Status**: Backend error - all GET endpoints appear to have backend configuration issues.

---

## POST Endpoints

### POST /api/GeoNFTs (minimal payload)
```bash
curl -X POST "http://localhost:50564/api/GeoNFTs" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"name": "Test", "description": "Test"}' | jq '.'
```

**Result**: ❌ 500 Internal Server Error - "The given key 'STARGeoNFTDNAJSON' was not present in the dictionary"

**Issue**: Requires `MetaData.STARGeoNFTDNAJSON` to be present.

---

### POST /api/GeoNFTs (with STARGeoNFTDNAJSON)
```bash
curl -X POST "http://localhost:50564/api/GeoNFTs" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"name": "Test", "description": "Test", "MetaData": {"STARGeoNFTDNAJSON": "{\"SourcePath\":\"GeoNFTs/Source/Test\"}"}}' | jq '.'
```

**Result**: ❌ 400 Bad Request - "Error creating geo NFT: Exception of type 'NextGenSoftware.OASIS.API.Core.Exceptions.OASISException' was thrown."

**Issue**: Passes validation but backend `UpdateAsync` fails because it expects SourcePath to exist, but the directory doesn't exist yet. This is the backend architecture issue we identified.

---

### POST /api/GeoNFTs/create
```bash
curl -X POST "http://localhost:50564/api/GeoNFTs/create" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"name": "Test", "description": "Test", "SourceFolderPath": "GeoNFTs/Source/Test"}' | jq '.'
```

**Result**: ❌ 400 Bad Request - "The CreateOptions field is required."

**Issue**: This endpoint uses `CreateAsync` (the correct method) but requires a `CreateOptions` parameter which is complex.

---

## Summary

### What Works
- ✅ Authentication with OASIS API
- ✅ Token extraction from response
- ✅ Authorization header format
- ✅ Basic request structure

### What Doesn't Work
- ❌ All GET endpoints return backend OASISException errors
- ❌ POST /api/GeoNFTs uses `UpdateAsync` which fails for new GeoNFTs (SourcePath issue)
- ❌ POST /api/GeoNFTs/create requires complex `CreateOptions` parameter

### Root Cause
The STAR API backend has issues:
1. GET endpoints likely have OASIS boot/configuration problems
2. POST /api/GeoNFTs endpoint uses wrong method (`UpdateAsync` instead of `CreateAsync` for new GeoNFTs)
3. POST /api/GeoNFTs/create endpoint requires complex parameters that are not documented

### Next Steps
The backend needs to be fixed:
1. Fix GET endpoints (OASIS configuration/boot issues)
2. Fix POST /api/GeoNFTs to use `CreateAsync` for new GeoNFTs OR handle SourcePath initialization in `UpdateAsync`
3. Document/simplify POST /api/GeoNFTs/create endpoint parameters
