#!/bin/bash
# Test agent-to-agent payment using curl

# Configuration
API_URL="${OASIS_API_URL:-https://api.oasisweb4.com}"
AGENT_WALLET="6XXEbtk5XZr5rewYCdSmsjM21fw3UT6cq5mXSLGEkhVD"

echo "=" | awk '{printf "%.0s", $1; for(i=0;i<60;i++) printf "="; print ""}'
echo "  Agent Payment Test"
echo "=" | awk '{printf "%.0s", $1; for(i=0;i<60;i++) printf "="; print ""}'
echo ""
echo "Agent wallet: $AGENT_WALLET"
echo "API URL: $API_URL"
echo ""

# Step 1: Authenticate as an agent (we'll use payment_test_sender_v2)
echo "🔐 Step 1: Authenticating sender agent..."
SENDER_USERNAME="payment_test_sender_v2"
# Use deterministic password
PASSWORD_HASH=$(echo -n "$SENDER_USERNAME" | shasum -a 256 | cut -d' ' -f1 | cut -c1-16)
SENDER_PASSWORD="test_$PASSWORD_HASH"

AUTH_RESPONSE=$(curl -s -X POST "$API_URL/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$SENDER_USERNAME\",\"password\":\"$SENDER_PASSWORD\"}")

# Check if authentication worked
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

SENDER_AVATAR_ID=$(echo "$AUTH_RESPONSE" | python3 -c "
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

if [ -z "$TOKEN" ] || [ "$TOKEN" == "null" ]; then
    echo "❌ Authentication failed - agent may not exist"
    echo "   Trying to register agent first..."
    
    # Try to register
    REGISTER_RESPONSE=$(curl -s -X POST "$API_URL/api/avatar/register" \
      -H "Content-Type: application/json" \
      -d "{
        \"username\":\"$SENDER_USERNAME\",
        \"email\":\"agent_${SENDER_USERNAME}@agents.local\",
        \"password\":\"$SENDER_PASSWORD\",
        \"confirmPassword\":\"$SENDER_PASSWORD\",
        \"firstName\":\"Agent\",
        \"lastName\":\"Sender\",
        \"title\":\"Agent\",
        \"avatarType\":\"User\",
        \"acceptTerms\":true
      }")
    
    # Try to authenticate again
    AUTH_RESPONSE=$(curl -s -X POST "$API_URL/api/avatar/authenticate" \
      -H "Content-Type: application/json" \
      -d "{\"username\":\"$SENDER_USERNAME\",\"password\":\"$SENDER_PASSWORD\"}")
    
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
    
    SENDER_AVATAR_ID=$(echo "$AUTH_RESPONSE" | python3 -c "
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
fi

if [ -z "$TOKEN" ] || [ "$TOKEN" == "null" ]; then
    echo "❌ Failed to authenticate sender agent"
    exit 1
fi

echo "✅ Sender authenticated: $SENDER_AVATAR_ID"
echo ""

# Step 2: Get sender wallet and verify it matches
echo "🔍 Step 2: Checking sender wallet..."
WALLETS_RESPONSE=$(curl -s -X GET "$API_URL/api/wallet/avatar/$SENDER_AVATAR_ID/wallets" \
  -H "Authorization: Bearer $TOKEN")

SENDER_WALLET=$(echo "$WALLETS_RESPONSE" | python3 -c "
import sys, json
try:
    data = json.load(sys.stdin)
    result = data.get('result', {})
    
    # Handle different response structures
    wallets = []
    if isinstance(result, dict):
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
        if provider_type in ['SolanaOASIS', 'Solana', 3]:
            address = wallet.get('address') or wallet.get('walletAddress') or wallet.get('publicKey')
            if address:
                print(address)
                sys.exit(0)
    
    print('')
except:
    print('')
" 2>/dev/null)

if [ -z "$SENDER_WALLET" ]; then
    echo "   No Solana wallet found - generating one..."
    
    # Step 2a: Generate keypair
    echo "   🔑 Generating keypair..."
    KEYPAIR_RESPONSE=$(curl -s -X POST "$API_URL/api/keys/generate_keypair_for_provider/SolanaOASIS" \
      -H "Authorization: Bearer $TOKEN")
    
    PRIVATE_KEY=$(echo "$KEYPAIR_RESPONSE" | python3 -c "
import sys, json
try:
    data = json.load(sys.stdin)
    result = data.get('result', {})
    inner_result = result.get('result', {})
    keypair = inner_result or result
    print(keypair.get('privateKey', ''))
except:
    print('')
" 2>/dev/null)
    
    PUBLIC_KEY=$(echo "$KEYPAIR_RESPONSE" | python3 -c "
import sys, json
try:
    data = json.load(sys.stdin)
    result = data.get('result', {})
    inner_result = result.get('result', {})
    keypair = inner_result or result
    print(keypair.get('publicKey', '') or keypair.get('walletAddress', ''))
except:
    print('')
" 2>/dev/null)
    
    if [ -z "$PRIVATE_KEY" ] || [ -z "$PUBLIC_KEY" ]; then
        echo "❌ Failed to generate keypair"
        exit 1
    fi
    
    echo "   ✅ Keypair generated: ${PUBLIC_KEY:0:20}..."
    
    # Step 2b: Link private key (creates wallet)
    echo "   🔗 Linking private key..."
    LINK_PRIVATE_RESPONSE=$(curl -s -X POST "$API_URL/api/keys/link_provider_private_key_to_avatar_by_id" \
      -H "Content-Type: application/json" \
      -H "Authorization: Bearer $TOKEN" \
      -d "{
        \"AvatarID\": \"$SENDER_AVATAR_ID\",
        \"ProviderType\": \"SolanaOASIS\",
        \"ProviderKey\": \"$PRIVATE_KEY\"
      }")
    
    WALLET_ID=$(echo "$LINK_PRIVATE_RESPONSE" | python3 -c "
import sys, json
try:
    data = json.load(sys.stdin)
    result = data.get('result', {})
    inner_result = result.get('result', {})
    wallet_id = inner_result.get('walletId') or result.get('walletId') or ''
    print(wallet_id)
except:
    print('')
" 2>/dev/null)
    
    if [ -z "$WALLET_ID" ]; then
        echo "❌ Failed to link private key"
        exit 1
    fi
    
    echo "   ✅ Private key linked, wallet ID: $WALLET_ID"
    
    # Step 2c: Link public key (completes wallet)
    echo "   🔗 Linking public key..."
    LINK_PUBLIC_RESPONSE=$(curl -s -X POST "$API_URL/api/keys/link_provider_public_key_to_avatar_by_id" \
      -H "Content-Type: application/json" \
      -H "Authorization: Bearer $TOKEN" \
      -d "{
        \"AvatarID\": \"$SENDER_AVATAR_ID\",
        \"ProviderType\": \"SolanaOASIS\",
        \"ProviderKey\": \"$PUBLIC_KEY\",
        \"WalletId\": \"$WALLET_ID\"
      }")
    
    echo "   ✅ Public key linked"
    SENDER_WALLET="$PUBLIC_KEY"
    
    echo ""
    echo "✅ Sender wallet generated: $SENDER_WALLET"
    if [ "$SENDER_WALLET" == "$AGENT_WALLET" ]; then
        echo "   ✅ Matches expected agent wallet!"
    else
        echo "   ⚠️  Does not match expected agent wallet ($AGENT_WALLET)"
        echo "   💡 You may need to transfer SOL to: $SENDER_WALLET"
    fi
else
    echo "✅ Sender wallet: $SENDER_WALLET"
    if [ "$SENDER_WALLET" == "$AGENT_WALLET" ]; then
        echo "   ✅ Matches expected agent wallet!"
    else
        echo "   ⚠️  Does not match expected agent wallet ($AGENT_WALLET)"
    fi
fi
echo ""

# Step 3: Authenticate receiver agent
echo "🔐 Step 3: Authenticating receiver agent..."
RECEIVER_USERNAME="payment_test_receiver_v2"
RECEIVER_PASSWORD_HASH=$(echo -n "$RECEIVER_USERNAME" | shasum -a 256 | cut -d' ' -f1 | cut -c1-16)
RECEIVER_PASSWORD="test_$RECEIVER_PASSWORD_HASH"

RECEIVER_AUTH_RESPONSE=$(curl -s -X POST "$API_URL/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$RECEIVER_USERNAME\",\"password\":\"$RECEIVER_PASSWORD\"}")

RECEIVER_AVATAR_ID=$(echo "$RECEIVER_AUTH_RESPONSE" | python3 -c "
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

if [ -z "$RECEIVER_AVATAR_ID" ] || [ "$RECEIVER_AVATAR_ID" == "null" ]; then
    echo "   Receiver doesn't exist, registering..."
    REGISTER_RESPONSE=$(curl -s -X POST "$API_URL/api/avatar/register" \
      -H "Content-Type: application/json" \
      -d "{
        \"username\":\"$RECEIVER_USERNAME\",
        \"email\":\"agent_${RECEIVER_USERNAME}@agents.local\",
        \"password\":\"$RECEIVER_PASSWORD\",
        \"confirmPassword\":\"$RECEIVER_PASSWORD\",
        \"firstName\":\"Agent\",
        \"lastName\":\"Receiver\",
        \"title\":\"Agent\",
        \"avatarType\":\"User\",
        \"acceptTerms\":true
      }")
    
    RECEIVER_AUTH_RESPONSE=$(curl -s -X POST "$API_URL/api/avatar/authenticate" \
      -H "Content-Type: application/json" \
      -d "{\"username\":\"$RECEIVER_USERNAME\",\"password\":\"$RECEIVER_PASSWORD\"}")
    
    RECEIVER_AVATAR_ID=$(echo "$RECEIVER_AUTH_RESPONSE" | python3 -c "
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
fi

if [ -z "$RECEIVER_AVATAR_ID" ] || [ "$RECEIVER_AVATAR_ID" == "null" ]; then
    echo "❌ Failed to get receiver avatar ID"
    exit 1
fi

echo "✅ Receiver avatar: $RECEIVER_AVATAR_ID"
echo ""

# Step 4: Send payment
echo "💳 Step 4: Sending payment (0.01 SOL)..."
echo "   From: $SENDER_AVATAR_ID ($SENDER_WALLET)"
echo "   To: $RECEIVER_AVATAR_ID"
echo ""

AMOUNT="0.01"
MEMO_TEXT="Test payment from agent wallet"

PAYMENT_RESPONSE=$(curl -s -X POST \
  "$API_URL/api/solana/SendToAvatar/$RECEIVER_AVATAR_ID?memoText=$MEMO_TEXT" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d "$AMOUNT")

# Check result - handle empty response
if [ -z "$PAYMENT_RESPONSE" ] || [ "$PAYMENT_RESPONSE" == "null" ]; then
    echo "❌ PAYMENT FAILED - Empty response from API"
    echo "   This might indicate the API endpoint is not available or there's a connection issue"
    exit 1
fi

IS_ERROR=$(echo "$PAYMENT_RESPONSE" | python3 -c "
import sys, json
try:
    data = json.load(sys.stdin)
    is_error = data.get('isError', False) or data.get('result', {}).get('isError', False)
    print('true' if is_error else 'false')
except Exception as e:
    print('true')
" 2>/dev/null)

# Check if it's actually a success (isError is false)
if [ "$IS_ERROR" == "false" ]; then
    echo "✅ PAYMENT SUCCESSFUL!"
    TX_HASH=$(echo "$PAYMENT_RESPONSE" | python3 -c "
import sys, json
try:
    data = json.load(sys.stdin)
    result = data.get('result', {})
    tx_hash = result.get('transactionHash') or result.get('result', {}).get('transactionHash') or 'N/A'
    print(tx_hash)
except:
    print('N/A')
" 2>/dev/null)
    echo "   Transaction: $TX_HASH"
    echo "   Amount: $AMOUNT SOL"
    echo "   Memo: $MEMO_TEXT"
    echo ""
    echo "   View on Solana Explorer:"
    echo "   https://explorer.solana.com/tx/$TX_HASH?cluster=devnet"
    exit 0
elif [ "$IS_ERROR" == "true" ] || [ -z "$IS_ERROR" ]; then
    echo "❌ PAYMENT FAILED"
    ERROR_MSG=$(echo "$PAYMENT_RESPONSE" | python3 -c "
import sys, json
try:
    data = json.load(sys.stdin)
    msg = data.get('message') or data.get('result', {}).get('message') or data.get('result', {}).get('result', {}).get('message') or 'Unknown error'
    print(msg)
except Exception as e:
    print(f'Parse error: {str(e)}')
" 2>/dev/null)
    echo "   Error: $ERROR_MSG"
    echo ""
    echo "   Full response:"
    echo "$PAYMENT_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$PAYMENT_RESPONSE"
    
    # Check if it's a balance issue
    if echo "$ERROR_MSG" | grep -qi "balance\|insufficient\|debit.*credit\|no record"; then
        echo ""
        echo "   💡 This looks like a balance issue - the sender wallet may not have enough SOL"
        echo "   💡 Check balance: solana balance $SENDER_WALLET --url devnet"
        echo "   💡 Fund wallet: https://faucet.solana.com/?address=$SENDER_WALLET"
    fi
    exit 1
else
    echo "✅ PAYMENT SUCCESSFUL!"
    TX_HASH=$(echo "$PAYMENT_RESPONSE" | python3 -c "
import sys, json
try:
    data = json.load(sys.stdin)
    result = data.get('result', {})
    tx_hash = result.get('transactionHash') or result.get('result', {}).get('transactionHash') or 'N/A'
    print(tx_hash)
except:
    print('N/A')
" 2>/dev/null)
    echo "   Transaction: $TX_HASH"
    echo "   Amount: $AMOUNT SOL"
    echo "   Memo: $MEMO_TEXT"
    echo ""
    echo "   View on Solana Explorer:"
    echo "   https://explorer.solana.com/tx/$TX_HASH?cluster=devnet"
    exit 0
fi

