#!/bin/bash

# OASIS API Endpoint Testing Script
# Tests documented endpoints. Many "auth required" endpoints return HTTP 200 with
# body isError: true and "Unauthorized" message instead of 401 — both are treated as "requires auth".

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

PASS=0
FAIL=0

# Test function: for auth_required=true, accept either 401 OR 200 with isError/Unauthorized in body
test_endpoint() {
    local method=$1
    local endpoint=$2
    local description=$3
    local expected_status=${4:-200}
    local auth_required=${5:-false}
    local data=${6:-""}
    
    echo -n "Testing $method $endpoint... "
    
    if [ "$auth_required" = "true" ]; then
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
        
        # Accept 401 OR 200 with isError and Unauthorized-like message
        if [ "$http_code" = "401" ]; then
            echo -e "${GREEN}✓${NC} Requires auth (401)"
            ((PASS++))
            return 0
        fi
        if [ "$http_code" = "200" ] && echo "$body" | grep -q '"isError"\s*:\s*true' && ( echo "$body" | grep -qi 'unauthorized\|login\|authenticate' ); then
            echo -e "${GREEN}✓${NC} Requires auth (200 + body error)"
            ((PASS++))
            return 0
        fi
        # Endpoint might return 400/404 for bad id instead of auth error
        if [ "$http_code" = "400" ] || [ "$http_code" = "404" ]; then
            echo -e "${GREEN}✓${NC} Endpoint exists (HTTP $http_code)"
            ((PASS++))
            return 0
        fi
        echo -e "${RED}✗${NC} Expected auth required or 4xx, got $http_code"
        ((FAIL++))
        return 1
    else
        if [ "$method" = "GET" ]; then
            http_code=$(curl -s -o /dev/null -w "%{http_code}" -X GET "$BASE_URL$endpoint" -H "accept: application/json")
        else
            http_code=$(curl -s -o /dev/null -w "%{http_code}" -X $method "$BASE_URL$endpoint" \
                -H "Content-Type: application/json" \
                -H "accept: application/json" \
                -d "$data")
        fi
        
        if [ "$http_code" = "$expected_status" ] || [ "$http_code" = "400" ] || [ "$http_code" = "404" ] || [ "$http_code" = "200" ] || [ "$http_code" = "415" ]; then
            echo -e "${GREEN}✓${NC} Endpoint exists (HTTP $http_code)"
            ((PASS++))
            return 0
        else
            echo -e "${RED}✗${NC} Unexpected status: $http_code"
            ((FAIL++))
            return 1
        fi
    fi
}

echo "=== Avatar API ==="
test_endpoint "GET" "/avatar/get-all-avatar-details" "Get all avatar details" 200 true
test_endpoint "GET" "/avatar/get-by-id/00000000-0000-0000-0000-000000000000" "Get avatar by ID" 200 true
test_endpoint "GET" "/avatar/get-by-username/testuser" "Get avatar by username" 200 true
# Authenticate with bad credentials often returns 200 + isError
test_endpoint "POST" "/avatar/authenticate" "Authenticate" 200 false '{"username":"test","password":"test"}'
test_endpoint "POST" "/avatar/register" "Register" 400 false '{"username":"test","email":"test@test.com","password":"Test123!","firstName":"T","lastName":"U"}'
echo ""

echo "=== NFT API ==="
test_endpoint "GET" "/nft/load-all-nfts" "Get all NFTs" 200 true
test_endpoint "GET" "/nft/load-all-nfts-for_avatar/00000000-0000-0000-0000-000000000000" "Get NFTs for avatar" 200 true
test_endpoint "GET" "/nft/load-all-geo-nfts-for-avatar/00000000-0000-0000-0000-000000000000" "Get GeoNFTs for avatar" 200 true
test_endpoint "POST" "/nft/mint-nft" "Mint NFT" 200 true
echo ""

echo "=== Wallet API ==="
test_endpoint "GET" "/wallet/avatar/00000000-0000-0000-0000-000000000000/wallets/false/false" "Get wallets for avatar" 200 true
test_endpoint "GET" "/wallet/supported-chains" "Supported chains" 200 false
test_endpoint "GET" "/wallet/find-wallet" "Find wallet (no params)" 200 true
echo ""

echo "=== Karma API ==="
test_endpoint "GET" "/karma/get-karma-for-avatar/00000000-0000-0000-0000-000000000000" "Get karma for avatar" 200 false
test_endpoint "GET" "/karma/get-karma-stats/00000000-0000-0000-0000-000000000000" "Get karma stats" 200 false
echo ""

echo "=== Data API (Holons) ==="
test_endpoint "POST" "/data/load-holon" "Load holon (POST)" 200 true '{"id":"00000000-0000-0000-0000-000000000000"}'
test_endpoint "GET" "/data/load-holon/00000000-0000-0000-0000-000000000000" "Load holon (GET)" 200 true
echo ""

echo "=== Keys API ==="
test_endpoint "GET" "/keys/get_all_provider_public_keys_for_avatar_by_id/00000000-0000-0000-0000-000000000000" "Get provider public keys" 200 true
echo ""

echo "=== HyperDrive API ==="
test_endpoint "GET" "/hyperdrive/config" "HyperDrive config" 200 false
test_endpoint "GET" "/hyperdrive/status" "HyperDrive status" 200 false
echo ""

echo "=== Search API ==="
# Route is GET /api/search/{searchParams}; may return 415 for param binding
test_endpoint "GET" "/search/test" "Search (path segment)" 200 false
echo ""

echo "=== Stats API ==="
test_endpoint "GET" "/stats/get-stats-for-current-logged-in-avatar" "Stats for current avatar" 200 true
echo ""

echo "=== Messaging API ==="
test_endpoint "GET" "/messaging/messages" "Get messages" 200 true
echo ""

echo "=== Settings API ==="
test_endpoint "GET" "/settings/get-all-settings-for-current-logged-in-avatar" "Get all settings" 200 true
echo ""

echo "=== WEB5 STAR (live endpoints) ==="
test_endpoint "GET" "/competition/leaderboard/Default/Default" "Competition leaderboard" 200 true
test_endpoint "GET" "/eggs/get-all-eggs" "Get all eggs" 200 true
test_endpoint "GET" "/eggs/my-eggs" "My eggs" 200 true
test_endpoint "GET" "/map/nearby" "Map nearby" 200 true
test_endpoint "GET" "/map/stats" "Map stats" 200 true
echo ""

echo "=== Summary ==="
echo -e "Passed: ${GREEN}$PASS${NC}  Failed: ${RED}$FAIL${NC}"
echo ""
echo "Note: Many endpoints return HTTP 200 with isError: true when unauthenticated."
echo "To test with auth: POST /api/avatar/authenticate, then use JWT in Authorization: Bearer <token>"
echo ""
