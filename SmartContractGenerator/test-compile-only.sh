#!/bin/bash
# Test compilation separately

API_URL="http://localhost:5000"
TEMP_DIR="/tmp/compile-test-$$"
mkdir -p "$TEMP_DIR"

echo "📝 Step 1: Generate contract..."
cat > "$TEMP_DIR/spec.json" << 'SPEC'
{
  "programName": "test_deploy",
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

echo "   Generating..."
HTTP_CODE=$(curl -s -o "$TEMP_DIR/generated.zip" -w "%{http_code}" \
  -X POST "$API_URL/api/v1/contracts/generate" \
  -F "Language=Rust" \
  -F "JsonFile=@$TEMP_DIR/spec.json")

if [ "$HTTP_CODE" != "200" ]; then
    echo "❌ Generation failed: $HTTP_CODE"
    exit 1
fi

echo "✅ Generated ($(ls -lh "$TEMP_DIR/generated.zip" | awk '{print $5}'))"
echo ""

# Verify ZIP is valid
if ! unzip -t "$TEMP_DIR/generated.zip" > /dev/null 2>&1; then
    echo "❌ Generated ZIP is corrupted!"
    exit 1
fi

echo "✅ ZIP file is valid"
echo ""

echo "🔨 Step 2: Compiling (this will take several minutes)..."
echo "   Starting compilation at $(date)"
echo ""

HTTP_CODE=$(curl -s -o "$TEMP_DIR/compiled.zip" -w "%{http_code}" \
  -X POST "$API_URL/api/v1/contracts/compile" \
  -F "Language=Rust" \
  -F "Source=@$TEMP_DIR/generated.zip" \
  --max-time 1200)

echo ""
echo "   Compilation finished at $(date)"
echo "   HTTP Code: $HTTP_CODE"

if [ "$HTTP_CODE" != "200" ]; then
    echo "❌ Compilation failed"
    echo "Response:"
    head -50 "$TEMP_DIR/compiled.zip"
    exit 1
fi

echo "✅ Compilation successful!"
echo "   Size: $(ls -lh "$TEMP_DIR/compiled.zip" | awk '{print $5}')"
echo ""

echo "📦 Contents:"
unzip -l "$TEMP_DIR/compiled.zip" 2>/dev/null | head -20

echo ""
echo "✅ Ready for deployment!"
echo "   Files saved in: $TEMP_DIR"
