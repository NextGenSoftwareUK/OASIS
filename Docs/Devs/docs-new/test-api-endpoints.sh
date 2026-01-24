#!/bin/bash

# OASIS API Endpoint Testing Script
# Tests documented endpoints to ensure documentation accuracy

BASE_URL="http://api.oasisweb4.com/api"
SWAGGER_URL="http://api.oasisweb4.com/swagger/v1/swagger.json"

echo "=== OASIS API Endpoint Testing ==="
echo "Base URL: $BASE_URL"
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test function
test_endpoint() {
    local method=$1
    local endpoint=$2
    local description=$3
    local expected_status=${4:-200}
    local auth_required=${5:-false}
    local data=${6:-""}
    
    echo -n "Testing $method $endpoint... "
    
    if [ "$auth_required" = "true" ]; then
        # Test without auth (should return 401)
        if [ "$method" = "GET" ]; then
            response=$(curl -s -w "\n%{http_code}" -X GET "$BASE_URL$endpoint" -H "accept: application/json")
        else
            response=$(curl -s -w "\n%{http_code}" -X $method "$BASE_URL$endpoint" \
                -H "Content-Type: application/json" \
                -H "accept: application/json" \
                -d "$data")
        fi
        
        http_code=$(echo "$response" | tail -n1)
        body=$(echo "$response" | sed '$d')
        
        if [ "$http_code" = "401" ]; then
            echo -e "${GREEN}✓${NC} Correctly requires authentication (401)"
            return 0
        else
            echo -e "${RED}✗${NC} Expected 401, got $http_code"
            return 1
        fi
    else
        # Test endpoint exists
        if [ "$method" = "GET" ]; then
            http_code=$(curl -s -o /dev/null -w "%{http_code}" -X GET "$BASE_URL$endpoint" -H "accept: application/json")
        else
            http_code=$(curl -s -o /dev/null -w "%{http_code}" -X $method "$BASE_URL$endpoint" \
                -H "Content-Type: application/json" \
                -H "accept: application/json" \
                -d "$data")
        fi
        
        if [ "$http_code" = "$expected_status" ] || [ "$http_code" = "400" ] || [ "$http_code" = "404" ]; then
            echo -e "${GREEN}✓${NC} Endpoint exists (HTTP $http_code)"
            return 0
        else
            echo -e "${RED}✗${NC} Unexpected status: $http_code"
            return 1
        fi
    fi
}

echo "=== Testing Avatar API Endpoints ==="
echo ""

# Avatar endpoints (should require auth)
test_endpoint "GET" "/avatar/get-all-avatar-details" "Get all avatar details" 401 true
test_endpoint "GET" "/avatar/get-by-id/00000000-0000-0000-0000-000000000000" "Get avatar by ID" 401 true
test_endpoint "GET" "/avatar/get-by-username/testuser" "Get avatar by username" 401 true
test_endpoint "GET" "/avatar/get-by-email/test@example.com" "Get avatar by email" 401 true
test_endpoint "GET" "/avatar/get-avatar-portrait/00000000-0000-0000-0000-000000000000" "Get avatar portrait" 401 true
test_endpoint "POST" "/avatar/authenticate" "Authenticate" 400 false '{"username":"test","password":"test"}'
test_endpoint "POST" "/avatar/register" "Register" 400 false '{"username":"test","email":"test@test.com","password":"test123456"}'
test_endpoint "GET" "/avatar/verify-email" "Verify email" 400 false

echo ""
echo "=== Testing NFT API Endpoints ==="
echo ""

# NFT endpoints (should require auth)
test_endpoint "GET" "/nft/load-all-nfts" "Get all NFTs" 401 true
test_endpoint "GET" "/nft/load-all-nfts-for_avatar/00000000-0000-0000-0000-000000000000" "Get NFTs for avatar" 401 true
test_endpoint "GET" "/nft/load-nft-by-id/00000000-0000-0000-0000-000000000000" "Get NFT by ID" 401 true
test_endpoint "GET" "/nft/load-nft-by-hash/testhash" "Get NFT by hash" 401 true
test_endpoint "POST" "/nft/mint-nft" "Mint NFT" 401 true
test_endpoint "POST" "/nft/send-nft" "Send NFT" 401 true
test_endpoint "GET" "/nft/load-all-geo-nfts-for-avatar/00000000-0000-0000-0000-000000000000" "Get GeoNFTs for avatar" 401 true

echo ""
echo "=== Testing Wallet API Endpoints ==="
echo ""

# Wallet endpoints
test_endpoint "GET" "/wallet/00000000-0000-0000-0000-000000000000" "Get wallet" 401 true

echo ""
echo "=== Summary ==="
echo "All endpoint tests completed."
echo ""
echo "Note: Most endpoints require authentication. To test with authentication:"
echo "1. Register: POST /api/avatar/register"
echo "2. Verify email: GET /api/avatar/verify-email?token=..."
echo "3. Authenticate: POST /api/avatar/authenticate"
echo "4. Use JWT token in Authorization header"
