# MCP Server Usage Examples

## 🎯 Create Operations

### Create a New Avatar

**You ask Cursor:**
> "Create a new avatar with username 'alice', email 'alice@example.com', password 'secure123', first name 'Alice', last name 'Smith'"

**What happens:**
- MCP calls `oasis_register_avatar` tool
- Creates new avatar in OASIS
- Returns avatar ID and details

**Response:**
```json
{
  "result": {
    "id": "abc123-def456-...",
    "username": "alice",
    "email": "alice@example.com",
    "firstName": "Alice",
    "lastName": "Smith",
    "isEmailVerified": false
  },
  "isError": false,
  "message": "Avatar successfully registered"
}
```

### Mint an NFT

**You ask Cursor:**
> "Mint a Solana NFT with metadata from https://example.com/nft-metadata.json, symbol 'MYNFT', and send it to avatar abc123 after minting"

**What happens:**
- MCP calls `oasis_mint_nft` tool
- Mints NFT on Solana blockchain
- Sends NFT to specified avatar
- Returns transaction hash and mint address

**Response:**
```json
{
  "result": {
    "transactionHash": "5xKXtg2...",
    "mintAddress": "abc123...",
    "nftTokenAddress": "xyz789..."
  },
  "isError": false
}
```

### Create a Holon

**You ask Cursor:**
> "Create a holon named 'MyProject' with description 'A test project' and some custom data"

**What happens:**
- MCP calls `oasis_save_holon` tool
- Creates holon in OASIS storage
- Returns created holon with ID

**Response:**
```json
{
  "result": {
    "id": "holon-id-123",
    "name": "MyProject",
    "description": "A test project",
    "data": {...}
  },
  "isSaved": true
}
```

### Create a Wallet

**You ask Cursor:**
> "Create an Ethereum wallet for avatar abc123"

**What happens:**
- MCP calls `oasis_create_wallet` tool
- Creates wallet for avatar
- Returns wallet address

**Response:**
```json
{
  "result": {
    "walletAddress": "0x1234...",
    "walletType": "Ethereum"
  }
}
```

### Send Transaction

**You ask Cursor:**
> "Send 100 tokens from avatar abc123 to avatar xyz789"

**What happens:**
- MCP calls `oasis_send_transaction` tool
- Executes blockchain transaction
- Returns transaction hash

**Response:**
```json
{
  "result": {
    "transactionHash": "0xabcd...",
    "from": "0x1234...",
    "to": "0x5678...",
    "amount": 100
  }
}
```

## 🔍 Read Operations

### Get Avatar

**You ask Cursor:**
> "Get avatar information for username 'alice'"

**Response:**
```json
{
  "result": {
    "id": "abc123...",
    "username": "alice",
    "email": "alice@example.com",
    "karma": 100
  }
}
```

### Get NFTs

**You ask Cursor:**
> "Show me all NFTs for avatar abc123"

**Response:**
```json
{
  "result": [
    {
      "id": "nft-1",
      "name": "My NFT",
      "symbol": "MYNFT",
      "mintAddress": "abc123..."
    }
  ]
}
```

## 🔄 Complete Workflows

### Create Avatar + Wallet + Mint NFT

**You ask Cursor:**
> "Create a new avatar 'bob', create a wallet for them, then mint an NFT and send it to them"

**What happens:**
1. Creates avatar
2. Creates wallet
3. Mints NFT
4. Sends NFT to avatar

**All in one conversation!**

### Authenticate and Update Avatar

**You ask Cursor:**
> "Login as username 'alice' with password 'secure123', then update my description to 'Blockchain developer'"

**What happens:**
1. Authenticates and gets JWT token
2. Updates avatar description
3. Returns updated avatar

## 💡 Pro Tips

1. **Chain Operations:** You can chain multiple operations in one request
   - "Create avatar, create wallet, mint NFT"

2. **Use Natural Language:** Describe what you want, Cursor will figure out the tools
   - "I want to create a new user account"
   - "Mint me an NFT"

3. **Get Details First:** Query before creating
   - "Check if avatar 'alice' exists, if not create it"

4. **Combine Read + Write:** 
   - "Get my NFTs, then mint a new one"





















