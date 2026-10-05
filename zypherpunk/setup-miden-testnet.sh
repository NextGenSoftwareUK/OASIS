#!/bin/bash

# Miden Testnet Setup Script
# This script helps set up Miden testnet access and configuration

set -e

echo "🚀 Miden Testnet Setup"
echo "======================"
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Step 1: Check if Miden Wallet extension is installed
echo -e "${BLUE}Step 1: Checking Miden Wallet...${NC}"
echo ""
echo "Please ensure you have:"
echo "  1. Installed Miden Wallet browser extension from https://miden.xyz/"
echo "  2. Created a wallet and saved your recovery phrase"
echo "  3. Copied your Miden wallet address"
echo ""
read -p "Do you have the Miden Wallet installed? (y/n): " has_wallet

if [ "$has_wallet" != "y" ]; then
    echo ""
    echo -e "${YELLOW}Please install Miden Wallet first:${NC}"
    echo "  1. Visit: https://miden.xyz/"
    echo "  2. Download the browser extension"
    echo "  3. Create a new wallet"
    echo "  4. Save your recovery phrase securely"
    echo ""
    exit 1
fi

# Step 2: Get wallet address
echo ""
echo -e "${BLUE}Step 2: Wallet Configuration${NC}"
read -p "Enter your Miden wallet address (miden1...): " MIDEN_ADDRESS

if [ -z "$MIDEN_ADDRESS" ]; then
    echo -e "${YELLOW}Error: Wallet address is required${NC}"
    exit 1
fi

# Step 3: Get testnet tokens
echo ""
echo -e "${BLUE}Step 3: Getting Testnet Tokens${NC}"
echo ""
echo "Please visit the Miden Faucet to get test tokens:"
echo "  URL: https://faucet.testnet.miden.io/"
echo ""
echo "Steps:"
echo "  1. Paste your address: ${MIDEN_ADDRESS}"
echo "  2. Choose 'Send Public Note' or 'Send Private Note'"
echo "  3. Wait for transaction (1-2 minutes)"
echo "  4. Open Miden Wallet → Receive → Claim"
echo ""
read -p "Have you requested tokens from the faucet? (y/n): " has_tokens

if [ "$has_tokens" != "y" ]; then
    echo ""
    echo -e "${YELLOW}Please get testnet tokens first:${NC}"
    echo "  Visit: https://faucet.testnet.miden.io/"
    echo ""
    exit 1
fi

# Step 4: Set environment variables
echo ""
echo -e "${BLUE}Step 4: Setting Environment Variables${NC}"

# Create .env file for Miden
ENV_FILE=".env.miden"
cat > "$ENV_FILE" << EOF
# Miden Testnet Configuration
export MIDEN_API_URL="https://testnet.miden.xyz"
export MIDEN_API_KEY=""
export MIDEN_WALLET_ADDRESS="${MIDEN_ADDRESS}"
export MIDEN_BRIDGE_POOL_ADDRESS="miden_bridge_pool"
export MIDEN_NETWORK="testnet"
EOF

echo -e "${GREEN}✅ Created ${ENV_FILE}${NC}"
echo ""
echo "To use these variables, run:"
echo "  source ${ENV_FILE}"
echo ""

# Step 5: Test connection
echo ""
echo -e "${BLUE}Step 5: Testing Connection${NC}"

# Test API endpoint (if available)
echo "Testing Miden testnet API..."
if curl -s -f "https://testnet.miden.xyz/health" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Miden testnet API is accessible${NC}"
else
    echo -e "${YELLOW}⚠️  Could not verify API endpoint (may require authentication)${NC}"
fi

# Step 6: Update OASIS configuration
echo ""
echo -e "${BLUE}Step 6: OASIS Configuration${NC}"
echo ""
echo "Add to your OASIS DNA configuration:"
echo ""
cat << EOF
{
  "Providers": {
    "MidenOASIS": {
      "IsEnabled": true,
      "ApiUrl": "https://testnet.miden.xyz",
      "ApiKey": "",
      "Network": "testnet",
      "WalletAddress": "${MIDEN_ADDRESS}"
    }
  }
}
EOF

echo ""
echo -e "${GREEN}✅ Setup Complete!${NC}"
echo ""
echo "Next steps:"
echo "  1. Source the environment file: source ${ENV_FILE}"
echo "  2. Update OASIS DNA with the configuration above"
echo "  3. Test private note creation"
echo "  4. Test bridge operations"
echo ""

