#!/bin/bash
# Get OASIS_ADMIN avatar's Solana wallet address

# Configuration
API_URL="${OASIS_API_URL:-http://localhost:5004}"
USERNAME="OASIS_ADMIN"
PASSWORD="Uppermall1!"

echo "=" | awk '{printf "%.0s", $1; for(i=0;i<60;i++) printf "="; print ""}'
echo "  Get OASIS_ADMIN Solana Wallet Address"
echo "=" | awk '{printf "%.0s", $1; for(i=0;i<60;i++) printf "="; print ""}'
echo ""
echo "API URL: $API_URL"
echo ""

# Step 1: Authenticate
echo "🔐 Authenticating as OASIS_ADMIN..."
AUTH_RESPONSE=$(curl -s -X POST "$API_URL/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}")

# Extract JWT token, avatar ID, and wallets from auth response
TOKEN=$(echo "$AUTH_RESPONSE" | python3 -c "
import sys, json
try:
    data = json.load(sys.stdin)
    result = data.get('result', {})
    inner_result = result.get('result', {})
    token = inner_result.get('jwtToken') or result.get('jwtToken') or data.get('jwtToken') or ''
    print(token)
except:
    print('')
" 2>/dev/null)

AVATAR_ID=$(echo "$AUTH_RESPONSE" | python3 -c "
import sys, json
try:
    data = json.load(sys.stdin)
    result = data.get('result', {})
    inner_result = result.get('result', {})
    avatar_id = inner_result.get('avatarId') or inner_result.get('id') or result.get('avatarId') or ''
    print(avatar_id)
except:
    print('')
" 2>/dev/null)

# Check if authentication was successful
if [ -z "$TOKEN" ] || [ "$TOKEN" == "null" ] || [ -z "$TOKEN" ]; then
    echo "❌ Authentication failed - no JWT token received"
    echo "$AUTH_RESPONSE" | python3 -m json.tool 2>/dev/null | head -50 || echo "$AUTH_RESPONSE" | head -20
    exit 1
fi

if [ -z "$AVATAR_ID" ] || [ "$AVATAR_ID" == "null" ]; then
    echo "❌ Authentication failed - no avatar ID received"
    echo "$AUTH_RESPONSE" | python3 -m json.tool 2>/dev/null | head -50 || echo "$AUTH_RESPONSE" | head -20
    exit 1
fi

echo "✅ Authenticated as OASIS_ADMIN"
echo "   Avatar ID: $AVATAR_ID"
echo ""

# Try to extract Solana wallet from auth response first (it often includes wallets)
echo "🔍 Looking for Solana wallet in authentication response..."
WALLET_ADDRESS=$(echo "$AUTH_RESPONSE" | python3 -c "
import sys, json
try:
    data = json.load(sys.stdin)
    result = data.get('result', {})
    inner_result = result.get('result', {})
    provider_wallets = inner_result.get('providerWallets', {})
    
    # Check for SolanaOASIS wallet
    solana_wallets = provider_wallets.get('SolanaOASIS', [])
    if solana_wallets and len(solana_wallets) > 0:
        wallet = solana_wallets[0]
        address = wallet.get('walletAddress') or wallet.get('publicKey') or wallet.get('address')
        if address:
            print(address)
            sys.exit(0)
    
    print('')
except Exception as e:
    print('')
" 2>/dev/null)

# If not found in auth response, try fetching wallets separately
if [ -z "$WALLET_ADDRESS" ] || [ "$WALLET_ADDRESS" == "null" ]; then
    echo "   Not found in auth response, fetching wallets separately..."
    WALLETS_RESPONSE=$(curl -s -X GET "$API_URL/api/wallet/avatar/$AVATAR_ID/wallets" \
      -H "Authorization: Bearer $TOKEN")
    
    # Extract Solana wallet address from wallets response
    WALLET_ADDRESS=$(echo "$WALLETS_RESPONSE" | python3 -c "
import sys, json
try:
    data = json.load(sys.stdin)
    result = data.get('result', {})
    
    # Handle different response structures
    wallets = []
    if isinstance(result, dict):
        # Dictionary of provider types -> wallets
        for provider, wallet_list in result.items():
            if isinstance(wallet_list, list):
                wallets.extend(wallet_list)
            elif wallet_list:
                wallets.append(wallet_list)
    elif isinstance(result, list):
        wallets = result
    
    # Find Solana wallet
    for wallet in wallets:
        provider_type = wallet.get('providerType') or wallet.get('provider_type')
        if provider_type in ['SolanaOASIS', 'Solana', 3]:  # 3 is the enum value for SolanaOASIS
            address = wallet.get('address') or wallet.get('walletAddress') or wallet.get('publicKey') or wallet.get('PublicKey')
            if address:
                print(address)
                sys.exit(0)
    
    print('')
except Exception as e:
    print('')
" 2>/dev/null)
fi

if [ -z "$WALLET_ADDRESS" ] || [ "$WALLET_ADDRESS" == "null" ]; then
    echo "❌ No Solana wallet found"
    echo "   Response structure:"
    echo "$WALLETS_RESPONSE" | python3 -m json.tool 2>/dev/null | head -50 || echo "$WALLETS_RESPONSE" | head -20
    exit 1
fi

echo "✅ Found Solana wallet!"
echo ""
echo "=" | awk '{printf "%.0s", $1; for(i=0;i<60;i++) printf "="; print ""}'
echo "  SOLANA WALLET ADDRESS"
echo "=" | awk '{printf "%.0s", $1; for(i=0;i<60;i++) printf "="; print ""}'
echo "  $WALLET_ADDRESS"
echo "=" | awk '{printf "%.0s", $1; for(i=0;i<60;i++) printf "="; print ""}'
echo ""
echo "💡 You can now:"
echo "   1. Fund this wallet: https://faucet.solana.com/?address=$WALLET_ADDRESS"
echo "   2. Transfer SOL from this wallet to agent wallets"
echo "   3. Check balance: solana balance $WALLET_ADDRESS --url devnet"
echo ""

