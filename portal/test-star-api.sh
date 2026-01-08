#!/bin/bash

# STAR API Test Script
# Usage: ./test-star-api.sh [your-jwt-token]
# Or set STAR_API_TOKEN environment variable

STAR_API_BASE="http://localhost:50564"
TOKEN="${1:-${STAR_API_TOKEN}}"

if [ -z "$TOKEN" ]; then
    echo "Error: JWT token required"
    echo "Usage: $0 <jwt-token>"
    echo "   OR: export STAR_API_TOKEN=your-token && $0"
    echo ""
    echo "To get your token from localStorage in browser console:"
    echo "  JSON.parse(localStorage.getItem('oasis_auth')).token"
    exit 1
fi

echo "=== STAR API Test Script ==="
echo "Base URL: $STAR_API_BASE"
echo "Token: ${TOKEN:0:20}..."
echo ""

# Test 1: Get All GeoNFTs
echo "=== Test 1: GET /api/GeoNFTs ==="
curl -X GET "${STAR_API_BASE}/api/GeoNFTs" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -w "\nHTTP Status: %{http_code}\n" \
  -s | jq '.' 2>/dev/null || cat
echo ""

# Test 2: Get GeoNFTs for Avatar
echo "=== Test 2: GET /api/GeoNFTs/load-all-for-avatar ==="
curl -X GET "${STAR_API_BASE}/api/GeoNFTs/load-all-for-avatar?showAllVersions=false&version=0" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -w "\nHTTP Status: %{http_code}\n" \
  -s | jq '.' 2>/dev/null || cat
echo ""

# Test 3: Create GeoNFT - Minimal (just name and description)
echo "=== Test 3: POST /api/GeoNFTs (Minimal) ==="
curl -X POST "${STAR_API_BASE}/api/GeoNFTs" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test GeoNFT Minimal",
    "description": "Testing with minimal payload"
  }' \
  -w "\nHTTP Status: %{http_code}\n" \
  -s | jq '.' 2>/dev/null || cat
echo ""

# Test 4: Create GeoNFT - With MetaData but no STARNETDNA
echo "=== Test 4: POST /api/GeoNFTs (With MetaData) ==="
curl -X POST "${STAR_API_BASE}/api/GeoNFTs" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test GeoNFT With MetaData",
    "description": "Testing with MetaData field",
    "MetaData": {
      "test": "value"
    }
  }' \
  -w "\nHTTP Status: %{http_code}\n" \
  -s | jq '.' 2>/dev/null || cat
echo ""

# Test 5: Create GeoNFT - With STARNETDNA structure
echo "=== Test 5: POST /api/GeoNFTs (With STARNETDNA) ==="
curl -X POST "${STAR_API_BASE}/api/GeoNFTs" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test GeoNFT Full",
    "description": "Testing with STARNETDNA structure",
    "MetaData": {
      "STARGeoNFTDNAJSON": "{\"Name\":\"Test GeoNFT Full\",\"Description\":\"Testing with STARNETDNA structure\",\"STARNETHolonType\":\"STARGeoNFT\",\"STARNETCategory\":\"Image\",\"MetaData\":{\"lat\":51507400,\"long\":-127800},\"CreatedByAvatarId\":\"00000000-0000-0000-0000-000000000000\",\"Version\":\"1.0.0\",\"VersionSequence\":1}"
    },
    "NFTType": "Image",
    "GeoNFTId": "00000000-0000-0000-0000-000000000000"
  }' \
  -w "\nHTTP Status: %{http_code}\n" \
  -s | jq '.' 2>/dev/null || cat
echo ""

# Test 6: Create GeoNFT - Empty object to see what's required
echo "=== Test 6: POST /api/GeoNFTs (Empty - to see required fields) ==="
curl -X POST "${STAR_API_BASE}/api/GeoNFTs" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{}' \
  -w "\nHTTP Status: %{http_code}\n" \
  -s | jq '.' 2>/dev/null || cat
echo ""

echo "=== Tests Complete ==="






























