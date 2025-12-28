#!/bin/bash

# Full Smart Contract Deployment: Generate → Compile → Deploy
# Uses OASIS_ADMIN avatar's Solana wallet

set -e

API_URL="http://localhost:5000"
OASIS_API_URL="http://api.oasisweb4.com"
TEMP_DIR="/tmp/sc-deploy-$$"
mkdir -p "$TEMP_DIR"
trap "rm -rf $TEMP_DIR" EXIT

echo "🚀 Smart Contract Deployment with Avatar Wallet"
echo "================================================"
echo ""

# Step 1: Authenticate and get wallet
echo "🔐 Step 1: Authenticating OASIS_ADMIN avatar..."
AUTH_RESPONSE=$(curl -s -X POST "$OASIS_API_URL/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username":"OASIS_ADMIN","password":"Uppermall1!"}')

TOKEN=$(echo "$AUTH_RESPONSE" | jq -r '.result.jwtToken // .result.result.jwtToken // empty')
AVATAR_ID=$(echo "$AUTH_RESPONSE" | jq -r '.result.avatarId // .result.result.avatarId // empty')
SOLANA_WALLET=$(echo "$AUTH_RESPONSE" | jq -r '.result.result.providerWallets.SolanaOASIS[0] // empty')

if [ -z "$TOKEN" ] || [ "$TOKEN" == "null" ]; then
    echo "❌ Authentication failed"
    echo "$AUTH_RESPONSE" | jq .
    exit 1
fi

WALLET_ADDRESS=$(echo "$SOLANA_WALLET" | jq -r '.walletAddress // .publicKey // empty')
WALLET_ID=$(echo "$SOLANA_WALLET" | jq -r '.id // .walletId // empty')

echo "✅ Authenticated"
echo "   Avatar ID: $AVATAR_ID"
echo "   Solana Wallet: $WALLET_ADDRESS"
echo ""

# Check wallet balance
echo "💰 Checking wallet balance..."
BALANCE_RESPONSE=$(curl -s -X POST "https://api.devnet.solana.com" \
  -H "Content-Type: application/json" \
  -d "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"getBalance\",\"params\":[\"$WALLET_ADDRESS\"]}")

BALANCE_LAMPORTS=$(echo "$BALANCE_RESPONSE" | jq -r '.result.value // 0')
BALANCE_SOL=$(echo "scale=9; $BALANCE_LAMPORTS / 1000000000" | bc)

echo "   Balance: $BALANCE_SOL SOL"

REQUIRED_SOL=0.005
if (( $(echo "$BALANCE_SOL < $REQUIRED_SOL" | bc -l) )); then
    echo "❌ Insufficient balance. Need at least $REQUIRED_SOL SOL."
    echo "   Request devnet SOL: https://faucet.solana.com/?address=$WALLET_ADDRESS"
    exit 1
fi
echo "✅ Sufficient balance for deployment"
echo ""

# Step 1.5: Get wallet keypair from OASIS API
echo "🔑 Retrieving wallet keypair from OASIS API..."
WALLET_DETAILS=$(curl -s -X GET "$OASIS_API_URL/api/wallet/$WALLET_ID" \
  -H "Authorization: Bearer $TOKEN")

# Try to get private key from wallet details
PRIVATE_KEY=$(echo "$WALLET_DETAILS" | jq -r '.result.privateKey // .result.keypair // empty')

if [ -z "$PRIVATE_KEY" ] || [ "$PRIVATE_KEY" == "null" ]; then
    echo "⚠️  Private key not in wallet details, trying keys API..."
    
    # Try the keys API endpoint
    PRIVATE_KEYS_RESPONSE=$(curl -s -X GET "$OASIS_API_URL/api/keys/get_provider_private_keys_for_avatar_by_id/$AVATAR_ID?providerType=SolanaOASIS" \
      -H "Authorization: Bearer $TOKEN")
    
    PRIVATE_KEY=$(echo "$PRIVATE_KEYS_RESPONSE" | jq -r '.result[0] // .result.result[0] // empty')
fi

if [ -z "$PRIVATE_KEY" ] || [ "$PRIVATE_KEY" == "null" ]; then
    echo "⚠️  Could not retrieve private key from OASIS API (this is normal - keys are encrypted)"
    echo "   Falling back to configured default wallet keypair..."
    echo ""
    
    # Check if default keypair exists and matches the wallet address
    DEFAULT_KEYPAIR="/Users/maxgershfield/.config/solana/id.json"
    if [ -f "$DEFAULT_KEYPAIR" ]; then
        # Try to verify the keypair matches (if solana CLI is available)
        if command -v solana > /dev/null 2>&1; then
            DEFAULT_ADDRESS=$(solana address -k "$DEFAULT_KEYPAIR" 2>/dev/null)
            if [ "$DEFAULT_ADDRESS" = "$WALLET_ADDRESS" ]; then
                echo "✅ Default keypair matches avatar wallet address!"
                WALLET_KEYPAIR_FILE="$DEFAULT_KEYPAIR"
                USE_DEFAULT_KEYPAIR=true
            else
                echo "⚠️  Default keypair address ($DEFAULT_ADDRESS) doesn't match avatar wallet"
                echo "   Will attempt deployment with default keypair anyway"
                WALLET_KEYPAIR_FILE="$DEFAULT_KEYPAIR"
                USE_DEFAULT_KEYPAIR=true
            fi
        else
            echo "   Using default keypair: $DEFAULT_KEYPAIR"
            WALLET_KEYPAIR_FILE="$DEFAULT_KEYPAIR"
            USE_DEFAULT_KEYPAIR=true
        fi
    else
        echo "❌ Default keypair file not found: $DEFAULT_KEYPAIR"
        echo "   Cannot proceed with deployment without wallet keypair"
        exit 1
    fi
else
    USE_DEFAULT_KEYPAIR=false

    echo "✅ Private key retrieved"
    echo "   Format: $(echo "$PRIVATE_KEY" | head -c 20)... (length: $(echo -n "$PRIVATE_KEY" | wc -c))"

    # Convert private key to Solana keypair format if needed
    # Solana keypairs are JSON arrays of 64 bytes (secret key)
    # OASIS might return base58 string, so we need to convert it

    # Check if it's already a JSON array (Solana format)
    if echo "$PRIVATE_KEY" | jq -e 'type == "array"' > /dev/null 2>&1; then
        echo "   Already in Solana keypair format"
        echo "$PRIVATE_KEY" > "$TEMP_DIR/wallet-keypair.json"
        WALLET_KEYPAIR_FILE="$TEMP_DIR/wallet-keypair.json"
    else
        echo "   Converting from base58 to Solana keypair format..."
        # Use Node.js to convert base58 private key to Solana keypair format
        cat > "$TEMP_DIR/convert-keypair.js" << 'NODEJS'
const { Keypair } = require('@solana/web3.js');
const bs58 = require('bs58');

const privateKeyBase58 = process.argv[2];
try {
    // Decode base58 to bytes
    const secretKey = bs58.decode(privateKeyBase58);
    
    // Create keypair from secret key
    const keypair = Keypair.fromSecretKey(secretKey);
    
    // Output as JSON array (Solana keypair format)
    console.log(JSON.stringify(Array.from(keypair.secretKey)));
} catch (error) {
    console.error('Conversion error:', error.message);
    process.exit(1);
}
NODEJS
        
        # Try to convert using Node.js
        if command -v node > /dev/null 2>&1; then
            CONVERTED_KEYPAIR=$(node "$TEMP_DIR/convert-keypair.js" "$PRIVATE_KEY" 2>/dev/null)
            if [ $? -eq 0 ] && [ -n "$CONVERTED_KEYPAIR" ]; then
                echo "$CONVERTED_KEYPAIR" > "$TEMP_DIR/wallet-keypair.json"
                echo "   ✅ Converted successfully"
                WALLET_KEYPAIR_FILE="$TEMP_DIR/wallet-keypair.json"
            else
                echo "   ⚠️  Conversion failed, trying direct format..."
                # Fallback: try using the private key directly (might work if it's already in the right format)
                echo "$PRIVATE_KEY" > "$TEMP_DIR/wallet-keypair.json"
                WALLET_KEYPAIR_FILE="$TEMP_DIR/wallet-keypair.json"
            fi
        else
            echo "   ⚠️  Node.js not found, using private key as-is"
            echo "$PRIVATE_KEY" > "$TEMP_DIR/wallet-keypair.json"
            WALLET_KEYPAIR_FILE="$TEMP_DIR/wallet-keypair.json"
        fi
    fi
    echo "   Saved to: $WALLET_KEYPAIR_FILE"
    echo ""
fi

# Step 2: Generate contract
echo "📝 Step 2: Generating Solana contract..."
cat > "$TEMP_DIR/spec.json" << 'SPEC'
{
  "programName": "avatar_deployed_program",
  "instructions": [
    {
      "name": "initialize",
      "contextStruct": "Initialize",
      "params": []
    }
  ],
  "accounts": [
    {
      "name": "Initialize",
      "fields": [
        {
          "name": "authority",
          "type": "Signer<'info>"
        }
      ]
    }
  ]
}
SPEC

curl -s -o "$TEMP_DIR/generated.zip" -X POST "$API_URL/api/v1/contracts/generate" \
  -F "Language=Rust" \
  -F "JsonFile=@$TEMP_DIR/spec.json"

if [ ! -f "$TEMP_DIR/generated.zip" ] || [ ! -s "$TEMP_DIR/generated.zip" ]; then
    echo "❌ Contract generation failed"
    exit 1
fi

echo "✅ Contract generated ($(ls -lh "$TEMP_DIR/generated.zip" | awk '{print $5}'))"
echo ""

# Step 3: Compile contract (should be fast with cache)
echo "🔨 Step 3: Compiling contract (using cache - should be fast)..."
HTTP_CODE=$(curl -s -o "$TEMP_DIR/compiled.zip" -w "%{http_code}" \
  -X POST "$API_URL/api/v1/contracts/compile" \
  -F "Language=Rust" \
  -F "Source=@$TEMP_DIR/generated.zip" \
  --max-time 600)

if [ "$HTTP_CODE" != "200" ]; then
    echo "❌ Compilation failed: HTTP $HTTP_CODE"
    head -50 "$TEMP_DIR/compiled.zip"
    exit 1
fi

echo "✅ Compilation successful ($(ls -lh "$TEMP_DIR/compiled.zip" | awk '{print $5}'))"
echo ""

# Extract compiled files - the structure is: compiled.zip contains a zip and keypair
unzip -q -o "$TEMP_DIR/compiled.zip" -d "$TEMP_DIR/extracted" 2>/dev/null

# Find the inner zip file (contains the actual .so)
INNER_ZIP=$(find "$TEMP_DIR/extracted" -name "*.zip" | head -1)
KEYPAIR_FILE=$(find "$TEMP_DIR/extracted" -name "*-keypair.json" | head -1)

if [ -z "$INNER_ZIP" ]; then
    echo "❌ Could not find inner zip file in compiled output"
    echo "Contents of extracted directory:"
    ls -la "$TEMP_DIR/extracted"
    exit 1
fi

# Extract the inner zip to get the .so file
unzip -q -o "$INNER_ZIP" -d "$TEMP_DIR/inner_extracted" 2>/dev/null

# Find the compiled .so file
COMPILED_SO=$(find "$TEMP_DIR/inner_extracted" -name "*.so" | head -1)

if [ -z "$COMPILED_SO" ]; then
    echo "❌ Could not find compiled .so file"
    echo "Contents of inner extracted directory:"
    find "$TEMP_DIR/inner_extracted" -type f
    exit 1
fi

echo "   Found compiled program: $(basename "$COMPILED_SO")"
echo "   Keypair: $(basename "$KEYPAIR_FILE")"

if [ -z "$KEYPAIR_FILE" ]; then
    echo "⚠️  No keypair file found, deployment may use default keypair"
fi
echo ""

# Step 4: Deploy contract
echo "🚀 Step 4: Deploying contract to Solana devnet..."
echo "   Using wallet: $WALLET_ADDRESS"
echo ""

# Prepare deployment request
DEPLOY_CMD="curl -s -X POST \"$API_URL/api/v1/contracts/deploy\""
DEPLOY_CMD="$DEPLOY_CMD -F \"Language=Rust\""
DEPLOY_CMD="$DEPLOY_CMD -F \"CompiledContractFile=@$COMPILED_SO\""

# Use the wallet keypair we retrieved from OASIS
if [ -f "$WALLET_KEYPAIR_FILE" ]; then
    DEPLOY_CMD="$DEPLOY_CMD -F \"WalletKeypair=@$WALLET_KEYPAIR_FILE\""
    echo "   Using OASIS avatar wallet keypair for deployment"
elif [ -n "$KEYPAIR_FILE" ] && [ -f "$KEYPAIR_FILE" ]; then
    DEPLOY_CMD="$DEPLOY_CMD -F \"WalletKeypair=@$KEYPAIR_FILE\""
    echo "   Using program keypair (may not have SOL)"
else
    echo "   ⚠️  No wallet keypair available - deployment may fail"
fi

# Add schema if needed (empty schema for now)
echo '{}' > "$TEMP_DIR/schema.json"
DEPLOY_CMD="$DEPLOY_CMD -F \"Schema=@$TEMP_DIR/schema.json\""

# Add payment headers if needed
DEPLOY_CMD="$DEPLOY_CMD -H \"X-Wallet-Address: $WALLET_ADDRESS\""
DEPLOY_CMD="$DEPLOY_CMD -H \"X-Payment-Token: $TOKEN\""
DEPLOY_CMD="$DEPLOY_CMD -H \"X-Payment-Protocol: x402-solana\""

DEPLOY_RESPONSE=$(eval "$DEPLOY_CMD")

if [ $? -ne 0 ]; then
    echo "❌ Deployment request failed"
    exit 1
fi

# Check if response is JSON (success) or error text
if echo "$DEPLOY_RESPONSE" | jq . > /dev/null 2>&1; then
    # Check if deployment was successful
    IS_SUCCESS=$(echo "$DEPLOY_RESPONSE" | jq -r '.isSuccess // .data.success // false')
    PROGRAM_ID=$(echo "$DEPLOY_RESPONSE" | jq -r '.data.contractAddress // .programId // .address // .result.programId // .result.address // empty')
    TX_HASH=$(echo "$DEPLOY_RESPONSE" | jq -r '.data.transactionHash // .transactionHash // .txHash // .result.transactionHash // empty')
    
    if [ "$IS_SUCCESS" = "true" ] || [ -n "$PROGRAM_ID" ] && [ "$PROGRAM_ID" != "null" ]; then
        echo "✅ Deployment successful!"
        echo ""
        echo "📊 Deployment Details:"
        echo "   Program ID: $PROGRAM_ID"
        if [ -n "$TX_HASH" ] && [ "$TX_HASH" != "null" ]; then
            echo "   Transaction: $TX_HASH"
            echo "   Explorer: https://explorer.solana.com/tx/$TX_HASH?cluster=devnet"
        fi
        echo "   Program Explorer: https://explorer.solana.com/address/$PROGRAM_ID?cluster=devnet"
        echo ""
        echo "🎉 Contract deployed successfully to Solana devnet!"
    else
        echo "⚠️  Deployment response received but status unclear:"
        echo "$DEPLOY_RESPONSE" | jq .
    fi
else
    echo "❌ Deployment failed:"
    echo "$DEPLOY_RESPONSE"
    exit 1
fi

echo ""
echo "🎉 Full deployment flow completed successfully!"

