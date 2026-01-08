# CURL Testing Flow for GeoNFT Creation

## Current Status
- ✅ OASIS API running (port 5000 or api.oasisweb4.com)
- ✅ STAR API running (port 50564)
- ⚠️ Testing authentication and GeoNFT creation flow

## Authentication Flow

```bash
# Step 1: Authenticate to get JWT token
AUTH_RESPONSE=$(curl -s -X POST "http://api.oasisweb4.com/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username": "OASIS_ADMIN", "password": "Uppermall1!"}')

# Extract token
TOKEN=$(echo "$AUTH_RESPONSE" | jq -r '.result.result.jwtToken')
AVATAR_ID=$(echo "$AUTH_RESPONSE" | jq -r '.result.result.id')
```

## GeoNFT Creation Endpoint

**Endpoint**: `POST /api/GeoNFTs/create`

**Required Fields**:
- `name` (string) - Name of the GeoNFT
- `description` (string) - Description
- `sourceFolderPath` (string) - Path where GeoNFT source files will be stored
- `holonSubType` (int) - HolonType enum value (86 = GeoNFT)

**Request**:
```bash
curl -X POST "http://localhost:50564/api/GeoNFTs/create" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "MyGeoNFT",
    "description": "Description of my GeoNFT",
    "sourceFolderPath": "GeoNFTs/Source/MyGeoNFT",
    "holonSubType": 86
  }'
```

## Known Issues

1. **OASIS Not Booted**: STAR API may not have OASIS booted, causing OASISException
2. **CreateOptions Validation**: Fixed by making CreateOptions nullable
3. **Backend Error**: CreateAsync may fail if OASIS is not properly initialized

## Testing Checklist

- [ ] Authentication works
- [ ] JWT token extracted correctly
- [ ] STAR API GET /api/GeoNFTs works (indicates OASIS is booted)
- [ ] POST /api/GeoNFTs/create accepts request
- [ ] POST /api/GeoNFTs/create creates GeoNFT successfully
- [ ] Verify GeoNFT appears in GET /api/GeoNFTs response






























