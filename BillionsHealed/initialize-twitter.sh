#!/bin/bash

# BillionsHealed - Twitter Integration Initialization Script
# This script helps you set up Twitter API integration

echo "🐦 BillionsHealed - Twitter Integration Setup"
echo "=============================================="
echo ""

# Check if backend directory exists
if [ ! -d "backend" ]; then
    echo "❌ Error: backend/ directory not found"
    echo "Please run this script from the BillionsHealed root directory"
    exit 1
fi

cd backend

# Check if .env file exists
if [ -f ".env" ]; then
    echo "⚠️  .env file already exists"
    read -p "Do you want to overwrite it? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Keeping existing .env file"
        exit 0
    fi
fi

# Prompt for Twitter Bearer Token
echo ""
echo "Please enter your Twitter API Bearer Token:"
echo "(Get it from: https://developer.twitter.com/en/portal/dashboard)"
echo ""
read -p "Bearer Token: " BEARER_TOKEN

if [ -z "$BEARER_TOKEN" ]; then
    echo "❌ Error: Bearer Token cannot be empty"
    exit 1
fi

# Create .env file
cat > .env << EOL
# Twitter API Configuration
TWITTER_BEARER_TOKEN=${BEARER_TOKEN}
TWITTER_HASHTAG=#billionshealed
BACKEND_URL=http://localhost:3002

# Set to 'true' to use real Twitter API (default: mock data)
USE_REAL_TWITTER=true

# Server Configuration
PORT=3002
NODE_ENV=development
EOL

echo ""
echo "✅ .env file created successfully!"
echo ""

# Check if dotenv is installed
if ! npm list dotenv &> /dev/null; then
    echo "📦 Installing dotenv package..."
    npm install dotenv
    echo "✅ dotenv installed"
else
    echo "✅ dotenv already installed"
fi

echo ""
echo "=============================================="
echo "🎉 Twitter Integration Setup Complete!"
echo "=============================================="
echo ""
echo "Next steps:"
echo "1. Start the backend: npm start"
echo "2. Initialize Twitter service (run in another terminal):"
echo ""
echo "   curl -X POST http://localhost:3002/api/twitter/initialize \\"
echo "     -H 'Content-Type: application/json' \\"
echo "     -d '{\"bearerToken\": \"${BEARER_TOKEN}\", \"hashtag\": \"#billionshealed\"}'"
echo ""
echo "3. Check status:"
echo "   curl http://localhost:3002/api/twitter/status"
echo ""
echo "4. Open frontend/index.html in your browser"
echo ""
echo "Or run ./test-twitter.sh to do steps 2-3 automatically"
echo ""
echo "🌡️ Happy healing! #billionshealed"

