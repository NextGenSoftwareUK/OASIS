#!/bin/bash

# Create metabricks_admin avatar if it doesn't exist
API_BASE="https://localhost:5004/api"

echo "Creating metabricks_admin avatar..."
echo ""

curl -k -X POST "$API_BASE/avatar" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "metabricks_admin",
    "email": "max.gershfield1@gmail.com",
    "password": "Uppermall1!",
    "firstName": "MetaBricks",
    "lastName": "Admin",
    "avatarType": "User",
    "createdOASISType": "OASISAPIREST",
    "acceptTerms": true
  }' | python3 -m json.tool 2>/dev/null

echo ""
echo ""
echo "Avatar creation complete. Now try running ./test-auth.sh to authenticate!"

