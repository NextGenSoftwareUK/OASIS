# OASIS Unified MCP Server - Testing Guide

This guide helps you test the OASIS Unified MCP Server with Cursor IDE.

## Prerequisites

- **Node.js 20+** installed ([Download](https://nodejs.org/))
- **Cursor IDE** installed ([Download](https://cursor.sh/))
- **OASIS API** running (local or remote)

## Quick Setup (5 minutes)

### Step 1: Clone or Download

```bash
# Option A: If you have the repository
cd /path/to/OASIS_CLEAN/MCP

# Option B: If downloading as ZIP
# Extract and navigate to the MCP folder
cd MCP
```

### Step 2: Install Dependencies

```bash
npm install
```

### Step 3: Build

```bash
npm run build
```

This creates the `dist/` folder with compiled JavaScript.

### Step 4: Configure Environment

Create a `.env` file (or copy from `env.example`):

```bash
cp env.example .env
```

Edit `.env` with your OASIS API settings:

```bash
# OASIS API Configuration
OASIS_API_URL=http://localhost:5000
# Or use remote API:
# OASIS_API_URL=https://api.oasisplatform.world

# Optional: API Key if required
OASIS_API_KEY=your_api_key_here

# Smart Contract Generator API (if using)
SMART_CONTRACT_API_URL=http://localhost:5001
```

### Step 5: Configure Cursor

Create or edit `~/.cursor/mcp.json`:

**On macOS/Linux:**
```bash
mkdir -p ~/.cursor
nano ~/.cursor/mcp.json
```

**On Windows:**
```powershell
# Create folder if it doesn't exist
New-Item -ItemType Directory -Force -Path "$env:APPDATA\Cursor\User"
# Edit the file
notepad "$env:APPDATA\Cursor\User\mcp.json"
```

**Configuration:**

```json
{
  "mcpServers": {
    "oasis-unified": {
      "command": "node",
      "args": ["/absolute/path/to/OASIS_CLEAN/MCP/dist/index.js"],
      "env": {
        "OASIS_API_URL": "http://localhost:5000",
        "SMART_CONTRACT_API_URL": "http://localhost:5001",
        "OASIS_API_KEY": ""
      }
    }
  }
}
```

**Important:** 
- Replace `/absolute/path/to/OASIS_CLEAN/MCP/dist/index.js` with your actual absolute path
- Use forward slashes even on Windows: `C:/Users/YourName/OASIS_CLEAN/MCP/dist/index.js`

### Step 6: Restart Cursor

**Completely close and reopen Cursor IDE** (not just reload window).

## Verify Installation

### Check 1: MCP Server Status

1. Open Cursor
2. Open Developer Tools: `Help → Toggle Developer Tools` (or `Cmd+Option+I` / `Ctrl+Shift+I`)
3. Look for MCP-related logs
4. Should see: `[MCP] OASIS Unified MCP Server started`

### Check 2: Test in Cursor

Ask Cursor:

```
"Check the OASIS API health status"
```

If it works, you should see a response with API health information.

### Check 3: List Available Tools

Ask Cursor:

```
"What MCP tools are available for OASIS?"
```

Or try:

```
"Show me all available OASIS tools"
```

## Testing Checklist

### Basic Operations

- [ ] **Health Check**
  - Ask: "Check OASIS API health"
  - Expected: Returns API status

- [ ] **Get Avatar**
  - Ask: "Get avatar information for username 'testuser'"
  - Expected: Returns avatar data or "not found"

- [ ] **List NFTs**
  - Ask: "Show me all NFTs for avatar ID [some-id]"
  - Expected: Returns NFT list or empty array

### Write Operations (Requires Authentication)

- [ ] **Register Avatar**
  - Ask: "Create a new avatar with username 'testuser123', email 'test@example.com', password 'test123'"
  - Expected: Creates avatar and returns ID

- [ ] **Authenticate**
  - Ask: "Authenticate avatar with username 'testuser123' and password 'test123'"
  - Expected: Returns JWT token

- [ ] **Mint NFT** (After authentication)
  - Ask: "Mint an NFT with symbol 'TESTNFT', title 'Test NFT', and metadata URL 'https://example.com/nft.json'"
  - Expected: Creates NFT and returns details

### Advanced Operations

- [ ] **Smart Contract Generation**
  - Ask: "Generate a Solana smart contract for token vesting"
  - Expected: Returns generated contract code

- [ ] **Wallet Operations**
  - Ask: "Create a Solana wallet for avatar [avatar-id]"
  - Expected: Creates wallet and returns address

- [ ] **A2A Agent Discovery**
  - Ask: "Discover SERV agents that provide data analysis services"
  - Expected: Returns list of agents

## Common Issues & Solutions

### Issue: "MCP Server Not Found"

**Solution:**
1. Verify the path in `mcp.json` is **absolute** (not relative)
2. Check the file exists: `ls -la /path/to/MCP/dist/index.js`
3. Make sure you ran `npm run build`
4. Check file permissions: `chmod +x /path/to/MCP/dist/index.js`

### Issue: "Connection Error" or "API Not Responding"

**Solution:**
1. Verify OASIS API is running:
   ```bash
   curl http://localhost:5000/api/health
   ```
2. Check the `OASIS_API_URL` in your `mcp.json` matches your API
3. If using HTTPS, you may need to disable SSL verification (for development)

### Issue: "Module Not Found" or Build Errors

**Solution:**
1. Make sure dependencies are installed:
   ```bash
   rm -rf node_modules
   npm install
   ```
2. Rebuild:
   ```bash
   npm run build
   ```
3. Check Node.js version: `node --version` (should be 20+)

### Issue: MCP Tools Not Available in Cursor

**Solution:**
1. **Completely restart Cursor** (quit and reopen, not just reload)
2. Check Cursor logs: `View → Output → MCP`
3. Verify `mcp.json` is in the correct location:
   - macOS/Linux: `~/.cursor/mcp.json`
   - Windows: `%APPDATA%\Cursor\User\mcp.json`
4. Check JSON syntax is valid (no trailing commas, proper quotes)

### Issue: "Invalid JSON" or Parse Errors

**Solution:**
1. Validate your `mcp.json` JSON syntax
2. Use a JSON validator: https://jsonlint.com/
3. Make sure all paths use forward slashes `/` even on Windows

## Testing Different Scenarios

### Scenario 1: Local API

```json
{
  "mcpServers": {
    "oasis-unified": {
      "command": "node",
      "args": ["/path/to/MCP/dist/index.js"],
      "env": {
        "OASIS_API_URL": "http://localhost:5000"
      }
    }
  }
}
```

### Scenario 2: Remote API

```json
{
  "mcpServers": {
    "oasis-unified": {
      "command": "node",
      "args": ["/path/to/MCP/dist/index.js"],
      "env": {
        "OASIS_API_URL": "https://api.oasisplatform.world",
        "OASIS_API_KEY": "your-api-key"
      }
    }
  }
}
```

### Scenario 3: Development Mode (with logs)

Run MCP server manually to see logs:

```bash
cd /path/to/MCP
npm run dev
```

Then in another terminal, test with:

```bash
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | npm run dev
```

## Providing Feedback

When reporting issues, please include:

1. **Your setup:**
   - OS (macOS/Windows/Linux)
   - Node.js version: `node --version`
   - Cursor version
   - OASIS API URL and version

2. **What you tried:**
   - Exact command/prompt you used
   - Expected vs actual result

3. **Error messages:**
   - Full error text
   - Cursor logs (View → Output → MCP)
   - Terminal output if running manually

4. **Configuration:**
   - Your `mcp.json` (remove sensitive keys)
   - Your `.env` file (remove sensitive keys)

## Next Steps After Testing

Once you've tested:

1. **Report Issues:** Create an issue with details above
2. **Suggest Improvements:** Share ideas for new tools or features
3. **Share Use Cases:** Tell us how you're using it
4. **Contribute:** If you want to add features or fix bugs

## Quick Reference

### Available Tool Categories

- **OASIS Tools:** Avatar, NFT, Wallet, Karma, Holon operations
- **Smart Contract Tools:** Generate, compile, deploy contracts
- **Solana RPC Tools:** Direct blockchain operations
- **A2A Protocol Tools:** Agent discovery and communication

### Example Prompts

```
"Check OASIS API health"
"Get avatar for username 'testuser'"
"Mint an NFT with symbol 'MYNFT' and title 'My NFT'"
"Create a Solana wallet for avatar [id]"
"Generate a Solana smart contract for token vesting"
"Discover SERV agents"
```

## Support

- **Documentation:** See [README.md](./README.md) for full documentation
- **Issues:** Report bugs or request features
- **Questions:** Ask in the community

---

**Happy Testing! 🚀**
