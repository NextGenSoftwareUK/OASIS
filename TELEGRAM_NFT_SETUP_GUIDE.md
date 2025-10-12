# 🔧 Telegram NFT Integration - Proper Setup

## The Problem

The OASIS NFT API requires:
1. **Authentication** - JWT token from avatar login
2. **Provider Registration** - Register SolanaOASIS and MongoDBOASIS  
3. **Provider Activation** - Activate both providers
4. **Proper Image/JSON Format** - Valid image URLs and metadata
5. **Correct Payload Structure** - PascalCase with specific enum formats

## The Solution

We need a **site-wide authenticated avatar** that the Telegram bot uses for all NFT minting operations.

---

## 🚀 Step 1: Create Site Avatar for Telegram Bot

### Option A: Use Existing metabricks_admin

If you already have a site avatar (like `metabricks_admin`), use those credentials.

### Option B: Create New Avatar

```bash
curl -X POST "https://oasisweb4.one/api/avatar" \
  -H "Content-Type: application/json" \
  -d '{
    "Username": "telegram_bot",
    "Email": "bot@experiences.fun",
    "Password": "YourSecurePassword123!",
    "FirstName": "Telegram",
    "LastName": "Bot",
    "Title": "Telegram Bot Avatar"
  }'
```

**Save these credentials** - you'll need them in the bot configuration.

---

## 🔐 Step 2: Test Authentication

```bash
curl -X POST "https://oasisweb4.one/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "telegram_bot",
    "password": "YourSecurePassword123!"
  }'
```

**Response**:
```json
{
  "result": {
    "jwtToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "avatarId": "89d907a8-5859-4171-b6c5-621bfe96930d"
  }
}
```

**Save the token and avatarId!**

---

## 🔌 Step 3: Register & Activate Providers

### Register SolanaOASIS

```bash
curl -X POST "https://oasisweb4.one/api/provider/register-provider-type/SolanaOASIS" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

### Activate SolanaOASIS

```bash
curl -X POST "https://oasisweb4.one/api/provider/activate-provider/SolanaOASIS" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

### Register MongoDBOASIS

```bash
curl -X POST "https://oasisweb4.one/api/provider/register-provider-type/MongoDBOASIS" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

### Activate MongoDBOASIS

```bash
curl -X POST "https://oasisweb4.one/api/provider/activate-provider/MongoDBOASIS" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

---

## 🎨 Step 4: Prepare Image & JSON

### Option A: Use Placeholder (Testing)

For initial testing, use placeholder images:
- Image: `https://via.placeholder.com/500/FF6B6B/FFFFFF?text=Achievement+Badge`
- JSON: Leave empty, API will generate

### Option B: Upload to Pinata (Production)

1. **Upload Image**:
```bash
curl -X POST "https://api.pinata.cloud/pinning/pinFileToIPFS" \
  -H "Authorization: Bearer YOUR_PINATA_JWT" \
  -F "file=@badge.png"
```

2. **Get IPFS URL**: `https://gateway.pinata.cloud/ipfs/QmXXXXXXXXXXXXX`

3. **Create & Upload JSON**:
```json
{
  "name": "Achievement Badge",
  "description": "Completed 30-day challenge",
  "image": "https://gateway.pinata.cloud/ipfs/QmXXXXXXXXXXXXX",
  "attributes": [
    { "trait_type": "Type", "value": "Achievement" },
    { "trait_type": "Rarity", "value": "Common" }
  ]
}
```

---

## 🔧 Step 5: Update NFTService

The current NFTService needs to be enhanced to:
1. **Authenticate on startup** (or use cached token)
2. **Handle token expiration** (re-authenticate if needed)
3. **Proper image handling** (validate URLs or use placeholders)
4. **Correct payload format** (as per nft-mint-frontend)

---

## 📝 Recommended Approach

### For MVP / Testing:

**Use a single bot avatar with pre-authenticated token:**

1. Create `telegram_bot` avatar
2. Authenticate once, get token
3. Store token in `OASIS_DNA.json`:
```json
{
  "TelegramOASIS": {
    "BotToken": "7927576561:...",
    "BotAvatarUsername": "telegram_bot",
    "BotAvatarPassword": "YourPassword",
    "BotAvatarId": "89d907a8-5859-4171-b6c5-621bfe96930d",
    "BotJWTToken": "eyJhbGciOiJIUzI1...",
    // ... existing config
  }
}
```

4. NFTService uses this token for all mints
5. Images use placeholders initially
6. Providers are activated once on startup

### For Production:

1. **Implement token caching** - refresh when expired
2. **Provider session management** - check if active before minting
3. **Image service** - integrate Pinata for custom badges
4. **User wallets** - link Solana wallets to OASIS avatars
5. **Queue system** - handle multiple mints gracefully

---

## 🚦 Quick Test Sequence

```bash
# 1. Authenticate
TOKEN=$(curl -s -X POST "https://oasisweb4.one/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username":"telegram_bot","password":"YourPassword"}' \
  | jq -r '.result.jwtToken')

# 2. Register Providers
curl -X POST "https://oasisweb4.one/api/provider/register-provider-type/SolanaOASIS" \
  -H "Authorization: Bearer $TOKEN"
  
curl -X POST "https://oasisweb4.one/api/provider/register-provider-type/MongoDBOASIS" \
  -H "Authorization: Bearer $TOKEN"

# 3. Activate Providers
curl -X POST "https://oasisweb4.one/api/provider/activate-provider/SolanaOASIS" \
  -H "Authorization: Bearer $TOKEN"
  
curl -X POST "https://oasisweb4.one/api/provider/activate-provider/MongoDBOASIS" \
  -H "Authorization: Bearer $TOKEN"

# 4. Mint Test NFT
curl -X POST "https://oasisweb4.one/api/nft/mint-nft" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "Title": "Test Badge",
    "Description": "Testing NFT minting",
    "Symbol": "TEST",
    "OnChainProvider": {"value": 3, "name": "SolanaOASIS"},
    "OffChainProvider": {"value": 23, "name": "MongoDBOASIS"},
    "NFTOffChainMetaType": {"value": 3, "name": "ExternalJsonURL"},
    "NFTStandardType": {"value": 2, "name": "SPL"},
    "ImageUrl": "https://via.placeholder.com/500?text=Test",
    "JSONMetaDataURL": "",
    "Price": 0,
    "NumberToMint": 1,
    "StoreNFTMetaDataOnChain": false,
    "MintedByAvatarId": "YOUR_AVATAR_ID",
    "SendToAddressAfterMinting": "YOUR_SOLANA_WALLET",
    "WaitTillNFTSent": true,
    "WaitForNFTToSendInSeconds": 60,
    "AttemptToSendEveryXSeconds": 5
  }'
```

---

## 🎯 Next Steps

1. **Run the test sequence above** to verify the full flow works
2. **Get the bot avatar credentials**
3. **Update OASIS_DNA.json** with bot credentials
4. **Enhance NFTService** to handle authentication
5. **Test /mintnft command** from Telegram

Would you like me to:
1. Create the enhanced NFTService with authentication?
2. Add authentication helper to Startup.cs?
3. Update OASIS_DNA.json template?

