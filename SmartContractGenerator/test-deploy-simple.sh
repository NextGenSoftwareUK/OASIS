#!/bin/bash
# Simplified deployment test - step by step

API_URL="http://localhost:5000"
TEMP_DIR="/tmp/sc-simple-$$"
mkdir -p "$TEMP_DIR"

echo "🧪 Simplified Deployment Test"
echo ""

# Step 1: Generate
echo "📝 Generating contract..."
cat > "$TEMP_DIR/spec.json" << 'SPEC'
{
  "programName": "simple_test",
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

HTTP_CODE=$(curl -s -o "$TEMP_DIR/generated.zip" -w "%{http_code}" \
  -X POST "$API_URL/api/v1/contracts/generate" \
  -F "Language=Rust" \
  -F "JsonFile=@$TEMP_DIR/spec.json")

if [ "$HTTP_CODE" != "200" ]; then
    echo "❌ Generation failed: $HTTP_CODE"
    exit 1
fi

echo "✅ Generated ($(ls -lh "$TEMP_DIR/generated.zip" | awk '{print $5}'))"

# Step 2: Compile (with progress)
echo ""
echo "🔨 Compiling (this will take 1-2 minutes)..."
echo "   Progress will be shown..."

HTTP_CODE=$(curl -s -o "$TEMP_DIR/compiled.zip" -w "%{http_code}" \
  -X POST "$API_URL/api/v1/contracts/compile" \
  -F "Language=Rust" \
  -F "Source=@$TEMP_DIR/generated.zip" \
  --max-time 300)

if [ "$HTTP_CODE" != "200" ]; then
    echo "❌ Compilation failed: $HTTP_CODE"
    echo "Response:"
    head -20 "$TEMP_DIR/compiled.zip"
    exit 1
fi

echo "✅ Compiled ($(ls -lh "$TEMP_DIR/compiled.zip" | awk '{print $5}'))"

# Extract .so file
unzip -q -o "$TEMP_DIR/compiled.zip" -d "$TEMP_DIR/extracted" 2>/dev/null
COMPILED_SO=$(find "$TEMP_DIR/extracted" -name "*.so" | head -1)

if [ -z "$COMPILED_SO" ]; then
    echo "⚠️  No .so file found, listing contents:"
    unzip -l "$TEMP_DIR/compiled.zip" 2>/dev/null
    COMPILED_SO="$TEMP_DIR/compiled.zip"
fi

echo "   Using: $COMPILED_SO"
echo ""

# Step 3: Deploy
echo "🚀 Deploying to devnet..."
echo '{}' > "$TEMP_DIR/schema.json"

HTTP_CODE=$(curl -s -o "$TEMP_DIR/deploy.json" -w "%{http_code}" \
  -X POST "$API_URL/api/v1/contracts/deploy" \
  -F "Language=Rust" \
  -F "CompiledContractFile=@$COMPILED_SO" \
  -F "Schema=@$TEMP_DIR/schema.json")

if [ "$HTTP_CODE" != "200" ]; then
    echo "❌ Deployment failed: $HTTP_CODE"
    echo "Response:"
    cat "$TEMP_DIR/deploy.json"
    exit 1
fi

echo "✅ Deployment successful!"
echo ""
echo "Result:"
cat "$TEMP_DIR/deploy.json" | jq . 2>/dev/null || cat "$TEMP_DIR/deploy.json"

PROGRAM_ID=$(cat "$TEMP_DIR/deploy.json" | jq -r '.programId // .address // empty' 2>/dev/null)
if [ -n "$PROGRAM_ID" ] && [ "$PROGRAM_ID" != "null" ]; then
    echo ""
    echo "🎉 Program ID: $PROGRAM_ID"
    echo "   Explorer: https://explorer.solana.com/address/$PROGRAM_ID?cluster=devnet"
fi

echo ""
echo "Files in: $TEMP_DIR"
