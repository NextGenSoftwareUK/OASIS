#!/bin/bash
# Deploy Aztec Bridge Contract
# This script helps deploy the bridge contract to Aztec testnet

set -e

export PATH="$HOME/.aztec/bin:$PATH"
export NODE_URL="${AZTEC_NODE_URL:-https://aztec-testnet-fullnode.zkv.xyz}"
export ACCOUNT_ALIAS="${AZTEC_ACCOUNT:-maxgershfield}"

echo "🚀 Deploying Aztec Bridge Contract"
echo "Node URL: $NODE_URL"
echo "Account: $ACCOUNT_ALIAS"
echo ""

# Check if account exists
echo "📋 Checking account..."
if ! aztec-wallet list-accounts --node-url "$NODE_URL" 2>&1 | grep -q "$ACCOUNT_ALIAS"; then
    echo "❌ Account '$ACCOUNT_ALIAS' not found!"
    echo "Create account first: aztec-wallet create-account --node-url $NODE_URL"
    exit 1
fi

echo "✅ Account found"
echo ""

# For now, we'll use a simplified approach:
# 1. The contract code is ready in ~/aztec-bridge-contract
# 2. We need to compile it first (dependency issue to resolve)
# 3. Then deploy using aztec-wallet

echo "📝 Contract Status:"
echo "   - Contract code: ~/aztec-bridge-contract/src/main.nr"
echo "   - Compilation: ⚠️  Needs dependency fix"
echo ""

echo "🔧 Next Steps:"
echo ""
echo "1. Fix Nargo.toml dependency:"
echo "   Update ~/aztec-bridge-contract/Nargo.toml to use:"
echo "   tag = \"v3.0.0-devnet.5\""
echo "   directory = \"noir-projects/aztec-nr/aztec\""
echo ""
echo "2. Compile contract:"
echo "   cd ~/aztec-bridge-contract"
echo "   rm -rf ~/nargo  # Clear cache"
echo "   aztec-nargo compile"
echo ""
echo "3. Deploy contract:"
echo "   aztec-wallet deploy \\"
echo "       --node-url $NODE_URL \\"
echo "       --from accounts:$ACCOUNT_ALIAS \\"
echo "       --payment method=fpc-sponsored,fpc=contracts:sponsoredfpc \\"
echo "       --alias bridge \\"
echo "       ~/aztec-bridge-contract/target/bridge_contract-BridgeContract.json"
echo ""
echo "4. Update OASIS config with deployed contract address"
echo ""

# Alternative: Try to deploy using TypeScript if available
if command -v node &> /dev/null; then
    echo "💡 Alternative: Deploy using TypeScript/Node.js"
    echo "   See: ~/aztec-starter for TypeScript deployment example"
fi

echo ""
echo "📚 Full guide: /Volumes/Storage/OASIS_CLEAN/AZTEC_BRIDGE_DEPLOYMENT_GUIDE.md"

