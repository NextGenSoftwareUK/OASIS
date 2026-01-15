# OASIS Unified MCP Server

Unified Model Context Protocol (MCP) server for interacting with OASIS, OpenSERV, and STAR platforms.

## 🚀 Quick Start

### 1. Install Dependencies

```bash
cd MCP
npm install
```

### 2. Configure Environment

Create a `.env` file:

```bash
# Copy the example
cp env.example .env

# Edit .env with your settings
# OASIS_API_URL=http://localhost:5000
# OASIS_API_KEY=your_api_key_here (optional)
```

### 3. Build

```bash
npm run build
```

### 4. Run in Development

```bash
npm run dev
```

### 5. Configure Cursor

Create or edit `~/.cursor/mcp.json`:

```json
{
  "mcpServers": {
    "oasis-unified": {
      "command": "node",
      "args": ["/Users/maxgershfield/OASIS_CLEAN/MCP/dist/index.js"],
      "env": {
        "OASIS_API_URL": "http://localhost:5000",
        "OASIS_API_KEY": "your_api_key_here"
      }
    }
  }
}
```

**Important:** Use absolute paths in the config file.

### 6. Restart Cursor

Restart Cursor IDE to load the MCP server.

## 📋 Available Tools

### OASIS Tools

#### Read Operations
- `oasis_get_avatar` - Get avatar by ID, username, or email
- `oasis_get_karma` - Get karma score for avatar
- `oasis_get_nfts` - Get all NFTs for avatar
- `oasis_get_nft` - Get NFT details by ID
- `oasis_get_wallet` - Get wallet for avatar
- `oasis_get_holon` - Get holon (data object) by ID
- `oasis_health_check` - Check OASIS API health

#### Create/Write Operations ✨
- `oasis_register_avatar` - **Create** a new avatar
- `oasis_authenticate_avatar` - Authenticate (login) and get JWT token
- `oasis_mint_nft` - **Mint** a new NFT on Solana
- `oasis_save_holon` - **Create or update** a holon (data object)
- `oasis_update_avatar` - **Update** avatar information
- `oasis_create_wallet` - **Create** a wallet for an avatar (basic)
- `oasis_create_wallet_full` - **Create** wallet with full options (recommended)
- `oasis_set_default_wallet` - **Set** default wallet for an avatar
- `oasis_send_transaction` - **Send** tokens between avatars/addresses

**See [WALLET_CREATION_GUIDE.md](WALLET_CREATION_GUIDE.md) for detailed wallet creation instructions.**

### A2A Protocol & SERV Infrastructure Tools 🤖

#### Complete Agent Workflow
1. **`oasis_authenticate_avatar`** - Authenticate as a User or Wizard avatar (the owner)
2. **`oasis_register_avatar`** - Create an Agent avatar (set `avatarType: "Agent"`) - **Requires authentication from User/Wizard**
3. **`oasis_authenticate_avatar`** - Authenticate as the Agent avatar to get JWT token
4. **`oasis_register_agent_capabilities`** - Register agent services and skills (agent manages itself)
5. **`oasis_register_agent_as_serv_service`** - Register agent in SERV infrastructure
6. **`oasis_discover_agents_via_serv`** - Discover registered agents (no auth required)
7. **`oasis_get_my_agents`** - Get all agents owned by authenticated user (requires User/Wizard auth)

#### Agent Discovery
- `oasis_get_agent_card` - Get agent card by ID
- `oasis_get_all_agents` - List all A2A agents
- `oasis_get_agents_by_service` - Find agents by service name
- `oasis_get_my_agents` - Get agents owned by authenticated user (requires auth)
- `oasis_discover_agents_via_serv` - Discover agents via SERV infrastructure

#### Agent Registration
- `oasis_register_agent_capabilities` - Register agent capabilities (requires auth)
- `oasis_register_agent_as_serv_service` - Register agent as SERV service (requires auth + capabilities)

#### A2A Communication
- `oasis_send_a2a_jsonrpc_request` - Send JSON-RPC 2.0 requests (ping, payment_request, etc.)
- `oasis_get_pending_a2a_messages` - Get pending messages for agent
- `oasis_mark_a2a_message_processed` - Mark message as processed

#### OpenSERV Integration
- `oasis_register_openserv_agent` - Register OpenSERV AI agent as A2A agent
- `oasis_execute_ai_workflow` - Execute AI workflows via A2A Protocol

**See [A2A_SERV_INTEGRATION.md](A2A_SERV_INTEGRATION.md) for detailed documentation.**

### Smart Contract Generator Tools 🔨

- `scgen_generate_contract` - **Generate** smart contract from JSON specification
- `scgen_compile_contract` - **Compile** smart contract source code
- `scgen_deploy_contract` - **Deploy** compiled contract to blockchain
- `scgen_generate_and_compile` - **Generate and compile** in one step
- `scgen_get_cache_stats` - Get compilation cache statistics

**Supported Blockchains:**
- Ethereum (Solidity)
- Solana (Rust/Anchor)

### Solana RPC Tools ⚡

Direct Solana blockchain operations via RPC (bypasses OASIS API):

- `solana_send_sol` - **Send** SOL tokens directly via Solana RPC
- `solana_get_balance` - **Get** SOL balance for a wallet address
- `solana_get_transaction` - **Get** transaction details by signature

**See [SOLANA_RPC_ENDPOINTS.md](SOLANA_RPC_ENDPOINTS.md) for detailed documentation.**
- Radix (Scrypto)

## 🧪 Testing

Test the MCP server directly:

```bash
# Run in dev mode (with logs)
npm run dev

# In another terminal, test with echo
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | npm run dev
```

## 🔧 Development

### Watch Mode

```bash
npm run watch
```

### Build for Production

```bash
npm run build
npm start
```

## 📝 Example Usage in Cursor

Once configured, you can ask Cursor:

### Read Operations
- "Get avatar information for username 'testuser'"
- "Show me all NFTs for avatar ID abc123"
- "What's the karma score for avatar xyz789?"
- "Check OASIS API health"

### Create/Write Operations ✨
- "Create a new avatar with username 'alice', email 'alice@example.com', password 'secure123'"
- "Mint an NFT with metadata URL 'https://example.com/nft.json' and symbol 'MYNFT'"
- "Create a holon with name 'MyData' and description 'Test data'"
- "Create an Ethereum wallet for avatar abc123"
- "Send 100 tokens from avatar abc123 to avatar xyz789"

### A2A/SERV Operations 🤖
- "Discover SERV agents that provide data analysis services"
- "Get the agent card for agent abc-123"
- "Register my agent with capabilities for data analysis and Python skills"
- "Send a payment request for 0.01 SOL to agent xyz-789"
- "Show me all pending A2A messages"
- "Execute an AI workflow to analyze this data"

### Smart Contract Operations 🔨
- "Generate a Solana smart contract from this JSON spec: {programName: 'token_vesting', ...}"
- "Compile this Solidity contract: [contract code]"
- "Deploy this compiled contract to Ethereum"
- "Generate and compile a token vesting contract for Solana"

## 🐛 Troubleshooting

### MCP Server Not Found

- Check that the path in `~/.cursor/mcp.json` is absolute
- Verify the file exists: `ls -la /Users/maxgershfield/OASIS_CLEAN/MCP/dist/index.js`
- Check file permissions: `chmod +x /Users/maxgershfield/OASIS_CLEAN/MCP/dist/index.js`

### Connection Errors

- Verify OASIS API is running: `curl http://localhost:5000/api/health`
- Check environment variables in `.env`
- Check Cursor logs: View → Output → MCP

### Build Errors

- Ensure Node.js 20+ is installed: `node --version`
- Clear node_modules and reinstall: `rm -rf node_modules && npm install`

## 📚 Documentation

- **[A2A_SERV_INTEGRATION.md](A2A_SERV_INTEGRATION.md)** - Complete guide to A2A Protocol & SERV Infrastructure tools
- **[ENDPOINT_INVENTORY.md](ENDPOINT_INVENTORY.md)** - Full list of available endpoints and MCP tools
- **[HOW_TO_USE_MCP.md](HOW_TO_USE_MCP.md)** - Detailed usage guide

## 📚 Next Steps

- [x] ✅ Add A2A Protocol & SERV Infrastructure tools
- [ ] Add STAR dApp creation tools
- [ ] Add more OpenSERV workflow tools

