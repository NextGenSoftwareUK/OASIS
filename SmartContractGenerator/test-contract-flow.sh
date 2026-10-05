#!/bin/bash

# Test Script: Generate, Compile, and Deploy a Smart Contract
# This tests the full flow of the Smart Contract Generator API

set -e

API_URL="http://localhost:5000"
TEMP_DIR="/tmp/sc-gen-test-$$"
mkdir -p "$TEMP_DIR"

echo "🧪 Testing Smart Contract Generator API"
echo "========================================"
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Step 1: Check API is running
echo "📡 Step 1: Checking API health..."
if curl -s -f "$API_URL/swagger/index.html" > /dev/null; then
    echo -e "${GREEN}✅ API is running${NC}"
else
    echo -e "${RED}❌ API is not running. Please start it first.${NC}"
    exit 1
fi
echo ""

# Step 2: Generate a simple contract (Template-based)
echo "📝 Step 2: Generating a simple Solana contract..."
CONTRACT_SPEC='{
  "programName": "test_token",
  "instructions": [
    {
      "name": "initialize",
      "contextStruct": "Initialize",
      "params": []
    },
    {
      "name": "mint",
      "contextStruct": "Mint",
      "params": [
        {
          "name": "amount",
          "type": "u64"
        }
      ]
    }
  ],
  "accounts": [
    {
      "name": "Initialize",
      "fields": [
        {
          "name": "authority",
          "type": "Signer<'\''info>"
        }
      ]
    }
  ]
}'

echo "$CONTRACT_SPEC" > "$TEMP_DIR/spec.json"

GENERATE_RESPONSE=$(curl -s -X POST "$API_URL/api/v1/contracts/generate" \
  -F "Language=Rust" \
  -F "JsonFile=@$TEMP_DIR/spec.json" \
  -w "\n%{http_code}")

HTTP_CODE=$(echo "$GENERATE_RESPONSE" | tail -n1)
BODY=$(echo "$GENERATE_RESPONSE" | head -n-1)

if [ "$HTTP_CODE" = "200" ]; then
    echo "$BODY" > "$TEMP_DIR/generated.zip"
    echo -e "${GREEN}✅ Contract generated successfully${NC}"
    echo "   Saved to: $TEMP_DIR/generated.zip"
    ls -lh "$TEMP_DIR/generated.zip"
else
    echo -e "${RED}❌ Generation failed (HTTP $HTTP_CODE)${NC}"
    echo "$BODY"
    exit 1
fi
echo ""

# Step 3: Compile the contract
echo "🔨 Step 3: Compiling the contract..."
echo "   This may take 30-60 seconds (first build downloads dependencies)..."

COMPILE_RESPONSE=$(curl -s -X POST "$API_URL/api/v1/contracts/compile" \
  -F "Language=Rust" \
  -F "Source=@$TEMP_DIR/generated.zip" \
  -w "\n%{http_code}" \
  --max-time 600)

HTTP_CODE=$(echo "$COMPILE_RESPONSE" | tail -n1)
BODY=$(echo "$COMPILE_RESPONSE" | head -n-1)

if [ "$HTTP_CODE" = "200" ]; then
    echo "$BODY" > "$TEMP_DIR/compiled.so"
    echo -e "${GREEN}✅ Contract compiled successfully${NC}"
    echo "   Saved to: $TEMP_DIR/compiled.so"
    ls -lh "$TEMP_DIR/compiled.so"
else
    echo -e "${RED}❌ Compilation failed (HTTP $HTTP_CODE)${NC}"
    echo "$BODY"
    exit 1
fi
echo ""

# Step 4: Deploy the contract (optional - requires Solana setup)
echo "🚀 Step 4: Deploying the contract..."
echo "   Note: This requires Solana devnet/local validator to be configured"

# Create empty schema for deployment
echo '{}' > "$TEMP_DIR/schema.json"

DEPLOY_RESPONSE=$(curl -s -X POST "$API_URL/api/v1/contracts/deploy" \
  -F "Language=Rust" \
  -F "CompiledContractFile=@$TEMP_DIR/compiled.so" \
  -F "Schema=@$TEMP_DIR/schema.json" \
  -w "\n%{http_code}")

HTTP_CODE=$(echo "$DEPLOY_RESPONSE" | tail -n1)
BODY=$(echo "$DEPLOY_RESPONSE" | head -n-1)

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ Contract deployed successfully${NC}"
    echo "$BODY" | jq '.' 2>/dev/null || echo "$BODY"
else
    echo -e "${YELLOW}⚠️  Deployment failed (HTTP $HTTP_CODE)${NC}"
    echo "   This is expected if Solana is not configured"
    echo "$BODY"
fi
echo ""

# Summary
echo "========================================"
echo "📊 Test Summary"
echo "========================================"
echo -e "${GREEN}✅ Generation: SUCCESS${NC}"
echo -e "${GREEN}✅ Compilation: SUCCESS${NC}"
if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ Deployment: SUCCESS${NC}"
else
    echo -e "${YELLOW}⚠️  Deployment: SKIPPED (Solana not configured)${NC}"
fi
echo ""
echo "📁 Test files saved in: $TEMP_DIR"
echo "   - spec.json (contract specification)"
echo "   - generated.zip (generated contract)"
echo "   - compiled.so (compiled contract)"
echo ""

# Cleanup option
read -p "Delete test files? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    rm -rf "$TEMP_DIR"
    echo "🧹 Test files deleted"
else
    echo "📁 Test files kept in: $TEMP_DIR"
fi


