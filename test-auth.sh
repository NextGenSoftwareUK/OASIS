#!/bin/bash

# Quick authentication test
API_BASE="https://localhost:5004/api"

echo "Testing authentication with metabricks_admin..."
echo ""

curl -k -X POST "$API_BASE/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "metabricks_admin",
    "password": "Uppermall1!"
  }' | python3 -m json.tool 2>/dev/null

echo ""
echo ""
echo "If you see a token above, authentication worked!"

