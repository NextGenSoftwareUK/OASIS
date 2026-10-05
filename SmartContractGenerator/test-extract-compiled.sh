#!/bin/bash
# Test what's inside the compiled zip
TEMP=$(mktemp -d)
echo "Extracting compiled.zip..."
unzip -q test-compile-only.sh 2>/dev/null || echo "Need to generate first"

# Actually, let's generate and compile fresh to see structure
API_URL="http://localhost:5000"
TEMP_DIR="/tmp/test-extract-$$"
mkdir -p "$TEMP_DIR"

cat > "$TEMP_DIR/spec.json" << 'SPEC'
{
  "programName": "extract_test",
  "instructions": [{"name": "initialize", "contextStruct": "Initialize", "params": []}],
  "accounts": [{"name": "Initialize", "fields": [{"name": "authority", "type": "Signer<'info>"}]}]
}
SPEC

curl -s -o "$TEMP_DIR/generated.zip" -X POST "$API_URL/api/v1/contracts/generate" \
  -F "Language=Rust" \
  -F "JsonFile=@$TEMP_DIR/spec.json"

curl -s -o "$TEMP_DIR/compiled.zip" -X POST "$API_URL/api/v1/contracts/compile" \
  -F "Language=Rust" \
  -F "Source=@$TEMP_DIR/generated.zip"

echo "Contents of compiled.zip:"
unzip -l "$TEMP_DIR/compiled.zip"

echo ""
echo "Extracting compiled.zip..."
unzip -q -o "$TEMP_DIR/compiled.zip" -d "$TEMP_DIR/extracted"

echo "Contents of extracted directory:"
find "$TEMP_DIR/extracted" -type f

echo ""
echo "Looking for .so files:"
find "$TEMP_DIR/extracted" -name "*.so"

echo ""
echo "If there's a zip inside, extracting it..."
INNER_ZIP=$(find "$TEMP_DIR/extracted" -name "*.zip" | head -1)
if [ -n "$INNER_ZIP" ]; then
    echo "Found inner zip: $INNER_ZIP"
    unzip -q -o "$INNER_ZIP" -d "$TEMP_DIR/inner_extracted"
    echo "Contents after inner extraction:"
    find "$TEMP_DIR/inner_extracted" -type f
    echo ""
    echo ".so files:"
    find "$TEMP_DIR/inner_extracted" -name "*.so"
fi

rm -rf "$TEMP_DIR"
