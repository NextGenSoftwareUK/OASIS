#!/bin/bash

# Full Smart Contract Flow: Generate → Compile → Deploy
# Uses OASIS_ADMIN avatar wallet for deployment

set -e

API_URL="http://localhost:5000"
OASIS_API_URL="http://api.oasisweb4.com"
TEMP_DIR="/tmp/sc-deploy-test-$$"
mkdir -p "$TEMP_DIR"

echo "🚀 Full Smart Contract Deployment Test"
echo "========================================"
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Step 1: Authenticate and get wallet
echo "🔐 Step 1: Authenticating OASIS_ADMIN avatar..."
AUTH_RESPONSE=$(curl -s -X POST "$OASIS_API_URL/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username":"OASIS_ADMIN","password":"Uppermall1!"}')

TOKEN=$(echo "$AUTH_RESPONSE" | jq -r '.result.jwtToken // .result.result.jwtToken // empty')
AVATAR_ID=$(echo "$AUTH_RESPONSE" | jq -r '.result.avatarId // .result.result.avatarId // .result.result.id // empty')
SOLANA_WALLET=$(echo "$AUTH_RESPONSE" | jq -r '.result.result.providerWallets.SolanaOASIS[0] // empty')

if [ -z "$TOKEN" ] || [ "$TOKEN" == "null" ]; then
    echo -e "${RED}❌ Authentication failed${NC}"
    exit 1
fi

WALLET_ADDRESS=$(echo "$SOLANA_WALLET" | jq -r '.walletAddress // .publicKey // empty')
WALLET_ID=$(echo "$SOLANA_WALLET" | jq -r '.walletId // .id // empty')

echo -e "${GREEN}✅ Authenticated${NC}"
echo "   Avatar ID: $AVATAR_ID"
echo "   Solana Wallet: $WALLET_ADDRESS"
echo ""

# Check wallet balance
echo "💰 Checking wallet balance..."
BALANCE_RESPONSE=$(curl -s -X POST "https://api.devnet.solana.com" \
  -H "Content-Type: application/json" \
  -d "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"getBalance\",\"params\":[\"$WALLET_ADDRESS\"]}")

BALANCE_LAMPORTS=$(echo "$BALANCE_RESPONSE" | jq -r '.result.value // 0')
BALANCE_SOL=$(echo "scale=9; $BALANCE_LAMPORTS / 1000000000" | bc)

echo "   Balance: $BALANCE_SOL SOL"
if (( $(echo "$BALANCE_SOL < 0.1" | bc -l) )); then
    echo -e "${YELLOW}⚠️  Low balance! You may need more SOL for deployment.${NC}"
else
    echo -e "${GREEN}✅ Sufficient balance for deployment${NC}"
fi
echo ""

# Step 2: Generate contract
echo "📝 Step 2: Generating Solana contract..."
cat > "$TEMP_DIR/spec.json" << 'SPEC'
{
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
          "type": "Signer<'info>"
        }
      ]
    },
    {
      "name": "Mint",
      "fields": [
        {
          "name": "mint",
          "type": "Account<'info, Mint>"
        },
        {
          "name": "authority",
          "type": "Signer<'info>"
        }
      ]
    }
  ]
}
SPEC

HTTP_CODE=$(curl -s -o "$TEMP_DIR/generated.zip" -w "%{http_code}" -X POST "$API_URL/api/v1/contracts/generate" \
  -F "Language=Rust" \
  -F "JsonFile=@$TEMP_DIR/spec.json")

if [ "$HTTP_CODE" != "200" ]; then
    echo -e "${RED}❌ Generation failed (HTTP $HTTP_CODE)${NC}"
    cat "$TEMP_DIR/generated.zip"
    exit 1
fi
echo -e "${GREEN}✅ Contract generated${NC}"
echo "   File: $TEMP_DIR/generated.zip ($(ls -lh "$TEMP_DIR/generated.zip" | awk '{print $5}'))"
echo ""

# Step 3: Compile contract
echo "🔨 Step 3: Compiling contract..."
echo "   This may take 30-60 seconds (first build downloads dependencies)..."
echo ""

HTTP_CODE=$(curl -s -o "$TEMP_DIR/compiled.zip" -w "%{http_code}" -X POST "$API_URL/api/v1/contracts/compile" \
  -F "Language=Rust" \
  -F "Source=@$TEMP_DIR/generated.zip" \
  --max-time 600)

if [ "$HTTP_CODE" != "200" ]; then
    echo -e "${RED}❌ Compilation failed (HTTP $HTTP_CODE)${NC}"
    cat "$TEMP_DIR/compiled.zip" | head -50
    exit 1
fi
echo -e "${GREEN}✅ Contract compiled${NC}"
echo "   File: $TEMP_DIR/compiled.zip ($(ls -lh "$TEMP_DIR/compiled.zip" | awk '{print $5}'))"
echo ""

# Extract compiled .so file from ZIP
echo "📦 Extracting compiled contract..."
unzip -q -o "$TEMP_DIR/compiled.zip" -d "$TEMP_DIR/extracted" 2>/dev/null || true
COMPILED_SO=$(find "$TEMP_DIR/extracted" -name "*.so" | head -1)

if [ -z "$COMPILED_SO" ] || [ ! -f "$COMPILED_SO" ]; then
    echo -e "${YELLOW}⚠️  Could not find .so file in compiled ZIP${NC}"
    echo "   Contents:"
    unzip -l "$TEMP_DIR/compiled.zip" 2>/dev/null | head -20
    echo ""
    echo "   Trying to deploy with ZIP file directly..."
    COMPILED_SO="$TEMP_DIR/compiled.zip"
fi

echo "   Using: $COMPILED_SO"
echo ""

# Step 4: Get wallet keypair from OASIS API
echo "🔑 Step 4: Retrieving wallet keypair from OASIS..."
WALLET_DETAILS=$(curl -s -X GET "$OASIS_API_URL/api/wallet/$WALLET_ID" \
  -H "Authorization: Bearer $TOKEN")

KEYPAIR_DATA=$(echo "$WALLET_DETAILS" | jq -r '.result.privateKey // .result.keypair // .result.secretRecoveryPhrase // empty')

if [ -z "$KEYPAIR_DATA" ] || [ "$KEYPAIR_DATA" == "null" ]; then
    echo -e "${YELLOW}⚠️  Could not retrieve keypair from API${NC}"
    echo "   Response:"
    echo "$WALLET_DETAILS" | jq . 2>/dev/null || echo "$WALLET_DETAILS"
    echo ""
    echo "   Attempting deployment without keypair (API may use configured wallet)..."
    KEYPAIR_FILE=""
else
    # Save keypair to file
    echo "$KEYPAIR_DATA" > "$TEMP_DIR/keypair.json"
    KEYPAIR_FILE="$TEMP_DIR/keypair.json"
    echo -e "${GREEN}✅ Keypair retrieved${NC}"
    echo "   Saved to: $KEYPAIR_FILE"
fi
echo ""

# Step 5: Deploy contract
echo "🚀 Step 5: Deploying contract to Solana devnet..."
echo "   Using wallet: $WALLET_ADDRESS"
echo ""

# Create empty schema
echo '{}' > "$TEMP_DIR/schema.json"

DEPLOY_CMD="curl -s -o \"$TEMP_DIR/deploy-response.json\" -w \"%{http_code}\" -X POST \"$API_URL/api/v1/contracts/deploy\" \
  -F \"Language=Rust\" \
  -F \"CompiledContractFile=@$COMPILED_SO\" \
  -F \"Schema=@$TEMP_DIR/schema.json\""

if [ -n "$KEYPAIR_FILE" ] && [ -f "$KEYPAIR_FILE" ]; then
    DEPLOY_CMD="$DEPLOY_CMD -F \"WalletKeypair=@$KEYPAIR_FILE\""
fi

HTTP_CODE=$(eval $DEPLOY_CMD)
BODY=$(cat "$TEMP_DIR/deploy-response.json" 2>/dev/null || echo "")

if [ "$HTTP_CODE" != "200" ]; then
    echo -e "${RED}❌ Deployment failed (HTTP $HTTP_CODE)${NC}"
    echo "Response:"
    echo "$BODY" | head -100
    echo ""
    echo "Troubleshooting:"
    echo "  - Check API logs for errors"
    echo "  - Verify wallet has sufficient SOL"
    echo "  - Check Solana RPC endpoint is accessible"
    exit 1
fi

echo -e "${GREEN}✅ Deployment successful!${NC}"
echo ""
echo "📋 Deployment Result:"
echo "$BODY" | jq . 2>/dev/null || echo "$BODY"
echo ""

# Extract program ID if available
PROGRAM_ID=$(echo "$BODY" | jq -r '.programId // .address // .result.programId // .result.address // empty' 2>/dev/null)
TRANSACTION=$(echo "$BODY" | jq -r '.transactionHash // .signature // .result.transactionHash // .result.signature // empty' 2>/dev/null)

if [ -n "$PROGRAM_ID" ] && [ "$PROGRAM_ID" != "null" ]; then
    echo "🎉 Program ID: $PROGRAM_ID"
    echo "   Explorer: https://explorer.solana.com/address/$PROGRAM_ID?cluster=devnet"
fi

if [ -n "$TRANSACTION" ] && [ "$TRANSACTION" != "null" ]; then
    echo "📝 Transaction: $TRANSACTION"
    echo "   Explorer: https://explorer.solana.com/tx/$TRANSACTION?cluster=devnet"
fi

echo ""
echo "========================================"
echo "✅ Full Flow Complete!"
echo "========================================"
echo "Generated → Compiled → Deployed"
echo ""
echo "📁 Test files saved in: $TEMP_DIR"
echo "   - spec.json (contract specification)"
echo "   - generated.zip (generated contract)"
echo "   - compiled.zip (compiled contract)"
if [ -n "$KEYPAIR_FILE" ]; then
    echo "   - keypair.json (wallet keypair - SECURE!)"
fi
echo ""

# Cleanup option
read -p "Delete test files? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    rm -rf "$TEMP_DIR"
    echo "🧹 Test files deleted"
else
    echo "📁 Test files kept in: $TEMP_DIR"
    if [ -n "$KEYPAIR_FILE" ]; then
        echo -e "${YELLOW}⚠️  WARNING: Keypair file contains sensitive data!${NC}"
    fi
fi

