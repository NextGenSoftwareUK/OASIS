# Working CURL Commands for STAR API

## Authentication

```bash
AUTH_RESPONSE=$(curl -s -X POST "http://api.oasisweb4.com/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username": "OASIS_ADMIN", "password": "Uppermall1!"}')

TOKEN=$(echo "$AUTH_RESPONSE" | jq -r '.result.result.jwtToken')
```

## Create GeoNFT (Working Endpoint)

**Endpoint:** `POST /api/GeoNFTs/create`

**Working Request:**
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

**Required Fields:**
- `name` (string)
- `description` (string)
- `sourceFolderPath` (string) - Path where GeoNFT source files will be stored
- `holonSubType` (int) - HolonType enum value (86 = GeoNFT)

**Optional Fields:**
- `createOptions` - Can be omitted (now nullable after fix)

**HolonType Values:**
- 86 = GeoNFT
- Check enum for other types if needed






























