#!/bin/bash

# Bridge Template Generator
# Creates bridge service files for new EVM-compatible chains

# Usage: ./create-bridge-templates.sh

echo "Creating bridge templates for remaining EVM chains..."

CHAINS=(
  "Optimism:OP:10:https://mainnet.optimism.io"
  "BNBChain:BNB:56:https://bsc-dataseed.binance.org"
  "Avalanche:AVAX:43114:https://api.avax.network/ext/bc/C/rpc"
  "Fantom:FTM:250:https://rpcapi.fantom.network"
)

for chain_data in "${CHAINS[@]}"; do
  IFS=':' read -ra PARTS <<< "$chain_data"
  CHAIN_NAME="${PARTS[0]}"
  TOKEN_SYMBOL="${PARTS[1]}"
  CHAIN_ID="${PARTS[2]}"
  RPC_URL="${PARTS[3]}"
  
  echo "Processing $CHAIN_NAME..."
  echo "  - Token: $TOKEN_SYMBOL"
  echo "  - Chain ID: $CHAIN_ID"
  echo "  - RPC: $RPC_URL"
done

echo "Done! Bridge templates ready to create."

