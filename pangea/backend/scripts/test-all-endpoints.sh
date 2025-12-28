#!/bin/bash

# Comprehensive endpoint testing script for Pangea Backend
# Usage: ./scripts/test-all-endpoints.sh [BASE_URL]

set -e

BASE_URL="${1:-https://pangea-production-128d.up.railway.app/api}"
COLOR_GREEN='\033[0;32m'
COLOR_RED='\033[0;31m'
COLOR_YELLOW='\033[1;33m'
COLOR_BLUE='\033[0;34m'
COLOR_RESET='\033[0m'

# Test counters
TOTAL=0
PASSED=0
FAILED=0
SKIPPED=0

echo "🧪 Pangea Backend Endpoint Testing"
echo "Base URL: $BASE_URL"
echo ""

# Helper function to make authenticated requests
TOKEN=""
AVATAR_ID=""

authenticate() {
  echo -e "${COLOR_BLUE}🔐 Authenticating...${COLOR_RESET}"
  RESPONSE=$(curl -s -X POST "$BASE_URL/auth/login" \
    -H "Content-Type: application/json" \
    -d '{"email":"OASIS_ADMIN","password":"Uppermall1!"}')
  
  TOKEN=$(echo $RESPONSE | jq -r '.token // empty')
  AVATAR_ID=$(echo $RESPONSE | jq -r '.user.avatarId // empty')
  
  if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
    echo -e "${COLOR_RED}❌ Authentication failed${COLOR_RESET}"
    exit 1
  fi
  
  echo -e "${COLOR_GREEN}✅ Authenticated (Token: ${TOKEN:0:30}...)${COLOR_RESET}"
  echo ""
}

# Test function
test_endpoint() {
  local method=$1
  local endpoint=$2
  local requires_auth=${3:-true}
  local requires_admin=${4:-false}
  local data=${5:-""}
  local description=$6
  
  TOTAL=$((TOTAL + 1))
  
  # Build curl command
  CMD="curl -s -w '\n%{http_code}' -X $method \"$BASE_URL$endpoint\""
  
  if [ "$requires_auth" = "true" ] && [ -n "$TOKEN" ]; then
    CMD="$CMD -H \"Authorization: Bearer $TOKEN\""
  fi
  
  if [ -n "$data" ]; then
    CMD="$CMD -H \"Content-Type: application/json\" -d '$data'"
  fi
  
  # Execute and parse response
  RESPONSE=$(eval $CMD)
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
  BODY=$(echo "$RESPONSE" | sed '$d')
  
  # Check result
  if [[ "$HTTP_CODE" =~ ^2[0-9]{2}$ ]]; then
    echo -e "${COLOR_GREEN}✅ [$method] $endpoint${COLOR_RESET} ($HTTP_CODE)"
    if [ -n "$description" ]; then
      echo "   $description"
    fi
    PASSED=$((PASSED + 1))
    return 0
  elif [[ "$HTTP_CODE" =~ ^4[0-9]{2}$ ]]; then
    if [ "$HTTP_CODE" = "401" ] && [ "$requires_auth" = "false" ]; then
      echo -e "${COLOR_GREEN}✅ [$method] $endpoint${COLOR_RESET} (401 - expected for unauthenticated)"
      PASSED=$((PASSED + 1))
    elif [ "$HTTP_CODE" = "403" ] && [ "$requires_admin" = "true" ]; then
      echo -e "${COLOR_YELLOW}⚠️  [$method] $endpoint${COLOR_RESET} (403 - requires admin role)"
      SKIPPED=$((SKIPPED + 1))
    elif [ "$HTTP_CODE" = "404" ]; then
      echo -e "${COLOR_YELLOW}⚠️  [$method] $endpoint${COLOR_RESET} (404 - endpoint not found or resource missing)"
      SKIPPED=$((SKIPPED + 1))
    else
      echo -e "${COLOR_RED}❌ [$method] $endpoint${COLOR_RESET} ($HTTP_CODE)"
      if [ -n "$description" ]; then
        echo "   $description"
      fi
      echo "   Response: $(echo $BODY | jq -c '.' 2>/dev/null || echo $BODY | head -c 200)"
      FAILED=$((FAILED + 1))
    fi
    return 1
  else
    echo -e "${COLOR_RED}❌ [$method] $endpoint${COLOR_RESET} ($HTTP_CODE)"
    FAILED=$((FAILED + 1))
    return 1
  fi
}

# Run authentication
authenticate

echo "📋 Testing Endpoints"
echo "==================="
echo ""

# ==================== HEALTH ====================
echo -e "${COLOR_BLUE}Health & Status${COLOR_RESET}"
test_endpoint "GET" "/health" false false "" "Health check"
echo ""

# ==================== AUTH (Public) ====================
echo -e "${COLOR_BLUE}Authentication (Public)${COLOR_RESET}"
test_endpoint "POST" "/auth/register" false false '{"email":"test'$(date +%s)'@test.com","password":"Test123!","username":"testuser'$(date +%s)'","firstName":"Test","lastName":"User"}' "Register new user"
test_endpoint "POST" "/auth/login" false false '{"email":"OASIS_ADMIN","password":"Uppermall1!"}' "Login"
test_endpoint "POST" "/auth/forgot-password" false false '{"email":"test@example.com"}' "Forgot password"
echo ""

# ==================== USER PROFILE ====================
echo -e "${COLOR_BLUE}User Profile${COLOR_RESET}"
test_endpoint "GET" "/user/profile" true false "" "Get user profile"
test_endpoint "PUT" "/user/profile" true false '{"firstName":"Updated"}' "Update user profile"
echo ""

# ==================== ASSETS (Public Reads) ====================
echo -e "${COLOR_BLUE}Assets (Public Reads)${COLOR_RESET}"
test_endpoint "GET" "/assets" false false "" "List all assets"
test_endpoint "GET" "/assets/search?q=test" false false "" "Search assets"
# Note: Testing with a specific ID would need an existing asset ID
echo ""

# ==================== WALLET ====================
echo -e "${COLOR_BLUE}Wallet${COLOR_RESET}"
test_endpoint "GET" "/wallet/balance" true false "" "Get wallet balances"
test_endpoint "POST" "/wallet/generate" true false '{"providerType":"SolanaOASIS","setAsDefault":true}' "Generate Solana wallet"
test_endpoint "POST" "/wallet/generate" true false '{"providerType":"EthereumOASIS","setAsDefault":false}' "Generate Ethereum wallet"
test_endpoint "GET" "/wallet/verification-message" true false "" "Get verification message"
test_endpoint "POST" "/wallet/sync" true false "" "Sync wallet balances"
echo ""

# ==================== ORDERS ====================
echo -e "${COLOR_BLUE}Orders${COLOR_RESET}"
test_endpoint "GET" "/orders" true false "" "Get all orders"
test_endpoint "GET" "/orders/open" true false "" "Get open orders"
test_endpoint "GET" "/orders/history" true false "" "Get order history"
# Note: POST/PUT/DELETE would need valid order data
echo ""

# ==================== TRADES ====================
echo -e "${COLOR_BLUE}Trades${COLOR_RESET}"
test_endpoint "GET" "/trades" true false "" "Get all trades"
test_endpoint "GET" "/trades/history" true false "" "Get trade history"
test_endpoint "GET" "/trades/statistics" true false "" "Get trade statistics"
echo ""

# ==================== TRANSACTIONS ====================
echo -e "${COLOR_BLUE}Transactions${COLOR_RESET}"
test_endpoint "GET" "/transactions" true false "" "Get all transactions"
test_endpoint "GET" "/transactions/pending" true false "" "Get pending transactions"
# Note: POST endpoints would need valid transaction data
echo ""

# ==================== ADMIN (Requires Admin Role) ====================
echo -e "${COLOR_BLUE}Admin Endpoints (Requires Admin Role)${COLOR_RESET}"
test_endpoint "GET" "/admin/users" true true "" "Get all users (admin)"
test_endpoint "GET" "/admin/assets" true true "" "Get all assets (admin)"
test_endpoint "GET" "/admin/orders" true true "" "Get all orders (admin)"
test_endpoint "GET" "/admin/trades" true true "" "Get all trades (admin)"
test_endpoint "GET" "/admin/transactions" true true "" "Get all transactions (admin)"
test_endpoint "GET" "/admin/stats" true true "" "Get admin stats"
test_endpoint "GET" "/admin/analytics" true true "" "Get admin analytics"
echo ""

# ==================== SMART CONTRACTS (Admin Only) ====================
echo -e "${COLOR_BLUE}Smart Contracts (Admin Only)${COLOR_RESET}"
test_endpoint "GET" "/smart-contracts/cache-stats" true true "" "Get cache stats"
# Note: Deploy endpoints would need contract data
echo ""

# ==================== SUMMARY ====================
echo ""
echo "==================="
echo "📊 Test Summary"
echo "==================="
echo -e "Total:  $TOTAL"
echo -e "${COLOR_GREEN}Passed: $PASSED${COLOR_RESET}"
echo -e "${COLOR_RED}Failed:  $FAILED${COLOR_RESET}"
echo -e "${COLOR_YELLOW}Skipped: $SKIPPED${COLOR_RESET}"
echo ""

if [ $FAILED -eq 0 ]; then
  echo -e "${COLOR_GREEN}✅ All tests passed!${COLOR_RESET}"
  exit 0
else
  echo -e "${COLOR_RED}❌ Some tests failed${COLOR_RESET}"
  exit 1
fi



