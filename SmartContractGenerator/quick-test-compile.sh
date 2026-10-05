#!/bin/bash
# Quick test to verify compilation works now

API_URL="http://localhost:5000"
TEMP_DIR="/tmp/compile-test-$$"
mkdir -p "$TEMP_DIR"

echo "📝 Generating simple contract..."
cat > "$TEMP_DIR/spec.json" << 'SPEC'
{
  "programName": "quick_test",
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

echo "   Generating contract..."
curl -s -o "$TEMP_DIR/generated.zip" -X POST "$API_URL/api/v1/contracts/generate" \
  -F "Language=Rust" \
  -F "JsonFile=@$TEMP_DIR/spec.json"

if [ ! -f "$TEMP_DIR/generated.zip" ] || [ ! -s "$TEMP_DIR/generated.zip" ]; then
    echo "❌ Generation failed"
    exit 1
fi

echo "✅ Generated ($(ls -lh "$TEMP_DIR/generated.zip" | awk '{print $5}'))"
echo ""
echo "🔨 Compiling (should be fast now - using cache)..."
echo "   This should take 2-3 minutes with cache..."

HTTP_CODE=$(curl -s -o "$TEMP_DIR/compiled.zip" -w "%{http_code}" \
  -X POST "$API_URL/api/v1/contracts/compile" \
  -F "Language=Rust" \
  -F "Source=@$TEMP_DIR/generated.zip" \
  --max-time 600)

echo ""
echo "HTTP Code: $HTTP_CODE"

if [ "$HTTP_CODE" = "200" ]; then
    echo "✅ Compilation successful!"
    echo "   Size: $(ls -lh "$TEMP_DIR/compiled.zip" | awk '{print $5}')"
    echo ""
    echo "📦 Contents:"
    unzip -l "$TEMP_DIR/compiled.zip" 2>/dev/null | head -20
    echo ""
    echo "✅ Ready for deployment!"
else
    echo "❌ Compilation failed"
    head -50 "$TEMP_DIR/compiled.zip"
fi

rm -rf "$TEMP_DIR"
