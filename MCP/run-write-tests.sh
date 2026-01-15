#!/bin/bash

# Quick script to run write endpoint tests
# Usage: ./run-write-tests.sh

cd "$(dirname "$0")"

export TEST_AVATAR_ID="0df19747-fa32-4c2f-a6b8-b55ed76d04af"
export OASIS_PASSWORD="Uppermall1!"
export OASIS_USERNAME="OASIS_ADMIN"

echo "🚀 Running write endpoint tests..."
echo "   Avatar ID: $TEST_AVATAR_ID"
echo ""

npx tsx test-all-write-endpoints.ts
