#!/bin/bash
# Generate, Compile, and Deploy Aztec Bridge Contract using Contract Generator API
# The API handles the full lifecycle: generate → compile → deploy

set -e

API_URL="${CONTRACT_GENERATOR_API_URL:-http://localhost:5000}"
CONTRACT_NAME="${CONTRACT_NAME:-BridgeContract}"
PLATFORM="aztec"
CONTRACT_TYPE="bridge"

echo "🚀 Generating, Compiling, and Deploying Aztec Bridge Contract"
echo "API URL: $API_URL"
echo "Contract Name: $CONTRACT_NAME"
echo ""

# Check if API is running
if ! curl -s "$API_URL/health" > /dev/null 2>&1; then
    echo "⚠️  API health check failed. Is the contract generator API running?"
    echo "   Start it with: cd /Volumes/Storage/OASIS_CLEAN/contract-generator/api/src/SmartContractGen/ScGen.API && dotnet run"
    echo ""
fi

# Step 1: Generate the contract
echo "📝 Step 1: Generating contract..."
GENERATE_RESPONSE=$(curl -s -X POST "$API_URL/api/contracts/generate" \
    -H "Content-Type: application/json" \
    -d "{
        \"platform\": \"$PLATFORM\",
        \"contractType\": \"$CONTRACT_TYPE\",
        \"contractName\": \"$CONTRACT_NAME\",
        \"description\": \"Bridge contract for Zcash <-> Aztec cross-chain transfers. Supports private deposits and withdrawals with viewing key audit support.\",
        \"functions\": [
            {
                \"name\": \"deposit\",
                \"type\": \"private\",
                \"parameters\": [
                    {\"name\": \"owner\", \"type\": \"AztecAddress\"},
                    {\"name\": \"amount\", \"type\": \"Field\"}
                ],
                \"description\": \"Deposit funds into the bridge. Creates a private note.\"
            },
            {
                \"name\": \"withdraw\",
                \"type\": \"private\",
                \"parameters\": [
                    {\"name\": \"destination\", \"type\": \"AztecAddress\"},
                    {\"name\": \"amount\", \"type\": \"Field\"}
                ],
                \"description\": \"Withdraw funds from the bridge. Consumes private note.\"
            },
            {
                \"name\": \"get_deposits\",
                \"type\": \"utility\",
                \"parameters\": [
                    {\"name\": \"user\", \"type\": \"AztecAddress\"}
                ],
                \"returns\": \"Field\",
                \"description\": \"Get total deposits for a user\"
            },
            {
                \"name\": \"get_withdrawals\",
                \"type\": \"utility\",
                \"parameters\": [
                    {\"name\": \"user\", \"type\": \"AztecAddress\"}
                ],
                \"returns\": \"Field\",
                \"description\": \"Get total withdrawals for a user\"
            }
        ],
        \"storage\": [
            {
                \"name\": \"deposits\",
                \"type\": \"Map<AztecAddress, Field>\",
                \"description\": \"Maps user address to total deposited amount\"
            },
            {
                \"name\": \"withdrawals\",
                \"type\": \"Map<AztecAddress, Field>\",
                \"description\": \"Maps user address to total withdrawn amount\"
            }
        ]
    }")

if [ $? -ne 0 ] || [ -z "$GENERATE_RESPONSE" ]; then
    echo "❌ Failed to generate contract"
    exit 1
fi

echo "✅ Contract generated"
CONTRACT_ID=$(echo "$GENERATE_RESPONSE" | jq -r '.contractId // .id // empty' 2>/dev/null)

# Step 2: Compile the contract (API handles this)
echo ""
echo "🔨 Step 2: Compiling contract..."
if [ -n "$CONTRACT_ID" ]; then
    COMPILE_RESPONSE=$(curl -s -X POST "$API_URL/api/contracts/$CONTRACT_ID/compile" \
        -H "Content-Type: application/json")
    echo "✅ Contract compiled"
else
    echo "⚠️  No contract ID returned, assuming compilation is part of generation"
fi

# Step 3: Deploy the contract (API handles this)
echo ""
echo "🚀 Step 3: Deploying contract to Aztec testnet..."
DEPLOY_RESPONSE=$(curl -s -X POST "$API_URL/api/contracts/$CONTRACT_ID/deploy" \
    -H "Content-Type: application/json" \
    -d "{
        \"network\": \"testnet\",
        \"accountAlias\": \"maxgershfield\",
        \"nodeUrl\": \"https://aztec-testnet-fullnode.zkv.xyz\"
    }")

if [ $? -eq 0 ]; then
    CONTRACT_ADDRESS=$(echo "$DEPLOY_RESPONSE" | jq -r '.contractAddress // .address // empty' 2>/dev/null)
    
    if [ -n "$CONTRACT_ADDRESS" ]; then
        echo "✅ Contract deployed successfully!"
        echo ""
        echo "📋 Contract Details:"
        echo "   Address: $CONTRACT_ADDRESS"
        echo "   Network: Aztec Testnet"
        echo ""
        
        # Save contract address to config
        echo "💾 Saving contract address to OASIS config..."
        # Update appsettings.json or create a config file
        echo "   Update appsettings.json:"
        echo "   \"AztecBridge\": {"
        echo "     \"BridgeContractAddress\": \"$CONTRACT_ADDRESS\""
        echo "   }"
        echo ""
        
        # Also save to a file for easy reference
        echo "$CONTRACT_ADDRESS" > /tmp/aztec-bridge-contract-address.txt
        echo "   Contract address saved to: /tmp/aztec-bridge-contract-address.txt"
    else
        echo "⚠️  Deployment response received but no contract address found"
        echo "Response: $DEPLOY_RESPONSE"
    fi
else
    echo "❌ Failed to deploy contract"
    echo "Response: $DEPLOY_RESPONSE"
    exit 1
fi

echo ""
echo "🎉 Done! Next steps:"
echo "1. Update OASIS appsettings.json with contract address: $CONTRACT_ADDRESS"
echo "2. Update AztecBridgeService.cs to use the contract address"
echo "3. Test the bridge: Use the Bridge API endpoints to deposit/withdraw"
