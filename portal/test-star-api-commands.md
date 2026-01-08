# STAR API Test Commands

## Quick Test Commands (copy-paste ready)

### Prerequisites
Get your JWT token from browser console:
```javascript
JSON.parse(localStorage.getItem('oasis_auth')).token
```

Set it as an environment variable:
```bash
export TOKEN="your-jwt-token-here"
```

### Test 1: Get All GeoNFTs
```bash
curl -X GET "http://localhost:50564/api/GeoNFTs" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" | jq '.'
```

### Test 2: Get GeoNFTs for Avatar
```bash
curl -X GET "http://localhost:50564/api/GeoNFTs/load-all-for-avatar?showAllVersions=false&version=0" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" | jq '.'
```

### Test 3: Create GeoNFT - Minimal
```bash
curl -X POST "http://localhost:50564/api/GeoNFTs" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test GeoNFT",
    "description": "Test description"
  }' | jq '.'
```

### Test 4: Create GeoNFT - With MetaData
```bash
curl -X POST "http://localhost:50564/api/GeoNFTs" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test GeoNFT",
    "description": "Test description",
    "MetaData": {
      "test": "value"
    }
  }' | jq '.'
```

### Test 5: Create GeoNFT - Empty (to see required fields)
```bash
curl -X POST "http://localhost:50564/api/GeoNFTs" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{}' | jq '.'
```

### Test 6: Create GeoNFT - Full STARNETDNA (current portal format)
```bash
curl -X POST "http://localhost:50564/api/GeoNFTs" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test GeoNFT Full",
    "description": "Test with STARNETDNA",
    "MetaData": {
      "STARGeoNFTDNAJSON": "{\"Name\":\"Test GeoNFT Full\",\"Description\":\"Test with STARNETDNA\",\"STARNETHolonType\":\"STARGeoNFT\",\"STARNETCategory\":\"Image\",\"MetaData\":{\"lat\":51507400,\"long\":-127800},\"CreatedByAvatarId\":\"00000000-0000-0000-0000-000000000000\",\"Version\":\"1.0.0\",\"VersionSequence\":1}"
    },
    "NFTType": "Image",
    "GeoNFTId": "00000000-0000-0000-0000-000000000000"
  }' | jq '.'
```

## Using the Test Script

Or use the provided script:

```bash
cd portal
./test-star-api.sh "your-jwt-token-here"
```

## What to Look For

1. **400 Bad Request** with `errors` object = Validation errors (check the `errors` field for details)
2. **401 Unauthorized** = Token issue
3. **200 OK** = Success!
4. **500 Internal Server Error** = Backend error

The `errors` object will tell you exactly which fields are required or invalid.






























