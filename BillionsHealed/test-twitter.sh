#!/bin/bash

# BillionsHealed - Twitter Integration Test Script
# This script tests the Twitter API integration

echo "🧪 Testing Twitter Integration for BillionsHealed"
echo "=================================================="
echo ""

# Check if backend is running
echo "1️⃣ Checking if backend is running..."
if ! curl -s http://localhost:3002/health > /dev/null 2>&1; then
    echo "❌ Backend is not running"
    echo "Please start it with: cd backend && npm start"
    exit 1
fi
echo "✅ Backend is running"
echo ""

# Check if .env file exists
echo "2️⃣ Checking for .env configuration..."
if [ ! -f "backend/.env" ]; then
    echo "❌ .env file not found"
    echo "Please run ./initialize-twitter.sh first"
    exit 1
fi
echo "✅ .env file found"
echo ""

# Load Bearer Token from .env
source backend/.env

if [ -z "$TWITTER_BEARER_TOKEN" ]; then
    echo "❌ TWITTER_BEARER_TOKEN not set in .env file"
    exit 1
fi

# Initialize Twitter service
echo "3️⃣ Initializing Twitter service..."
INIT_RESPONSE=$(curl -s -X POST http://localhost:3002/api/twitter/initialize \
  -H "Content-Type: application/json" \
  -d "{\"bearerToken\": \"${TWITTER_BEARER_TOKEN}\", \"hashtag\": \"#billionshealed\"}")

echo "$INIT_RESPONSE" | jq . 2>/dev/null || echo "$INIT_RESPONSE"
echo ""

# Check Twitter service status
echo "4️⃣ Checking Twitter service status..."
STATUS_RESPONSE=$(curl -s http://localhost:3002/api/twitter/status)
echo "$STATUS_RESPONSE" | jq . 2>/dev/null || echo "$STATUS_RESPONSE"
echo ""

# Check if initialized
if echo "$STATUS_RESPONSE" | grep -q '"isInitialized":true'; then
    echo "✅ Twitter service initialized successfully"
else
    echo "⚠️  Twitter service initialization may have issues"
    echo "Check the backend logs for details"
fi
echo ""

# Fetch recent tweets
echo "5️⃣ Fetching recent #billionshealed tweets..."
TWEETS_RESPONSE=$(curl -s "http://localhost:3002/api/twitter/recent-tweets?limit=5")
echo "$TWEETS_RESPONSE" | jq . 2>/dev/null || echo "$TWEETS_RESPONSE"
echo ""

# Check if we got real tweets or mock data
if echo "$TWEETS_RESPONSE" | grep -q '"success":true'; then
    TWEET_COUNT=$(echo "$TWEETS_RESPONSE" | jq '.meta.result_count' 2>/dev/null || echo "0")
    echo "✅ Found $TWEET_COUNT tweets"
    
    if [ "$TWEET_COUNT" = "0" ]; then
        echo ""
        echo "ℹ️  No tweets found with #billionshealed yet"
        echo "This is normal if the hashtag is new!"
        echo ""
        echo "Try tweeting:"
        echo "\"Starting my healing journey today! 🌟 #billionshealed\""
    fi
else
    echo "⚠️  Could not fetch tweets - may be using mock data"
fi

echo ""
echo "=================================================="
echo "🎉 Twitter Integration Test Complete!"
echo "=================================================="
echo ""
echo "Next steps:"
echo "1. Open frontend/index.html in your browser"
echo "2. Tweet with #billionshealed from @BillionsHealed account"
echo "3. Wait 2 minutes for the backend to detect the tweet"
echo "4. Refresh the frontend to see the new tweet"
echo "5. Watch the thermometer rise! 🌡️"
echo ""
echo "Troubleshooting:"
echo "- If using Free tier: Only 1 search per 24 hours allowed"
echo "- If 429 error: Rate limit hit, wait 24 hours or upgrade to Elevated"
echo "- If 403 error: Check Bearer Token is correct"
echo ""
echo "🌡️ Together, we heal! #billionshealed"


