#!/bin/bash

# Bridge Testing Script for OASIS Universal Asset Bridge
# Tests Solana, Aztec, and Zcash bridge endpoints

BASE_URL="${OASIS_API_URL:-http://localhost:5000}"
echo "🧪 Testing OASIS Bridge at: $BASE_URL"
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test function
test_endpoint() {
    local method=$1
    local endpoint=$2
    local data=$3
    local description=$4
    
    echo -n "Testing $description... "
    
    if [ "$method" = "GET" ]; then
        response=$(curl -s -w "\n%{http_code}" "$BASE_URL$endpoint")
    else
        response=$(curl -s -w "\n%{http_code}" -X "$method" \
            -H "Content-Type: application/json" \
            -d "$data" \
            "$BASE_URL$endpoint")
    fi
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" -ge 200 ] && [ "$http_code" -lt 300 ]; then
        echo -e "${GREEN}✅ PASS${NC} (HTTP $http_code)"
        echo "$body" | jq '.' 2>/dev/null || echo "$body"
        return 0
    else
        echo -e "${RED}❌ FAIL${NC} (HTTP $http_code)"
        echo "$body"
        return 1
    fi
}

# Test 1: Get supported networks
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "1️⃣  Testing Supported Networks Endpoint"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
test_endpoint "GET" "/api/v1/networks" "" "Get Supported Networks"
echo ""

# Test 2: Get exchange rate (Solana to Radix)
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "2️⃣  Testing Exchange Rate Endpoint"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
test_endpoint "GET" "/api/v1/exchange-rate?fromToken=SOL&toToken=XRD" "" "SOL → XRD Exchange Rate"
echo ""

# Test 3: Get exchange rate (Zcash to Aztec)
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
test_endpoint "GET" "/api/v1/exchange-rate?fromToken=ZEC&toToken=AZTEC" "" "ZEC → AZTEC Exchange Rate"
echo ""

# Test 4: Create a test bridge order (Solana to Radix)
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "3️⃣  Testing Bridge Order Creation (Solana → Radix)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
ORDER_DATA='{
    "fromToken": "SOL",
    "toToken": "XRD",
    "amount": 1.0,
    "fromAddress": "test-solana-address",
    "toAddress": "test-radix-address",
    "fromChain": "Solana",
    "toChain": "Radix"
}'
test_endpoint "POST" "/api/v1/orders" "$ORDER_DATA" "Create Bridge Order"
ORDER_ID=$(echo "$body" | jq -r '.orderId' 2>/dev/null)
echo ""

if [ -n "$ORDER_ID" ] && [ "$ORDER_ID" != "null" ]; then
    # Test 5: Check order balance
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "4️⃣  Testing Order Balance Check"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    test_endpoint "GET" "/api/v1/orders/$ORDER_ID/check-balance" "" "Check Order Balance"
    echo ""
fi

# Test 6: Create private bridge order (Zcash to Aztec)
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "5️⃣  Testing Private Bridge Order (Zcash → Aztec)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
PRIVATE_ORDER_DATA='{
    "fromToken": "ZEC",
    "toToken": "AZTEC",
    "amount": 0.5,
    "fromAddress": "zt1test123",
    "toAddress": "aztec-test-address",
    "fromChain": "Zcash",
    "toChain": "Aztec"
}'
test_endpoint "POST" "/api/v1/orders/private" "$PRIVATE_ORDER_DATA" "Create Private Bridge Order"
echo ""

# Test 7: Record viewing key (for audit)
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "6️⃣  Testing Viewing Key Audit"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
VIEWING_KEY_DATA='{
    "transactionId": "test-tx-123",
    "viewingKey": "test-viewing-key",
    "address": "zt1test123",
    "submittedBy": "test-user",
    "purpose": "bridge-audit"
}'
test_endpoint "POST" "/api/v1/viewing-keys/audit" "$VIEWING_KEY_DATA" "Record Viewing Key"
echo ""

# Test 8: Verify proof
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "7️⃣  Testing Proof Verification"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
PROOF_DATA='{
    "proofPayload": "test-proof-payload",
    "proofType": "STARK"
}'
test_endpoint "POST" "/api/v1/proofs/verify" "$PROOF_DATA" "Verify Proof"
echo ""

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🎉 Bridge Testing Complete!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

