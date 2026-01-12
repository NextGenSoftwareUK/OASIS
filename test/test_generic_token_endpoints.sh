#!/bin/bash

# Test script for Generic Token Operations (ERC-20 compatible)
# Tests the new generic token endpoints in WalletController

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
API_BASE_URL="${API_BASE_URL:-http://localhost:5003}"
JWT_TOKEN="${JWT_TOKEN:-}"
AVATAR_ID="${AVATAR_ID:-}"
MNEE_CONTRACT="0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF"
USDC_CONTRACT="0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48" # Example: USDC on Ethereum
SPENDER_ADDRESS="${SPENDER_ADDRESS:-0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb}" # Example spender

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Generic Token Operations Test Suite${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Check if JWT token is set
if [ -z "$JWT_TOKEN" ]; then
    echo -e "${RED}❌ Error: JWT_TOKEN environment variable is not set${NC}"
    echo "Please set it with: export JWT_TOKEN='your_token_here'"
    exit 1
fi

# Check if API is reachable
echo -e "${YELLOW}Checking API availability...${NC}"
if ! curl -s -k "${API_BASE_URL}/api/health" > /dev/null 2>&1; then
    echo -e "${YELLOW}⚠️  Warning: API health check failed. Continuing anyway...${NC}"
fi
echo ""

# Test counter
PASSED=0
FAILED=0

# Function to test endpoint
test_endpoint() {
    local name=$1
    local method=$2
    local url=$3
    local data=$4
    
    echo -e "${BLUE}Testing: ${name}${NC}"
    echo -e "  ${YELLOW}${method} ${url}${NC}"
    
    if [ -z "$data" ]; then
        response=$(curl -s -k -X "${method}" \
            -H "Authorization: Bearer ${JWT_TOKEN}" \
            -H "Content-Type: application/json" \
            "${API_BASE_URL}${url}")
    else
        response=$(curl -s -k -X "${method}" \
            -H "Authorization: Bearer ${JWT_TOKEN}" \
            -H "Content-Type: application/json" \
            -d "${data}" \
            "${API_BASE_URL}${url}")
    fi
    
    # Check if response contains error
    if echo "$response" | grep -q '"isError":true'; then
        echo -e "  ${RED}❌ API Error${NC}"
        echo "  Response: $(echo "$response" | jq -r '.message // .' 2>/dev/null || echo "$response")"
        FAILED=$((FAILED + 1))
    elif echo "$response" | grep -q '"isError":false\|"result"'; then
        echo -e "  ${GREEN}✅ Success${NC}"
        echo "  Response: $(echo "$response" | jq '.' 2>/dev/null || echo "$response" | head -c 200)"
        PASSED=$((PASSED + 1))
    else
        echo -e "  ${YELLOW}⚠️  Unexpected response${NC}"
        echo "  Response: $(echo "$response" | head -c 200)"
        FAILED=$((FAILED + 1))
    fi
    echo ""
}

# Test 1: Get MNEE Token Balance
echo -e "${GREEN}=== Test 1: Get MNEE Token Balance ===${NC}"
if [ -n "$AVATAR_ID" ]; then
    test_endpoint "Get MNEE Balance" "GET" \
        "/api/wallet/token/balance?tokenContractAddress=${MNEE_CONTRACT}&providerType=EthereumOASIS&avatarId=${AVATAR_ID}"
else
    test_endpoint "Get MNEE Balance (default avatar)" "GET" \
        "/api/wallet/token/balance?tokenContractAddress=${MNEE_CONTRACT}&providerType=EthereumOASIS"
fi

# Test 2: Get USDC Token Balance (example of different token)
echo -e "${GREEN}=== Test 2: Get USDC Token Balance (Generic) ===${NC}"
if [ -n "$AVATAR_ID" ]; then
    test_endpoint "Get USDC Balance" "GET" \
        "/api/wallet/token/balance?tokenContractAddress=${USDC_CONTRACT}&providerType=EthereumOASIS&avatarId=${AVATAR_ID}"
else
    test_endpoint "Get USDC Balance (default avatar)" "GET" \
        "/api/wallet/token/balance?tokenContractAddress=${USDC_CONTRACT}&providerType=EthereumOASIS"
fi

# Test 3: Get Token Info (MNEE)
echo -e "${GREEN}=== Test 3: Get MNEE Token Info ===${NC}"
test_endpoint "Get MNEE Token Info" "GET" \
    "/api/wallet/token/info?tokenContractAddress=${MNEE_CONTRACT}&providerType=EthereumOASIS"

# Test 4: Get Token Info (USDC)
echo -e "${GREEN}=== Test 4: Get USDC Token Info (Generic) ===${NC}"
test_endpoint "Get USDC Token Info" "GET" \
    "/api/wallet/token/info?tokenContractAddress=${USDC_CONTRACT}&providerType=EthereumOASIS"

# Test 5: Get Token Allowance
echo -e "${GREEN}=== Test 5: Get Token Allowance ===${NC}"
if [ -n "$AVATAR_ID" ]; then
    test_endpoint "Get MNEE Allowance" "GET" \
        "/api/wallet/token/allowance?tokenContractAddress=${MNEE_CONTRACT}&spenderAddress=${SPENDER_ADDRESS}&providerType=EthereumOASIS&avatarId=${AVATAR_ID}"
else
    test_endpoint "Get MNEE Allowance (default avatar)" "GET" \
        "/api/wallet/token/allowance?tokenContractAddress=${MNEE_CONTRACT}&spenderAddress=${SPENDER_ADDRESS}&providerType=EthereumOASIS"
fi

# Test 6: Approve Token (requires private key, may fail if not set up)
echo -e "${GREEN}=== Test 6: Approve Token ===${NC}"
if [ -n "$AVATAR_ID" ]; then
    approval_data=$(cat <<EOF
{
    "avatarId": "${AVATAR_ID}",
    "tokenContractAddress": "${MNEE_CONTRACT}",
    "spenderAddress": "${SPENDER_ADDRESS}",
    "amount": 100.0,
    "providerType": "EthereumOASIS"
}
EOF
)
    test_endpoint "Approve MNEE" "POST" "/api/wallet/token/approve" "$approval_data"
else
    approval_data=$(cat <<EOF
{
    "avatarId": "00000000-0000-0000-0000-000000000000",
    "tokenContractAddress": "${MNEE_CONTRACT}",
    "spenderAddress": "${SPENDER_ADDRESS}",
    "amount": 100.0,
    "providerType": "EthereumOASIS"
}
EOF
)
    test_endpoint "Approve MNEE (default avatar)" "POST" "/api/wallet/token/approve" "$approval_data"
fi

# Summary
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Test Summary${NC}"
echo -e "${BLUE}========================================${NC}"
echo -e "${GREEN}✅ Passed: ${PASSED}${NC}"
echo -e "${RED}❌ Failed: ${FAILED}${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}🎉 All tests passed!${NC}"
    exit 0
else
    echo -e "${YELLOW}⚠️  Some tests failed. Check the output above.${NC}"
    exit 1
fi
