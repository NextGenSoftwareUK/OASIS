#!/bin/bash
API_URL="http://localhost:5000"
TEMP_DIR="/tmp/compile-fixed-$$"
mkdir -p "$TEMP_DIR"

echo "📝 Generating contract with programId fix..."
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

echo "   Calling API..."
HTTP_CODE=$(curl -s -o "$TEMP_DIR/generated.zip" -w "%{http_code}" \
  -X POST "$API_URL/api/v1/contracts/generate" \
  -F "Language=Rust" \
  -F "JsonFile=@$TEMP_DIR/spec.json")

echo "   HTTP Code: $HTTP_CODE"

if [ "$HTTP_CODE" != "200" ]; then
    echo "❌ Generation failed"
    if [ -f "$TEMP_DIR/generated.zip" ]; then
        head -30 "$TEMP_DIR/generated.zip"
    fi
    exit 1
fi

echo "✅ Generated"
echo ""

echo "🔨 Compiling..."
HTTP_CODE=$(curl -s -o "$TEMP_DIR/compiled.zip" -w "%{http_code}" \
  -X POST "$API_URL/api/v1/contracts/compile" \
  -F "Language=Rust" \
  -F "Source=@$TEMP_DIR/generated.zip" \
  --max-time 1200)

echo "   HTTP Code: $HTTP_CODE"

if [ "$HTTP_CODE" != "200" ]; then
    echo "❌ Compilation failed"
    head -50 "$TEMP_DIR/compiled.zip"
    exit 1
fi

echo "✅ Compilation successful!"
unzip -l "$TEMP_DIR/compiled.zip" 2>/dev/null | head -15
