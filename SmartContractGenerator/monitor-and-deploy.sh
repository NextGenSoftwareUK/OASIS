#!/bin/bash

# Monitor compilation and deploy when ready

API_URL="http://localhost:5000"
OASIS_API_URL="http://api.oasisweb4.com"
TEMP_DIR="/tmp/sc-deploy-$$"
mkdir -p "$TEMP_DIR"

echo "🔐 Authenticating..."
AUTH=$(curl -s -X POST "$OASIS_API_URL/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username":"OASIS_ADMIN","password":"Uppermall1!"}')

TOKEN=$(echo "$AUTH" | jq -r '.result.jwtToken // .result.result.jwtToken // empty')
WALLET_ADDRESS=$(echo "$AUTH" | jq -r '.result.result.providerWallets.SolanaOASIS[0].walletAddress // empty')

echo "✅ Authenticated"
echo "💰 Wallet: $WALLET_ADDRESS"
echo ""

# Generate contract
echo "📝 Generating contract..."
cat > "$TEMP_DIR/spec.json" << 'SPEC'
{
  "programName": "avatar_test",
  "instructions": [
    {
      "name": "initialize",
      "contextStruct": "Initialize",
      "params": []
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
    }
  ]
}
SPEC

curl -s -o "$TEMP_DIR/generated.zip" -X POST "$API_URL/api/v1/contracts/generate" \
  -F "Language=Rust" \
  -F "JsonFile=@$TEMP_DIR/spec.json" > /dev/null

echo "✅ Generated"
echo ""

# Compile (with longer timeout)
echo "🔨 Compiling (this may take 5-10 minutes for first build)..."
echo "   Monitoring progress..."

HTTP_CODE=$(curl -s -o "$TEMP_DIR/compiled.zip" -w "%{http_code}" \
  -X POST "$API_URL/api/v1/contracts/compile" \
  -F "Language=Rust" \
  -F "Source=@$TEMP_DIR/generated.zip" \
  --max-time 900)

if [ "$HTTP_CODE" != "200" ]; then
    echo "❌ Compilation failed: $HTTP_CODE"
    head -20 "$TEMP_DIR/compiled.zip"
    exit 1
fi

echo "✅ Compiled!"
echo ""

# Extract and deploy
unzip -q -o "$TEMP_DIR/compiled.zip" -d "$TEMP_DIR/extracted" 2>/dev/null
COMPILED_SO=$(find "$TEMP_DIR/extracted" -name "*.so" | head -1)

if [ -z "$COMPILED_SO" ]; then
    echo "⚠️  No .so file, using ZIP directly"
    COMPILED_SO="$TEMP_DIR/compiled.zip"
fi

echo "🚀 Deploying to devnet..."
echo '{}' > "$TEMP_DIR/schema.json"

HTTP_CODE=$(curl -s -o "$TEMP_DIR/deploy.json" -w "%{http_code}" \
  -X POST "$API_URL/api/v1/contracts/deploy" \
  -F "Language=Rust" \
  -F "CompiledContractFile=@$COMPILED_SO" \
  -F "Schema=@$TEMP_DIR/schema.json")

if [ "$HTTP_CODE" != "200" ]; then
    echo "❌ Deployment failed: $HTTP_CODE"
    cat "$TEMP_DIR/deploy.json"
    exit 1
fi

echo "✅ Deployment successful!"
echo ""
cat "$TEMP_DIR/deploy.json" | jq .

PROGRAM_ID=$(cat "$TEMP_DIR/deploy.json" | jq -r '.programId // .address // empty' 2>/dev/null)
if [ -n "$PROGRAM_ID" ] && [ "$PROGRAM_ID" != "null" ]; then
    echo ""
    echo "🎉 Program ID: $PROGRAM_ID"
    echo "   Explorer: https://explorer.solana.com/address/$PROGRAM_ID?cluster=devnet"
fi

echo ""
echo "Files: $TEMP_DIR"


