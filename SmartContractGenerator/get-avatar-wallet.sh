#!/bin/bash
# Get OASIS_ADMIN avatar Solana wallet and request devnet SOL

API_URL="http://api.oasisweb4.com"
USERNAME="OASIS_ADMIN"
PASSWORD="Uppermall1!"

echo "🔐 Authenticating OASIS_ADMIN..."
AUTH_RESPONSE=$(curl -s -X POST "$API_URL/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}")

# Extract token and avatar ID
TOKEN=$(echo "$AUTH_RESPONSE" | jq -r '.result.jwtToken // .result.result.jwtToken // empty')
AVATAR_ID=$(echo "$AUTH_RESPONSE" | jq -r '.result.avatarId // .result.result.avatarId // .result.result.id // empty')

if [ -z "$TOKEN" ] || [ "$TOKEN" == "null" ]; then
    echo "❌ Authentication failed"
    echo "$AUTH_RESPONSE" | jq -r '.message // .' 2>/dev/null || echo "$AUTH_RESPONSE"
    exit 1
fi

echo "✅ Authenticated"
echo "👤 Avatar ID: $AVATAR_ID"
echo ""

# Extract Solana wallet from auth response
SOLANA_WALLET=$(echo "$AUTH_RESPONSE" | jq -r '.result.result.providerWallets.SolanaOASIS[0] // empty')

if [ -z "$SOLANA_WALLET" ] || [ "$SOLANA_WALLET" == "null" ]; then
    echo "❌ No Solana wallet found in authentication response"
    exit 1
fi

WALLET_ADDRESS=$(echo "$SOLANA_WALLET" | jq -r '.walletAddress // .publicKey // empty')
WALLET_ID=$(echo "$SOLANA_WALLET" | jq -r '.walletId // .id // empty')
PUBLIC_KEY=$(echo "$SOLANA_WALLET" | jq -r '.publicKey // empty')

echo "✅ Found Solana wallet:"
echo "   Address: $WALLET_ADDRESS"
echo "   Public Key: $PUBLIC_KEY"
echo "   Wallet ID: $WALLET_ID"
echo ""

# Request devnet SOL (2 SOL = 2000000000 lamports)
echo "💰 Requesting 2 SOL from devnet faucet..."
FAUCET_RESPONSE=$(curl -s -X POST "https://api.devnet.solana.com" \
  -H "Content-Type: application/json" \
  -d "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"requestAirdrop\",\"params\":[\"$WALLET_ADDRESS\", 2000000000]}")

SIGNATURE=$(echo "$FAUCET_RESPONSE" | jq -r '.result // empty')

if [ -n "$SIGNATURE" ] && [ "$SIGNATURE" != "null" ]; then
    echo "✅ Airdrop requested!"
    echo "   Transaction: $SIGNATURE"
    echo "   Explorer: https://explorer.solana.com/tx/$SIGNATURE?cluster=devnet"
    echo ""
    echo "⏳ Waiting 10 seconds for transaction to confirm..."
    sleep 10
    
    # Check balance
    BALANCE_RESPONSE=$(curl -s -X POST "https://api.devnet.solana.com" \
      -H "Content-Type: application/json" \
      -d "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"getBalance\",\"params\":[\"$WALLET_ADDRESS\"]}")
    
    BALANCE_LAMPORTS=$(echo "$BALANCE_RESPONSE" | jq -r '.result.value // 0')
    BALANCE_SOL=$(echo "scale=9; $BALANCE_LAMPORTS / 1000000000" | bc)
    
    echo "💰 Current balance: $BALANCE_SOL SOL"
else
    echo "⚠️  Airdrop request response:"
    echo "$FAUCET_RESPONSE" | jq . 2>/dev/null || echo "$FAUCET_RESPONSE"
    echo ""
    echo "💡 You can also request manually:"
    echo "   https://faucet.solana.com/?address=$WALLET_ADDRESS"
fi

echo ""
echo "========================================"
echo "📋 Configuration"
echo "========================================"
echo "export OASIS_AVATAR_ID=\"$AVATAR_ID\""
echo "export OASIS_SOLANA_WALLET=\"$WALLET_ADDRESS\""
echo "export OASIS_WALLET_ID=\"$WALLET_ID\""
echo "export OASIS_JWT_TOKEN=\"$TOKEN\""
echo "export OASIS_API_URL=\"$API_URL\""
echo ""
echo "💾 Save to file:"
echo "   source <(./get-avatar-wallet.sh | grep '^export')"
