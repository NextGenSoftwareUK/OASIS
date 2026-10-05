#!/bin/bash

# Aztec Local Development Setup Script
# Sets up Aztec SDK starter for local bridge testing

set -e

echo "🔐 Aztec Bridge Local Development Setup"
echo "========================================"
echo ""

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo "❌ Node.js is not installed. Please install Node.js first."
    exit 1
fi

# Check if Yarn is installed
if ! command -v yarn &> /dev/null; then
    echo "⚠️  Yarn not found. Installing yarn..."
    npm install -g yarn
fi

# Directory for Aztec SDK
AZTEC_DIR="/Volumes/Storage/OASIS_CLEAN/aztec-sdk-starter"

# Check if already cloned
if [ -d "$AZTEC_DIR" ]; then
    echo "📁 Aztec SDK starter already exists at: $AZTEC_DIR"
    read -p "Do you want to update it? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "🔄 Updating Aztec SDK starter..."
        cd "$AZTEC_DIR"
        git pull
    else
        echo "✅ Using existing Aztec SDK starter"
    fi
else
    echo "📥 Cloning Aztec SDK starter..."
    cd /Volumes/Storage/OASIS_CLEAN
    git clone https://github.com/AztecProtocol/aztec-sdk-starter.git
fi

# Navigate to directory
cd "$AZTEC_DIR"

# Install dependencies
echo "📦 Installing dependencies..."
yarn install

# Check for .env file
if [ ! -f ".env" ]; then
    echo "⚙️  Creating .env file..."
    if [ -f ".env.example" ]; then
        cp .env.example .env
        echo "✅ Created .env from .env.example"
        echo "⚠️  Please edit .env and add your Ethereum private key or mnemonic"
    else
        echo "⚠️  .env.example not found. Creating basic .env..."
        cat > .env << EOF
# Aztec SDK Configuration
ETHEREUM_PRIVATE_KEY=your_private_key_here
# OR
# MNEMONIC=your twelve word mnemonic phrase here

# Network Configuration
NETWORK=local
EOF
        echo "✅ Created basic .env file"
        echo "⚠️  Please edit .env and add your Ethereum private key or mnemonic"
    fi
else
    echo "✅ .env file already exists"
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✅ Aztec SDK Starter Setup Complete!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📝 Next Steps:"
echo "   1. Edit $AZTEC_DIR/.env and add your Ethereum private key or mnemonic"
echo "   2. Start the local Aztec node:"
echo "      cd $AZTEC_DIR"
echo "      yarn start"
echo "   3. Verify the API is running:"
echo "      curl http://localhost:8080/health"
echo "   4. Update OASIS appsettings.json if needed:"
echo "      \"AztecBridge\": {"
echo "        \"ApiUrl\": \"http://localhost:8080\""
echo "      }"
echo ""
echo "📚 For more information, see: AZTEC_BRIDGE_SETUP.md"
echo ""

