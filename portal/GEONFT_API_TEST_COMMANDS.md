# GeoNFT API Test Commands

## Prerequisites
- STAR API running on `http://localhost:50564`
- OASIS API accessible at `http://api.oasisweb4.com`
- Valid credentials: `OASIS_ADMIN` / `Uppermall1!`

## Step 1: Authenticate and Get JWT Token

```bash
AUTH_RESPONSE=$(curl -s -X POST "http://api.oasisweb4.com/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username": "OASIS_ADMIN", "password": "Uppermall1!"}')

TOKEN=$(echo "$AUTH_RESPONSE" | jq -r '.result.result.jwtToken')
AVATAR_ID=$(echo "$AUTH_RESPONSE" | jq -r '.result.result.id')

echo "Token: ${TOKEN:0:50}..."
echo "Avatar ID: $AVATAR_ID"
```

## Step 2: Create a GeoNFT

### Using Default Provider (MongoDB)
```bash
curl -X POST "http://localhost:50564/api/GeoNFTs/create" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My Test GeoNFT",
    "description": "A test GeoNFT created via API",
    "sourceFolderPath": "GeoNFTs/Source/Test",
    "holonSubType": 86
  }' | jq '.'
```

### Using Solana Provider with Coordinates
```bash
curl -X POST "http://localhost:50564/api/GeoNFTs/create" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My Solana GeoNFT",
    "description": "A GeoNFT on Solana blockchain",
    "sourceFolderPath": "GeoNFTs/Source/Test",
    "holonSubType": 86,
    "providerType": "SolanaOASIS",
    "latitude": 51500000,
    "longitude": -100000
  }' | jq '.'
```

**Note**: Coordinates are in micro-degrees:
- Latitude: 51500000 = 51.5° (London, UK)
- Longitude: -100000 = -0.1° (London, UK)

### Request Parameters
- `name` (string, required): Name of the GeoNFT
- `description` (string, required): Description of the GeoNFT
- `sourceFolderPath` (string, required): Path to source folder (e.g., "GeoNFTs/Source/Test")
- `holonSubType` (int, required): Holon type (86 = GeoNFT)
- `providerType` (string, optional): Provider type (e.g., "SolanaOASIS", "EthereumOASIS", "Default" if omitted)
- `latitude` (long, optional): Latitude in micro-degrees (e.g., 51500000 = 51.5°)
- `longitude` (long, optional): Longitude in micro-degrees (e.g., -100000 = -0.1°)

**Coordinate Format**: Values are in micro-degrees (multiply degrees by 1,000,000)
- Example: 51.5° = 51500000, -0.1° = -100000

## Step 3: Get All GeoNFTs

```bash
# Get count and basic info (recommended - response can be large)
curl -X GET "http://localhost:50564/api/GeoNFTs" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" | jq '{resultsCount, isError, resultCount: (.result | length)}'

# Get full response (can be very large)
curl -X GET "http://localhost:50564/api/GeoNFTs" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" | jq '.'
```

## Step 4: Get GeoNFTs for Current Avatar

```bash
curl -X GET "http://localhost:50564/api/GeoNFTs/load-all-for-avatar?avatarId=${AVATAR_ID}" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" | jq '.'
```

## Complete Test Script

```bash
#!/bin/bash

# Step 1: Authenticate
echo "=== Authenticating ==="
AUTH_RESPONSE=$(curl -s -X POST "http://api.oasisweb4.com/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username": "OASIS_ADMIN", "password": "Uppermall1!"}')

TOKEN=$(echo "$AUTH_RESPONSE" | jq -r '.result.result.jwtToken')
AVATAR_ID=$(echo "$AUTH_RESPONSE" | jq -r '.result.result.id')

if [ "$TOKEN" == "null" ] || [ -z "$TOKEN" ]; then
  echo "❌ Authentication failed"
  echo "$AUTH_RESPONSE" | jq '.'
  exit 1
fi

echo "✅ Authenticated - Avatar ID: $AVATAR_ID"
echo ""

# Step 2: Create GeoNFT
echo "=== Creating GeoNFT ==="
CREATE_RESPONSE=$(curl -s -X POST "http://localhost:50564/api/GeoNFTs/create" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d "{
    \"name\": \"TestGeoNFT-$(date +%s)\",
    \"description\": \"Test GeoNFT created via API\",
    \"sourceFolderPath\": \"GeoNFTs/Source/Test\",
    \"holonSubType\": 86
  }")

IS_ERROR=$(echo "$CREATE_RESPONSE" | jq -r '.isError')
IS_SAVED=$(echo "$CREATE_RESPONSE" | jq -r '.isSaved')

if [ "$IS_ERROR" == "true" ]; then
  echo "❌ Creation failed"
  echo "$CREATE_RESPONSE" | jq '.message'
  exit 1
fi

echo "✅ GeoNFT created successfully"
GEO_NFT_ID=$(echo "$CREATE_RESPONSE" | jq -r '.result.id')
GEO_NFT_NAME=$(echo "$CREATE_RESPONSE" | jq -r '.result.name')
echo "GeoNFT ID: $GEO_NFT_ID"
echo "GeoNFT Name: $GEO_NFT_NAME"
echo ""

# Step 3: Get all GeoNFTs
echo "=== Getting all GeoNFTs ==="
curl -s -X GET "http://localhost:50564/api/GeoNFTs" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" | jq '{resultsCount, isError, count: (.result | length), items: [.result[] | {id, name, description}]}'
```

## Expected Response (Create)

```json
{
  "isError": false,
  "isSaved": false,
  "message": "Successfully created the GeoNFT on the Default provider by AvatarId ... for GeoNFTType DownloadedHolon.",
  "result": {
    "id": "05020d20-7a44-4ab0-8bd8-332045f50374",
    "name": "TestGeoNFT-1767574953",
    "description": "Test GeoNFT created via API",
    ...
  }
}
```

**Note**: `isSaved: false` in the response doesn't indicate failure - the GeoNFT is successfully created. The message confirms success.

## Expected Response (Get All)

```json
{
  "resultsCount": 1,
  "isError": false,
  "result": [
    {
      "id": "uuid-here",
      "name": "My Test GeoNFT",
      "description": "A test GeoNFT created via API",
      ...
    }
  ]
}
```

## Notes
- The `holonSubType` value of `86` corresponds to `GeoNFT` type
- GeoNFTs are stored in MongoDB Atlas (configured in OASIS_DNA.json)
- All requests require a valid JWT token in the Authorization header
- **Storage Provider**: Currently using MongoDB Atlas (database), NOT blockchain
- **Transaction IDs**: Not applicable - GeoNFTs are stored in database, not on blockchain
- To store on blockchain, configure blockchain provider in OASIS_DNA.json and use providerType parameter

## Storage Information

See `GEONFT_STORAGE_INFO.md` for detailed information about GeoNFT storage providers.

