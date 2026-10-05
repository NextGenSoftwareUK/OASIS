#!/bin/bash

# GitBook Auto-Upload Script
# Uploads OASIS documentation to GitBook using their API

# Configuration
GITBOOK_TOKEN="${GITBOOK_TOKEN:-}"
GITBOOK_SPACE_ID="${GITBOOK_SPACE_ID:-}"

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "📚 GitBook Documentation Upload Script"
echo "======================================"

# Check if token and space ID are set
if [ -z "$GITBOOK_TOKEN" ]; then
  echo -e "${RED}Error: GITBOOK_TOKEN environment variable not set${NC}"
  echo "Get your token from: https://app.gitbook.com/account/developer"
  echo ""
  echo "Usage:"
  echo "  export GITBOOK_TOKEN='your-token-here'"
  echo "  export GITBOOK_SPACE_ID='your-space-id'"
  echo "  ./upload-docs-to-gitbook.sh"
  exit 1
fi

if [ -z "$GITBOOK_SPACE_ID" ]; then
  echo -e "${RED}Error: GITBOOK_SPACE_ID environment variable not set${NC}"
  echo "Find your space ID in the GitBook URL: gitbook.com/s/{SPACE_ID}"
  exit 1
fi

echo -e "${GREEN}✓${NC} Configuration found"
echo "Space ID: $GITBOOK_SPACE_ID"
echo ""

# Key documentation files to upload
DOCS=(
  "README.md:Introduction"
  "OASIS_PROVIDER_ARCHITECTURE_GUIDE.md:Provider Architecture"
  "HYPERDRIVE_ARCHITECTURE_DIAGRAM.md:HyperDrive Architecture"
  "WEB4_HYPERDRIVE_ARCHITECTURE_DIAGRAMS.md:Web4 Diagrams"
  "WEB4_ECOSYSTEM_COMPLETE.md:Web4 Ecosystem"
  "WEB4_TOKEN_TECHNICAL_DEEP_DIVE_AND_VALUATION.md:Web4 Technical Deep Dive"
  "QUSDC_COMPLETE_ARCHITECTURE.md:qUSDC Architecture"
  "HYPERDRIVE_LIQUIDITY_POOLS_PLATFORM.md:Liquidity Pools"
  "OASIS_API_COMPLETE_ENDPOINTS_SUMMARY.md:API Endpoints"
)

# Upload each document
for doc in "${DOCS[@]}"; do
  IFS=':' read -r file title <<< "$doc"
  
  if [ ! -f "$file" ]; then
    echo -e "${YELLOW}⚠${NC} Skipping $file (not found)"
    continue
  fi
  
  echo -e "Uploading: ${GREEN}$title${NC} ($file)..."
  
  # Read file content and escape for JSON
  CONTENT=$(cat "$file" | jq -Rs .)
  
  # Upload to GitBook
  RESPONSE=$(curl -s -X POST \
    "https://api.gitbook.com/v1/spaces/${GITBOOK_SPACE_ID}/content/path/${file}" \
    -H "Authorization: Bearer ${GITBOOK_TOKEN}" \
    -H "Content-Type: application/json" \
    -d "{
      \"content\": $CONTENT,
      \"message\": \"Update $title\"
    }")
  
  # Check if successful
  if echo "$RESPONSE" | grep -q "error"; then
    echo -e "${RED}✗${NC} Failed: $title"
    echo "$RESPONSE"
  else
    echo -e "${GREEN}✓${NC} Uploaded: $title"
  fi
  
  # Rate limiting - wait 1 second between uploads
  sleep 1
done

echo ""
echo -e "${GREEN}✓ All documentation uploaded!${NC}"
echo "View at: https://app.gitbook.com/s/${GITBOOK_SPACE_ID}"

