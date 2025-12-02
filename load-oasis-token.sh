#!/bin/bash

# Quick script to load saved OASIS token
# Usage: source ./load-oasis-token.sh

TOKEN_FILE="${HOME}/.oasis_token"

if [ -f "$TOKEN_FILE" ]; then
    export OASIS_TOKEN=$(cat "$TOKEN_FILE")
    echo "✅ OASIS token loaded from $TOKEN_FILE"
    echo "   Avatar ID: $(echo "$OASIS_TOKEN" | cut -d'.' -f2 | base64 -d 2>/dev/null | jq -r '.id // "unknown"' 2>/dev/null || echo "unknown")"
else
    echo "❌ No saved token found. Run ./auth-oasis.sh first."
    return 1 2>/dev/null || exit 1
fi

